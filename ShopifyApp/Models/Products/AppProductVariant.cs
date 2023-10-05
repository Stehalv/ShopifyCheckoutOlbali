using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Dapper;
using Newtonsoft.Json;
using ShopifyApp.Api.ExigoWebservice;
using ShopifyApp.Data;
using ShopifyApp.Services;
using ShopifyApp.Services.ShopService;
using ShopifySharp;

namespace ShopifyApp.Models
{
    public class AppProductVariant : IEntity
    {
        public AppProductVariant()
        {

        }
        public AppProductVariant(int tenantConfigId)
        {
            TenantConfigId = tenantConfigId;
        }
        public AppProductVariant(long shopifyId, int configId)
        {
            ShopifyVariantId = shopifyId.ToString();
            TenantConfigId = configId;
            TenantConfig = new TenantConfiguration().Get(configId);
            PopulateAppProductByVariantId();
        }
        public AppProductVariant (ShopifySharp.ProductVariant variant, int configId)
        {
            SKU = variant.SKU;
            ShopifyVariant = variant;
            ParentId = variant.ProductId.Value;
            Description = variant.Title;
            TenantConfigId = configId;
            TenantConfig = new TenantConfiguration().Get(configId);
            PopulateItemPrices();
            Task<bool> task = Task.Run<bool>(async () => await CheckIfInSync());
            InSync = task.Result;
        }
        public int Id { get; set; }
        public long ParentId { get; set; }
        public string ShopifyVariantId { get; set; }
        public int ExigoItemId { get; set; }
        public string SKU { get; set; }
        public string Description { get; set; }
        public int TenantConfigId { get; set; }
        public ProductVariant ShopifyVariant { get; set; }
        public Item ExigoItem { get; set; }
        public TenantConfiguration TenantConfig { get; set; }
        public List<Warehouse> Warehouses { get; set; }
        public bool InSync { get; set; }
        public string ExtendedPricesJson
        {
            get
            {
                return JsonConvert.SerializeObject(ItemPrices);
            }
        }
        public List<ItemPrice> ItemPrices { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }

        #region methods
        public void Get(int id)
        {
            using (var sql = SQLContext.Sql())
            {
                var product = sql.Query<AppProductVariant>($"Select * from {Settings.DatabaseContext}ProductVariants where Id = {id}").FirstOrDefault();
                Id = product.Id;
                ShopifyVariantId = product.ShopifyVariantId;
                SKU = product.SKU;
                Description = product.Description;
                TenantConfigId = product.TenantConfigId;
                CreatedDate = product.CreatedDate;
                ModifiedDate = product.ModifiedDate;
            }
            PopulateExigoItem();
            PopulateShopifyVariant();
        }
        
