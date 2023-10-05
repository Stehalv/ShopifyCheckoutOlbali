using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ShopifySharp;
using ShopifySharp.Filters;
using Newtonsoft.Json;
using ShopifyApp.Services.ShopService;
using ShopifyApp.Models;
using Newtonsoft.Json.Linq;
using ShopifySharp.Lists;

namespace ShopifyApp.Services.ShopService
{
    public class ShopifyDAL
    {
        private string shopUrl;
        private string token;
        private TenantConfiguration _tenantConfig;
        public ShopifyDAL(Models.TenantConfiguration tenantConfig)
        {
            _tenantConfig = tenantConfig;
            shopUrl = tenantConfig.ShopUrl;
            token = tenantConfig.ShopSecret;
        }
        public ShopifyDAL(int tenantConfigId)
        {
            _tenantConfig = new TenantConfiguration().Get(tenantConfigId);
            shopUrl = _tenantConfig.ShopUrl;
            token = _tenantConfig.ShopSecret;
        }
        private ShopifySharp.ShopService _shop => new ShopifySharp.ShopService(shopUrl, token);
        private CustomerService _customer => new CustomerService(shopUrl, token);
        private GraphService _graph => new GraphService(shopUrl, token);
        private OrderService _order => new OrderService(shopUrl, token);
        private PriceRuleService _priceRule => new PriceRuleService(shopUrl, token);
        private FulfillmentService _fulfillment => new FulfillmentService(shopUrl, token);
        private ProductService _product => new ProductService(shopUrl, token);
        private ProductVariantService _productVariant => new ProductVariantService(shopUrl, token);
        private MetaFieldService _metaFields => new MetaFieldService(shopUrl, token);
        private WebhookService _webhooks => new WebhookService(shopUrl, token);
        private DiscountCodeService _discount => new DiscountCodeService(shopUrl, token);
        private LocationService _location => new LocationService(shopUrl, token);
        private DraftOrderService _draft => new DraftOrderService(shopUrl, token);

        #region Draft Orders

        public async Task<ShopifySharp.DraftOrder> CreateDraftOrder(DraftOrder draftOrder)
        {
            var result = await _draft.CreateAsync(draftOrder);
            return result;
        }
        #endregion
        #region shops
        public async Task<Shop> GetShop()
        {
            return await _shop.GetAsync();
        }
        #endregion

        #region Customers
        public async Task<ShopifySharp.Customer> GetCustomer(string customerId)
        {
            var customer = await _customer.GetAsync(long.Parse(customerId));
            customer.Metafields = await GetResourceMetafields(customer.Id.Value, "customers");
            return customer;
        }
        public async Task<ShopifySharp.Customer> GetCustomerByEmail(string email)
        {
            var filter = new CustomerSearchListFilter
            {
                Query = email
            };
            var customers = await _customer.SearchAsync(filter);
            if (customers.Items.Any())
                return customers.Items.First();
            return null;
        }
        public async Task<ShopifySharp.Customer> CreateCustomer(ShopifySharp.Customer customer)
        {
            var newCustomer = await _customer.CreateAsync(customer);
            return newCustomer;
        }
        public async Task<bool> UpdateCustomerTag(long customerId, string tag)
        {
            var newCustomer = await _customer.UpdateAsync(customerId, new ShopifySharp.Customer { 
                Tags = tag
            });
            return true;
        }
        public async Task<string> GetAccountActivationLink(long customerId)
        {
            var newCustomer = await _customer.GetAccountActivationUrl(customerId);
            return newCustomer;
        }
        public async Task<List<ShopifySharp.CustomerSavedSearch>> GetCustomerSavedSearches()
        {
            var result = new List<CustomerSavedSearch>();
            var search = @"
                        {
                          segments(first: 50) {
                            edges {
                              node {
                                id,
                                name
                              }
                            }
                          }
                        }
                        ";
            JToken list = await _graph.PostAsync(search);
            var gqresult = GraphQL.SegmentModel.Result.FromJson(list.Root.ToString());
            var lists = gqresult.Data.Segments.Edges;
            foreach(var l in lists)
            {
                var splitId = l.Node.Id.Split('/');
                var gqIdLength = splitId.Length - 1;

                result.Add(new CustomerSavedSearch
                {
                    Id = (long)Convert.ToDecimal(splitId[gqIdLength]),
                    Name = l.Node.Name
                });
            }
            return result;
        }
        #endregion

        #region Fulfillments
        public async Task<List<ShopifySharp.Location>> GetLocations()
        {
            var locations = await _location.ListAsync();
            return locations.ToList();
        }
        #endregion

        #region Orders

