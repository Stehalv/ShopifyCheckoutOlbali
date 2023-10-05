using ShopifyApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ReplicatedSite.Areas.Shopify.ViewModels
{
    public class OrdersViewModel
    {
        public int PageSize { get; set; }
        public int PreviousPage
        {
            get
            {
                return (CurrentPage == 1) ? 1 : CurrentPage - 1;
            }
        }
        public int CurrentPage { get; set; }
        public int NextPage
        {
            get
            {
                return (CurrentPage * PageSize > TotalOrders) ? CurrentPage : CurrentPage + 1;
            }
        }
        public int StartRowNumber { get; set; }
        public int EndRowNumber { get; set; }
        public int TotalOrders { get; set; }
        public Tenant Tenant {get; set;}
        public List<Order> Orders { get; set; }
    }
}