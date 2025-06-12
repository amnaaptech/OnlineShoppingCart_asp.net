using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineShoppingCart.Data;
using OnlineShoppingCart.Models;

namespace OnlineShoppingCart.Controllers
{
    public class AdminController : Controller
    {
        private readonly ArtsContext db;
        private readonly IWebHostEnvironment env;
        private readonly IConfiguration _configuration;

        public AdminController(ArtsContext db, IWebHostEnvironment env, IConfiguration configuration)
        {
            this.db = db;
            this.env = env;
            _configuration = configuration;
        }

        // Authentication check method
        private bool CheckAdminAuth()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var userRole = HttpContext.Session.GetString("UserRole");

            return userId != null && (userRole == "Admin");
        }


        public IActionResult dashboard()
        {
            if (!CheckAdminAuth())
            {
                return RedirectToAction("Login", "Account");
            }

            var pendingRequests = new List<RequestNotification>();

            // Return Requests
            var returnRequests = db.Orders
                .Where(o => o.ReturnRequested && o.ReturnStatus == "Pending" && o.UpdatedDate.HasValue)
                .Select(o => new RequestNotification
                {
                    OrderId = o.OrderId,
                    OrderNumber = o.OrderNumber,
                    RequestType = "Return",

                    CustomerName = o.FullName ?? "N/A",
                    RequestDate = o.UpdatedDate.Value,

                })
                .ToList();
            pendingRequests.AddRange(returnRequests);

            // Replacement Requests
            var replacementRequests = db.Orders
                .Where(o => o.ReplacementRequested && o.ReplacementStatus == "Pending" && o.UpdatedDate.HasValue)
                .Select(o => new RequestNotification
                {
                    OrderId = o.OrderId,
                    OrderNumber = o.OrderNumber,
                    RequestType = "Replacement",

                    CustomerName = o.FullName ?? "N/A",
                    RequestDate = o.UpdatedDate.Value,

                })
                .ToList();
            pendingRequests.AddRange(replacementRequests);

            // Cancel Requests (if customer cancellation is allowed)
            var cancelRequests = db.Orders
                .Where(o => o.OrderStatus == "Canceled" && o.UpdatedBy == o.UserId && o.UpdatedDate.HasValue)
                .Select(o => new RequestNotification
                {
                    OrderId = o.OrderId,
                    OrderNumber = o.OrderNumber,
                    RequestType = "Cancel",
                    CustomerName = o.FullName ?? "N/A",
                    RequestDate = o.UpdatedDate.Value,

                })
                .ToList();
            pendingRequests.AddRange(cancelRequests);

            pendingRequests = pendingRequests.OrderByDescending(r => r.RequestDate).ToList();



            var viewModel = new AdminDashboardViewModel
            {
                PendingRequests = pendingRequests,
                CustomerCount = db.Users.Count(u => u.Role == "Customer"),
                EmployeeCount = db.Users.Count(u => u.Role == "Employee"),
                OrderCount = db.Orders.Count(), // Total amount of orders
                CategoryCount = db.Categories.Count(),
                ProductCount = db.Products.Count(),
                ReviewCount = db.ProductReviews.Count(),
                ContactCount = db.ContactUs.Count()
            };

            return View(viewModel);
        }


        [HttpGet]
        public IActionResult ShowContact()
        {
            if (!CheckAdminAuth())
            {
                return RedirectToAction("Login", "Account");
            }

            // Fetch contact data from database
            var contacts = db.ContactUs.ToList();

            return View(contacts); // Pass data to view
        }

        [HttpGet]
        public IActionResult DeleteContact(int id)
        {
            if (!CheckAdminAuth())
            {
                return RedirectToAction("Login", "Account");
            }

            var contact = db.ContactUs.Find(id);
            if (contact != null)
            {
                db.ContactUs.Remove(contact);
                db.SaveChanges();
                TempData["SuccessMessage"] = "Contact deleted successfully!";
            }

            return RedirectToAction("ShowContact");
        }


        //show reviewsss
        public IActionResult ShowReviews()
        {
            if (!CheckAdminAuth())
            {
                return RedirectToAction("Login", "Account");
            }

            var reviews = db.ProductReviews
                .Include(r => r.Product)
                .OrderByDescending(r => r.ReviewDate)
                .ToList();

            return View(reviews);
        }

        //review delete
        [HttpGet]
        public IActionResult DeleteReview(int id)
        {
            if (!CheckAdminAuth())
            {
                return RedirectToAction("Login", "Account");
            }

            var review = db.ProductReviews.FirstOrDefault(r => r.ReviewId == id);
            if (review != null)
            {
                db.ProductReviews.Remove(review);
                db.SaveChanges();
            }

            return RedirectToAction("ShowReviews");
        }

        public IActionResult RegisterUsers()
        {
            if (!CheckAdminAuth()) 
            {
                return RedirectToAction("Login", "Account");
            }

            var customers = db.Users
        .Where(u => u.Role == "Customer")
        .OrderByDescending(u => u.RegistrationDate)
        .ToList();

            return View(customers);
        }

     
      


        //Categoty

        [HttpGet]
        public IActionResult addCategory()
        {
            if (!CheckAdminAuth())
            {
                return RedirectToAction("Login", "Account");
            }
            return View();
        }

