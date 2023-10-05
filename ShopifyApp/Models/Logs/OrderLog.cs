using Dapper;
using ShopifyApp.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ShopifyApp.Models
{
    public class OrderLog
    {
        public OrderLog()
        {

        }
        public OrderLog(int orderId, string message, string webhookId, int tenantConfigId)
        {
            OrderId = orderId;
            Message = message;
            WebhookId = webhookId;
            TenantConfigId = tenantConfigId;
        }
        public OrderLog(string message, string webhookId, int tenantConfigId)
        {
            Message = message;
            WebhookId = webhookId;
            TenantConfigId = tenantConfigId;
        }
        public int Id { get; set; }
        public int OrderId { get; set; }
        public string Message { get; set; }
        public string WebhookId { get; set; }
        public int TenantConfigId { get; set; }
        public DateTime CreatedDate { get; set; }
        public List<OrderLog> GetAll(int orderId)
        {
            using (var sql = SQLContext.Sql())
            {
                return sql.Query<OrderLog>($"Select * from {Settings.DatabaseContext}OrderLogs where OrderId = {orderId}").ToList();
            }
        }
        public List<OrderLog> GetAllByWebhookId(string webhookId)
        {
            using (var sql = SQLContext.Sql())
            {
                return sql.Query<OrderLog>($"Select * from {Settings.DatabaseContext}OrderLogs where WebhookId = '{webhookId}'").ToList();
            }
        }
        public void Create()
        {
            using (var sql = SQLContext.Sql())
            {
                sql.Query($"INSERT INTO {Settings.DatabaseContext}OrderLogs (OrderId, Message, WebhookId, CreatedDate) VALUES ({OrderId}, @message, '{WebhookId}', getDate())", new { message = Message });
            }
        }
        public void CreateByExigoOrderId(int exigoOrderId)
        {
            OrderId = new Order().GetOrderIDByExigoOrderId(exigoOrderId, TenantConfigId);
            using (var sql = SQLContext.Sql())
            {
                sql.Query($"INSERT INTO {Settings.DatabaseContext}OrderLogs (OrderId, Message, WebhookId, CreatedDate) VALUES ({OrderId}, @message, '{WebhookId}', getDate())", new { message = Message });
            }
        }
        public void CreateByShopifyOrderId(int shopifyOrderId)
        {
            OrderId = new Order().GetOrderIDByShopifyOrderId(shopifyOrderId, TenantConfigId);
            using (var sql = SQLContext.Sql())
            {
                sql.Query($"INSERT INTO {Settings.DatabaseContext}OrderLogs (OrderId, Message, WebhookId, CreatedDate) VALUES ({OrderId}, @message, '{WebhookId}', getDate())", new { message = Message });
            }
        }
        public void CreateByShopifyOrderReference(string shopifyOrderreference)
        {
            OrderId = new Order().GetOrderIDByShopifyOrderReference(shopifyOrderreference, TenantConfigId);
            using (var sql = SQLContext.Sql())
            {
                sql.Query($"INSERT INTO {Settings.DatabaseContext}OrderLogs (OrderId, Message, WebhookId, CreatedDate) VALUES ({OrderId}, @message, '{WebhookId}', getDate())", new { message = Message });
            }
        }
        public void DeleteByOrderId(int orderId)
        {
            using (var sql = SQLContext.Sql())
            {
                sql.Query($"DELETE {Settings.DatabaseContext}OrderLogs Where OrderId = {orderId}");
            }
        }
    }
}