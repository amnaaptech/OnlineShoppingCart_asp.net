using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineShoppingCart.Data;
using OnlineShoppingCart.Models;
using System.Text.RegularExpressions;


namespace OnlineShoppingCart.Controllers
{
    public class CartController : Controller
    {
        private readonly ArtsContext _context;

        public CartController(ArtsContext context)
        {
            _context = context;
        }

        // Cart page view karne ke liye
        public IActionResult Index()
        {
            // Check karo user login hai ya nahi
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                TempData["ErrorMessage"] = "Please login first to view cart";
                return RedirectToAction("Login", "Account");
            }

            // User ke cart items get karo
            var cartItems = _context.Carts
                .Include(c => c.Product)
                .Include(c => c.Product.Category)
                .Where(c => c.UserId == userId.Value)
                .ToList();

            return View(cartItems);
        }

        // Add to Cart functionality





        [HttpPost]
        public IActionResult AddToCart(string productId, int quantity = 1)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    TempData["ErrorMessage"] = "Please login first to add items to cart";
                    return RedirectToAction("Login", "Account");
                }

                var product = _context.Products.FirstOrDefault(p => p.ProductId == productId);
                if (product == null)
                {
                    TempData["ErrorMessage"] = "Product not found";
                    return RedirectToAction("singleProduct", "Home", new { id = productId });
                }

                if (product.StockQuantity < quantity)
                {
                    TempData["ErrorMessage"] = "Not enough stock available";
                    return RedirectToAction("singleProduct", "Home", new { id = productId });
                }

                var existingCartItem = _context.Carts
                    .FirstOrDefault(c => c.UserId == userId.Value && c.ProductId == productId);

                if (existingCartItem != null)
                {
                    var newQuantity = existingCartItem.Quantity + quantity;

                    if (newQuantity > product.StockQuantity)
                    {
                        TempData["ErrorMessage"] = $"Cannot add more items. Only {product.StockQuantity} items available";
                        return RedirectToAction("singleProduct", "Home", new { id = productId });
                    }

                    existingCartItem.Quantity = newQuantity;
                    existingCartItem.AddedDate = DateTime.Now;
                }
                else
                {
                    var cartItem = new Cart
                    {
                        UserId = userId.Value,
                        ProductId = productId,
                        Quantity = quantity,
                        AddedDate = DateTime.Now
                    };
                    _context.Carts.Add(cartItem);
                }

                _context.SaveChanges();

                // Cart count calculate kar ke session mein store karein
                var cartCount = _context.Carts
                    .Where(c => c.UserId == userId.Value)
                    .Sum(c => c.Quantity);

                HttpContext.Session.SetInt32("CartCount", cartCount);

                TempData["SuccessMessage"] = "Product added to cart successfully!";
                TempData["CartUpdated"] = "true"; 

                return RedirectToAction("singleProduct", "Home", new { id = productId });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error adding product to cart";
                return RedirectToAction("singleProduct", "Home", new { id = productId });
            }
        }

        // RemoveFromCart 
        [HttpPost]
        public IActionResult RemoveFromCart(int cartId)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    TempData["ErrorMessage"] = "Please login first";
                    return RedirectToAction("Login", "Account");
                }

                var cartItem = _context.Carts
                    .FirstOrDefault(c => c.CartId == cartId && c.UserId == userId.Value);

                if (cartItem != null)
                {
                    _context.Carts.Remove(cartItem);
                    _context.SaveChanges();

                    // Cart count update karein
                    var cartCount = _context.Carts
                        .Where(c => c.UserId == userId.Value)
                        .Sum(c => c.Quantity);

                    HttpContext.Session.SetInt32("CartCount", cartCount);
                    TempData["SuccessMessage"] = "Item removed from cart successfully!";
                    TempData["CartUpdated"] = "true";
                }
                else
                {
                    TempData["ErrorMessage"] = "Cart item not found";
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error removing item from cart";
                return RedirectToAction("Index");
            }
        }

        // UpdateQuantity method bhi update karein
        [HttpPost]
        public IActionResult UpdateQuantity(int cartId, int quantity)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    TempData["ErrorMessage"] = "Please login first";
                    return RedirectToAction("Login", "Account");
                }

                if (quantity <= 0)
                {
                    TempData["ErrorMessage"] = "Quantity must be greater than 0";
                    return RedirectToAction("Index");
                }

                var cartItem = _context.Carts
                    .Include(c => c.Product)
                    .FirstOrDefault(c => c.CartId == cartId && c.UserId == userId.Value);

                if (cartItem != null)
                {
                    if (quantity > cartItem.Product.StockQuantity)
                    {
                        TempData["ErrorMessage"] = $"Only {cartItem.Product.StockQuantity} items available in stock";
                        return RedirectToAction("Index");
                    }

                    cartItem.Quantity = quantity;
                    _context.SaveChanges();

                    // Cart count update karein
                    var cartCount = _context.Carts
                        .Where(c => c.UserId == userId.Value)
                        .Sum(c => c.Quantity);

                    HttpContext.Session.SetInt32("CartCount", cartCount);
                    TempData["SuccessMessage"] = "Cart updated successfully!";
                    TempData["CartUpdated"] = "true";
                }
                else
                {
                    TempData["ErrorMessage"] = "Cart item not found";
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error updating cart";
                return RedirectToAction("Index");
            }
        }

        // ClearCart method bhi update karein
        [HttpPost]
        public IActionResult ClearCart()
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    TempData["ErrorMessage"] = "Please login first";
                    return RedirectToAction("Login", "Account");
                }

                var cartItems = _context.Carts.Where(c => c.UserId == userId.Value).ToList();

                if (cartItems.Any())
                {
                    _context.Carts.RemoveRange(cartItems);
                    _context.SaveChanges();

                    // Cart count reset karein
                    HttpContext.Session.SetInt32("CartCount", 0);
                    TempData["SuccessMessage"] = "Cart cleared successfully!";
                    TempData["CartUpdated"] = "true";
                }
                else
                {
                    TempData["InfoMessage"] = "Cart is already empty";
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error clearing cart";
                return RedirectToAction("Index");
            }
        }

        // GetCartCount method ko improve karein
        [HttpGet]
        public IActionResult GetCartCount()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                HttpContext.Session.SetInt32("CartCount", 0);
                return Json(0);
            }

            // Fetch from session first
            var sessionCount = HttpContext.Session.GetInt32("CartCount");
            if (sessionCount.HasValue)
            {
                return Json(sessionCount.Value);
            }

            // If not in session, fetch from database
            var count = _context.Carts
                .Where(c => c.UserId == userId.Value)
                .Sum(c => c.Quantity);

            HttpContext.Session.SetInt32("CartCount", count);
            return Json(count);
        }




        // Order Summary page
        public IActionResult OrderSummary()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                TempData["ErrorMessage"] = "Please login first";
                return RedirectToAction("Login", "Account");
            }

            var cartItems = _context.Carts
                .Include(c => c.Product)
                .Include(c => c.Product.Category)
                .Where(c => c.UserId == userId.Value)
                .ToList();

            if (!cartItems.Any())
            {
                TempData["ErrorMessage"] = "Your cart is empty";
                return RedirectToAction("Index");
            }

            return View(cartItems);
        }

        ///PLACEORDERRR

        [HttpPost]
        public IActionResult PlaceOrder(OrderViewModel orderModel)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    TempData["ErrorMessage"] = "Please login first";
                    return RedirectToAction("Login", "Account");
                }

                // Clear ModelState errors for non-relevant fields based on PaymentMethod
                if (orderModel.PaymentMethod == "cod")
                {
                    ModelState.Remove("CardHolderName");
                    ModelState.Remove("CardNumber");
                    ModelState.Remove("ExpiryDate");
                    ModelState.Remove("CVC");
                    ModelState.Remove("BankName");
                    ModelState.Remove("BankAccountNumber");
                    ModelState.Remove("TransactionId");
                }
                else if (orderModel.PaymentMethod == "card")
                {
                    ModelState.Remove("BankName");
                    ModelState.Remove("BankAccountNumber");
                    ModelState.Remove("TransactionId");
                }
                else if (orderModel.PaymentMethod == "bank")
                {
                    ModelState.Remove("CardHolderName");
                    ModelState.Remove("CardNumber");
                    ModelState.Remove("ExpiryDate");
                    ModelState.Remove("CVC");
                }

                // Validate required fields manually
                if (orderModel.PaymentMethod == "card")
                {
                    if (string.IsNullOrEmpty(orderModel.CardHolderName))
                        ModelState.AddModelError("CardHolderName", "Card holder name is required.");
                    if (string.IsNullOrEmpty(orderModel.CardNumber) || !Regex.IsMatch(orderModel.CardNumber, @"^[0-9]{16}$"))
                        ModelState.AddModelError("CardNumber", "Please enter a valid 16-digit card number.");
                    if (string.IsNullOrEmpty(orderModel.ExpiryDate) || !Regex.IsMatch(orderModel.ExpiryDate, @"^(0[1-9]|1[0-2])\/[0-9]{2}$"))
                        ModelState.AddModelError("ExpiryDate", "Please enter a valid expiry date (MM/YY).");
                    if (string.IsNullOrEmpty(orderModel.CVC) || !Regex.IsMatch(orderModel.CVC, @"^[0-9]{3,4}$"))
                        ModelState.AddModelError("CVC", "Please enter a valid 3 or 4-digit CVC.");
                }

                else if (orderModel.PaymentMethod == "bank")
                {
                    // Bank Name: Must not be empty
                    if (string.IsNullOrEmpty(orderModel.BankName))
                    {
                        ModelState.AddModelError("BankName", "Bank name is required.");
                    }

                    // Account Number: Exactly 16 digits
                    if (string.IsNullOrEmpty(orderModel.BankAccountNumber) ||
                        !Regex.IsMatch(orderModel.BankAccountNumber, @"^[0-9]{16}$"))
                    {
                        ModelState.AddModelError("BankAccountNumber", "Account number must be exactly 16 digits.");
                    }

                    // Transaction ID: Exactly 10 digits
                    if (string.IsNullOrEmpty(orderModel.TransactionId) ||
                        !Regex.IsMatch(orderModel.TransactionId, @"^\d{10}$"))
                    {
                        ModelState.AddModelError("TransactionId", "Transaction ID must be exactly 10 digits.");
                    }
                }


                // Validate core order fields
                if (string.IsNullOrEmpty(orderModel.FullName))
                    ModelState.AddModelError("FullName", "Full name is required.");
                //if (string.IsNullOrEmpty(orderModel.Phone) || !Regex.IsMatch(orderModel.Phone, @"^[0-9+\-\s()]+$"))
                //    ModelState.AddModelError("Phone", "Please enter a valid phone number.");
                if (string.IsNullOrEmpty(orderModel.Phone) || !Regex.IsMatch(orderModel.Phone, @"^[0-9]{11}$"))
                {
                    ModelState.AddModelError("Phone", "Please enter a valid 11-digit phone number.");
                }
                if (string.IsNullOrEmpty(orderModel.Address))
                    ModelState.AddModelError("Address", "Address is required.");
                if (string.IsNullOrEmpty(orderModel.City))
                    ModelState.AddModelError("City", "City is required.");
                if (string.IsNullOrEmpty(orderModel.State))
                    ModelState.AddModelError("State", "State is required.");
                if (string.IsNullOrEmpty(orderModel.ZipCode))
                    ModelState.AddModelError("ZipCode", "ZIP code is required.");

                // Check if ModelState is valid after manual validation
                if (!ModelState.IsValid)
                {
                    TempData["ErrorMessage"] = "Please fill all required fields correctly";
                    return RedirectToAction("OrderSummary");
                }

                // Get cart items
                var cartItems = _context.Carts
                    .Include(c => c.Product)
                    .Where(c => c.UserId == userId.Value)
                    .ToList();

                if (!cartItems.Any())
                {
                    TempData["ErrorMessage"] = "Your cart is empty";
                    return RedirectToAction("Index");
                }

                // Stock validation for all items
                foreach (var item in cartItems)
                {
                    if (item.Product.StockQuantity < item.Quantity)
                    {
                        TempData["ErrorMessage"] = $"Not enough stock for {item.Product.ProductName}. Only {item.Product.StockQuantity} available.";
                        return RedirectToAction("OrderSummary");
                    }
                }

                // Generate unique order number
                var orderNumber = GenerateOrderNumber(orderModel.PaymentMethod, cartItems.First().ProductId);

                // Create order
                var order = new Order
                {
                    OrderNumber = orderNumber,
                    UserId = userId.Value,
                    OrderDate = DateTime.Now,
                    Subtotal = orderModel.Subtotal,
                    Shipping = orderModel.Shipping,
                    Tax = orderModel.Tax,
                    Total = orderModel.Total,
                    PaymentMethod = orderModel.PaymentMethod,
                    PaymentStatus = orderModel.PaymentMethod == "cod" ? "PaidOnDelivery" : "Pending",
                    DeliveryType = GetDeliveryType(orderModel.PaymentMethod),
                    EstimatedDeliveryDate = DateTime.Parse(orderModel.EstimatedDelivery),
                    OrderStatus = "Pending",
                    FullName = orderModel.FullName,
                    Phone = orderModel.Phone,
                    Address = orderModel.Address,
                    City = orderModel.City,
                    State = orderModel.State,
                    ZipCode = orderModel.ZipCode,
                    CardHolderName = orderModel.CardHolderName,
                    CardNumber = orderModel.CardNumber,
                    ExpiryDate = orderModel.ExpiryDate,
                    Cvc = orderModel.CVC,
                    BankName = orderModel.BankName,
                    BankAccountNumber = orderModel.BankAccountNumber,
                    TransactionId = orderModel.TransactionId,
                    CreatedDate = DateTime.Now
                };

                _context.Orders.Add(order);
                _context.SaveChanges();

                // Create order items and update stock
                foreach (var cartItem in cartItems)
                {
                    var orderItem = new OrderItem
                    {
                        OrderId = order.OrderId,
                        ProductId = cartItem.ProductId,
                        Quantity = cartItem.Quantity,
                        UnitPrice = cartItem.Product.Price,
                        WarrantyPeriodDays = cartItem.Product.WarrantyPeriodDays
                    };
                    _context.OrderItems.Add(orderItem);

                    // Update stock
                    cartItem.Product.StockQuantity -= cartItem.Quantity;
                }

                // Clear cart
                _context.Carts.RemoveRange(cartItems);
                _context.SaveChanges();

                TempData["SuccessMessage"] = $"Order placed successfully! Your order number is: {orderNumber}";
                return RedirectToAction("OrderConfirmation", new { orderNumber = orderNumber });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error placing order. Please try again.";
                return RedirectToAction("OrderSummary");
            }
        }


        // Order Confirmation page
        public IActionResult OrderConfirmation(string orderNumber)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                TempData["ErrorMessage"] = "Please login first";
                return RedirectToAction("Login", "Account");
            }

            var order = _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .ThenInclude(p => p.Category)
                .FirstOrDefault(o => o.OrderNumber == orderNumber && o.UserId == userId.Value);

            if (order == null)
            {
                TempData["ErrorMessage"] = "Order not found";
                return RedirectToAction("Index");
            }

            return View(order);
        }


        //generate order number
        private string GenerateOrderNumber(string paymentMethod, string firstProductId)
        {
            // 1-digit delivery type based on payment method
            string deliveryType = GetDeliveryType(paymentMethod);

            // 7-digit product ID (use first product in cart)
            string productId = firstProductId;

            // 8-digit order number (timestamp based)
            string orderNum = DateTime.Now.ToString("yyyyMMdd");

            return deliveryType + productId + orderNum;
        }

        // Helper method to get delivery type
        private string GetDeliveryType(string paymentMethod)
        {
            return paymentMethod switch
            {
                "cod" => "1",      // Standard delivery for COD
                "card" => "2",     // Express delivery for card payment
                "bank" => "3",     // Bank transfer delivery
                _ => "1"           // Default standard delivery
            };
        }


        // My Orders page
        public IActionResult MyOrders()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                TempData["ErrorMessage"] = "Please login first";
                return RedirectToAction("Login", "Account");
            }

            var orders = _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .Where(o => o.UserId == userId.Value)
                .OrderByDescending(o => o.OrderDate)
                .ToList();

            return View(orders);
        }


        //Order detaill
        public IActionResult OrderDetails(int orderId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                TempData["ErrorMessage"] = "Please login first";
                return RedirectToAction("Login", "Account");
            }

            var order = _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .ThenInclude(p => p.Category)
                .FirstOrDefault(o => o.OrderId == orderId && o.UserId == userId.Value);

            if (order == null)
            {
                TempData["ErrorMessage"] = "Order not found";
                return RedirectToAction("MyOrders");
            }

            // ViewBag mein cancel option pass kero
            ViewBag.CanCancel = CanCancelOrder(order);
            ViewBag.CancelReason = GetCancelRestrictionReason(order);

            return View(order);
        }

        // Cancel nahi ho sakta toh reason batane kay liye
        private string GetCancelRestrictionReason(Order order)
        {
            if (order.DispatchedDate.HasValue)
                return "Order has been dispatched";

            if (order.OrderStatus == "Delivered")
                return "Order has been delivered";

            if (order.OrderStatus == "Canceled")
                return "Order is already Canceled";

            if (order.DeliveryDate.HasValue)
                return "Order has been delivered";

            return "";
        }

        // Cancel Order - Requirements kay mutabiq updated
        [HttpPost]
        public IActionResult CancelOrder(int orderId)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    TempData["ErrorMessage"] = "Please login first";
                    return RedirectToAction("Login", "Account");
                }

                var order = _context.Orders
                    .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                    .FirstOrDefault(o => o.OrderId == orderId && o.UserId == userId.Value);

                if (order == null)
                {
                    TempData["ErrorMessage"] = "Order not found";
                    return RedirectToAction("MyOrders");
                }

                // MAIN REQUIREMENT CHECK: Sirf non-dispatched orders cancel ho saktay hain
                if (order.DispatchedDate.HasValue)
                {
                    TempData["ErrorMessage"] = "Cannot cancel order as it has already been dispatched. You can request return/replacement instead.";
                    return RedirectToAction("OrderDetails", new { orderId = orderId });
                }

                // Additional checks - Delivered orders bilkul cancel nahi ho saktay
                if (order.OrderStatus == "Delivered")
                {
                    TempData["ErrorMessage"] = "Cannot cancel delivered order. Please use return/replacement option.";
                    return RedirectToAction("OrderDetails", new { orderId = orderId });
                }

                // Already cancelled check
                if (order.OrderStatus == "Canceled")
                {
                    TempData["InfoMessage"] = "Order is already cancelled";
                    return RedirectToAction("OrderDetails", new { orderId = orderId });
                }

                // PAYMENT HANDLING - Requirements kay mutabiq
                string refundMessage = "";

                // Agar payment clear ho chuki hai toh refund process
                if (order.PaymentStatus == "Cleared")
                {
                    // Payment cleared hai toh refund initiate kero
                    order.PaymentStatus = "Refund Initiated";
                    refundMessage = " Refund will be processed within 5-7 business days.";
                }
                else if (order.PaymentStatus == "PaidOnDelivery")
                {
                    // COD case - no refund needed
                    order.PaymentStatus = "Canceled";
                    refundMessage = " No payment was made, so no refund required.";
                }
                else
                {
                    // Pending payment case
                    order.PaymentStatus = "Canceled";
                    refundMessage = " Payment was not processed.";
                }

                // ORDER STATUS UPDATE
                order.OrderStatus = "Canceled";
                order.UpdatedDate = DateTime.Now;
                order.UpdatedBy = userId.Value; // Track kaun nay cancel kiya

                // STOCK RESTORE - Bilkul zaroori requirement
                foreach (var item in order.OrderItems)
                {
                    // Product ka stock wapis add kero
                    item.Product.StockQuantity += item.Quantity;
                }

                // RETURN/REPLACEMENT REQUESTS CLEAR - Agar koi pending hain
                order.ReturnRequested = false;
                order.ReturnStatus = null;
                order.ReplacementRequested = false;
                order.ReplacementStatus = null;

                // DATABASE SAVE
                _context.SaveChanges();

                // SUCCESS MESSAGE with payment info
                TempData["SuccessMessage"] = $"Order #{order.OrderNumber} Canceled successfully!{refundMessage}";

                // LOG ENTRY (Optional - for admin tracking)
                // You can add logging here if needed

                return RedirectToAction("MyOrders");
            }
            catch (Exception ex)
            {
                // ERROR LOGGING (Production mein proper logging kero)
                // Log the exception details for debugging

                TempData["ErrorMessage"] = "Error cancelling order. Please try again or contact support.";
                return RedirectToAction("MyOrders");
            }
        }

        // ADDITIONAL HELPER METHOD - Cancel kerne kay liye conditions check
        public bool CanCancelOrder(Order order)
        {
            // Basic conditions
            if (order.DispatchedDate.HasValue) return false;
            if (order.OrderStatus == "Delivered") return false;
            if (order.OrderStatus == "Canceled") return false;
            if (order.DeliveryDate.HasValue) return false;

            // Time limit check (Optional - agar 24 hours ka limit lagana hai)
            // if (DateTime.Now.Subtract(order.OrderDate).TotalHours > 24) return false;

            return true;
        }

        //return requestt 
        //[HttpPost]
        //public IActionResult RequestReturn(int orderId, string returnReason)
        //{
        //    try
        //    {
        //        var userId = HttpContext.Session.GetInt32("UserId");
        //        if (userId == null)
        //        {
        //            TempData["ErrorMessage"] = "Please login first";
        //            return RedirectToAction("Login", "Account");
        //        }

        //        var order = _context.Orders
        //            .FirstOrDefault(o => o.OrderId == orderId && o.UserId == userId.Value);

        //        if (order == null)
        //        {
        //            TempData["ErrorMessage"] = "Order not found";
        //            return RedirectToAction("MyOrders");
        //        }

        //        // Check: Order delivered hona chahiye
        //        if (order.OrderStatus != "Delivered" || !order.DeliveryDate.HasValue)
        //        {
        //            TempData["ErrorMessage"] = "Order must be delivered to request a return";
        //            return RedirectToAction("OrderDetails", new { orderId = orderId });
        //        }

        //        // REQUIREMENT: 7 din ka time limit
        //        var daysSinceDelivery = (DateTime.Now - order.DeliveryDate.Value).TotalDays;
        //        if (daysSinceDelivery > 7)
        //        {
        //            TempData["ErrorMessage"] = "Return request must be made within 7 days of delivery";
        //            return RedirectToAction("OrderDetails", new { orderId = orderId });
        //        }

        //        // Check: Already return ya replacement request nahi hona chahiye
        //        if (order.ReturnRequested || order.ReplacementRequested)
        //        {
        //            TempData["ErrorMessage"] = "A return or replacement request is already in progress";
        //            return RedirectToAction("OrderDetails", new { orderId = orderId });
        //        }

        //        // Return request set karo
        //        order.ReturnRequested = true;
        //        order.ReturnStatus = "Pending";
        //        order.UpdatedDate = DateTime.Now;
        //        order.UpdatedBy = userId.Value;

        //        //return reason
        //        order.ReturnReason = string.IsNullOrEmpty(returnReason) ? "No reason provided" : returnReason;

        //        _context.SaveChanges();
        //        TempData["SuccessMessage"] = $"Return request for order #{order.OrderNumber} submitted successfully!";
        //        return RedirectToAction("OrderDetails", new { orderId = orderId });
        //    }
        //    catch (Exception ex)
        //    {
        //        TempData["ErrorMessage"] = "Error submitting return request";
        //        return RedirectToAction("OrderDetails", new { orderId = orderId });
        //    }
        //}

        
