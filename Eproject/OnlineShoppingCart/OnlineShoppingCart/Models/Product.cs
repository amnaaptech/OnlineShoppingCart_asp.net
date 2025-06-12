using System;
using System.Collections.Generic;

namespace OnlineShoppingCart.Models;

public partial class Product
{
    public string ProductId { get; set; } = null!;

    public string ProductName { get; set; } = null!;

    public string? Description { get; set; }

    public string? ImagePath { get; set; }

    public decimal Price { get; set; }

    public int CategoryId { get; set; }

    public int StockQuantity { get; set; }

    public int? WarrantyPeriodDays { get; set; }

    public DateTime CreatedDate { get; set; }

    public virtual ICollection<Cart> Carts { get; set; } = new List<Cart>();

    public virtual Category Category { get; set; } = null!;

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual ICollection<ProductReview> ProductReviews { get; set; } = new List<ProductReview>();
}
