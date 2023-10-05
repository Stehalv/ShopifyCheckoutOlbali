using ShopifyApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ReplicatedSite.Areas.Shopify.ViewModels
{
    public class TenantConfigurationViewModel
    {
        public TenantConfiguration Config { get; set; }
        public WebHooksViewModel Webhooks { get; set; }
        public List<Item> Products { get; set; }
    }
}