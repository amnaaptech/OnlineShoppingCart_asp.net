using System.ComponentModel.DataAnnotations;

namespace OnlineShoppingCart.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid Email Address")]
        public string UserEmail { get; set; }

        [Required(ErrorMessage = "Password is required")]
        public string UserPassword { get; set; }
    }
}

