using Dapper;
using ShopifyApp.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace ShopifyApp.Models
{
    public class Webhook : IEntity
    {
        [Key]
        public int Id { get; set; }
        public int TenantConfigId { get; set; }
        public string WebhookId { get; set; }
        public string WebhookBody { get; set; }
        public string Type { get; set; }
        public string Method { get; set; }
        public int Status { get; set; }
        public int OrderId { get; set; }
        public int CustomerId { get; set; }
        public Nullable<DateTime> CreatedDate { get; set; }
        public bool IsSandbox { get; set; }

        public void Create()
        {
            using (var sql = SQLContext.Sql())
            {
                sql.Query($"INSERT INTO ShopifyContext.Webhooks (TenantConfigId, WebhookId, WebhookBody, Type, Method, CreatedDate, IsSandbox, Status) VALUES ({TenantConfigId}, '{WebhookId}', '{WebhookBody}', '{Type}', '{Method}', GetDate(), @isSandbox, 1)", new { isSandbox = IsSandbox });
            }
        }
        public void Delete()
        {
            using (var sql = SQLContext.Sql())
            {
                sql.Query($"Delete from {Settings.DatabaseContext}Webhooks where WebhookId = '{WebhookId}'");
            }
        }
        public List<Webhook> GetAll(WebhookStatus status = WebhookStatus.None)
        {
            using (var sql = SQLContext.Sql())
            {
                if(status == WebhookStatus.None)
                    return sql.Query<Webhook>($"Select * from {Settings.DatabaseContext}Webhooks").ToList();
                else
                    return sql.Query<Webhook>($"Select * from {Settings.DatabaseContext}Webhooks where Status = {(int)status}").ToList();

            }
        }
        public string GetWebhookMethod(string topic)
        {
            return new AvailableWebhook().GetAll().First(c => c.Topic == topic).MethodName;
        }
        public List<Webhook> GetWebhookByTypeAndConfig(string type, int configId)
        {
            using (var sql = SQLContext.Sql())
            {
                return sql.Query<Webhook>($"Select * from {Settings.DatabaseContext}Webhooks Where Type = '{type}' and TenantConfigId = {configId}").ToList();
            }
        }
        public Webhook GetWebhookById(string webhookId)
        {
            using (var sql = SQLContext.Sql())
            {
                return sql.Query<Webhook>($"Select * from {Settings.DatabaseContext}Webhooks Where WebhookId = '{webhookId}'").FirstOrDefault();
            }
        }
        public void UpdateWebhookStatus(string webhookId, int status)
        {
            using (var sql = SQLContext.Sql())
            {
                sql.Query($"Update {Settings.DatabaseContext}Webhooks SET Status = {status} WHERE WebhookId = '{webhookId}'");
            }
        }
        public void CreateTable(string context)
        {

            context = context.Replace(".", "");

            var sql1 = "SET ANSI_NULLS ON";

            var sql2 = "SET QUOTED_IDENTIFIER ON";

            var sql3 = $"CREATE TABLE[{context}].[Webhooks](" +
               "[Id][int] IDENTITY(1, 1) NOT NULL," +
               "[WebhookId] [nvarchar](4000) NULL," +
                "[WebhookBody] [nvarchar](max)NOT NULL," +
                "[Type] [nvarchar](4000) NULL," +
                "[TenantConfigId] [int] NOT NULL," +
                "[Method] [nvarchar](4000) NULL," +
                "[IsSandbox] [bit] NOT NULL," +
                "[CreatedDate] [datetime] NULL," +
                "[RowGuid] [uniqueidentifier] NOT NULL," +
                "[RowVersion] [bigint] NOT NULL," +
                "[Status] [int] NULL," +
             "CONSTRAINT[PK_Webhooks] PRIMARY KEY CLUSTERED" +
            "(" +
               "[Id] ASC" +
            ")WITH(PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON[PRIMARY]" +
            ") ON[PRIMARY] TEXTIMAGE_ON[PRIMARY]";

            var sql4 = $"ALTER TABLE[{context}].[Webhooks] ADD DEFAULT(newsequentialid()) FOR[RowGuid]";

            var sql5 = $"ALTER TABLE[{context}].[Webhooks] ADD DEFAULT((1)) FOR[RowVersion]";;

            using (var con = SQLContext.Sql())
            {
                var exists = con.Query<object>($"SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = '{context}' AND  TABLE_NAME = 'Webhooks'").FirstOrDefault();
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
        public void CleanUpWebhookLog()
        {
            using (var sql = SQLContext.Sql())
            {
                sql.Query($"Delete from {Settings.DatabaseContext}Webhooks WHERE CreatedDate <= CAST(DATEADD(day, -{Settings.WebhookLogTimeDays}, GETDATE()) as DATE)");
            }
        }
    }
}