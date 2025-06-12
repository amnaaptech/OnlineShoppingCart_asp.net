using System;
using System.Collections.Generic;

namespace OnlineShoppingCart.Models;

public partial class User
{
    public int UserId { get; set; }

    public string UserEmail { get; set; } = null!;

    public string UserName { get; set; } = null!;

    public string UserPassword { get; set; } = null!;

    public string Role { get; set; } = null!;

    public string? Address { get; set; }

    public DateTime RegistrationDate { get; set; }

    public string? PhoneNumber { get; set; }

    public string? ImagePath { get; set; }

    public virtual ICollection<Cart> Carts { get; set; } = new List<Cart>();

    public virtual ICollection<Order> OrderCreatedByNavigations { get; set; } = new List<Order>();

    public virtual ICollection<Order> OrderUpdatedByNavigations { get; set; } = new List<Order>();

    public virtual ICollection<Order> OrderUsers { get; set; } = new List<Order>();
}
