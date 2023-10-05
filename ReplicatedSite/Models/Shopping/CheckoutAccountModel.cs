using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ReplicatedSite.Models
{
    public class CheckoutAccountModel
    {
        public bool HasExigoAccount { get; set; }
        public bool HasShopAccount { get; set; }
        public string ShopUrl { get; set; }
        public string Email { get; set; }
        public int CustomerID { get; set; }
    }
}