using Dapper;
using ShopifyApp.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ShopifyApp.Models
{
    public class CustomerLog
    {
        public CustomerLog()
        {

        }
        public CustomerLog(int customerId, string message, string webhookId)
        {
            CustomerId = customerId;
            Message = message;
            WebhookId = webhookId;
        }
        public CustomerLog(string message, string webhookId)
        {
            Message = message;
            WebhookId = webhookId;
        }
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public string Message { get; set; }
        public string WebhookId { get; set; }
        public DateTime CreatedDate { get; set; }
        public List<CustomerLog> GetAll(int customerId)
        {
            using (var sql = SQLContext.Sql())
            {
                return sql.Query<CustomerLog>($"Select * from {Settings.DatabaseContext}CustomerLogs where CustomerId = {customerId}").ToList();
            }
        }
        public List<CustomerLog> GetAllByWebhookId(string webhookId)
        {
            using (var sql = SQLContext.Sql())
            {
                return sql.Query<CustomerLog>($"Select * from {Settings.DatabaseContext}CustomerLogs where WebhookId = '{webhookId}'").ToList();
            }
        }
        public void Create()
        {
            using (var sql = SQLContext.Sql())
            {
                sql.Query($"INSERT INTO {Settings.DatabaseContext}CustomerLogs (CustomerId, Message, WebhookId, CreatedDate) VALUES ({CustomerId}, @message, '{WebhookId}', getDate())", new { message = Message });
            }
        }
        public void CreateByExigoCustomerId(int exigoCustomerId, int tenantConfigId)
        {
            CustomerId = new Customer().GetCustomerIDByExigoCustomerId(exigoCustomerId, tenantConfigId);
            using (var sql = SQLContext.Sql())
            {
                sql.Query($"INSERT INTO {Settings.DatabaseContext}CustomerLogs (CustomerId, Message, WebhookId, CreatedDate) VALUES ({CustomerId}, @message, {WebhookId} getDate())", new { message = Message });
            }
        }
        public void CreateByShopifyCustomerId(string shopifyCustomerId, int tenantConfigId )
        {
            CustomerId = new Customer().GetCustomerIDByShopifyCustomerId(shopifyCustomerId, tenantConfigId);
            using (var sql = SQLContext.Sql())
            {
                sql.Query($"INSERT INTO {Settings.DatabaseContext}CustomerLogs (CustomerId, Message, WebhookId, CreatedDate) VALUES ({CustomerId}, @message, {WebhookId} getDate())", new { message = Message });
            }
        }
        public void DeleteByOrderId(int customerId)
        {
            using (var sql = SQLContext.Sql())
            {
                sql.Query($"DELETE {Settings.DatabaseContext}CustomerLogs Where CustomerId = {customerId}");
            }
        }
    }
}