        public async Task<ShopifySharp.Order> GetOrder(string orderId)
        {
            var order = await _order.GetAsync(long.Parse(orderId));
            return order;
        }
        public async Task<ShopifySharp.Order> GetShopifyOrder(string orderId)
        {
            var order = await _order.GetAsync(long.Parse(orderId));
            return order;
        }
        public int GetOrderIdFromWebhook(Models.Webhook hook)
        {
            return JsonConvert.DeserializeObject<ShopifySharp.Order>(hook.WebhookBody).OrderNumber.Value;
        }
        public SyncOrderObject GetOrderFromWebhook(Models.Webhook hook)
        {
            var order = JsonConvert.DeserializeObject<ShopifySharp.Order>(hook.WebhookBody);
            return new SyncOrderObject(order, hook);

        }
        public ShopifySharp.Customer GetCustomerFromWebhook(Models.Webhook hook)
        {
            var customer = JsonConvert.DeserializeObject<ShopifySharp.Customer>(hook.WebhookBody);
            return customer;
        }
        public async Task<int> CountOrderFulfillments(long orderId)
        {
            var _task = await _fulfillment.CountAsync(orderId);
            return _task;
        }
        public async Task<Fulfillment> CreateOrderFulfillment(long orderId, Fulfillment fulfillment)
        {
            var _task = await _fulfillment.CreateAsync(orderId, fulfillment);
            return _task;
        }
        public async Task<Fulfillment> UpdateOrderFulfillment(long orderId, long fulfillmentId, Fulfillment fulfillment)
        {
            var _task = await _fulfillment.UpdateAsync(orderId, fulfillmentId, fulfillment);
            return _task;
        }
        #endregion

        #region PriceRules
        public async Task<List<PriceRule>> GetPricerules()
        {
            var items = await _priceRule.ListAsync();
            return items.Items.ToList();
        }
        public async Task<PriceRule> CreatePriceRule(PriceRule rule)
        {
            var item = await _priceRule.CreateAsync(rule);
            return item;
        }
        public async Task<PriceRule> UpdatePriceRule(PriceRule rule)
        {
            var item = await _priceRule.UpdateAsync(rule.Id.Value, rule);
            return item;
        }
        public async void DeletePriceRule(long id)
        {
            await _priceRule.DeleteAsync(id);
        }
        #endregion

        #region webhooks
        public async Task<ShopWebhook> GetWebhookAsync(long id)
        {
            return new ShopWebhook(await _webhooks.GetAsync(id));
        }
        public async Task<List<ShopWebhook>> ListWebhooksAsync()
        {
            var input = await _webhooks.ListAsync();
            var output = new List<ShopWebhook>();
            foreach (var hook in input.Items)
            {
                output.Add(new ShopWebhook(hook));
            }
            return output;
        }
        public async void CreateWebHook(string topic)
        {

            try
            {
                var service = _webhooks;
                ShopifySharp.Webhook hook = new ShopifySharp.Webhook()
                {
                    Address = Settings.AppUrl + "/webhook/registershopify",
                    CreatedAt = DateTime.Now,
                    Format = "json",
                    Topic = topic,
                };

                await service.CreateAsync(hook);
                new Log(LogType.Success, $"Webhook {topic} created successfully").Create();
            }
            catch (Exception ex)
            {
                new Log(LogType.Error, $"Webhook {topic} creation failed message: {ex.Message}").Create();
            }
        }
        public async Task<bool> DeleteWebhookAsync(long id)
        {
            await _webhooks.DeleteAsync(id);
            return true;
        }

        #endregion

