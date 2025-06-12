using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace OnlineShoppingCart.Models
{
    public class AddEmployeeViewModel
    {
        [Required(ErrorMessage = "Name is required")]
        [MinLength(2, ErrorMessage = "Name must be at least 2 characters")]
        public string UserName { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [RegularExpression(@"^[a-zA-Z]{4,13}\d{2,3}@gmail\.com$",
             ErrorMessage = "Email must start with 4-13 letters, followed by 2-3 digits, and end with @gmail.com")]
        public string UserEmail { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*#?&^])[A-Za-z\d@$!%*#?&^]{8,}$",
            ErrorMessage = "Password must include uppercase, lowercase, number, and special character")]
        public string UserPassword { get; set; }

        public string? Role { get; set; }
    }
}
