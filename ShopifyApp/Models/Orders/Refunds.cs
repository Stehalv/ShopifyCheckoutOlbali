using Dapper;
using ShopifyApp.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ShopifyApp.Models
{
    public class Refunds : IEntity
    {
        public Refunds() { }
        public Refunds(TenantConfiguration tenantConfig, string newRefundId, string shopOrderId, int exigoOrderID, decimal total, string webhookId, decimal refundedTax = 0, decimal refundedShipping = 0)
        {
            ShopRefundId = newRefundId;
            TenantConfigId = tenantConfig.Id;
            ShopOrderId = shopOrderId;
            ExigoOrderId = exigoOrderID;
            Amount = total;
            RefundedShipping = refundedShipping;
            RefundedTax = refundedTax;
            WebhookId = webhookId;
        }
        public int Id { get; set; }
        public string ShopRefundId { get; set; }
        public string ShopOrderId { get; set; }
        public int ExigoOrderId { get; set; }
        public decimal Amount { get; set; }
        public int TenantConfigId { get; set; }
        public string WebhookId { get; set; }
        public decimal RefundedShipping { get; set; }
        public decimal RefundedTax { get; set; }
        public List<ShopifySharp.RefundLineItem> RefundLineItems { get; set; }
        public List<Refunds> GetAllByOrderId(string id)
        {
            using (var sql = SQLContext.Sql())
            {
                return sql.Query<Refunds>($"Select * from {Settings.DatabaseContext}Refunds where ShopOrderId = '{id}'").ToList();
            }
        }
        public List<Refunds> GetByShopRefundId(string id)
        {
            using (var sql = SQLContext.Sql())
            {
                return sql.Query<Refunds>($"Select * from {Settings.DatabaseContext}Refunds where ShopRefundId = '{id}'").ToList();
            }
        }
        public void Create(string webhookId = "")
        {
            using (var sql = SQLContext.Sql())
            {
                sql.Query($"INSERT INTO {Settings.DatabaseContext}Refunds (ShopRefundId, ShopOrderId, TenantConfigId, Amount, ExigoOrderId, RefundedShipping, RefundedTax) VALUES ({ShopRefundId}, '{ShopOrderId}', {TenantConfigId}, {Amount}, {ExigoOrderId}, {RefundedShipping}, {RefundedTax})");
            }
        }
        public void CreateTable(string context)
        {

            context = context.Replace(".", "");
            var sql1 = @"SET ANSI_NULLS ON";

            var sql2 = @"SET QUOTED_IDENTIFIER ON";

            var sql3 = @"CREATE TABLE [" + context + "].[Refunds](" +
                  "[Id][int] IDENTITY(1, 1) NOT NULL," +
                  "[ShopRefundId] [nvarchar](4000) NULL," +
                  "[TenantConfigId] [int] NOT NULL," +
                  "[ShopOrderId] [nvarchar](4000) NOT NULL," +
                  "[ExigoOrderId] [int] NOT NULL," +
                  "[Amount] [money] NOT NULL," +
                  "[RefundedShipping] [money] NULL," +
                  "[RowGuid] [uniqueidentifier] NOT NULL," +
                  "[RowVersion] [bigint] NOT NULL," +
                "CONSTRAINT [PK_Refunds] PRIMARY KEY CLUSTERED " +
                "([Id] ASC)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]) ON [PRIMARY]";

            var sql4 = $"ALTER TABLE [{context}].[Refunds] ADD  DEFAULT (newsequentialid()) FOR [RowGuid]";

            var sql5 = $"ALTER TABLE [{context}].[Refunds] ADD  DEFAULT ((1)) FOR [RowVersion]";

            using (var con = SQLContext.Sql())
            {
                var exists = con.Query<object>($"SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = '{context}' AND  TABLE_NAME = 'Refunds'").FirstOrDefault();
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