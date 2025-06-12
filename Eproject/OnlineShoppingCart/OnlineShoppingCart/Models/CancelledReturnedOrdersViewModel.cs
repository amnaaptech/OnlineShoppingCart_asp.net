namespace OnlineShoppingCart.Models
{
    public class CancelledReturnedOrdersViewModel
    {
        public List<Order> UserCancelledOrders { get; set; } = new List<Order>();
        public List<Order> AdminCancelledOrders { get; set; } = new List<Order>();
        public List<Order> ReturnedOrders { get; set; } = new List<Order>();
        public List<Order> ReplacementOrders { get; set; } = new List<Order>();
        public string SearchOrderNumber { get; set; }
    }
}