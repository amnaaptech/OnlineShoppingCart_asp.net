using System;
using System.Collections.Generic;

namespace OnlineShoppingCart.Models;

public partial class ConfirmEmployee
{
    public int Id { get; set; }

    public string From { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string Subject { get; set; } = null!;

    public string Password { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string EmployeeId { get; set; } = null!;

    public string? Message { get; set; }

    public DateTime? SentAt { get; set; }
}
