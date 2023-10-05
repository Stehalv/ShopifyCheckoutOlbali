using Dapper;
using ShopifyApp.Data;
using ShopifyApp.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ShopifyApp.Models
{
    public class TenantConfiguration : IEntity
    {
        public TenantConfiguration()
        { }
        public int Id { get; set; }
        public bool UseSandbox { get; set; }
        public int SandBoxId { get; set; }
        public string ShopApiKey { get; set; }
        public string ShopSecret { get; set; }
        public string ShopUrl { get; set; }
        public int DefaultEnrollerID { get; set; }
        public string DefaultEnrollerWebAlias { get; set; }
        public string AuthenticationToken
        {
            get
            {
                return Security.Encrypt(new { shopUrl = ShopUrl });
            }
        }
        public long LocationId { get; set; }
        public int CreatedBy { get; set; }
        public int ModifiedBy { get; set; }
        public Nullable<DateTime> Created { get; set; }
        public Nullable<DateTime> Modified { get; set; }

        public TenantConfiguration Get(int id)
        {
            using (var sql = SQLContext.Sql())
            {
                var config = sql.Query<TenantConfiguration>($"Select * from {Settings.DatabaseContext}TenantConfigurations Where Id = {id}").FirstOrDefault();
                return config;
            }
        }
        public List<TenantConfiguration> GetAll()
        {
            using (var sql = SQLContext.Sql())
            {
                return sql.Query<TenantConfiguration>($"Select * from {Settings.DatabaseContext}TenantConfigurations").ToList();
            }
        }
        public TenantConfiguration GetByDomain(string domain)
        {
            using (var sql = SQLContext.Sql())
            {
                var config = sql.Query<TenantConfiguration>($"Select * from {Settings.DatabaseContext}TenantConfigurations Where ShopUrl = '{domain}'").FirstOrDefault();
                return config;
            }
        }
        public void Create()
        {
            using (var sql = SQLContext.Sql())
            {
                sql.Query($"INSERT INTO {Settings.DatabaseContext}TenantConfigurations (UseSandbox, SandBoxId, ShopApiKey, ShopSecret, ShopUrl, LocationId, DefaultEnrollerID, DefaultEnrollerWebAlias, CreatedBy, ModifiedBy, Created, IntegrationType) VALUES (@useSandbox, {SandBoxId}, '{ShopApiKey}', '{ShopSecret}', '{ShopUrl}', {LocationId}, {DefaultEnrollerID}, '{DefaultEnrollerWebAlias}', {CreatedBy}, {CreatedBy}, GetDate(), 0)", new { useSandbox = UseSandbox });

            }
        }
        public void Update()
        {
            using (var sql = SQLContext.Sql())
            {
                sql.Query($"Update {Settings.DatabaseContext}TenantConfigurations SET UseSandbox = @useSandbox, SandBoxId = {SandBoxId}, ShopApiKey = '{ShopApiKey}', ShopSecret = '{ShopSecret}', ShopUrl = '{ShopUrl}', LocationId = {LocationId}, DefaultEnrollerId = {DefaultEnrollerID},  DefaultEnrollerWebAlias = '{DefaultEnrollerWebAlias}', ModifiedBy = {ModifiedBy}, Modified = GetDate()  WHERE Id = '{Id}'", new { useSandbox = UseSandbox });
            }
        }
        public void Delete()
        {
            using (var sql = SQLContext.Sql())
            {
                sql.Query<User>($"Delete from {Settings.DatabaseContext}TenantConfigurations Where Id = {Id}");
            }
        }
        public void CleanInstall()
        {
            using (var sql = SQLContext.Sql())
            {
                var tenantOrders = new Order().GetAllByTenantConfigId(Id);
                foreach(var order in tenantOrders)
                {
                    new OrderLog().DeleteByOrderId(order.Id);
                }
                var tenantCustomers = new Customer().GetAllByTenantConfigId(Id);
                foreach (var customer in tenantCustomers)
                {
                    new CustomerLog().DeleteByOrderId(customer.Id);
                }
                sql.Query($"Delete from {Settings.DatabaseContext}Orders where TenantConfigId = {Id}");
                sql.Query($"Delete from {Settings.DatabaseContext}Customers where TenantConfigId = {Id}");
                sql.Query($"Delete from {Settings.DatabaseContext}Webhooks where TenantConfigId = {Id}");
                sql.Query($"Delete from {Settings.DatabaseContext}UnfulfilledOrders where TenantConfigId = {Id}");
            }
        }
        public void CreateTable(string context)
        {
        }
    }
}