using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineShoppingCart.Data;
using OnlineShoppingCart.Models;

namespace OnlineShoppingCart.Controllers
{
    public class AccountController : Controller
    {
        private readonly ArtsContext db;
        private readonly IWebHostEnvironment env;

        public AccountController(ArtsContext db , IWebHostEnvironment env)
        {
            this.db = db;
            this.env = env;
        }
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel Rmodel)
        {
            try
            {
                // Check if model is valid
                if (!ModelState.IsValid)
                {
                    return View(Rmodel);
                }

                // Check if email already exists
                var exist = await db.Users.AnyAsync(u => u.UserEmail == Rmodel.UserEmail.Trim());
                if (exist)
                {
                    ModelState.AddModelError("UserEmail", "Email Already Exists");
                    return View(Rmodel);
                }

                // Default Role
                string defaultRole = Rmodel.Role ?? "Customer";

                // Default Images
                string defaultImgPath = "~/frontassets/img/person/profile.png";

                //// Hash the password
                //string hashedPassword = BCrypt.Net.BCrypt.HashPassword(Rmodel.UserPassword.Trim());

                var user = new User
                {
                    UserEmail = Rmodel.UserEmail.Trim(),
                    UserName = Rmodel.UserName.Trim(),
                    UserPassword = Rmodel.UserPassword.Trim(), // Store hashed password
                    Role = defaultRole,
                    Address = Rmodel.Address?.Trim(),
                    PhoneNumber = Rmodel.PhoneNumber?.Trim(),
                    ImagePath = defaultImgPath,
                    RegistrationDate = DateTime.Now
                };

                db.Users.Add(user);
                await db.SaveChangesAsync();

                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                // Log the exception
                ModelState.AddModelError("", "An error occurred while creating your account. Please try again.");
                return View(Rmodel);
            }
        }

        //Login

        [HttpGet]
        public IActionResult Login()
        {
            // Cache-Control headers add karna
            Response.Headers.Add("Cache-Control", "no-cache, no-store, must-revalidate");
            Response.Headers.Add("Pragma", "no-cache");
            Response.Headers.Add("Expires", "0");

            if (HttpContext.Session.GetString("UserId") != null)
            {
                var userrole = HttpContext.Session.GetString("UserRole");
                if (userrole == "Admin")
                    return RedirectToAction("dashboard", "Admin");
                else if (userrole == "Employee")
                    return RedirectToAction("ManageOrders", "Employee");
                else if (userrole == "Customer")
                    return RedirectToAction("Index", "Home");
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel loginmodel)
        {
            if (!ModelState.IsValid)
            {
                return View(loginmodel);
            }

            // 1. Email check
            var user = await db.Users.FirstOrDefaultAsync(u => u.UserEmail == loginmodel.UserEmail.Trim());

            if (user == null)
            {
                ModelState.AddModelError("UserEmail", "Email not registered.");
                return View(loginmodel);
            }

            // 2. Password match
            if (user.UserPassword.Trim() != loginmodel.UserPassword.Trim())
            {
                ModelState.AddModelError("UserPassword", "Wrong password.");
                return View(loginmodel);
            }

            // 3. Set session values
            string userImagePath = user.ImagePath ?? "~/frontassets/img/person/profile.png";
            HttpContext.Session.SetInt32("UserId", user.UserId);
            HttpContext.Session.SetString("UserName", user.UserName);
            HttpContext.Session.SetString("UserRole", user.Role);
            HttpContext.Session.SetString("UserImagePath", userImagePath);



            // 4. Set CartCount from database
            var cartCount = db.Carts
                .Where(c => c.UserId == user.UserId)
                .Sum(c => c.Quantity);
            HttpContext.Session.SetInt32("CartCount", cartCount);


            // 4. Redirect based on role
            return user.Role switch
            {
                "Admin" => RedirectToAction("dashboard", "Admin"),
                "Employee" => RedirectToAction("ManageOrders", "Employee"),
                "Customer" => RedirectToAction("Index", "Home"),
                _ => RedirectToAction("Login")
            };
        }


        [HttpPost] // POST method use karna logout ke liye
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            // Session completely clear karna
            HttpContext.Session.Clear();

            // Cache headers set karna
            Response.Headers.Add("Cache-Control", "no-cache, no-store, must-revalidate");
            Response.Headers.Add("Pragma", "no-cache");
            Response.Headers.Add("Expires", "0");

            // TempData use kar ke message pass karna
            TempData["LogoutMessage"] = "You have been successfully logged out.";

            return RedirectToAction("Login");
        }

        // GET method bhi rakhna agar koi direct link access kare
        [HttpGet]
        public IActionResult LogoutGet()
        {
            return RedirectToAction("Logout");
        }



        // Profile image update functionality
        [HttpGet]
        public IActionResult UpdateProfile()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var user = db.Users.FirstOrDefault(u => u.UserId == userId);

            if (user == null)
            {
                return RedirectToAction("Login");
            }

            // Return the user model to the view for display
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfilePicture(IFormFile ProfileImage)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var user = await db.Users.FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null)
            {
                return RedirectToAction("Login");
            }

            if (ProfileImage != null && ProfileImage.Length > 0)
            {
                // Save the new profile image
                string uploadsFolder = Path.Combine(env.WebRootPath, "ProfileImages");
                string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(ProfileImage.FileName);
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                // Save the file to disk
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await ProfileImage.CopyToAsync(fileStream);
                }

                // Update the user's image path in the database
                user.ImagePath = "/ProfileImages/" + uniqueFileName;
                db.Update(user);
                await db.SaveChangesAsync();

                // Update session with new image path
                HttpContext.Session.SetString("UserImagePath", user.ImagePath);
            }

            return RedirectToAction("UpdateProfile"); // Redirect back to the profile page
        }












    }
}
