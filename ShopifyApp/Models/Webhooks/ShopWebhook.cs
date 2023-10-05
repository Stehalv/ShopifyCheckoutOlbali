using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ShopifyApp.Models
{
    public class ShopWebhook
    {
        public ShopWebhook()
        {

        }
        public ShopWebhook(ShopifySharp.Webhook hook)
        {
            Id = hook.Id.Value;
            Topic = hook.Topic;
            CreatedDate = hook.CreatedAt.Value.DateTime;
            Address = hook.Address;
        }
        public ShopWebhook(WooCommerceNET.WooCommerce.v3.Webhook hook)
        {
            Id = (long)hook.id;
            Topic = hook.topic;
            Address = hook.delivery_url;
            CreatedDate = hook.date_created.Value;
        }
        public long Id { get; set; }
        public string Topic { get; set; }
        public Nullable<DateTime> CreatedDate { get; set; }
        public string Address { get; set; }
    }
}