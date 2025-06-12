using System;
using System.Collections.Generic;

namespace OnlineShoppingCart.Models;

public partial class Faqcategory
{
    public int FcategoryId { get; set; }

    public string Fcategory { get; set; } = null!;

    public virtual ICollection<Faq> Faqs { get; set; } = new List<Faq>();
}
