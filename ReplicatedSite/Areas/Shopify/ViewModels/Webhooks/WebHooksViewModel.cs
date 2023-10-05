using ShopifyApp.Models;
using System.Collections.Generic;

namespace ReplicatedSite.Areas.Shopify.ViewModels
{
    public class WebHooksViewModel
    {
        public WebHooksViewModel()
        {
            ConfigWebhooks = new List<ConfigWebhooks>();
            Backlog = new List<Webhook>();
            AvailableWebhooks = new List<AvailableWebhook>();
        }
        public Tenant Tenant { get; set; }
        public int Count { get; set; }
        public List<ConfigWebhooks> ConfigWebhooks { get; set; }
        public dynamic WebHook { get; set; }
        public List<Webhook> Backlog { get; set; }
        public List<AvailableWebhook> AvailableWebhooks { get; set; }
    }
}