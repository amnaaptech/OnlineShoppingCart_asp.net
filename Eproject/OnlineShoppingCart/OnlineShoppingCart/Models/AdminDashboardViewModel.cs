namespace OnlineShoppingCart.Models
{
    public class AdminDashboardViewModel
    {
        public List<RequestNotification> PendingRequests { get; set; } = new List<RequestNotification>();
        public int CustomerCount { get; set; }
        public int EmployeeCount { get; set; }
        public decimal TotalOrders { get; set; }
        public int CategoryCount { get; set; }

        public int OrderCount { get; set; }
        public int ProductCount { get; set; }
        public int ReviewCount { get; set; }
        public int ContactCount { get; set; }
    }

    public class RequestNotification
    {
        public int OrderId { get; set; }
        public string OrderNumber { get; set; }
        public string RequestType { get; set; } // Return, Replacement, Cancel
        public string RequestStatus { get; set; } // Pending, Approved, Rejected
        public string CustomerName { get; set; }
        public DateTime RequestDate { get; set; }
        public string Reason { get; set; } // ReturnReason, ReplacementReason, or CancelReason
    }
}