using Dapper;
using ShopifyApp.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ShopifyApp.Models
{
    public class ShipmentModel
    {
        public int ExigoOrderId { get; set; }
        public string ShopifyOrderId { get; set; }
        public int ShopifyOrderNum { get; set; }
        public int OrderStatusId { get; set; }
        public string TrackingNo { get; set; }
        public List<ShipmentModel> GetOrderList()
        {
            using (var sql = SQLContext.Sql())
            {
                return sql.Query<ShipmentModel>(@"SELECT
                    so.ExigoOrderId,
                    o.OrderStatusId,
                    so.ShopifyOrderId,
                    o.TrackingNumber1 AS TrackingNo
                FROM ShopifyContext.UnfulfilledOrders as so
                Inner Join dbo.Orders as o ON o.OrderID = so.ExigoOrderId
                ORDER BY o.OrderStatusId").ToList();
            }
        }
    }
}