using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineShoppingCart.Data;
using OnlineShoppingCart.Models;

namespace OnlineShoppingCart.Controllers
{
    public class EmployeeController : Controller
    {
        private readonly ArtsContext db;
        private readonly IWebHostEnvironment env;

        public EmployeeController(ArtsContext db, IWebHostEnvironment env)
        {
            this.env = env;
            this.db = db;
        }

        private bool CheckEmployeeAuth()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var userRole = HttpContext.Session.GetString("UserRole");
            return userId != null && userRole == "Employee";
        }

        //public IActionResult EmployeeDashBoard()
        //{
        //    if (!CheckEmployeeAuth())
        //    {
        //        return RedirectToAction("Login", "Account");
        //    }
        //    return View();
        //}

        [HttpGet]
        public async Task<IActionResult> UpdateProfile()
        {
            if (!CheckEmployeeAuth())
            {
                return RedirectToAction("Login", "Account");
            }

            var userId = HttpContext.Session.GetInt32("UserId");
            var user = await db.Users.FindAsync(userId);
            if (user == null) return NotFound();

            ViewBag.ExistingImage = string.IsNullOrEmpty(user.ImagePath) ? "~/frontassets/img/person/profile.png" : user.ImagePath;
            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProfile(IFormFile profileImage, string newPassword, string fullName, string UserPassword)
        {
            if (!CheckEmployeeAuth())
            {
                return RedirectToAction("Login", "Account");
            }

            var userId = HttpContext.Session.GetInt32("UserId");
            var user = await db.Users.FindAsync(userId);
            if (user == null) return NotFound();

            if (!string.IsNullOrEmpty(newPassword))
            {
                if (string.IsNullOrEmpty(UserPassword))
                {
                    ModelState.AddModelError("UserPassword", "Please enter your current password.");
                    ViewBag.ExistingImage = string.IsNullOrEmpty(user.ImagePath) ? "~/frontassets/img/person/profile.png" : user.ImagePath;
                    return View(user);
                }

                if (UserPassword.Trim() != user.UserPassword)
                {
                    ModelState.AddModelError("UserPassword", "Current password is incorrect.");
                    ViewBag.ExistingImage = string.IsNullOrEmpty(user.ImagePath) ? "~/frontassets/img/person/profile.png" : user.ImagePath;
                    return View(user);
                }

                if (newPassword.Trim() == user.UserPassword)
                {
                    ModelState.AddModelError("newPassword", "New password cannot be the same as the current password.");
                    ViewBag.ExistingImage = string.IsNullOrEmpty(user.ImagePath) ? "~/frontassets/img/person/profile.png" : user.ImagePath;
                    return View(user);
                }

                user.UserPassword = newPassword.Trim();
            }

            if (!string.IsNullOrWhiteSpace(fullName))
            {
                user.UserName = fullName.Trim();
                HttpContext.Session.SetString("UserName", user.UserName);
            }

            if (profileImage != null && profileImage.Length > 0)
            {
                var uploadsFolder = Path.Combine(env.WebRootPath, "ProfileImages");
                var fileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(profileImage.FileName);
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await profileImage.CopyToAsync(stream);
                }

                user.ImagePath = "/ProfileImages/" + fileName;
                HttpContext.Session.SetString("UserImagePath", user.ImagePath);
            }

            db.Users.Update(user);
            await db.SaveChangesAsync();

            return RedirectToAction("ManageOrders");
        }

