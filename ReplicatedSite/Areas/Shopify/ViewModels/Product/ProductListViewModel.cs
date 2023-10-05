using ShopifyApp.Models;
using ShopifySharp;
using ShopifySharp.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ReplicatedSite.Areas.Shopify.ViewModels
{
    public class ProductListViewModel
    {
        public int PageSize { get; set; }
        public string NextFilter { get; set; }
        public string PreviousFilter { get; set; }
        public List<ShopifySharp.Product> Items { get; set; }
    }
}