using Dapper;
using ShopifyApp.Data;
using ShopifyApp.Services;
using ShopifyApp.Services.ShopService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace ShopifyApp.Models
{
    public class UnfulfilledOrder : IEntity
    {
        public UnfulfilledOrder()
        {

        }
        public UnfulfilledOrder(string shopifyId, int exigoId, int tenantConfigId, string webhookId = null)
        {
            ShopifyOrderId = shopifyId;
            ExigoOrderId = exigoId;
            TenantConfigId = tenantConfigId;
            WebhookId = webhookId;

        }
        public int Id { get; set; }
        public string ShopifyOrderId { get; set; }
        public int ExigoOrderId { get; set; }
        public int TenantConfigId { get; set; }
        public int Fulfillments { get; set; }
        public string WebhookId { get; set; }
        public DateTime CreatedDate { get; set; }
        public UnfulfilledOrder Get(int id)
        {
            using (var sql = SQLContext.Sql())
            {
                return sql.Query<UnfulfilledOrder>($"Select * from {Settings.DatabaseContext}UnfulfilledOrders where Id = {id}").FirstOrDefault();
            }
        }
        public UnfulfilledOrder GetByShopifyId(string shopifyId)
        {
            using (var sql = SQLContext.Sql())
            {
                return sql.Query<UnfulfilledOrder>($"Select * from {Settings.DatabaseContext}UnfulfilledOrders where ShopifyOrderId = '{shopifyId}'").FirstOrDefault();
            }
        }

        public void Create()
        {
            using (var sql = SQLContext.Sql())
            {
                sql.Query($"INSERT INTO {Settings.DatabaseContext}UnfulfilledOrders (ShopifyOrderId, ExigoOrderId, TenantConfigId, Fulfillments, CreatedDate) VALUES ('{ShopifyOrderId}', {ExigoOrderId}, {TenantConfigId}, 0, getDate())");
            }
        }

        public void UpdateFulfillments()
        {
            using (var sql = SQLContext.Sql())
            {
                sql.Query($"Update {Settings.DatabaseContext}UnfulfilledOrders SET Fulfillments = {Fulfillments} Where ShopifyOrderId = '{ShopifyOrderId}'");
            }
        }
        public void Delete()
        {
            using (var sql = SQLContext.Sql())
            {
                sql.Query($"Delete from {Settings.DatabaseContext}UnfulfilledOrders where ShopifyOrderId = '{ShopifyOrderId}'");
            }
        }
        public List<UnfulfilledOrder> GetAll()
        {
            using (var sql = SQLContext.Sql())
            {
                return sql.Query<UnfulfilledOrder>($"Select * from {Settings.DatabaseContext}UnfulfilledOrders").ToList();
            }
        }
        public void CreateTable(string context)
        {
        }
    }
}