using Newtonsoft.Json;
using ShopifyApp.Api.ExigoWebservice;
using ShopifyApp.Models;
using ShopifyApp.Services;
using ShopifyApp.Services.ShopService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace ReplicatedSite.Areas.Shopify.ViewModels
{
    public class OrderDetailsViewModel
    {
        public OrderDetailsViewModel(int id)
        {

            Order = new Order().Get(id);
        }
        public Order Order { get; set; }
        public ShopifySharp.Order ShopOrder { get; set; }
        public string ShopifyOrderJson
        {
            get
            {
                return JsonConvert.SerializeObject(ShopOrder);
            }
        }
        public OrderResponse ExigoOrder { get; set; }
        public string ExigoOrderJson
        {
            get
            {
                return JsonConvert.SerializeObject(ExigoOrder);
            }
        }
        public List<Refunds> ExigoRefunds { get; set; }
        public List<OrderLog> Log { get; set; }
        public Customer Customer { get; set; }
        public async Task<bool> Populate()
        {
            var tenantConfig = new TenantConfiguration().Get(Order.TenantConfigId);
            ShopOrder = await new ShopifyDAL(tenantConfig).GetOrder(Order.ShopOrderReference);
            ExigoOrder = ShopifyApp.Services.Exigo.GetOrder(tenantConfig, Order.ExigoOrderId);
            ExigoRefunds = (ShopOrder.Refunds.Any()) ? new Refunds().GetAllByOrderId(ShopOrder.OrderNumber.ToString()) : new List<Refunds>();
            Log = new OrderLog().GetAll(Order.Id);
            return true;
        }

    }
}