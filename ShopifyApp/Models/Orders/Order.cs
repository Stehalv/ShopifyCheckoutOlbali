using Dapper;
using ShopifyApp.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ShopifyApp.Models
{
    public class Order : IEntity
    {
        public Order() { }
        public Order(TenantConfiguration tenantConfig, int shopOrderId, int exigoOrderID, string shopOrderReference, int exigoCustomerId, decimal orderTotal, string webhookId)
        {
            TenantConfigId = tenantConfig.Id;
            ShopOrderId = shopOrderId;
            ExigoOrderId = exigoOrderID;
            ShopOrderReference = shopOrderReference;
            ExigoCustomerId = exigoCustomerId;
            OrderTotal = orderTotal;
            WebhookId = webhookId;
        }
        public int Id { get; set; }
        public int TenantConfigId { get; set; }
        public int ExigoCustomerId { get; set; }
        public string ShopOrderReference { get; set; }
        public int ShopOrderId { get; set; }
        public int ExigoOrderId { get; set; }
        public decimal OrderTotal { get; set; }
        public string WebhookId { get; set; }
        public DateTime CreatedDate { get; set; }
        public Order Get(int id)
        {
            using (var sql = SQLContext.Sql())
            {
                return sql.Query<Order>($"Select * from {Settings.DatabaseContext}Orders where Id = {id}").FirstOrDefault();
            }
        }
        public List<Order> GetByExigoCustomerId(int id, int tenantConfigId)
        {
            using (var sql = SQLContext.Sql())
            {
                return sql.Query<Order>($"Select * from {Settings.DatabaseContext}Orders where ExigoCustomerId = {id} AND TenantConfigId = {tenantConfigId}").ToList();
            }
        }
        public Order GetByOrderIdAndConfig(int id, int configId)
        {
            using (var sql = SQLContext.Sql())
            {
                return sql.Query<Order>($"Select * from {Settings.DatabaseContext}Orders where ShopOrderId = {id} and TenantConfigId = {configId}").FirstOrDefault();
            }
        }
        public Order GetByShopifyOrderReference(string reference, int tenantConfigId)
        {
            using (var sql = SQLContext.Sql())
            {
                return sql.Query<Order>($"Select * from {Settings.DatabaseContext}Orders where ShopOrderReference = '{reference}' AND TenantConfigId = {tenantConfigId}").FirstOrDefault();
            }
        }
        public int GetOrderIDByShopifyOrderReference(string reference, int tenantConfigId)
        {
            using (var sql = SQLContext.Sql())
            {
                return sql.Query<int>($"Select Id from {Settings.DatabaseContext}Orders where ShopOrderReference = '{reference}' AND TenantConfigId = {tenantConfigId}").FirstOrDefault();
            }
        }
        public int GetOrderIDByShopifyOrderId(int orderId, int tenantConfigId)
        {
            using (var sql = SQLContext.Sql())
            {
                return sql.Query<int>($"Select Id from {Settings.DatabaseContext}Orders where ShopOrderId = {orderId} AND TenantConfigId = {tenantConfigId}").FirstOrDefault();
            }
        }
        public int GetOrderIDByExigoOrderId(int orderId, int tenantConfigId)
        {
            using (var sql = SQLContext.Sql())
            {
                return sql.Query<int>($"Select Id from {Settings.DatabaseContext}Orders where ExigoOrderId = {orderId} AND TenantConfigId = {tenantConfigId}").FirstOrDefault();
            }
        }
        public Order GetByShopifyId(int id, int tenantConfigId)
        {
            using (var sql = SQLContext.Sql())
            {
                return sql.Query<Order>($"Select * from {Settings.DatabaseContext}Orders where ShopOrderId = {id} AND TenantConfigId = {tenantConfigId}").FirstOrDefault();
            }
        }
        public List<Order> GetAll()
        {
            using (var sql = SQLContext.Sql())
            {
                return sql.Query<Order>($"Select * from {Settings.DatabaseContext}Orders order by CreatedDate desc").ToList();
            }
        }
        public List<Order> GetAllByTenantConfigId(int id)
        {
            using (var sql = SQLContext.Sql())
            {
                return sql.Query<Order>($"Select * from {Settings.DatabaseContext}Orders where TenantConfigId = {id}").ToList();
            }
        }
        public int OrderCount()
        {
            using (var sql = SQLContext.Sql())
            {
                return sql.Query<int>($"Select Count(*) from {Settings.DatabaseContext}Orders").FirstOrDefault();
            }
        }
        public List<Order> LoadPaginatedOrders(int currentPage = 1, int pageSize = 50, int startRowNumber = 0, int endRowNumber = 0)
        {
            using (var sql = SQLContext.Sql())
            {
                return sql.Query<Order>(@"

                        SELECT 
                            tmp.Id, tmp.ExigoOrderId, tmp.OrderTotal, tmp.CreatedDate, tmp.TenantConfigId, tmp.ShopOrderId, tmp.ShopOrderReference, tmp.ExigoCustomerId
                            FROM
                            (
	                            SELECT
	                            -- Total records, a bit redundant but only need one time select
	                            COUNT(1) OVER() AS TotalRecords,
	                            --Row number
	                            ROW_NUMBER() OVER(ORDER BY CreatedDate DESC) AS RowNumber,
	                            --Other columns
	                            Id, ExigoOrderId, OrderTotal, CreatedDate, TenantConfigId, ShopOrderId, ShopOrderReference, ExigoCustomerId
	                            FROM " + Settings.DatabaseContext + @"Orders WITH(NOLOCK)
                            ) tmp
                            WHERE tmp.RowNumber BETWEEN @startRowNumber AND @endRowNumber", new { pageSize, currentPage, startRowNumber, endRowNumber }).ToList();
            }
        }
        public void Create()
        {
            using (var sql = SQLContext.Sql())
            {
                sql.Query($"INSERT INTO {Settings.DatabaseContext}Orders (ShopOrderId, ShopOrderReference, ExigoOrderId, ExigoCustomerId, OrderTotal, TenantConfigId, CreatedDate) VALUES ({ShopOrderId}, '{ShopOrderReference}', {ExigoOrderId}, {ExigoCustomerId}, {OrderTotal}, {TenantConfigId}, GetDate())");
            }
            Id = GetOrderIDByExigoOrderId(ExigoOrderId, TenantConfigId);
        }
        public void CreateTable(string context)
        {
            context = context.Replace(".", "");
            var sql1 = "SET ANSI_NULLS ON";

            var sql2 = "SET QUOTED_IDENTIFIER ON";

            var sql3 = $"CREATE TABLE [{context}].[Orders](" +
                            "[Id] [int] IDENTITY(1,1) NOT NULL," +
                            "[ExigoOrderId] [int] NOT NULL," +
                            "[OrderTotal] [money] NOT NULL," +
                            "[CreatedDate] [datetime] NOT NULL," +
                            "[TenantConfigId] [int] NOT NULL," +
                            "[ShopOrderId] [int] NOT NULL," +
                            "[ShopOrderReference] [nvarchar](4000) NULL," +
                            "[ExigoCustomerId] [int] NOT NULL," +
                            "[RowGuid] [uniqueidentifier] NOT NULL," +
                            "[RowVersion] [bigint] NOT NULL," +
                         "CONSTRAINT [PK_Orders] PRIMARY KEY CLUSTERED " +
                        "(" +
                        "    [Id] ASC" +
                        ")WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]" +
                        ") ON [PRIMARY]";



             var sql4 = $"ALTER TABLE [{context}].[Orders] ADD  DEFAULT (newsequentialid()) FOR [RowGuid]";

             var sql5 = $"ALTER TABLE [{context}].[Orders] ADD  DEFAULT ((1)) FOR [RowVersion]";

             using (var con = SQLContext.Sql())
            {
                var exists = con.Query<object>($"SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = '{context}' AND  TABLE_NAME = 'Orders'").FirstOrDefault();
                if (exists == null)
                {
                    con.Query(sql1);
                    con.Query(sql2);
                    con.Query(sql3);
                    con.Query(sql4);
                    con.Query(sql5);
                }
            }
        }

    }
}