        public bool CheckIfExistsBySKU(string sku)
        {
            using (var sql = SQLContext.Sql())
            {
                return sql.Query<bool>($"Select case when count(*) > 0 then 1 else 0 end from {Settings.DatabaseContext}ProductVariants where SKU = '{sku}'").FirstOrDefault();
            }
        }
        public async Task<bool> CheckIfInSync()
        {
            var result = true;
            if (ShopifyVariant != null && ItemPrices.Any())
            {
                ShopifyVariant.Metafields = await new ShopifyDAL(TenantConfigId).GetResourceMetafields(ShopifyVariant.Id.Value, "variants");
                foreach (var price in ItemPrices)
                {
                    var itemPrice = price.Price * 100;
                    var metafieldString = price.PriceTypeId + "_" + price.CurrencyCode.ToLower();
                    var metafield = ShopifyVariant.Metafields.Where(c => c.Key == metafieldString).FirstOrDefault();
                    if (metafield != null)
                    {
                        if (Convert.ToDecimal(metafield.Value) != itemPrice)
                        {
                            result = false;
                        }
                    }
                    else
                    {
                        result = false;
                    }
                }
            }
            return result;
        }
        public void Create()
        {
            using (var sql = SQLContext.Sql())
            {
                sql.Query<AppProductVariant>($@"Insert Into {Settings.DatabaseContext}ProductVariants
                                        (ParentId, TenantConfigId, ShopifyVariantId, ExigoItemId, SKU, Description, CreatedDate, ModifiedDate) 
                                        Values ({ParentId}, {TenantConfigId}, {ShopifyVariantId}, {ExigoItemId}, '{SKU}', '{Description}', GetDate(), GetDate())").ToList();
            }
        }
        public List<AppProductVariant> GetVariantsByParent(int parentId, TenantConfiguration config)
        {
            var variants = new List<AppProductVariant>();
            using (var sql = SQLContext.Sql())
            {
                var variantIds = sql.Query<string>($"Select ParentId from ShopifyContext.ProductVariants where ParentId = {parentId}").ToList();
                foreach(var id in variantIds)
                {
                    variants.Add(new AppProductVariant((long)Convert.ToDecimal(id), config.Id));
                }
                return variants;
            }
        }
        public void CreateExigoItemFromShopify()
        {
            var exigorequest = new CreateItemRequest { 
                ItemCode = SKU,
                Description = Description
            };
            new ExigoApi().CreateItem(exigorequest);
        }
        public void UpdateExigoItem()
        {
            var exigorequest = new UpdateItemRequest
            {
                ItemCode = SKU,
                Description = ShopifyVariant.Title
            };
            new ExigoApi().UpdateItem(exigorequest);
        }
        #endregion

        #region PopulateModel
        public void PopulateAppProductBySKU()
        {
            using (var sql = SQLContext.Sql())
            {
                var product = sql.Query<AppProductVariant>($"Select * from {Settings.DatabaseContext}Products where SKU = '{SKU}'").FirstOrDefault();
                ShopifyVariantId = product.ShopifyVariantId;
                CreatedDate = product.CreatedDate;
                ModifiedDate = product.ModifiedDate;
            }
            PopulateExigoItem();
            PopulateShopifyVariant();
        }
        public void PopulateAppProductByVariantId()
        {
            using (var sql = SQLContext.Sql())
            {
                var product = sql.Query<AppProductVariant>($"Select * from {Settings.DatabaseContext}ProductVariants where ShopifyVariantId = '{ShopifyVariantId}'").FirstOrDefault();
                ShopifyVariantId = product.ShopifyVariantId;
                CreatedDate = product.CreatedDate;
                ModifiedDate = product.ModifiedDate;
            }
            PopulateExigoItem();
            PopulateShopifyVariant();
        }
        private void PopulateExigoItem()
        {
            //var item = Exigo.WebServiceNoConfig().GetItems(new GetItemsRequest
            //{
            //    ItemCodes = new string[] { SKU }
            //}).Items.FirstOrDefault();
            //if (item == null)
            //    item = new Item();
            //ExigoItem = item;
            //PopulateItemPrices();
            //PopulateWareHouses();
        }
        private void PopulateItemPrices()
        {
            using (var sql = SQLContext.Sql())
            {
                ExigoItemId = sql.Query<int>($"Select ItemID from items where ItemCode = '{SKU}'").FirstOrDefault();
                ItemPrices = sql.Query<ItemPrice>($"Select * from dbo.ItemPrices where ItemID = {ExigoItemId}").ToList();
            }
        }
        private void PopulateWareHouses()
        {
            using (var sql = SQLContext.Sql())
            {
                Warehouses = sql.Query<Warehouse>($"Select * from dbo.Warehouses where ItemID = {ExigoItem.ItemID}").ToList();
            }
        }
        private async void PopulateShopifyVariantById()
        {
            var context = new ShopifyDAL(TenantConfig);
            var variant = await context.GetProductVariant((long)Convert.ToDouble(ShopifyVariantId));
            if (variant == null)
                variant = new ProductVariant();
            ShopifyVariant = variant;
        }
        private async void PopulateShopifyVariant()
        {
            var context = new ShopifyDAL(TenantConfig);
            var variant = await context.GetProductVariant((long)Convert.ToDouble(ShopifyVariantId));
            if (variant == null)
                variant = new ProductVariant();
            ShopifyVariant = variant;
        }
        #endregion

        #region Lists
        public List<AppProductVariant> GetAll()
        {
            using (var sql = SQLContext.Sql())
            {
                return sql.Query<AppProductVariant>($@"Select * from {Settings.DatabaseContext}ProductVariants").ToList();
            }
        }
        public void SyncFromExigo()
        {
            using (var sql = SQLContext.Sql())
            {
                var items = Exigo.GetAllExigoItems();
                foreach (var item in items)
                { 
                    if(!CheckIfExistsBySKU(item.ItemCode))
                    {
                        var variant = new AppProductVariant(TenantConfigId);
                        variant.SKU = item.ItemCode;
                        variant.ExigoItemId = item.ItemID;
                        variant.Description = item.ItemDescription;
                        variant.Create();
                    }
                }
            }
        }
        #endregion
        public void CreateTable(string context)
        {

        }
    }
}