        [HttpPost]
        public IActionResult addCategory(Category addcate, IFormFile cateImg)
        {
            if (!CheckAdminAuth())
            {
                return RedirectToAction("Login", "Account");
            }

            if (cateImg == null || cateImg.Length == 0)
            {
                ModelState.AddModelError("CategoryImage", "Select a valid image file");
                return View(addcate);
            }

            var extensionAllow = new[] { ".jpeg", ".png", ".jpg" };
            var fileExt = Path.GetExtension(cateImg.FileName).ToLower();
            if (!extensionAllow.Contains(fileExt))
            {
                ModelState.AddModelError("CategoryImage", "Only jpeg, png, and jpg are allowed.");
                return View(addcate);
            }

            var maxSizeInBytes = 5 * 1024 * 1024; // 5MB
            if (cateImg.Length > maxSizeInBytes)
            {
                ModelState.AddModelError("CategoryImage", "The image size cannot exceed 5MB.");
                return View(addcate);
            }

            var filename = Guid.NewGuid().ToString() + fileExt;
            string cateFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/categoryImages");
            if (!Directory.Exists(cateFolder))
            {
                Directory.CreateDirectory(cateFolder);
            }
            string filepath = Path.Combine(cateFolder, filename);
            using (var stream = new FileStream(filepath, FileMode.Create))
            {
                cateImg.CopyTo(stream);
            }

            addcate.CategoryImage = filename;
            db.Categories.Add(addcate);
            db.SaveChanges();

            return RedirectToAction("ShowCategory");
        }

        public IActionResult ShowCategory()
        {
            if (!CheckAdminAuth())
            {
                return RedirectToAction("Login", "Account");
            }
            var showCate = db.Categories.ToList();
            db.SaveChanges();
            return View(showCate);
        }

        public IActionResult deleteCategory(int id)
        {
            if (!CheckAdminAuth())
            {
                return RedirectToAction("Login", "Account");
            }
            var deleteCate = db.Categories.Find(id);
            db.Categories.Remove(deleteCate);
            db.SaveChanges();
            return RedirectToAction("ShowCategory");
        }

        [HttpGet]
        public IActionResult editCategory(int id)
        {
            if (!CheckAdminAuth())
            {
                return RedirectToAction("Login", "Account");
            }

            if (id == null) return NotFound();

            var editcate = db.Categories.Find(id);
            if (editcate == null) return NotFound();
            return View(editcate);
        }

        [HttpPost]
        public IActionResult editCategory(Category addcate, IFormFile cateImg)
        {
            if (!CheckAdminAuth())
            {
                return RedirectToAction("Login", "Account");
            }

            var existingCate = db.Categories.FirstOrDefault(c => c.CategoryId == addcate.CategoryId);

            if (existingCate == null)
            {
                return NotFound();
            }

            if (cateImg != null && cateImg.Length > 0)
            {
                string fileExt = Path.GetExtension(cateImg.FileName).ToLower();
                var extensionAllow = new[] { ".jpeg", ".png", ".jpg" };

                if (!extensionAllow.Contains(fileExt))
                {
                    ModelState.AddModelError("CategoryImage", "Only jpeg, png, and jpg formats are allowed.");
                    return View(addcate);
                }

                string folder = Path.Combine(env.WebRootPath, "categoryImages");

                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }

                if (!string.IsNullOrEmpty(existingCate.CategoryImage))
                {
                    string oldPath = Path.Combine(folder, Path.GetFileName(existingCate.CategoryImage));
                    if (System.IO.File.Exists(oldPath))
                    {
                        System.IO.File.Delete(oldPath);
                    }
                }

                string fileName = Guid.NewGuid().ToString() + fileExt;
                string filePath = Path.Combine(folder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    cateImg.CopyTo(stream);
                }

                existingCate.CategoryImage = fileName;
            }

            existingCate.CategoryName = addcate.CategoryName;
            existingCate.Description = addcate.Description;

            db.SaveChanges();

