using System.ComponentModel.DataAnnotations.Schema;

namespace OnlineShoppingCart.Models
{
    public class FAQsViewmodel
    {
        public IEnumerable<Faqcategory> CategoryList { get; set; }
        public Faq FAQ { get; set; }

   
    }
}
