using Dapper;
using ShopifyApp.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using ShopifyApp.Models;
using ShopifyApp.Api.ExigoWebservice;

namespace ShopifyApp.Services
{
    public static class InstallService
    {
        public static void CreateContext(string context)
        {
            context = context.Replace(".", "");
            var schema = new Schema
            {
                Name = context
            };
            var newSchema = new CreateExtendedDbSchemeRequest
            {
                Schema = schema,
            };
            try
            {
                var response = Exigo.WebServiceNoConfig().CreateExtendedDbSchema(newSchema);
            }
            catch (Exception e)
            {
                var message = e.Message;
            }
        }
        public static void CreateEntity(string entityName)
        {

            var entity = GetEntityByTableName(entityName);
            entity.SchemaName = entity.SchemaName.Replace(".", "");
            var newEntity = new CreateEntityRequest
            {
                Entity = entity
            };
            try
            {
                var response = Exigo.WebServiceNoConfig().CreateExtendedDbEntity(newEntity);
            }
            catch (Exception e)
            {
                var message = e.Message;
            }
        }
        public static bool CheckApiConnection()
        {
            try
            {
                var api = Exigo.WebServiceNoConfig();
                var realtimeresponse = api.GetCustomers(new GetCustomersRequest
                {
                    CustomerID = 1
                });
                if (realtimeresponse.Result.Status == ResultStatus.Success)
                    return true;
                else
                    return false;
            }
            catch (Exception e)
            {
                var s = e;
                return false;
            }
        }
        public static bool CheckSQLConnection()
        {
            try
            {
                using (var sql = SQLContext.Sql())
                {
                    var exists = sql.Query<object>($"SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND  TABLE_NAME = 'customers'").FirstOrDefault();
                    if (exists != null)
                        return true;
                    else
                        return false;
                }
            }
            catch
            {
                return false;
            }
        }
        public static bool CheckSQLSetup()
        {
            try
            {
                using (var sql = SQLContext.Sql())
                {
                    var context = Settings.DatabaseContext;
                    context = context.Replace(".", "");
                    var exists = sql.Query<object>($"SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = '{context}' AND  TABLE_NAME = 'Users'").FirstOrDefault();
                    if (exists != null)
                        return true;
                    else
                        return false;
                }
            }
            catch
            {
                return false;
            }
        }
        public static List<DatabaseTable> CheckTables(string context)
        {
            var _result = new List<DatabaseTable>();
            foreach (var table in Settings.Tables)
            {
                using (var sql = SQLContext.Sql())
                {
                    var _newTable = new DatabaseTable();
                    _newTable.Name = table.Name;
                    context = context.Replace(".", "");
                    var exists = sql.Query<object>($"SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = '{context}' AND  TABLE_NAME = '{table.Name}'").FirstOrDefault();
                    if (exists != null)
                        table.Exists = true;
                    _result.Add(table);
                }
            }
            return _result;
        }
        private static Entity GetEntityByTableName(string name)
        {
            return new EntityViewModel(name).Get();
        }
    }
    public class DatabaseTable
    {
        public DatabaseTable() { }
        public DatabaseTable(string name, bool exists, IEntity entity)
        {
            Name = name;
            Exists = exists;
            Entity = entity;
        }
        public string Name { get; set; }
        public bool Exists { get; set; }
        public IEntity Entity { get; set; }
    }
}