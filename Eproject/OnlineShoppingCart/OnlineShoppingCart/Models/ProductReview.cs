using System;
using System.Collections.Generic;

namespace OnlineShoppingCart.Models;

public partial class ProductReview
{
    public int ReviewId { get; set; }

    public string ProductId { get; set; } = null!;

    public string ReviewerName { get; set; } = null!;

    public string ReviewerEmail { get; set; } = null!;

    public string ReviewContent { get; set; } = null!;

    public int Rating { get; set; }

    public DateOnly? ReviewDate { get; set; }

    public virtual Product Product { get; set; } = null!;
}
