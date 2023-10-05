using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Dapper;
using ShopifyApp.Data;
using ShopifyApp.Services.ShopService;
using ShopifySharp;
using ShopifySharp.Lists;

namespace ShopifyApp.Models
{
    public class AppProduct : IEntity
    {
        public AppProduct()
        {

        }
        public AppProduct(long shopifyId, TenantConfiguration config)
        {
            ShopifyId = shopifyId;
            TenantConfigId = config.Id;
            TenantConfig = config;
            Task<bool> task = Task.Run<bool>(async () => await GetShopifyProduct());
            var finished = task.Result;
            GetVariants();
            CheckifIsInSync();
        }
        public AppProduct(Product product, TenantConfiguration config)
        {
            ShopifyId = product.Id.Value;
            ShopifyProduct = product;
            TenantConfigId = config.Id;
            TenantConfig = config;
            GetVariants();
            CheckifIsInSync();
        }
        public long ShopifyId { get; set; }
        public string Description { get; set; }
        public int TenantConfigId { get; set; }
        public ShopifySharp.Product ShopifyProduct { get; set; }
        public List<AppProductVariant> ProductVariants { get; set; }
        public TenantConfiguration TenantConfig { get; set; }
        public bool InSync { get; set; }

        #region methods
        public async Task<bool> GetByShopifyId()
        {
            var result = false;
            using (var sql = SQLContext.Sql())
            {
                TenantConfigId = TenantConfig.Id;
                result = await GetShopifyProduct();
                result = GetVariants();
            }
            return result;
        }
        public async Task<bool> GetShopifyProduct()
        {
            var context = new ShopifyDAL(TenantConfig);
            ShopifyProduct = await context.GetProduct(ShopifyId);
            return true;
        }
        public bool GetVariants()
        {
            var list = new List<AppProductVariant>();
            foreach(var variant in ShopifyProduct.Variants)
            {
                list.Add(new AppProductVariant(variant, TenantConfigId));
            }
            ProductVariants = list;
            return true;
        }
        public void CheckifIsInSync()
        {
            var result = true;
            foreach (var variant in ProductVariants)
            {
                if (!variant.InSync)
                    InSync = false;
            }
            InSync = result;
        }
        public List<AppProduct> ConvertList(ListResult<Product> products, TenantConfiguration config)
        {
            var list = new List<AppProduct>();
            foreach(var prod in products.Items)
            {
                list.Add(new AppProduct(prod, config));
            }
            return list;
        }
        #endregion

        public void CreateTable(string context)
        {

        }
    }
}