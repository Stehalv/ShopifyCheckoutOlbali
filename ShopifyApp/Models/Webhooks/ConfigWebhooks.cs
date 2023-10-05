using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ShopifyApp.Models
{
    public class ConfigWebhooks
    {
        public int TenantConfigId { get; set; }
        public TenantConfiguration TenantConfig { get; set; }
        public string ShopifyUrl { get; set; }
        public List<ShopWebhook> Webhooks { get; set; }
    }
}