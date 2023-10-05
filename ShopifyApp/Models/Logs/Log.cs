using Dapper;
using ShopifyApp.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ShopifyApp.Models
{
    public class Log : IEntity
    {
        public Log()
        {

        }
        public Log(LogType type, string message, LogSection section = ShopifyApp.LogSection.Global, string webhookId = null)
        {
            LogType = type.ToString();
            Message = message;
            LogSection = (int)section;
            WebhookId = webhookId;
        }
        public int Id { get; set; }
        public string LogType { get; set; }
        public int LogSection { get; set; }
        public string Message { get; set; }
        public string WebhookId { get; set; }
        public DateTime CreatedDate { get; set; }
        public bool IsSandbox { get; set; }
        public List<Log> GetAll()
        {
            using (var sql = SQLContext.Sql())
            {
                return sql.Query<Log>($"Select * from {Settings.DatabaseContext}Logs").ToList();
            }
        }
        public List<Log> GetAllByWebhookId(string webhookId)
        {
            using (var sql = SQLContext.Sql())
            {
                return sql.Query<Log>($"Select * from {Settings.DatabaseContext}Logs where WebhookId = '{webhookId}'").ToList();
            }
        }
        public void DeleteAll()
        {
            using (var sql = SQLContext.Sql())
            {
               sql.Query($"Delete from {Settings.DatabaseContext}Logs").ToList();
            }
        }
        public void Create()
        {
            using (var sql = SQLContext.Sql())
            {
                try
                {
                    sql.Query($"INSERT INTO {Settings.DatabaseContext}Logs (LogType, Message, LogSection, CreatedDate, WebhookId) VALUES (@Type, @message, @section, getDate(), @webhookId)", new { Type = LogType, section = LogSection, message = Message, webhookId = WebhookId });
                }
                catch (Exception e) 
                {
                    var p = e; 
                }
            }
        }
        public void CreateTable(string table)
        {

        }
    }
}