using System;
using System.Collections.Generic;

namespace OnlineShoppingCart.Models;

public partial class Cart
{
    public int CartId { get; set; }

    public int UserId { get; set; }

    public string ProductId { get; set; } = null!;

    public int Quantity { get; set; }

    public DateTime AddedDate { get; set; }

    public virtual Product Product { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
