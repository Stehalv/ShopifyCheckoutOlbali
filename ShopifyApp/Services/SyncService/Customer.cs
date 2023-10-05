using System;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using ShopifyApp.Api.ExigoWebservice;
using ShopifyApp.Data;
using ShopifyApp.Models;
using ShopifyApp.Services.ShopService;

namespace ShopifyApp.Services
{
    public partial class SyncService
    {
        public bool CreateCustomer(Webhook hook)
        {
            var tenantConfig = new TenantConfiguration().Get(hook.TenantConfigId);
            _shopDAL = new ShopifyDAL(tenantConfig);
            try
            {
                if (hook != null)
                {
                    var customer = _shopDAL.GetCustomerFromWebhook(hook);
                    var success = SyncNewCustomerNoOrder(tenantConfig, customer, hook.WebhookId);
                    if (success)
                    {
                        new Webhook().UpdateWebhookStatus(hook.WebhookId, (int)WebhookStatus.Processed);
                        new Log(LogType.Success, $"Webhook Processed {hook.WebhookId} Topic {hook.Type} ", LogSection.Webhooks, hook.WebhookId).Create();
                    }
                    return true;
                }
                else
                {
                    new Webhook().UpdateWebhookStatus(hook.WebhookId, (int)WebhookStatus.Error);
                    new Log(LogType.Error, $"Attempt to process Order webhookId {hook.WebhookId} failed, record not found", LogSection.Orders).Create();
                    return false;
                }

            }
            catch (Exception exception)
            {
                new Webhook().UpdateWebhookStatus(hook.WebhookId, (int)WebhookStatus.Error);
                new Log(LogType.Error, $"Order creation failed webhook: {hook.WebhookId} message: {exception.Message}", LogSection.Orders).Create();
                return false;
            }
        }
        public bool UpdateCustomer(Webhook hook)
        {
            var tenantConfig = new TenantConfiguration().Get(hook.TenantConfigId);
            _shopDAL = new ShopifyDAL(tenantConfig);
            try
            {
                if (hook != null)
                {
                    var customer = _shopDAL.GetCustomerFromWebhook(hook);
                    var success = SyncCustomer(customer, tenantConfig, hook.WebhookId);
                    if (success)
                    {
                        new Webhook().UpdateWebhookStatus(hook.WebhookId, (int)WebhookStatus.Processed);
                        new Log(LogType.Success, $"Webhook Processed {hook.WebhookId} Topic {hook.Type} ", LogSection.Webhooks, hook.WebhookId).Create();
                    }
                    return true;
                }
                else
                {
                    new Webhook().UpdateWebhookStatus(hook.WebhookId, (int)WebhookStatus.Error);
                    new Log(LogType.Error, $"Attempt to process Order webhookId {hook.WebhookId} failed, record not found", LogSection.Orders).Create();
                    return false;
                }

            }
            catch (Exception exception)
            {
                new Webhook().UpdateWebhookStatus(hook.WebhookId, (int)WebhookStatus.Error);
                new Log(LogType.Error, $"Order creation failed webhook: {hook.WebhookId} message: {exception.Message}", LogSection.Orders).Create();
                return false;
            }
        }
        public static bool SyncCustomer(ShopifySharp.Customer customer, TenantConfiguration tenantConfig, string webhookId)
        {
            try
            {
                var appCustomer = new Customer().Get(customer.Id.ToString(), tenantConfig.Id);
                var request = new UpdateCustomerRequest
                {
                    CustomerID = appCustomer.ExigoCustomerId,
                    CustomerStatus = CustomerStatuses.Active,
                    CustomerType = Settings.DefaultCustomerTypeId,
                    FirstName = customer.FirstName,
                    LastName = customer.LastName,
                    MainCountry = GetCustomerCountry(tenantConfig, customer.Note),
                    Phone = customer.Phone

                };
                Exigo.WebService(tenantConfig).UpdateCustomer(request);
                appCustomer.CartToken = GetCartToken(customer.Note);
                appCustomer.Update();

                new CustomerLog(appCustomer.Id, $"Customer updated with name and carttoken.", webhookId).Create();
                return true;
            }
            catch (Exception e)
            {
                new Log(LogType.Error, $"Customer update failed ShopId: {customer.Id} message: {e.Message}", LogSection.Customers).Create();
                return false;
            }
        }
        public static bool SyncNewCustomerNoOrder(TenantConfiguration tenantConfig, ShopifySharp.Customer customer, string webhookId)
        {
            if(customer.Note == null)
            {
                return true;
            }
            try
            {
                var newsletterSignup = false;
                var exigoCustomerId = CheckIfCustomerExist(customer.Id.Value, tenantConfig.UseSandbox);
                exigoCustomerId = (exigoCustomerId == 0) ?  CheckIfCustomerExistByEmail(customer.Email, tenantConfig, true) : exigoCustomerId;
                if (exigoCustomerId == 0)
                {
                    if (customer.FirstName.IsNullOrEmpty())
                    {
                        customer.FirstName = "NS:";
                        newsletterSignup = true;
                    }
                    if (customer.LastName.IsNullOrEmpty())
                        customer.LastName = customer.Email;
                    var enrollerId = GetCustomerReferral(tenantConfig, customer.Note);
                    var createCustomerRequest = new CreateCustomerRequest
                    {
                        FirstName = customer.FirstName,
                        LastName = customer.LastName,
                        Email = customer.Email,
                        Phone = customer.Phone,
                        EntryDate = DateTime.Now,
                        MainCountry = GetCustomerCountry(tenantConfig, customer.Note),
                        InsertEnrollerTree = true,
                        EnrollerID = enrollerId,
                        CanLogin = true,
                        CustomerStatus = CustomerStatuses.Pending,
                        CustomerType = CustomService.NewCustomerWithoutOrderTypeID(customer),
                        Notes = $"Customer imported with App Shopify from site: {tenantConfig.ShopUrl} CustomerId: {customer.Id}"
                    };
                    createCustomerRequest.LoginPassword = System.Web.Security.Membership.GeneratePassword(8, 1);
                    createCustomerRequest.LoginName = customer.Email;
                    var response = Exigo.WebService(tenantConfig).CreateCustomer(createCustomerRequest);
                    exigoCustomerId = response.CustomerID;
                    PlaceBinaryNodeRequest treeInsertRequest = null;
                    //Log result
                    var fullName = customer.FirstName + " " + customer.LastName;
                    var newCustomer = new Customer(tenantConfig, customer.Id.ToString(), response.CustomerID, fullName, webhookId, enrollerId, false, true, "");
                    newCustomer.Create();
                    var shopify = new ShopifyDAL(tenantConfig);
                    var result = Task.Run(async () => await shopify.CreateSilentLoginToken(newCustomer));
                    new CustomerLog(newCustomer.Id, $"Silent login added to Shopify", webhookId).Create();
                    if (newsletterSignup)
                        new CustomerLog(newCustomer.Id, $"Exigo Customer: {newCustomer.ExigoCustomerId} created from newsletter signup customerid: {newCustomer.ShopCustomerId}", webhookId).Create();
                    else
                        new CustomerLog(newCustomer.Id, $"Exigo Customer: {newCustomer.ExigoCustomerId} created from shopify customerid: {newCustomer.ShopCustomerId}", webhookId).Create();

                    if (response.Result.Status == ResultStatus.Success)
                    {
                        if (Settings.PlaceBinaryTree)
                        {
                            treeInsertRequest = new PlaceBinaryNodeRequest()
                            {
                                CustomerID = response.CustomerID,
                                PlacementType = BinaryPlacementType.EnrollerPreference,
                                ToParentID = enrollerId,
                                Reason = "Initial placement from shopping cart"
                            };
                            Exigo.WebService(tenantConfig).PlaceBinaryNode(treeInsertRequest);
                            new CustomerLog(newCustomer.Id, $"Customer placed in binary tree of {newCustomer.EnrollerId} ", webhookId).Create();
                        }
                    }

                }
                return true;

            }
            catch (Exception exception)
            {
                //Log result
                new Log(LogType.Error, $"Customer creation failed ShopId: {customer.Id} message: {exception.Message}", LogSection.Customers).Create();
                return false;
                //Log Exception
            }
        }
        public static int SyncCustomerWithOrder(TenantConfiguration tenantConfig, SyncOrderObject order, int warehouseId)
        {
            try
            {  
                var exigoCustomerId = CheckIfCustomerExist(order.ShopCustomerId, tenantConfig.UseSandbox);
                if (exigoCustomerId == 0)
                {
                    exigoCustomerId = CheckIfCustomerExistByEmail(order.Customer.Email, tenantConfig);
                    if (exigoCustomerId == 0)
                    {
                        var enrollerId = GetOrderReferral(tenantConfig, order.ReferralWebalias);
                        var createCustomerRequest = new CreateCustomerRequest
                        {
                            FirstName = order.Customer.FirstName,
                            LastName = order.Customer.LastName,
                            Email = order.Customer.Email,
                            Phone = order.Customer.Phone,
                            EntryDate = DateTime.Now,
                            MainAddress1 = order.Customer.MainAddress1,
                            MainAddress2 = order.Customer.MainAddress2,
                            MainCity = order.Customer.MainCity,
                            MainState = order.Customer.MainState,
                            MainZip = order.Customer.MainZip,
                            MainCountry = order.Customer.MainCountry,
                            DefaultWarehouseID = warehouseId,
                            InsertEnrollerTree = true,
                            EnrollerID = enrollerId,
                            CustomerStatus = CustomerStatuses.Active,
                            CustomerType = CustomService.NewCustomerWithOrderTypeID(order),
                            Notes = $"Customer imported with App Shopify from site: {tenantConfig.ShopUrl} CustomerId: {order.ShopCustomerId}"
                        };
                        if(order.Customer.Email == null)
                        {
                            createCustomerRequest.CanLogin = false;
                        }
                        else
                        {
                            createCustomerRequest.CanLogin = true;
                            createCustomerRequest.LoginPassword = System.Web.Security.Membership.GeneratePassword(8, 1);
                            createCustomerRequest.LoginName = order.Customer.Email;
                        }
                        var response = Exigo.WebService(tenantConfig).CreateCustomer(createCustomerRequest);
                        
                        //Log result
                        var fullName = order.Customer.FirstName + " " + order.Customer.LastName;
                        var newCustomer = new Customer(tenantConfig, order.ShopCustomerId.ToString(), response.CustomerID, fullName, order.WebhookId, enrollerId, true, false);
                        newCustomer.Create();
                        new CustomerLog(newCustomer.Id, $"Customer: {newCustomer.ExigoCustomerId} created from Shopify customer: {newCustomer.ShopCustomerId}", order.WebhookId).Create();
                        new CustomerLog(newCustomer.Id, $"Customer placed in binary tree of {newCustomer.EnrollerId} ", order.WebhookId).Create();
                        return response.CustomerID;
                    }
                    return exigoCustomerId;
                }
                UpdateCustomerFromOrder(tenantConfig, order, warehouseId);
                return exigoCustomerId;

            }
            catch (Exception exception)
            {
                //Log result
                new Log(LogType.Error, $"Customer creation failed ShopId: {order.ShopCustomerId} message: {exception.Message}", LogSection.Customers).Create();
                return 0;
                //Log Exception
            }
        }
        private static void UpdateCustomerFromOrder(TenantConfiguration tenantConfig, SyncOrderObject order, int warehouseId)
        {
            var customer = new Customer().Get(order.ShopCustomerId.ToString(), tenantConfig.Id);
            if (!customer.HasAddress || customer.NoOrder)
            {
                var request = new UpdateCustomerRequest
                {
                    CustomerID = customer.ExigoCustomerId,
                    CustomerStatus = CustomerStatuses.Active,
                    CustomerType = CustomService.NewCustomerWithOrderTypeID(order),
                    FirstName = order.Customer.FirstName,
                    LastName = order.Customer.LastName,
                    Phone = order.Customer.Phone,
                    MainAddress1 = order.Customer.MainAddress1,
                    MainAddress2 = order.Customer.MainAddress2,
                    MainCity = order.Customer.MainCity,
                    MainState = order.Customer.MainState,
                    MainZip = order.Customer.MainZip,
                    MainCountry = order.Customer.MainCountry,
                    DefaultWarehouseID = warehouseId,

                };
                Exigo.WebService(tenantConfig).UpdateCustomer(request);
                customer.HasAddress = true;
                customer.NoOrder = false;
                customer.Update();
                
                new CustomerLog(customer.Id, $"Customer updated with address info and set to active.", order.WebhookId).Create();
            }
        }
        private static string GetCartToken(string customerNote)
        {
            var token = string.Empty;
            var splitNotes = customerNote.Split('\n');
            foreach (var note in splitNotes)
            {
                var _note = note.Split(new string[] { ": " }, StringSplitOptions.None);
                if (_note[0] == "carttoken")
                    token = _note[1];
            }
            return token;
        }
        private static string GetCustomerCountry(TenantConfiguration tenantConfig, string customerNote)
        {
            var country = string.Empty;
            var splitNotes = customerNote.Split('\n');
            foreach (var note in splitNotes)
            {
                var _note = note.Split(new string[] { ": " }, StringSplitOptions.None);
                if (_note[0] == "country")
                    country = _note[1];
                else
                    country = "US";
            }
            return country;
        }
        private static int GetCustomerReferral(TenantConfiguration tenantConfig, string customerNote)
        {
            var referralId = string.Empty;
            var splitNotes = customerNote.Split('\n');
            foreach(var note in splitNotes)
            {
                var _note = note.Split(new string[] { ": " }, StringSplitOptions.None);
                if (_note[0] == "referral")
                    referralId = _note[1];
            }
            try
            {
                return GetOrderReferral(tenantConfig, referralId);
            }
            catch (Exception ex)
            {
                new Log(LogType.Error, $"Issue with recorded webalias not found: {referralId}  at newOrderSync Errors: {ex.Message}", LogSection.Orders).Create();
                return tenantConfig.DefaultEnrollerID;
            }
        }
        private static int GetOrderReferral(TenantConfiguration tenantConfig, string referralId)
        {
            try
            {
                using (var context = SQLContext.Sql())
                {
                    var customerId = context.Query<int>($"Select * from CustomerSites where Webalias = @referralId", new
                    {
                        referralId
                    }).FirstOrDefault();
                    if (customerId == 0)
                    {
                        return tenantConfig.DefaultEnrollerID;
                    }
                    else
                    {
                        return customerId;
                    }
                }
            }
            catch(Exception ex)
            {
                new Log(LogType.Error, $"Issue with recorded webalias not found: {referralId}  at newOrderSync Errors: {ex.Message}", LogSection.Orders).Create();
                return tenantConfig.DefaultEnrollerID;
            }
        }
        public static int CheckIfCustomerExist(long shopifyId, bool isSandbox = false)
        {
            var customer =  new Customer().GetByShopId(shopifyId.ToString(), isSandbox);
            if (customer != null)
                return customer.ExigoCustomerId;
            else
                return 0;
        }
        public static int CheckIfCustomerExistByEmail(string email, TenantConfiguration config = null, bool realtime = false)
        {
            return Exigo.CheckCustomerByEmail(email, config, realtime);
        }
    }
}
