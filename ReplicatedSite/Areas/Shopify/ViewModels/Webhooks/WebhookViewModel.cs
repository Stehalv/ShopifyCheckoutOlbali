using ShopifyApp.Models;
using System.Collections.Generic;

namespace ReplicatedSite.Areas.Shopify.ViewModels
{
    public class WebhookViewModel
    {
        public WebhookViewModel(string webhookId)
        {
            WebhookId = webhookId;
            Populate();
        }
        public string WebhookId { get; set; }
        public List<Log> MainLog { get; set; }
        public List<OrderLog> OrderLog { get; set; }
        public List<CustomerLog> CustomerLog { get; set; }
        public Webhook Hook { get; set; }
        public void Populate()
        {
            Hook = new Webhook().GetWebhookById(WebhookId);
            MainLog = new Log().GetAllByWebhookId(WebhookId);
            OrderLog = new OrderLog().GetAllByWebhookId(WebhookId);
            CustomerLog = new CustomerLog().GetAllByWebhookId(WebhookId);
        }
    }
}