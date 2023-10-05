using ShopifyApp.Models;
using ShopifyApp.Services.ShopService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ReplicatedSite.Areas.Shopify.ViewModels
{
    public class TenantConfigurationPriceRuleViewModel
    {
        public TenantConfiguration Config { get; set; } 
        public List<ShopifySharp.PriceRule> Rules { get; set; }
        public async void GetAll()
        {
            Rules = await new ShopifyDAL(Config).GetPricerules();
        }
    }
}