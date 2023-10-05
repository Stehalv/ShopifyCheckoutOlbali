using Newtonsoft.Json;
using ShopifyApp.Api.ExigoWebservice;
using ShopifyApp.Models;
using ShopifyApp.Services;
using ShopifyApp.Services.ShopService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace ReplicatedSite.Areas.Shopify.ViewModels
{
    public class CustomerDetailViewModel
    {
        public CustomerDetailViewModel()
        {
        }
        public CustomerDetailViewModel(int id)
        {
            Id = id;
        }
        public int Id {get; set;}
        public Customer Customer { get; set; }
        public ShopifySharp.Customer ShopifyCustomer { get; set; }
        public bool ExigoSilentLoginExsists { get; set; }
        public string ShopifyCustomerJson
        {
            get
            {
                return JsonConvert.SerializeObject(ShopifyCustomer);
            }
        }
        public ExigoService.Customer ExigoCustomer { get; set; }
        public string ExigoCustomerJson
        {
            get
            {
                return JsonConvert.SerializeObject(ExigoCustomer);
            }
        }
        public List<Order> Orders { get; set; }
        public List<CustomerLog> Log { get; set; }
        public async Task<bool> Populate()
        {
            Customer = new Customer().GetById(Id);
            var tenantConfig = new TenantConfiguration().Get(Customer.TenantConfigId);
            ShopifyCustomer = await new ShopifyDAL(tenantConfig).GetCustomer(Customer.ShopCustomerId);
            ExigoCustomer = ExigoService.ExigoDAL.GetCustomer(Customer.ExigoCustomerId);
            Log = new CustomerLog().GetAll(Customer.Id);
            Orders = new Order().GetByExigoCustomerId(Customer.ExigoCustomerId, Customer.TenantConfigId);
            return true;
        }
        public async Task<bool> PopulateExigoId(int ExigoId, int tenantConfigId)
        {
            var order = new Order().GetByExigoCustomerId(ExigoId, tenantConfigId).FirstOrDefault();
            var tenantConfig = new TenantConfiguration().Get(tenantConfigId);
            var shOrder = await new ShopifyDAL(tenantConfig).GetOrder(order.ShopOrderReference);
            ShopifyCustomer = shOrder.Customer;
            ExigoCustomer = ExigoService.ExigoDAL.GetCustomer(ExigoId);
            Customer = new Customer
            {
                ExigoCustomerId = ExigoId,
                ShopCustomerId = ShopifyCustomer.Id.ToString(),
                CreatedDate = DateTime.Now,
                TenantConfigId = tenantConfigId,
                CustomerName = ShopifyCustomer.FirstName + " " + ShopifyCustomer.LastName
            };
            Orders = new Order().GetByExigoCustomerId(Customer.ExigoCustomerId, Customer.TenantConfigId);
            return true;
        }
    }
}