[HttpPost]
public IActionResult RequestReturn(int orderId, string returnReason, string refundMethod, string refundEasypaisaNumber, string refundBankAccountNumber)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    TempData["ErrorMessage"] = "Please login first";
                    return RedirectToAction("Login", "Account");
                }

                var order = _context.Orders
                    .FirstOrDefault(o => o.OrderId == orderId && o.UserId == userId.Value);

                if (order == null)
                {
                    TempData["ErrorMessage"] = "Order not found";
                    return RedirectToAction("MyOrders");
                }

                // Check: Order delivered hona chahiye
                if (order.OrderStatus != "Delivered" || !order.DeliveryDate.HasValue)
                {
                    TempData["ErrorMessage"] = "Order must be delivered to request a return";
                    return RedirectToAction("OrderDetails", new { orderId = orderId });
                }

                // REQUIREMENT: 7 din ka time limit
                var daysSinceDelivery = (DateTime.Now - order.DeliveryDate.Value).TotalDays;
                if (daysSinceDelivery > 7)
                {
                    TempData["ErrorMessage"] = "Return request must be made within 7 days of delivery";
                    return RedirectToAction("OrderDetails", new { orderId = orderId });
                }

                // Check: Already return ya replacement request nahi hona chahiye
                if (order.ReturnRequested || order.ReplacementRequested)
                {
                    TempData["ErrorMessage"] = "A return or replacement request is already in progress";
                    return RedirectToAction("OrderDetails", new { orderId = orderId });
                }

                // COD orders ke liye refund method validation
                if (order.PaymentMethod == "cod")
                {
                    if (string.IsNullOrEmpty(refundMethod))
                    {
                        TempData["ErrorMessage"] = "Please select a refund method";
                        return RedirectToAction("OrderDetails", new { orderId = orderId });
                    }

                    if (refundMethod == "easypaisa" && (string.IsNullOrEmpty(refundEasypaisaNumber) || !Regex.IsMatch(refundEasypaisaNumber, @"^[0-9]{11}$")))
                    {
                        TempData["ErrorMessage"] = "Please provide a valid 11-digit Easypaisa number";
                        return RedirectToAction("OrderDetails", new { orderId = orderId });
                    }

                    if (refundMethod == "bank" && (string.IsNullOrEmpty(refundBankAccountNumber) || !Regex.IsMatch(refundBankAccountNumber, @"^[0-9]{16}$")))
                    {
                        TempData["ErrorMessage"] = "Please provide a valid 16-digit bank account number";
                        return RedirectToAction("OrderDetails", new { orderId = orderId });
                    }
                }

                // Return request set karo
                order.ReturnRequested = true;
                order.ReturnStatus = "Pending";
                order.ReturnReason = string.IsNullOrEmpty(returnReason) ? "No reason provided" : returnReason;
                order.RefundEasypaisaNumber = refundMethod == "easypaisa" ? refundEasypaisaNumber : null;
                order.RefundBankAccountNumber = refundMethod == "bank" ? refundBankAccountNumber : null;
                order.UpdatedDate = DateTime.Now;
                order.UpdatedBy = userId.Value;

                _context.SaveChanges();
                TempData["SuccessMessage"] = $"Return request for order #{order.OrderNumber} submitted successfully! Refund will be processed to your {(refundMethod == "easypaisa" ? "Easypaisa account" : "bank account")}.";
                return RedirectToAction("OrderDetails", new { orderId = orderId });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error submitting return request";
                return RedirectToAction("OrderDetails", new { orderId = orderId });
            }
        }

        // User: Request Replacement
        [HttpPost]
        public IActionResult RequestReplacement(int orderId, string replacementReason)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    TempData["ErrorMessage"] = "Please login first";
                    return RedirectToAction("Login", "Account");
                }

                var order = _context.Orders
                    .FirstOrDefault(o => o.OrderId == orderId && o.UserId == userId.Value);

                if (order == null)
                {
                    TempData["ErrorMessage"] = "Order not found";
                    return RedirectToAction("MyOrders");
                }

                // Check: Order delivered hona chahiye
                if (order.OrderStatus != "Delivered" || !order.DeliveryDate.HasValue)
                {
                    TempData["ErrorMessage"] = "Order must be delivered to request a replacement";
                    return RedirectToAction("OrderDetails", new { orderId = orderId });
                }

                // REQUIREMENT: 7 din ka time limit
                var daysSinceDelivery = (DateTime.Now - order.DeliveryDate.Value).TotalDays;
                if (daysSinceDelivery > 7)
                {
                    TempData["ErrorMessage"] = "Replacement request must be made within 7 days of delivery";
                    return RedirectToAction("OrderDetails", new { orderId = orderId });
                }

                // Check: Already return ya replacement request nahi hona chahiye
                if (order.ReturnRequested || order.ReplacementRequested)
                {
                    TempData["ErrorMessage"] = "A return or replacement request is already in progress";
                    return RedirectToAction("OrderDetails", new { orderId = orderId });
                }

                // Replacement request set karo
                order.ReplacementRequested = true;
                order.ReplacementStatus = "Pending";
                order.UpdatedDate = DateTime.Now;
                order.UpdatedBy = userId.Value;

                // Replacement reason ko store karne ke liye ek field chahiye
                // Assuming Orders table mein ek 'ReplacementReason' VARCHAR(500) field hai
                order.ReplacementReason = string.IsNullOrEmpty(replacementReason) ? "No reason provided" : replacementReason;

                _context.SaveChanges();
                TempData["SuccessMessage"] = $"Replacement request for order #{order.OrderNumber} submitted successfully!";
                return RedirectToAction("OrderDetails", new { orderId = orderId });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error submitting replacement request";
                return RedirectToAction("OrderDetails", new { orderId = orderId });
            }
        }






    }

    // Order View Model
    public class OrderViewModel
    {
        public decimal Subtotal { get; set; }
        public decimal Shipping { get; set; }
        public decimal Tax { get; set; }
        public decimal Total { get; set; }
        public string PaymentMethod { get; set; }
        public string EstimatedDelivery { get; set; }
        public string FullName { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string ZipCode { get; set; }
        // New fields for card and bank
        public string CardHolderName { get; set; }
        public string CardNumber { get; set; }
        public string ExpiryDate { get; set; }
        public string CVC { get; set; }
        public string BankName { get; set; }
        public string BankAccountNumber { get; set; }
        public string TransactionId { get; set; }
    }
}
