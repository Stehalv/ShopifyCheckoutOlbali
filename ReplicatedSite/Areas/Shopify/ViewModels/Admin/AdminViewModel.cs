using ShopifyApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ReplicatedSite.Areas.Shopify.ViewModels
{
    public class AdminViewModel
    {
        public List<User> Users { get; set; }
    }
}