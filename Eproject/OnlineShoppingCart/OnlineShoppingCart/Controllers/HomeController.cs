using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using OnlineShoppingCart.Data;
using OnlineShoppingCart.Models;
using System.Diagnostics;

namespace OnlineShoppingCart.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ArtsContext db;
        private readonly IWebHostEnvironment env;

        public HomeController(ILogger<HomeController> logger, ArtsContext db, IWebHostEnvironment env)
        {
            _logger = logger;
            this.db = db;
            this.env = env;
        }

        public IActionResult Index()
        {
            var viewModel = new ProductViewModel
            {
                ProductsToDisplay = db.Products.OrderByDescending(P => P.CreatedDate).ToList(), // ✅ Saare products frontend ke liye
                CategoryToDisplay = db.Categories.ToList()     // ✅ Saari categories frontend ke liye
            };

            return View(viewModel); // ✅ Isko Index.cshtml me bhejnah hai
        }



        ///filtration of category products
        public IActionResult ProductsByCategory(int id)
        {
            var products = db.Products.Where(p => p.CategoryId == id).ToList();

            var category = db.Categories.ToList();

            var viewModel = new ProductViewModel
            {
                ProductsToDisplay = products,
                CategoryToDisplay = category,
            };

            return View("ProductList", viewModel);
        }

        
        /// category prod list
       
        public IActionResult ProductList()
        {
            return View();
        }

        /// Alll prod list



   public IActionResult AllProducts(string searchQuery)
        {
            var viewModel = new ProductViewModel
            {
                ProductsToDisplay = string.IsNullOrWhiteSpace(searchQuery)
                    ? db.Products.ToList()
                    : db.Products.AsEnumerable() 
                        .Where(p => p.ProductName.Contains(searchQuery, StringComparison.OrdinalIgnoreCase))
                        .ToList(),
                CategoryToDisplay = db.Categories.ToList()
            };

            ViewBag.SearchQuery = searchQuery;

            return View(viewModel);
        }


        // Single Product
        public IActionResult SingleProduct(string id)
        {
            var product = db.Products
                .Include(p => p.Category)
                .Include(p => p.ProductReviews)
                .FirstOrDefault(p => p.ProductId == id);

            if (product == null)
                return NotFound();

            var model = new ProductViewModel
            {
                productList = product,
                Reviews = product.ProductReviews.OrderByDescending(r => r.ReviewDate).ToList()
            };

            return View(model);
        }



        /// REVIEW PROD//
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SubmitReview(ProductReview review)
        {
            // Check if user is logged in via session
            if (HttpContext.Session.GetString("UserId") == null)
            {
                return RedirectToAction("Login", "Account", new { returnUrl = HttpContext.Request.Path });
            }

            // Remove Product from ModelState validation since form only sends ProductId
            ModelState.Remove("Product");
            ModelState.Remove("ReviewerName"); // Remove these from validation
            ModelState.Remove("ReviewerEmail"); // since we'll set them manually

            if (!ModelState.IsValid)
            {
                var product = db.Products
                    .Include(p => p.Category)
                    .Include(p => p.ProductReviews)
                    .FirstOrDefault(p => p.ProductId == review.ProductId);
                var model = new ProductViewModel
                {
                    productList = product,
                    Reviews = product?.ProductReviews.OrderByDescending(r => r.ReviewDate).ToList() ?? new List<ProductReview>(),
                    categoryList = new List<Category>(),
                    ProductsToDisplay = new List<Product>(),
                    CategoryToDisplay = new List<Category>()
                };
                return View("SingleProduct", model);
            }

            try
            {
                // Get user ID from session
                int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                var user = db.Users.FirstOrDefault(u => u.UserId == userId);

                if (user == null)
                {
                    // If user not found in DB, clear session and redirect to login
                    HttpContext.Session.Clear();
                    return RedirectToAction("Login", "Account", new { returnUrl = HttpContext.Request.Path });
                }

                // Set ReviewerName and ReviewerEmail from user's data
                review.ReviewerName = user.UserName;
                review.ReviewerEmail = user.UserEmail;
                review.ReviewDate = DateOnly.FromDateTime(DateTime.Now);

                db.ProductReviews.Add(review);
                db.SaveChanges();
            }
            catch (Exception)
            {
                var product = db.Products
                    .Include(p => p.Category)
                    .Include(p => p.ProductReviews)
                    .FirstOrDefault(p => p.ProductId == review.ProductId);
                var model = new ProductViewModel
                {
                    productList = product,
                    Reviews = product?.ProductReviews.OrderByDescending(r => r.ReviewDate).ToList() ?? new List<ProductReview>(),
                    categoryList = new List<Category>(),
                    ProductsToDisplay = new List<Product>(),
                    CategoryToDisplay = new List<Category>()
                };
                ModelState.AddModelError("", "An error occurred while saving the review. Please try again.");
                return View("SingleProduct", model);
            }

            return RedirectToAction("SingleProduct", new { id = review.ProductId });
        }



        ////FAQS SHOWW WORK

        public IActionResult Faqs()
        {
            var categoriesWithFaqs = db.Faqcategories
             .Where(c => c.Faqs.Any()) // Only categories that have FAQs
              .Select(c => new Faqcategory
      {
          FcategoryId = c.FcategoryId,
          Fcategory = c.Fcategory,
          Faqs = c.Faqs.ToList()
      }).ToList();

            return View(categoriesWithFaqs);
        }







        /// Update user profile work

        [HttpGet]
        public async Task<IActionResult> UpdateProfile()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var user = await db.Users.FindAsync(userId);
            if (user == null) return NotFound();

            ViewBag.ExistingImage = string.IsNullOrEmpty(user.ImagePath) ? "~/frontassets/img/person/profile.png" : user.ImagePath;
            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProfile(IFormFile profileImage, string newPassword, string fullName, string address, string phoneNumber, string UserPassword)
        {
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

                // ✅ Save the new password to existing column
                user.UserPassword = newPassword.Trim();
            }


            if (!string.IsNullOrWhiteSpace(fullName))
            {
                user.UserName = fullName.Trim();
                HttpContext.Session.SetString("UserName", user.UserName);
            }

            if (!string.IsNullOrWhiteSpace(address))
            {
                user.Address = address.Trim();
            }

            if (!string.IsNullOrWhiteSpace(phoneNumber))
            {
                user.PhoneNumber = phoneNumber.Trim();
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

            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Contact()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Contact(ContactU contact)
        {
            if (ModelState.IsValid)
            {
                db.ContactUs.Add(contact);
                db.SaveChanges();

                TempData["SuccessMessage"] = "Contact submitted successfully!";
                return RedirectToAction("Contact");
            }

            return View(contact);
        }

























        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
