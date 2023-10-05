using Dapper;
using ShopifyApp.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ShopifyApp.Models
{
    public class AvailableWebhook 
    {
        public string MethodName { get; set; }
        public string Topic { get; set; }
        public List<AvailableWebhook> GetAll()
        {
            return new List<AvailableWebhook>
            {
                new AvailableWebhook{ MethodName = "CreateOrder", Topic = "orders/create"},
                new AvailableWebhook{ MethodName = "CancelOrder", Topic = "orders/cancelled"},
                new AvailableWebhook{ MethodName = "UpdateOrder", Topic = "orders/updated"},
                new AvailableWebhook{ MethodName = "CreateCustomer", Topic = "customers/create" },
                new AvailableWebhook{ MethodName = "UpdateCustomer", Topic = "customers/update" },
                new AvailableWebhook{ MethodName = "UpdateProduct", Topic = "products/update" }
            };
        }
    }
}