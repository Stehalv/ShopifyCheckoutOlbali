using ShopifyApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace ShopifyApp.Services
{
    public static class CheckoutService
    {
        public static async Task<bool> SyncCustomerFromExigo(int id, int customerTypeId, int tenantConfigId)
        {
            var shopCustomerId = new ShopifyApp.Models.Customer().GetByExigoId(id).ShopCustomerId;
            await new ShopifyApp.Services.ShopService.ShopifyDAL(tenantConfigId).AddCustomerType((long)Convert.ToDecimal(shopCustomerId), customerTypeId);
            return true;
        }
        public static async Task<bool> SyncCustomerFromShopify(long id, TenantConfiguration config = null)
        {
            if (config == null)
                config = new TenantConfiguration().GetAll().First();
            var exigoCustomerId = new ShopifyApp.Models.Customer().GetByShopId(id.ToString(), config.UseSandbox).ExigoCustomerId;
            var customerType = Exigo.GetCustomerType(config, exigoCustomerId);
            await new ShopService.ShopifyDAL(config.Id).AddCustomerType(id, customerType);
            return true;
        }
    }
}