        // Employee View Orders
        public IActionResult ManageOrders(string searchOrderNumber, string orderStatus)
        {
            if (!CheckEmployeeAuth())
            {
                return RedirectToAction("Login", "Account");
            }

            var orders = db.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .Where(o => o.OrderStatus == "ReadyToDispatch" || o.OrderStatus == "Dispatched") // Only relevant statuses
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchOrderNumber))
            {
                orders = orders.Where(o => o.OrderNumber != null && o.OrderNumber.Contains(searchOrderNumber));
            }

            if (!string.IsNullOrEmpty(orderStatus))
            {
                orders = orders.Where(o => o.OrderStatus == orderStatus);
            }

            orders = orders.OrderByDescending(o => o.OrderDate);

            ViewBag.SearchOrderNumber = searchOrderNumber;
            ViewBag.OrderStatus = orderStatus;

            return View(orders.ToList());
        }

        // Employee: Order Details
        public IActionResult OrderDetails(int orderId)
        {
            if (!CheckEmployeeAuth())
            {
                return RedirectToAction("Login", "Account");
            }

            var order = db.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefault(o => o.OrderId == orderId);

            if (order == null)
            {
                TempData["ErrorMessage"] = "Order not found";
                return RedirectToAction("ManageOrders");
            }

            return View(order);
        }

        

        // Employee: Mark Order as Delivered
        [HttpPost]
        public IActionResult MarkDelivered(int orderId)
        {
            if (!CheckEmployeeAuth())
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var order = db.Orders
                    .FirstOrDefault(o => o.OrderId == orderId);

                if (order == null)
                {
                    TempData["ErrorMessage"] = "Order not found";
                    return RedirectToAction("ManageOrders");
                }

                if (order.OrderStatus != "Dispatched")
                {
                    TempData["ErrorMessage"] = "Order must be dispatched before marking as delivered";
                    return RedirectToAction("OrderDetails", new { orderId = orderId });
                }

                order.OrderStatus = "Delivered";
                order.DeliveryDate = DateTime.Now;
                if (order.PaymentMethod == "cod")
                {
                    order.PaymentStatus = "PaidOnDelivery";
                }
                order.UpdatedDate = DateTime.Now;
                order.UpdatedBy = HttpContext.Session.GetInt32("UserId") ?? 0;

                db.SaveChanges();
                TempData["SuccessMessage"] = $"Order #{order.OrderNumber} marked as delivered!";
                return RedirectToAction("OrderDetails", new { orderId = orderId });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error marking order as delivered";
                return RedirectToAction("OrderDetails", new { orderId = orderId });
            }
        }

        [HttpPost]
        public IActionResult DispatchOrder(int orderId)
        {
            if (!CheckEmployeeAuth())
            {
                return RedirectToAction("Login", "Account");
            }
            try
            {
                var order = db.Orders
                    .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                    .FirstOrDefault(o => o.OrderId == orderId);
                if (order == null)
                {
                    TempData["ErrorMessage"] = "Order not found";
                    return RedirectToAction("ManageOrders");
                }
                if (order.OrderStatus != "ReadyToDispatch")
                {
                    TempData["ErrorMessage"] = "Order must be ready to dispatch";
                    return RedirectToAction("OrderDetails", new { orderId = orderId });
                }
                if (order.PaymentStatus != "Cleared" && order.PaymentMethod != "cod")
                {
                    TempData["ErrorMessage"] = "Cannot dispatch order: Payment not cleared";
                    return RedirectToAction("OrderDetails", new { orderId = orderId });
                }
                if (order.DispatchedDate.HasValue)
                {
                    TempData["ErrorMessage"] = "Order already dispatched";
                    return RedirectToAction("OrderDetails", new { orderId = orderId });
                }
                order.DispatchedDate = DateTime.Now;
                order.OrderStatus = "Dispatched";
                order.UpdatedDate = DateTime.Now;
                order.UpdatedBy = HttpContext.Session.GetInt32("UserId") ?? 0;
                foreach (var item in order.OrderItems)
                {
                    item.Product.StockQuantity -= item.Quantity; // Stock kam karo
                }
                db.SaveChanges();
                TempData["SuccessMessage"] = $"Order #{order.OrderNumber} dispatched successfully!";
                return RedirectToAction("OrderDetails", new { orderId = orderId });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error dispatching order";
                return RedirectToAction("OrderDetails", new { orderId = orderId });
            }
        }

        public IActionResult DeliveryReports(string searchOrderNumber)
        {
            if (!CheckEmployeeAuth())
            {
                return RedirectToAction("Login", "Account");
            }
            var orders = db.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .Where(o => o.OrderStatus == "Delivered")
                .AsQueryable();
            if (!string.IsNullOrEmpty(searchOrderNumber))
            {
                orders = orders.Where(o => o.OrderNumber.Contains(searchOrderNumber));
            }
            orders = orders.OrderByDescending(o => o.DeliveryDate);
            ViewBag.SearchOrderNumber = searchOrderNumber;
            return View(orders.ToList());
        }












    }
}