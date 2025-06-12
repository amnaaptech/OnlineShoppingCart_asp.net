using System;
using System.Collections.Generic;

namespace OnlineShoppingCart.Models;

public partial class Faq
{
    public int Faqid { get; set; }

    public string Fquestion { get; set; } = null!;

    public string Fanswer { get; set; } = null!;

    public int FcategoryId { get; set; }

    public DateTime? CreatedDate { get; set; }

    public virtual Faqcategory Fcategory { get; set; } = null!;
}
