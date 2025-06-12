using System;
using System.Collections.Generic;

namespace OnlineShoppingCart.Models;

public partial class Order
{
    public int OrderId { get; set; }

    public string OrderNumber { get; set; } = null!;

    public int UserId { get; set; }

    public DateTime OrderDate { get; set; }

    public decimal Subtotal { get; set; }

    public decimal Shipping { get; set; }

    public decimal Tax { get; set; }

    public decimal Total { get; set; }

    public string PaymentMethod { get; set; } = null!;

    public string PaymentStatus { get; set; } = null!;

    public string DeliveryType { get; set; } = null!;

    public DateTime EstimatedDeliveryDate { get; set; }

    public DateTime? DispatchedDate { get; set; }

    public DateTime? DeliveryDate { get; set; }

    public string OrderStatus { get; set; } = null!;

    public string FullName { get; set; } = null!;

    public string Phone { get; set; } = null!;

    public string Address { get; set; } = null!;

    public string City { get; set; } = null!;

    public string State { get; set; } = null!;

    public string ZipCode { get; set; } = null!;

    public bool ReturnRequested { get; set; }

    public string? ReturnStatus { get; set; }

    public bool ReplacementRequested { get; set; }

    public string? ReplacementStatus { get; set; }

    public int? CreatedBy { get; set; }

    public int? UpdatedBy { get; set; }

    public DateTime CreatedDate { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public string? ReturnReason { get; set; }

    public string? ReplacementReason { get; set; }

    public string? CardNumber { get; set; }

    public string? BankAccountNumber { get; set; }

    public string? TransactionId { get; set; }

    public string? CardHolderName { get; set; }

    public string? BankName { get; set; }

    public string? ExpiryDate { get; set; }

    public string? Cvc { get; set; }

    public string? RefundEasypaisaNumber { get; set; }

    public string? RefundBankAccountNumber { get; set; }

    public virtual User? CreatedByNavigation { get; set; }

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual User? UpdatedByNavigation { get; set; }

    public virtual User User { get; set; } = null!;
}
