using System;
using System.Collections.Generic;

namespace OnlineShoppingCart.Models;

public partial class ContactU
{
    public int ContactId { get; set; }

    public string ContactName { get; set; } = null!;

    public string ContactEmail { get; set; } = null!;

    public string? ContactSubject { get; set; }

    public string ContactMessage { get; set; } = null!;
}
