using Dapper;
using ShopifyApp.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using ShopifyApp.Services;
using ShopifyApp.Services.ShopService;
using System.Threading.Tasks;

namespace ShopifyApp.Models
{
    public class Customer : IEntity
    {
        public Customer()
        {

        }
        public Customer(TenantConfiguration tenantConfig, string shopCustomerId, int exigoCustomerID, string fullName, string webhookId, int enrollerId, bool hasAddress, bool noOrder, string cartToken = "")
        {
            TenantConfigId = tenantConfig.Id;
            ShopCustomerId = shopCustomerId;
            ExigoCustomerId = exigoCustomerID;
            CustomerName = fullName;
            WebhookId = WebhookId;
            EnrollerId = enrollerId;
            HasAddress = hasAddress;
            NoOrder = noOrder;
            CartToken = cartToken;
        }
        public int Id { get; set; }
        public int TenantConfigId { get; set; }
        public string ShopCustomerId { get; set; }
        public int ExigoCustomerId { get; set; }
        public string CustomerName { get; set; }
        public string WebhookId { get; set; }
        public bool HasAddress { get; set; }
        public bool NoOrder { get; set; }
        public string CartToken { get; set; }
        public string SilentLoginToken
        {
            get
            {
                return Security.Encrypt(new { ShopCustomerId = ShopCustomerId, ExigoCustomerId = ExigoCustomerId });
            }
        }
        public int EnrollerId { get; set; }
        public List<CustomerLog> Log { get; set; }
        public DateTime CreatedDate { get; set; }
        public Customer GetByShopId(string shopId, bool isSandBox)
        {
            using (var sql = SQLContext.Sql(isSandBox))
            {
                return sql.Query<Customer>($"Select * from {Settings.DatabaseContext}Customers Where ShopCustomerId = '{shopId}'").FirstOrDefault();
            }
        }
        public Customer GetByExigoId(int exigoId, bool isSandBox = false)
        {
            using (var sql = SQLContext.Sql(isSandBox))
            {
                return sql.Query<Customer>($"Select * from {Settings.DatabaseContext}Customers Where ExigoCustomerId = '{exigoId}'").FirstOrDefault();
            }
        }
        public Customer Get(string shopCustomerId, int tenantConfigId)
        {
            using (var sql = SQLContext.Sql())
            {
                return sql.Query<Customer>($"Select * from {Settings.DatabaseContext}Customers where ShopCustomerId = '{shopCustomerId}' AND TenantConfigId = {tenantConfigId}").FirstOrDefault();
            }
        }
        public Customer GetById(int id)
        {
            using (var sql = SQLContext.Sql())
            {
                return sql.Query<Customer>($"Select * from {Settings.DatabaseContext}Customers where Id = {id}").FirstOrDefault();
            }
        }
        public int GetCustomerIDByExigoCustomerId(int exigoCustomerId, int tenantConfigId)
        {
            using (var sql = SQLContext.Sql())
            {
                return sql.Query<int>($"Select Id from {Settings.DatabaseContext}Customers where ExigoCustomerId = {exigoCustomerId} AND TenantConfigId = {tenantConfigId}").FirstOrDefault();
            }
        }
        public int GetCustomerIDByShopifyCustomerId(string shopifyCustomerId, int tenantConfigId)
        {
            using (var sql = SQLContext.Sql())
            {
                return sql.Query<int>($"Select Id from {Settings.DatabaseContext}Customers where ShopCustomerId = '{shopifyCustomerId}' AND TenantConfigId = {tenantConfigId}").FirstOrDefault();
            }
        }
        public List<Customer> GetAll()
        {
            using (var sql = SQLContext.Sql())
            {
                return sql.Query<Customer>($"Select * from {Settings.DatabaseContext}Customers").ToList();
            }
        }
        public int CustomerCount()
        {
            using (var sql = SQLContext.Sql())
            {
                return sql.Query<int>($"Select Count(*) from {Settings.DatabaseContext}Customers").FirstOrDefault();
            }
        }
        public List<Customer> LoadPaginatedCustomers(int currentPage = 1, int pageSize = 50, int startRowNumber = 0, int endRowNumber = 0)
        {
            using (var sql = SQLContext.Sql())
            {
                return sql.Query<Customer>(@"

                        SELECT 
                            tmp.Id, tmp.ExigoCustomerId, tmp.CustomerName, tmp.CreatedDate, tmp.TenantConfigId, tmp.ShopCustomerId, tmp.HasAddress, tmp.NoOrder
                            FROM
                            (
	                            SELECT
	                            -- Total records, a bit redundant but only need one time select
	                            COUNT(1) OVER() AS TotalRecords,
	                            --Row number
	                            ROW_NUMBER() OVER(ORDER BY CreatedDate DESC) AS RowNumber,
	                            --Other columns
	                            Id, ExigoCustomerId, CustomerName, CreatedDate, TenantConfigId, ShopCustomerId, HasAddress, NoOrder
	                            FROM " + Settings.DatabaseContext + @"Customers WITH(NOLOCK)
                            ) tmp
                            WHERE tmp.RowNumber BETWEEN @startRowNumber AND @endRowNumber", new { pageSize, currentPage, startRowNumber, endRowNumber }).ToList();
            }
        }
        public List<Customer> Search(string query)
        {
            using (var sql = SQLContext.Sql())
            {
                return sql.Query<Customer>($"SELECT  * from {Settings.DatabaseContext}Customers where CustomerName LIKE '{query}%'", new { query }).ToList();
            }
        }
        public List<Customer> GetAllByTenantConfigId(int id)
        {
            using (var sql = SQLContext.Sql())
            {
                return sql.Query<Customer>($"Select * from {Settings.DatabaseContext}Customers where TenantConfigId = {id}").ToList();
            }
        }
        public void Create()
        {
            using (var sql = SQLContext.Sql())
            {
                sql.Query($"INSERT INTO {Settings.DatabaseContext}Customers (ShopCustomerId, ExigoCustomerId, CustomerName, TenantConfigId, CreatedDate, HasAddress, NoOrder, CartToken, SilentLoginToken) VALUES ('{ShopCustomerId}', {ExigoCustomerId}, '{CustomerName}', {TenantConfigId}, GetDate(), @hasAddress, @noOrder, '{CartToken}', '{SilentLoginToken}')", new { hasAddress = HasAddress, noOrder = NoOrder });
            }
            Id = GetCustomerIDByExigoCustomerId(ExigoCustomerId, TenantConfigId);
        }
        public void CreateTable(string context)
        {
        }
        public void Update()
        {
            using (var sql = SQLContext.Sql())
            {
                sql.Query($"Update {Settings.DatabaseContext}Customers SET HasAddress = @hasAddress, NoOrder = @noOrder, SilentLoginToken = @token WHERE Id = '{Id}'", new { hasAddress = HasAddress, noOrder = NoOrder, token = SilentLoginToken });
            }
        }
        public async Task<bool> CreateSilentLoginToken()
        {
            var tenantConfig = new TenantConfiguration().Get(TenantConfigId);
            return await new ShopifyDAL(tenantConfig).CreateSilentLoginToken(this);
        }
        public async Task<bool> UpdateSHopify()
        {
            var tenantConfig = new TenantConfiguration().Get(TenantConfigId);
            return await ShopifyApp.Services.CheckoutService.SyncCustomerFromShopify((long)Convert.ToDecimal(ShopCustomerId), tenantConfig);
        }
    }
}