            return RedirectToAction("ShowCategory");
        }

        ///End Category ....


        /////start product work
        [HttpGet]
        public IActionResult addProducts()
        {
            if (!CheckAdminAuth())
            {
                return RedirectToAction("Login", "Account");
            }

            ProductViewModel pv = new ProductViewModel()
            {
                categoryList = db.Categories.ToList(),
                productList = new Product()
            };
            return View(pv);
        }

        [HttpPost]
        public IActionResult addProducts(ProductViewModel products, IFormFile prodImg)
        {
            if (!CheckAdminAuth())
            {
                return RedirectToAction("Login", "Account");
            }

            if (products.productList.CategoryId == 0)
            {
                ModelState.AddModelError("productList.CategoryId", "Please select category also");
                products.categoryList = db.Categories.ToList();
                return View(products);
            }

            if (prodImg == null || prodImg.Length == 0)
            {
                ModelState.AddModelError("productList.ImagePath", "Please select a valid image file.");
                products.categoryList = db.Categories.ToList();
                return View(products);
            }

            var extensionAllow = new[] { ".jpeg", ".png", ".jpg" };
            var fileExt = Path.GetExtension(prodImg.FileName).ToLower();
            if (!extensionAllow.Contains(fileExt))
            {
                ModelState.AddModelError("productList.ImagePath", "Only jpeg, png, and jpg are allowed.");
                products.categoryList = db.Categories.ToList();
                return View(products);
            }

            var maxSizeInBytes = 5 * 1024 * 1024; // 5MB
            if (prodImg.Length > maxSizeInBytes)
            {
                ModelState.AddModelError("productList.ImagePath", "The image size cannot exceed 5MB.");
                products.categoryList = db.Categories.ToList();
                return View(products);
            }

            var filename = Guid.NewGuid().ToString() + fileExt;
            string imgFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/ProdImages");
            if (!Directory.Exists(imgFolder))
            {
                Directory.CreateDirectory(imgFolder);
            }

            string filePath = Path.Combine(imgFolder, filename);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                prodImg.CopyTo(stream);
            }

            products.productList.ImagePath = filename;
            db.Products.Add(products.productList);
            db.SaveChanges();

            return RedirectToAction("ShowProducts");
        }


        public IActionResult ShowProducts()
        {
            if (!CheckAdminAuth())
            {
                return RedirectToAction("Login", "Account");
            }

            var allProducts = db.Products.ToList();

            var productView = allProducts.Select(p => new ProductViewModel
            {
                productList = p,
                categoryList = db.Categories.ToList(),
            });

            return View(productView);
        }


        public IActionResult deleteProducts(string id)
        {
            if (!CheckAdminAuth())
            {
                return RedirectToAction("Login", "Account");
            }

            var product = db.Products.FirstOrDefault(p => p.ProductId == id);
            if (product != null)
            {
                string imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/ProdImages", product.ImagePath);
                if (System.IO.File.Exists(imagePath))
                {
                    System.IO.File.Delete(imagePath);
                }

                db.Products.Remove(product);
                db.SaveChanges();
            }

            return RedirectToAction("ShowProducts");
        }


        [HttpGet]
        public IActionResult editProduct(string id)
        {
            if (!CheckAdminAuth())
            {
                return RedirectToAction("Login", "Account");
            }

            var product = db.Products.FirstOrDefault(p => p.ProductId == id);
            if (product == null) return NotFound();

            ProductViewModel vm = new ProductViewModel
            {
                productList = product,
                categoryList = db.Categories.ToList()
            };

            return View(vm);
        }


        [HttpPost]
        public IActionResult editProduct(ProductViewModel model, IFormFile prodImg)
        {
            if (!CheckAdminAuth())
            {
                return RedirectToAction("Login", "Account");
            }

            var existing = db.Products.FirstOrDefault(p => p.ProductId == model.productList.ProductId);
            if (existing == null) return NotFound();

            existing.ProductName = model.productList.ProductName;
            existing.Description = model.productList.Description;
            existing.Price = model.productList.Price;
            existing.StockQuantity = model.productList.StockQuantity;
            existing.WarrantyPeriodDays = model.productList.WarrantyPeriodDays;
            existing.CategoryId = model.productList.CategoryId;

            if (prodImg != null && prodImg.Length > 0)
            {
                string[] allowedExt = { ".jpg", ".jpeg", ".png" };
                var ext = Path.GetExtension(prodImg.FileName).ToLower();
                if (!allowedExt.Contains(ext))
                {
                    ModelState.AddModelError("productList.ImagePath", "Only .jpg, .jpeg, .png allowed.");
                    model.categoryList = db.Categories.ToList();
                    return View(model);
                }

                if (prodImg.Length > (5 * 1024 * 1024))
                {
                    ModelState.AddModelError("productList.ImagePath", "Max allowed size is 2MB.");
                    model.categoryList = db.Categories.ToList();
                    return View(model);
                }

                string oldImagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/ProdImages", existing.ImagePath);
                if (System.IO.File.Exists(oldImagePath))
                {
                    System.IO.File.Delete(oldImagePath);
                }

                string newFileName = Guid.NewGuid().ToString() + ext;
                string folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/ProdImages");
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

                string filePath = Path.Combine(folder, newFileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    prodImg.CopyTo(stream);
                }

                existing.ImagePath = newFileName;
            }

            db.SaveChanges();
            return RedirectToAction("ShowProducts");
        }



        ///end product work


        ///UpdateAdmin Profile

        [HttpGet]
        public async Task<IActionResult> UpdateProfile()
        {
            if (!CheckAdminAuth())
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
            if (!CheckAdminAuth())
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

            return RedirectToAction("dashboard");
        }


        ///ADD EMPLOYEES
        [HttpGet]
        public IActionResult AddEmployee()
        {
            if (!CheckAdminAuth())
            {
                return RedirectToAction("Login", "Account");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddEmployee(AddEmployeeViewModel model)
        {
            if (!CheckAdminAuth())
            {
                return RedirectToAction("Login", "Account");
            }

            if (!ModelState.IsValid)
                return View(model);

            var exists = await db.Users.AnyAsync(u => u.UserEmail == model.UserEmail);
            if (exists)
            {
                ModelState.AddModelError("UserEmail", "Email already exists.");
                return View(model);
            }

            var employee = new User
            {
                UserEmail = model.UserEmail.Trim(),
                UserName = model.UserName.Trim(),
                UserPassword = model.UserPassword.Trim(),
                Role = "Employee",
                RegistrationDate = DateTime.Now,
                ImagePath = "~/frontassets/img/person/profile.png"
            };

            db.Users.Add(employee);
            await db.SaveChangesAsync();

            //send email to employees
            var emailModel = new ConfirmEmployee
            {
                From = "minalrazzaq21@gmail.com",
                Email = employee.UserEmail,
                Name = employee.UserName,
                Password = employee.UserPassword,
                Subject = "Wellcome to ARTS",
                EmployeeId = employee.UserId.ToString()
            };
            var emailService = new EmailService(_configuration);
            await emailService.SendEmailAsync(emailModel);




            TempData["SuccessMessage"] = "Employee added and welcome email sent.";
            return RedirectToAction("EmployeesList");
        }


        //EmployeesList
        public async Task<IActionResult> EmployeesList()
        {
            if (!CheckAdminAuth())
            {
                return RedirectToAction("Login", "Account");
            }
            var employees = await db.Users.Where(u => u.Role == "Employee").ToListAsync();
            return View(employees);
        }


        //DeleteEmployee
        [HttpGet]
        public async Task<IActionResult> DeleteEmployee(int id)
        {
            if (!CheckAdminAuth())
            {
                return RedirectToAction("Login", "Account");
            }

            var employee = await db.Users.FindAsync(id);
            if (employee == null || employee.Role != "Employee")
            {
                return NotFound();
            }

            db.Users.Remove(employee);
            await db.SaveChangesAsync();

            TempData["SuccessMessage"] = "Employee deleted successfully.";
            return RedirectToAction("EmployeesList");
        }




        ///Order listt

        public IActionResult ManageOrders(string searchOrderNumber, string deliveryType, DateTime? orderDate)
        {
            if (!CheckAdminAuth())
            {
                return RedirectToAction("Login", "Account");
            }

            var orders = db.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchOrderNumber))
            {
                orders = orders.Where(o => o.OrderNumber.Contains(searchOrderNumber));
            }

            if (!string.IsNullOrEmpty(deliveryType))
            {
                orders = orders.Where(o => o.DeliveryType == deliveryType);
            }

            if (orderDate.HasValue)
            {
                orders = orders.Where(o => o.OrderDate.Date == orderDate.Value.Date);
            }

            orders = orders.OrderByDescending(o => o.OrderDate);

            ViewBag.SearchOrderNumber = searchOrderNumber;
            ViewBag.DeliveryType = deliveryType;
            ViewBag.OrderDate = orderDate?.ToString("yyyy-MM-dd");

            return View(orders.ToList());
        }



        public IActionResult OrderDetails(int orderId)
        {
            if (!CheckAdminAuth())
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

            // Pass a flag to disable actions for Canceled, Returned, or Replaced orders
            ViewBag.IsOrderLocked = order.OrderStatus == "Canceled" ||
                                   order.OrderStatus == "Returned" ||
                                   order.OrderStatus == "Replaced";

            return View(order);
        }



        [HttpPost]
        public IActionResult UpdatePaymentStatus(int orderId, string newStatus)
        {
            if (!CheckAdminAuth())
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var order = db.Orders.FirstOrDefault(o => o.OrderId == orderId);
                if (order == null)
                {
                    TempData["ErrorMessage"] = "Order not found";
                    return RedirectToAction("OrderDetails", new { orderId });
                }

                // Valid payment statuses
                var validStatuses = new[] { "Pending", "Cleared", "PaidOnDelivery", "Refund Initiated" };
                if (!validStatuses.Contains(newStatus))
                {
                    TempData["ErrorMessage"] = "Invalid payment status";
                    return RedirectToAction("OrderDetails", new { orderId });
                }

                // For COD orders
                if (order.PaymentMethod == "cod")
                {
                    if (newStatus != "PaidOnDelivery")
                    {
                        TempData["ErrorMessage"] = "For COD orders, only 'Paid on Delivery' status is allowed.";
                        return RedirectToAction("OrderDetails", new { orderId });
                    }
                    if (order.OrderStatus != "Delivered")
                    {
                        TempData["ErrorMessage"] = "COD payment status can only be set to 'Paid on Delivery' after delivery.";
                        return RedirectToAction("OrderDetails", new { orderId });
                    }
                    order.PaymentStatus = newStatus;
                }
                // For Card or Bank orders
                else if (order.PaymentMethod == "card" || order.PaymentMethod == "bank")
                {
                    // Prevent updates if order is canceled or returned, except for Refund Initiated
                    if (order.OrderStatus == "Canceled" || order.OrderStatus == "Returned")
                    {
                        if (newStatus == "Refund Initiated" &&
                            ((order.OrderStatus == "Canceled" && order.PaymentStatus == "Cleared") ||
                             (order.OrderStatus == "Returned" && order.ReturnStatus == "Approved")))
                        {
                            // Verify card/bank details before initiating refund
                            if (order.PaymentMethod == "card" && string.IsNullOrEmpty(order.CardNumber))
                            {
                                TempData["ErrorMessage"] = "Card details missing for refund.";
                                return RedirectToAction("OrderDetails", new { orderId });
                            }
                            if (order.PaymentMethod == "bank" && string.IsNullOrEmpty(order.BankAccountNumber))
                            {
                                TempData["ErrorMessage"] = "Bank account details missing for refund.";
                                return RedirectToAction("OrderDetails", new { orderId });
                            }
                            order.PaymentStatus = newStatus;
                        }
                        else
                        {
                            TempData["ErrorMessage"] = "Invalid payment status for canceled or returned order. Only 'Refund Initiated' is allowed in valid cases.";
                            return RedirectToAction("OrderDetails", new { orderId });
                        }
                    }
                    else
                    {
                        // Validate newStatus for non-canceled/returned card/bank orders
                        if (newStatus == "PaidOnDelivery")
                        {
                            TempData["ErrorMessage"] = "'Paid on Delivery' is not allowed for card or bank orders.";
                            return RedirectToAction("OrderDetails", new { orderId });
                        }
                        if (newStatus == "Refund Initiated")
                        {
                            TempData["ErrorMessage"] = "'Refund Initiated' is only allowed for canceled or returned orders.";
                            return RedirectToAction("OrderDetails", new { orderId });
                        }
                        if (newStatus == "Cleared")
                        {
                            // Verify card/bank details before marking as cleared
                            if (order.PaymentMethod == "card" && string.IsNullOrEmpty(order.CardNumber))
                            {
                                TempData["ErrorMessage"] = "Card details missing for payment clearance.";
                                return RedirectToAction("OrderDetails", new { orderId });
                            }
                            if (order.PaymentMethod == "bank" && (string.IsNullOrEmpty(order.BankAccountNumber) || string.IsNullOrEmpty(order.TransactionId)))
                            {
                                TempData["ErrorMessage"] = "Bank account or transaction ID missing for payment clearance.";
                                return RedirectToAction("OrderDetails", new { orderId });
                            }
                            order.PaymentStatus = newStatus;
                            order.OrderStatus = "ReadyToDispatch";
                        }
                        else if (newStatus == "Pending" && order.PaymentStatus != "Cleared")
                        {
                            order.PaymentStatus = newStatus;
                        }
                        else
                        {
                            TempData["ErrorMessage"] = "Invalid payment status update for the current order state.";
                            return RedirectToAction("OrderDetails", new { orderId });
                        }
                    }
                }
                else
                {
                    TempData["ErrorMessage"] = "Unknown payment method.";
                    return RedirectToAction("OrderDetails", new { orderId });
                }

                order.UpdatedDate = DateTime.Now;
                order.UpdatedBy = HttpContext.Session.GetInt32("UserId") ?? 0;
                db.SaveChanges();

                TempData["SuccessMessage"] = $"Payment status for order #{order.OrderNumber} updated to {newStatus}";
                return RedirectToAction("OrderDetails", new { orderId });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error updating payment status";
                return RedirectToAction("OrderDetails", new { orderId });
            }
        }


        // Order dispatch karne ke liye
        [HttpPost]
        public IActionResult DispatchOrder(int orderId)
        {
            if (!CheckAdminAuth())
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
                // Prevent dispatch if order is canceled
                if (order.OrderStatus == "Canceled")
                {
                    TempData["ErrorMessage"] = "Cannot dispatch a canceled order";
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

                // Stock quantity check aur update karo
                foreach (var item in order.OrderItems)
                {
                    if (item.Product.StockQuantity < item.Quantity)
                    {
                        TempData["ErrorMessage"] = $"Not enough stock for product {item.Product.ProductName}";
                        return RedirectToAction("OrderDetails", new { orderId = orderId });
                    }
                    item.Product.StockQuantity -= item.Quantity;
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
        //Orderstatus
        [HttpPost]
        public IActionResult UpdateOrderStatus(int orderId, string newStatus)
        {
            if (!CheckAdminAuth())
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
                // Prevent status updates if order is canceled
                if (order.OrderStatus == "Canceled")
                {
                    TempData["ErrorMessage"] = "Cannot update status of a canceled order";
                    return RedirectToAction("OrderDetails", new { orderId = orderId });
                }

                // Valid order statuses
                if (newStatus != "Pending" && newStatus != "Processing" &&
                    newStatus != "ReadyToDispatch" && newStatus != "Dispatched" &&
                    newStatus != "Delivered")
                {
                    TempData["ErrorMessage"] = "Invalid order status";
                    return RedirectToAction("OrderDetails", new { orderId = orderId });
                }

                // Status flow validation
                if (order.OrderStatus == "Delivered" && newStatus != "Delivered")
                {
                    TempData["ErrorMessage"] = "Cannot change status of a delivered order";
                    return RedirectToAction("OrderDetails", new { orderId = orderId });
                }

                if (newStatus == "Delivered" && order.OrderStatus != "Dispatched")
                {
                    TempData["ErrorMessage"] = "Order must be dispatched before marking as delivered";
                    return RedirectToAction("OrderDetails", new { orderId = orderId });
                }

                if (newStatus == "Delivered")
                {
                    order.DeliveryDate = DateTime.Now;
                    // For COD orders, set PaymentStatus to PaidOnDelivery
                    if (order.PaymentMethod == "cod")
                    {
                        order.PaymentStatus = "PaidOnDelivery";
                    }
                }

                order.OrderStatus = newStatus;
                order.UpdatedDate = DateTime.Now;
                order.UpdatedBy = HttpContext.Session.GetInt32("UserId") ?? 0;

                db.SaveChanges();
                TempData["SuccessMessage"] = $"Order #{order.OrderNumber} status updated to {newStatus}";
                return RedirectToAction("OrderDetails", new { orderId = orderId });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error updating order status";
                return RedirectToAction("OrderDetails", new { orderId = orderId });
            }
        }

        // Admin Handle Return Request
        [HttpPost]
        public IActionResult HandleReturnRequest(int orderId, string newStatus)
        {
            if (!CheckAdminAuth())
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
                    return RedirectToAction("OrderDetails", new { orderId });
                }

                if (order.OrderStatus == "Canceled")
                {
                    TempData["ErrorMessage"] = "Cannot handle return request for a canceled order";
                    return RedirectToAction("OrderDetails", new { orderId });
                }

                if (!order.ReturnRequested)
                {
                    TempData["ErrorMessage"] = "No return request found for this order";
                    return RedirectToAction("OrderDetails", new { orderId });
                }

                if (!order.DeliveryDate.HasValue || (DateTime.Now - order.DeliveryDate.Value).TotalDays > 7)
                {
                    TempData["ErrorMessage"] = "Return request is only valid within 7 days of delivery";
                    return RedirectToAction("OrderDetails", new { orderId });
                }

                if (newStatus != "Pending" && newStatus != "Approved" && newStatus != "Rejected")
                {
                    TempData["ErrorMessage"] = "Invalid return status";
                    return RedirectToAction("OrderDetails", new { orderId });
                }

                order.ReturnStatus = newStatus;
                order.UpdatedDate = DateTime.Now;
                order.UpdatedBy = HttpContext.Session.GetInt32("UserId") ?? 0;

                if (newStatus == "Approved")
                {
                    order.OrderStatus = "Returned";

                    // Handle refund based on payment method
                    if (order.PaymentMethod == "card")
                    {
                        if (string.IsNullOrEmpty(order.CardNumber))
                        {
                            TempData["ErrorMessage"] = "Card details missing for refund.";
                            return RedirectToAction("OrderDetails", new { orderId });
                        }
                        order.PaymentStatus = "Refund Initiated";
                    }
                    else if (order.PaymentMethod == "bank")
                    {
                        if (string.IsNullOrEmpty(order.BankAccountNumber))
                        {
                            TempData["ErrorMessage"] = "Bank account details missing for refund.";
                            return RedirectToAction("OrderDetails", new { orderId });
                        }
                        order.PaymentStatus = "Refund Initiated";
                    }
                    else if (order.PaymentMethod == "cod")
                    {
                        if (string.IsNullOrEmpty(order.RefundEasypaisaNumber) && string.IsNullOrEmpty(order.RefundBankAccountNumber))
                        {
                            TempData["ErrorMessage"] = "Refund account details missing for COD order.";
                            return RedirectToAction("OrderDetails", new { orderId });
                        }
                        order.PaymentStatus = "Refund Initiated";
                    }

                    // Restock products
                    foreach (var item in order.OrderItems)
                    {
                        var product = db.Products.Find(item.ProductId);
                        if (product != null)
                        {
                            product.StockQuantity += item.Quantity;
                            db.Entry(product).State = EntityState.Modified;
                        }
                        else
                        {
                            TempData["ErrorMessage"] = $"Product not found for ProductId: {item.ProductId}";
                            return RedirectToAction("OrderDetails", new { orderId });
                        }
                    }
                }
                else if (newStatus == "Rejected")
                {
                    order.ReturnRequested = false;
                    order.RefundEasypaisaNumber = null;
                    order.RefundBankAccountNumber = null;
                }

                db.SaveChanges();

                string refundMessage = "";
                if (newStatus == "Approved" && order.PaymentMethod == "cod")
                {
                    refundMessage = $" Refund will be processed to {(string.IsNullOrEmpty(order.RefundEasypaisaNumber) ? "bank account" : "Easypaisa account")}.";
                }

                TempData["SuccessMessage"] = $"Return request for order #{order.OrderNumber} updated to {newStatus}.{refundMessage}";
                return RedirectToAction("OrderDetails", new { orderId });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error handling return request";
                return RedirectToAction("OrderDetails", new { orderId });
            }
        }


        // Admin: Handle Replacement Request
        [HttpPost]
        public IActionResult HandleReplacementRequest(int orderId, string newStatus)
        {
            if (!CheckAdminAuth())
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
                    return RedirectToAction("OrderDetails", new { orderId = orderId });
                }
                // Prevent replacement handling if order is canceled
                if (order.OrderStatus == "Canceled")
                {
                    TempData["ErrorMessage"] = "Cannot handle replacement request for a canceled order";
                    return RedirectToAction("OrderDetails", new { orderId = orderId });
                }

                if (!order.ReplacementRequested)
                {
                    TempData["ErrorMessage"] = "No replacement request found for this order";
                    return RedirectToAction("OrderDetails", new { orderId = orderId });
                }

                // Check: Replacement request within 7 days of delivery
                if (!order.DeliveryDate.HasValue || (DateTime.Now - order.DeliveryDate.Value).TotalDays > 7)
                {
                    TempData["ErrorMessage"] = "Replacement request is only valid within 7 days of delivery";
                    return RedirectToAction("OrderDetails", new { orderId = orderId });
                }

                if (newStatus != "Pending" && newStatus != "Approved" && newStatus != "Rejected")
                {
                    TempData["ErrorMessage"] = "Invalid replacement status";
                    return RedirectToAction("OrderDetails", new { orderId = orderId });
                }

                order.ReplacementStatus = newStatus;
                order.UpdatedDate = DateTime.Now;
                order.UpdatedBy = HttpContext.Session.GetInt32("UserId") ?? 0;

                if (newStatus == "Approved")
                {
                    order.OrderStatus = "Replaced";
                }

                db.SaveChanges();
                TempData["SuccessMessage"] = $"Replacement request for order #{order.OrderNumber} updated to {newStatus}";
                return RedirectToAction("OrderDetails", new { orderId = orderId });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error handling replacement request";
                return RedirectToAction("OrderDetails", new { orderId = orderId });
            }
        }



        public IActionResult CancelledReturnedOrders(string searchOrderNumber)
        {
            if (!CheckAdminAuth())
            {
                return RedirectToAction("Login", "Account");
            }

            // Fetch orders
            var ordersQuery = db.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .AsQueryable();

            // Apply search filter if provided
            if (!string.IsNullOrEmpty(searchOrderNumber))
            {
                ordersQuery = ordersQuery.Where(o => o.OrderNumber.Contains(searchOrderNumber));
            }

            // Categorize orders
            var viewModel = new CancelledReturnedOrdersViewModel
            {
                // User-cancelled orders (where UpdatedBy matches UserId and status is Canceled)
                UserCancelledOrders = ordersQuery
                    .Where(o => o.OrderStatus == "Canceled" && o.UpdatedBy == o.UserId)
                    .OrderByDescending(o => o.OrderDate)
                    .ToList(),

                // Admin-cancelled orders (where UpdatedBy is admin's UserId and status is Canceled)
                AdminCancelledOrders = ordersQuery
                    .Where(o => o.OrderStatus == "Canceled" && o.UpdatedBy != o.UserId)
                    .OrderByDescending(o => o.OrderDate)
                    .ToList(),

                // Returned orders (where OrderStatus is Returned)
                ReturnedOrders = ordersQuery
                    .Where(o => o.OrderStatus == "Returned")
                    .OrderByDescending(o => o.OrderDate)
                    .ToList(),

                // Replacement orders (where ReplacementRequested is true and ReplacementStatus is not null)
                ReplacementOrders = ordersQuery
                    .Where(o => o.ReplacementRequested && o.ReplacementStatus != null)
                    .OrderByDescending(o => o.OrderDate)
                    .ToList(),

                SearchOrderNumber = searchOrderNumber
            };

            return View(viewModel);
        }

        [HttpPost]
        public IActionResult CancelOrder(int orderId)
        {
            if (!CheckAdminAuth())
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

                if (order.DispatchedDate.HasValue)
                {
                    TempData["ErrorMessage"] = "Cannot cancel dispatched order";
                    return RedirectToAction("OrderDetails", new { orderId });
                }

                if (order.OrderStatus == "Canceled")
                {
                    TempData["ErrorMessage"] = "Order is already canceled";
                    return RedirectToAction("OrderDetails", new { orderId });
                }

                order.OrderStatus = "Canceled";
                order.UpdatedDate = DateTime.Now;
                order.UpdatedBy = HttpContext.Session.GetInt32("UserId") ?? 0;

                if (order.PaymentMethod == "card" && order.PaymentStatus == "Cleared")
                {
                    if (string.IsNullOrEmpty(order.CardNumber))
                    {
                        TempData["ErrorMessage"] = "Card details missing for refund.";
                        return RedirectToAction("OrderDetails", new { orderId });
                    }
                    order.PaymentStatus = "Refund Initiated";
                }
                else if (order.PaymentMethod == "bank" && order.PaymentStatus == "Cleared")
                {
                    if (string.IsNullOrEmpty(order.BankAccountNumber))
                    {
                        TempData["ErrorMessage"] = "Bank account details missing for refund.";
                        return RedirectToAction("OrderDetails", new { orderId });
                    }
                    order.PaymentStatus = "Refund Initiated";
                }

                foreach (var item in order.OrderItems)
                {
                    item.Product.StockQuantity += item.Quantity;
                }

                db.SaveChanges();
                TempData["SuccessMessage"] = $"Order #{order.OrderNumber} canceled successfully!";
                return RedirectToAction("OrderDetails", new { orderId });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error canceling order";
                return RedirectToAction("OrderDetails", new { orderId });
            }
        }

        [HttpPost]
        public IActionResult MarkDelivered(int orderId)
        {
            if (!CheckAdminAuth())
            {
                return RedirectToAction("Login", "Account");
            }
            try
            {
                var order = db.Orders.FirstOrDefault(o => o.OrderId == orderId);
                if (order == null)
                {
                    TempData["ErrorMessage"] = "Order not found";
                    return RedirectToAction("ManageOrders");
                }
                // Prevent marking as delivered if order is canceled
                if (order.OrderStatus == "Canceled")
                {
                    TempData["ErrorMessage"] = "Cannot mark a canceled order as delivered";
                    return RedirectToAction("OrderDetails", new { orderId = orderId });
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




        ////// FAQs Categories
        [HttpGet]
        public IActionResult AddFAQCat()
        {
            if (!CheckAdminAuth())
            {
                return RedirectToAction("Login", "Account");
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AddFAQCat(Faqcategory category)
        {
            if (!CheckAdminAuth())
            {
                return RedirectToAction("Login", "Account");
            }
            if (ModelState.IsValid)
            {
                db.Faqcategories.Add(category);
                await db.SaveChangesAsync();
                TempData["SuccessMessage"] = "FAQ Category Add successfully!";
                return RedirectToAction("ShowFAQCat");
            }
            return View(category);
        }

        // ✅ ShowFAQCat: Show All + Edit/Delete Actions
        public async Task<IActionResult> ShowFAQCat()
        {
            if (!CheckAdminAuth())
            {
                return RedirectToAction("Login", "Account");
            }
            var FAQcategories = db.Faqcategories.ToList();
            return View(FAQcategories);
        }


        // ✅ Delete Method (No View Needed)
        //[HttpPost]
        public async Task<IActionResult> DeleteFAQCat(int id)
        {
            if (!CheckAdminAuth())
            {
                return RedirectToAction("Login", "Account");
            }
            var category = await db.Faqcategories.FindAsync(id);
            if (category != null)
            {
                db.Faqcategories.Remove(category);
                //TempData["SuccessMessage"] = "FAQ Category deleted successfully!";
                await db.SaveChangesAsync();

            }
            return RedirectToAction("ShowFAQCat");
        }


        // ✅ Edit View: GET
        [HttpGet]
        public async Task<IActionResult> UpdateFAQCat(int id)
        {
            if (!CheckAdminAuth())
            {
                return RedirectToAction("Login", "Account");
            }
            var category = await db.Faqcategories.FindAsync(id);
            if (category == null) return NotFound();
            return View(category);
        }

        // ✅ Edit View: POST
        [HttpPost]
        public async Task<IActionResult> UpdateFAQCat(Faqcategory category)
        {
            if (!CheckAdminAuth())
            {
                return RedirectToAction("Login", "Account");
            }
            if (ModelState.IsValid)
            {
                db.Faqcategories.Update(category);
                await db.SaveChangesAsync();
                TempData["SuccessMessage"] = "FAQ updated successfully!";
                return RedirectToAction("ShowFAQCat");
            }
            return View(category);
        }



        [HttpGet]
        public IActionResult AddFAQ()
        {
            if (!CheckAdminAuth())
            {
                return RedirectToAction("Login", "Account");
            }
            var viewModel = new FAQsViewmodel
            {
                FAQ = new Faq(),
                CategoryList = db.Faqcategories.ToList()
            };
            return View(viewModel);
        }

        [HttpPost]
        public IActionResult AddFAQ(FAQsViewmodel model)
        {
            if (!CheckAdminAuth())
            {
                return RedirectToAction("Login", "Account");
            }
            db.Faqs.Add(model.FAQ);
            db.SaveChanges();

            TempData["SuccessMessage"] = "FAQ added successfully!";
            return RedirectToAction("ShowFAQ");
        }

        public IActionResult ShowFAQ()
        {
            if (!CheckAdminAuth())
            {
                return RedirectToAction("Login", "Account");
            }
            var data = db.Faqcategories.Include(c => c.Faqs).ToList();
            return View(data);
        }

        public IActionResult DeleteFAQ(int id)
        {
            if (!CheckAdminAuth())
            {
                return RedirectToAction("Login", "Account");
            }
            var faq = db.Faqs.Find(id);
            if (faq != null)
            {
                db.Faqs.Remove(faq);
                db.SaveChanges();
            }
            return RedirectToAction("ShowFAQ");
        }

        public IActionResult EditFAQ(int id)
        {
            if (!CheckAdminAuth())
            {
                return RedirectToAction("Login", "Account");
            }
            var faq = db.Faqs.Find(id);
            if (faq == null)
            {
                return NotFound();
            }

            var viewModel = new FAQsViewmodel
            {
                FAQ = faq,
                CategoryList = db.Faqcategories.ToList()
            };

            return View(viewModel);
        }


        [HttpPost]
        public IActionResult EditFAQ(FAQsViewmodel model)
        {
            if (model == null || model.FAQ == null) return BadRequest();

            var existingFaq = db.Faqs.Find(model.FAQ.Faqid);
            if (existingFaq == null) return NotFound();

            existingFaq.Fquestion = model.FAQ.Fquestion;
            existingFaq.Fanswer = model.FAQ.Fanswer;
            existingFaq.FcategoryId = model.FAQ.FcategoryId;

            db.SaveChanges();
            TempData["successmessage"] = "FAQ updated successfully!";
            return RedirectToAction("ShowFAQ");
        }


















    }
}
















    