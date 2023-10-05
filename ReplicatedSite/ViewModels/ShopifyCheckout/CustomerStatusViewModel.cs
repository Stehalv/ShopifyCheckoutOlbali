using ShopifyApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ReplicatedSite.ViewModels
{
    public class CustomerStatusViewModel
    {

        public int ExigoCustomerId { get; set; }
        public ExigoService.Customer ExigoCustomer { get; set; }
        public Customer Customer { get; set; }
        public CustomerType CustomerType { get; set; }
        public string ActivationLink { get; set; }
        public ShopifySharp.Customer ShopifyCustomer { get; set; }
        public List<string> Log { get; set; }

    }
}