        #region Metafields
        public async Task<List<MetaField>> GetResourceMetafields(long id, string type)
        {
            var result = await _metaFields.ListAsync(id, type);
            return result.Items.ToList();
        }
        public async Task<bool> CreateSilentLoginToken(Models.Customer customer)
        {
            var meta = await _metaFields.CreateAsync(new MetaField
            {
                Namespace = "SilentLogins",
                Key = "Exigo",
                Value = customer.SilentLoginToken,
                Type = "single_line_text_field",
                Description = "Token to log in to exigo customer/partner portal"
            }, (long)Convert.ToDecimal(customer.ShopCustomerId), "customers");
            if (meta.Id != null)
            {
                return true;
            }
            else
                return false;
        }
        public async Task<bool> AddPriceMetaField(long id, string pricetype, decimal value, string type = "variants")
        {
            await Task.Run(async () => //Task.Run automatically unwraps nested Task types!
            {
                await Task.Delay(1000);
            });
            var meta = await _metaFields.CreateAsync(new MetaField
            {
                Namespace = "pricing",
                Key = pricetype,
                Value = value * 100,
                Type = "number_decimal",
                Description = "Decimal with price for pricetype and currency"
            }, id, type);
            if (meta.Id != null)
                return true;
            else
                return false;
        }
        public async Task<bool> AddCVMetaField(long id, decimal value, string type = "variants")
        {
            await Task.Run(async () => //Task.Run automatically unwraps nested Task types!
            {
                await Task.Delay(1000);
            });
            var meta = await _metaFields.CreateAsync(new MetaField
            {
                Namespace = "exigo",
                Key = "cvpoints",
                Value = value,
                Type = "number_decimal",
                Description = "Decimal with CV"
            }, id, type);
            if (meta.Id != null)
                return true;
            else
                return false;
        }
        public async Task<bool> AddQVMetaField(long id, decimal value, string type = "variants")
        {
            await Task.Run(async () => //Task.Run automatically unwraps nested Task types!
            {
                await Task.Delay(1000);
            });
            var meta = await _metaFields.CreateAsync(new MetaField
            {
                Namespace = "exigo",
                Key = "qvpoints",
                Value = value,
                Type = "number_decimal",
                Description = "Decimal with QV"
            }, id, type);
            if (meta.Id != null)
                return true;
            else
                return false;
        }
        public async Task<bool> AddBVMetaField(long id, decimal value, string type = "variants")
        {
            await Task.Run(async () => //Task.Run automatically unwraps nested Task types!
            {
                await Task.Delay(1000);
            });
            var meta = await _metaFields.CreateAsync(new MetaField
            {
                Namespace = "exigo",
                Key = "bvpoints",
                Value = value,
                Type = "number_decimal",
                Description = "Decimal with BV"
            }, id, type);
            if (meta.Id != null)
                return true;
            else
                return false;
        }
        public async Task<bool> AddCustomerType(long id, int customertypeId)
        {
            try
            {
                //if(variant.Metafields[""])
                var meta = await _metaFields.CreateAsync(new MetaField
                {
                    Namespace = "Exigo",
                    Key = "CustomerType",
                    Value = customertypeId,
                    Type = "integer",
                    Description = "customertypeId"
                }, id, "customers");
                if (meta.Id != null)
                {
                    return await AddCustomePriceType(id, customertypeId);
                }
                else
                    return false;
            }
            catch(Exception e)
            {
                return false;
            }
        }
        public async Task<bool> AddCustomePriceType(long id, int customerTypeId)
        {
            var customerPriceTypeId = ShopifyApp.Settings.GetCustomerPriceType(customerTypeId);
            var meta = await _metaFields.CreateAsync(new MetaField
            {
                Namespace = "Exigo",
                Key = "CustomerPriceType",
                Value = customerPriceTypeId,
                Type = "integer",
                Description = "customertypeId"
            }, id, "customers");
            if (meta.Id != null)
                return true;
            else
                return false;
        }
        #endregion

        #region Products
        public async Task<ShopifySharp.ProductVariant> GetProductVariant(long id)
        {
            return await _productVariant.GetAsync(id);
        }

        public async Task<List<ShopifySharp.ProductVariant>> GetAllProductVariants(ListFilter<Product> filter = null)
        {
            var list = new List<ProductVariant>();
            var products = await GetProducts(filter, null, true);
            foreach(var product in products)
            {
                foreach(var variant in product.Variants)
                {
                    list.Add(variant);
                } 
            }
            return list;
        }
        public async Task<ListResult<Product>> GetProductListResult(ListFilter<Product> filter = null)
        {
            return await _product.ListAsync(filter);
        }
        public async Task<List<Product>> GetProducts(ListFilter<Product> filter = null, List<Product> list = null, bool getAll = false)
        {
            list = (list == null) ? new List<Product>() : list;
            var products = await _product.ListAsync(filter);
            foreach(var product in products.Items)
            {
                list.Add(product);
            }
            if(getAll && products.HasNextPage)
            {
                list = await GetProducts(products.GetNextPageFilter(), list, getAll);
            }
            return list;
        }
        public async Task<Product> GetProduct(long id)
        {
            return await _product.GetAsync(id);
        }
        public async Task<Product> CreateProduct(Product product)
        {
            return await _product.CreateAsync(product);
        }
        public async Task<ProductVariant> CreateVariant(ProductVariant variant)
        {
            return await _productVariant.CreateAsync(variant.ProductId.Value, variant);
        }
        public async Task<ProductVariant> UpdateVariantPrice(ProductVariant variant)
        {
            return await _productVariant.UpdateAsync(variant.Id.Value, new ProductVariant
            {
                Price = variant.Price
            });
        }
        #endregion

        #region Discount

        #endregion

        #region Assets
        #endregion
    }
}


