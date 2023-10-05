using Dapper;
using ShopifyApp.Data;
using ShopifyApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using ShopifyApp.Api.ExigoWebservice;

namespace ShopifyApp.Services
{
    public static partial class Exigo
    {
        #region Order

        public static OrderResponse GetOrder(TenantConfiguration tenantConfig, int orderId)
        {
            GetOrdersResponse response = Exigo.WebService(tenantConfig).GetOrders(new GetOrdersRequest { 
                OrderIDs = new[] { orderId },
                    
            });
            var order = response.Orders.FirstOrDefault();
            GetPaymentsResponse Payments = Exigo.WebService(tenantConfig).GetPayments(new GetPaymentsRequest
            {
                OrderID = order.OrderID
            });
            order.Payments = Payments.Payments;
            return order;
        }
        public static Item GetItemVolumes(string itemcode, TenantOrderConfiguration config)
        {
            using (var context = SQLContext.Sql())
            {
                return context.Query<Item>(@"Select CV = ip.CommissionableVolume, BV = ip.BusinessVolume From ItemPrices ip
                    Inner Join Items i on i.ItemID = ip.ItemID
                    Where i.ItemCode = @itemcode
                    and ip.PriceTypeID = @priceType
                    and ip.CurrencyCode = @currencyCode", new
                {
                    itemcode, 
                    currencyCode = config.CurrencyCode,
                    priceType = config.PriceTypeID
                }).FirstOrDefault();
            }
        }
        #endregion

    }
}