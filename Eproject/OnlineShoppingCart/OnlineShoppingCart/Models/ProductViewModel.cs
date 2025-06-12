namespace OnlineShoppingCart.Models
{
    public class ProductViewModel
    {
        public Product productList { get; set; } // ✅ For single product in Admin form (Add/Edit)

        public IEnumerable<Category> categoryList { get; set; } // ✅ For dropdown in Admin form



        public IEnumerable<Product> ProductsToDisplay { get; set; } // ✅ For frontend homepage (cards etc.)
        public IEnumerable<Category> CategoryToDisplay { get; set; }  // ✅ For frontend homepage (category cards etc.)

        public IEnumerable<ProductReview> Reviews { get; set; } // ✅  Reviews list for display

    }
}
