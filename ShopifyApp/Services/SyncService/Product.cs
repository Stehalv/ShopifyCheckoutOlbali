using Dapper;
using ShopifyApp.Data;
using ShopifyApp.Models;
using ShopifyApp.Services.ShopService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShopifyApp.Services
{
    public partial class SyncService
    {
        public async Task<int> SyncAllProductPrices(int tenantConfigId)
        {
            var result = 0;
            try
            {
                var products = await new ShopifyDAL(tenantConfigId).GetProducts(null, null, true);
                var newPrices = await UpdateAllExigoPrices(products, tenantConfigId);
                result = await SyncAllUnsyncedItems(tenantConfigId);
                new Log(ShopifyApp.LogType.Success, $"Pricesync completed. {result} items updated", ShopifyApp.LogSection.Global, "AppContext").Create();
                return result;
            }
            catch (Exception e)
            {
                new ShopifyApp.Models.Log(ShopifyApp.LogType.Error, $"Could not sync product prices, error: {e.Message}", ShopifyApp.LogSection.Global, "AppContext").Create();
                return result;
            }
        }
        public async Task<int> SyncAllUnsyncedItems(int tenantConfigId)
        {
            var count = 0;
            var parentProductList = new List<long>();
            var shopify = new ShopifyDAL(tenantConfigId);
            var unsyncedVariants = new VariantItemPrice().GetAllForTenantConfig(tenantConfigId).Where(c => c.ExigoPrice * 100 != c.ShopifyPrice ||(c.PriceType == "3_usd" &&  (c.BV != c.ShopifyBV || c.CV != c.ShopifyCV || c.QV != c.ShopifyQV)));
            var distinctVariantIds = unsyncedVariants.Select(c => c.ShopVariantId).Distinct();
            foreach(var variantId in distinctVariantIds)
            {
                var variant = await new ShopifyDAL(tenantConfigId).GetProductVariant(variantId);
                var metaFields = await new ShopifyDAL(tenantConfigId).GetResourceMetafields(variantId, "variants");
                foreach (var unsynced in unsyncedVariants.Where(c => c.ShopVariantId == variantId))
                {
                    var itemPrice = unsynced.ExigoPrice * 100;
                    var parentProductId = variant.ProductId.Value;
                    if(!parentProductList.Contains(parentProductId))
                    {
                        var productMetaFields = await new ShopifyDAL(tenantConfigId).GetResourceMetafields(parentProductId, "products");
                        var productPriceMetafield = productMetaFields.Where(c => c.Key == unsynced.PriceType).FirstOrDefault();
                        var productBVMetafield = productMetaFields.Where(c => c.Key == "bvpoints").FirstOrDefault();
                        var productCVMetafield = productMetaFields.Where(c => c.Key == "cvpoints").FirstOrDefault();
                        var productQVMetafield = productMetaFields.Where(c => c.Key == "qvpoints").FirstOrDefault();
                        if (productPriceMetafield != null)
                        {
                            if(Convert.ToDecimal(productPriceMetafield.Value) != itemPrice)
                                await shopify.AddPriceMetaField(parentProductId, unsynced.PriceType, unsynced.ExigoPrice, "products");
                        }
                        else
                        {
                            await shopify.AddPriceMetaField(parentProductId, unsynced.PriceType, unsynced.ExigoPrice, "products");
                        }
                        if(unsynced.PriceType.Contains("3"))
                        {
                            if (productBVMetafield != null)
                            {
                                if (Convert.ToDecimal(productBVMetafield.Value) != unsynced.BV)
                                    await shopify.AddBVMetaField(parentProductId, unsynced.BV, "products");
                            }
                            else
                            {
                                await shopify.AddBVMetaField(parentProductId, unsynced.BV, "products");
                            }
                            if (productCVMetafield != null)
                            {
                                if (Convert.ToDecimal(productCVMetafield.Value) != unsynced.CV)
                                    await shopify.AddCVMetaField(parentProductId, unsynced.CV, "products");
                            }
                            else
                            {
                                await shopify.AddCVMetaField(parentProductId, unsynced.CV, "products");
                            }
                            if (productQVMetafield != null)
                            {
                                if (Convert.ToDecimal(productQVMetafield.Value) != unsynced.QV)
                                    await shopify.AddQVMetaField(parentProductId, unsynced.QV, "products");
                            }
                            else
                            {
                                await shopify.AddQVMetaField(parentProductId, unsynced.QV, "products");
                            }
                            parentProductList.Add(parentProductId);
                        }
                    }
                    var metafieldString = unsynced.PriceType;
                    var update = false;
                    var pricemetafield = metaFields.Where(c => c.Key == metafieldString).FirstOrDefault();
                    var BVMetafield = metaFields.Where(c => c.Key == "bvpoints").FirstOrDefault();
                    var CVMetafield = metaFields.Where(c => c.Key == "cvpoints").FirstOrDefault();
                    var QVMetafield = metaFields.Where(c => c.Key == "qvpoints").FirstOrDefault();
                    if (pricemetafield != null)
                    {
                        if (Convert.ToDecimal(pricemetafield.Value) != itemPrice)
                        {
                            await shopify.AddPriceMetaField(unsynced.ShopVariantId, unsynced.PriceType, unsynced.ExigoPrice);
                            update = true;
                        }
                        else if(unsynced.ShopifyPrice != itemPrice)
                        {
                            update = true;
                        }
                    }
                    else
                    {
                        await shopify.AddPriceMetaField(unsynced.ShopVariantId, unsynced.PriceType, unsynced.ExigoPrice);
                        update = true;
                    }
                    if (unsynced.PriceType.Contains("3"))
                    {
                        if (BVMetafield != null)
                        {
                            if (Convert.ToDecimal(BVMetafield.Value) != unsynced.BV)
                            {
                                await shopify.AddBVMetaField(unsynced.ShopVariantId, unsynced.BV);
                                update = true;
                            }
                            else if (unsynced.BV != unsynced.ShopifyBV)
                                update = true;
                        }
                        else
                        {
                            await shopify.AddBVMetaField(unsynced.ShopVariantId, unsynced.BV);
                            update = true;
                        }
                        if (CVMetafield != null)
                        {
                            if (Convert.ToDecimal(CVMetafield.Value) != unsynced.CV)
                            {
                                await shopify.AddCVMetaField(unsynced.ShopVariantId, unsynced.CV);
                                update = true;
                            }
                            else if (unsynced.CV != unsynced.ShopifyCV)
                                update = true;
                        }
                        else
                        {
                            await shopify.AddCVMetaField(unsynced.ShopVariantId, unsynced.CV);
                            update = true;
                        }
                        if (QVMetafield != null)
                        {
                            if (Convert.ToDecimal(QVMetafield.Value) != unsynced.QV)
                            {
                                await shopify.AddQVMetaField(unsynced.ShopVariantId, unsynced.QV);
                                update = true;
                            }
                            else if (unsynced.QV != unsynced.ShopifyQV)
                                update = true;
                        }
                        else
                        {
                            await shopify.AddQVMetaField(unsynced.ShopVariantId, unsynced.QV);
                            update = true;
                        }
                    }
                    if(update)
                    {
                        if(unsynced.PriceType.Contains("3"))
                        {
                            unsynced.ShopifyBV = unsynced.BV;
                            unsynced.ShopifyCV = unsynced.CV;
                            unsynced.ShopifyQV = unsynced.QV;
                        }
                        unsynced.ShopifyPrice = itemPrice;
                        unsynced.UpdateShopifyPrice();
                        count++;
                    }
                }
            }
            return count;
        }
        public async Task<int> UpdateAllExigoPrices(List<ShopifySharp.Product> products, int tenantConfigId)
        {
            var count = 0;
            var variantItemPrices = new VariantItemPrice().GetAllForTenantConfig(tenantConfigId);
            foreach (var product in products)
            {
                foreach(var variant in product.Variants)
                {
                    using (var sql = SQLContext.Sql())
                    {
                        var ExigoItemId = sql.Query<int>($"Select ItemID from items where ItemCode = '{variant.SKU}'").FirstOrDefault();
                        var itemPrices = sql.Query<ItemPrice>($"Select * from dbo.ItemPrices where ItemID = {ExigoItemId}").ToList();
                        foreach(var price in itemPrices)
                        {
                            var pricetype = price.PriceTypeId + "_" + price.CurrencyCode;
                            var variantPrice = variantItemPrices.FirstOrDefault(c => c.SKU == variant.SKU && c.PriceType == pricetype);
                            if(variantPrice != null)
                            {
                                if(variantPrice.ExigoPrice != price.Price || variantPrice.BV != price.BV || variantPrice.CV != price.CV || variantPrice.QV != price.QV)
                                {
                                    variantPrice.ExigoPrice = price.Price;
                                    variantPrice.BV = price.BV;
                                    variantPrice.CV = price.CV;
                                    variantPrice.QV = price.QV;
                                    variantPrice.UpdateExigoPrice();
                                    count++;
                                }
                            }
                            else
                            {
                                var variantitemPrice = new VariantItemPrice
                                {
                                    SKU = variant.SKU,
                                    ShopVariantId = variant.Id.Value,
                                    ExigoPrice = price.Price,
                                    BV = price.BV,
                                    CV = price.CV,
                                    QV = price.QV,
                                    TenantConfigId = tenantConfigId,
                                    PriceType = pricetype
                                };
                                variantitemPrice.Create();
                                count++;
                            }
                        }
                    }
                }
            }
            return count;
        }
        public async Task<ShopifySharp.Product> SyncProductFromExigo(string itemCode, int tenantConfigId)
        {
            var item = Exigo.GetItem(itemCode);
            ShopifySharp.Product newProduct = new ShopifySharp.Product();
            newProduct.Title = item.ItemDescription;
            newProduct.BodyHtml = "<p>" + item.ShortDetail1 + "</p>";
            newProduct.Status = "draft";
            var variants = new List<ShopifySharp.ProductVariant>();
            if (item.IsGroupMaster)
            {
                foreach(var member in item.GroupMembers)
                {
                    var newVariant = new ShopifySharp.ProductVariant();
                    newVariant.SKU = member.ItemCode;
                    newVariant.Title = member.ItemDescription;
                    newVariant.Price = 0;
                    ShopifySharp.ProductVariant variant;
                    variants.Add(newVariant);
                }
            }
            else
            {
                var newVariant = new ShopifySharp.ProductVariant();
                newVariant.SKU = item.ItemCode;
                newVariant.Title = "Default Variant";
                newVariant.Price = 0;
                variants.Add(newVariant);
            }
            newProduct.Variants = variants.ToArray();
            return await new ShopifyDAL(tenantConfigId).CreateProduct(newProduct);
        }
    }
}
