using Common;
using Common.Api.ExigoWebService;
using Common.Providers;
using Common.Services;
using Dapper;
using ExigoService;
using Newtonsoft.Json;
using ReplicatedSite.Models;
using ReplicatedSite.Providers;
using ReplicatedSite.Services;
using ReplicatedSite.ViewModels;
using ShopifyApp.Models;
using ShopifyApp.Services.ShopService;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.Security;
using static ReplicatedSite.Controllers.AccountController;

namespace ReplicatedSite.Controllers
{
    public class ShopifyCheckoutController : Controller
    {
        private TenantConfiguration tenantConfig
        {
            get
            {
                if (PropertyBag.Cart != null && PropertyBag.Cart.ShopUrl.IsNotNullOrEmpty())
                    return new TenantConfiguration().GetByDomain(PropertyBag.Cart.ShopUrl);
                else
                    return new TenantConfiguration().GetAll().First();
            }
        }
        private Market CurrentMarket
        {
            get
            {
                return GlobalUtilities.GetCurrentMarket(this.HttpContext);
            }
        }
        public ActionResult GetAutoOrder()
        {
            var token = Security.Encrypt(new
            {
                AutoOrderID = 174,
                customerId = 40356
            });
            return Redirect(token);
        }
        public ActionResult GetAutoOrderDetails(string token)
        {
            var decryptedToken = Security.Decrypt(token);
            var customerID = (int)decryptedToken.customerId;
            var autoOrderId = (int)decryptedToken.AutoOrderID;
            var ao = ExigoDAL.GetCustomerAutoOrders(customerID, autoOrderId, includePaymentMethods: false).First();
            var details = new List<Models.AutoOrderDetail>();
            foreach (var aod in ao.Details)
            {
                var detail = new Models.AutoOrderDetail();
                var itemprice = new VariantItemPrice();
                itemprice.SKU = aod.ItemCode;
                detail.id = itemprice.GetItemPricesBySku().First().ShopVariantId;
                detail.quantity = (int)aod.Quantity;
                detail.AutoOrderId = ao.AutoOrderID;
                details.Add(detail);
            }
            return new JsonNetResult(new
            {
                items = details
            });
        }
        #region Properties

        public Language _currentLanguage { get; set; }
        public Language CurrentLanguage
        {
            get
            {
                if (_currentLanguage == null)
                {
                    _currentLanguage = GlobalUtilities.GetSelectedLanguage(this.HttpContext, null, this.CurrentMarket);
                }
                return _currentLanguage;
            }
        }
        public string ShoppingCartName = GlobalSettings.Globalization.CookieKey + "ShopifyShopping";
        public ExigoService.IOrderConfiguration OrderConfiguration
        {
            get
            {
                if (_orderConfiguration == null)
                {
                    _orderConfiguration = CurrentMarket.Configuration.Orders;
                }
                return _orderConfiguration;
            }
        }
        public ExigoService.IOrderConfiguration _orderConfiguration { get; set; }


        public ExigoService.IOrderConfiguration AutoOrderConfiguration
        {
            get
            {
                if (_autoOrderConfiguration == null)
                {
                    _autoOrderConfiguration = CurrentMarket.Configuration.AutoOrders;
                }
                return _autoOrderConfiguration;
            }
        }
        public ExigoService.IOrderConfiguration _autoOrderConfiguration { get; set; }

        public ExigoService.IOrderConfiguration OrderPackConfiguration
        {
            get
            {
                if (_orderPacksConfiguration == null)
                {
                    _orderPacksConfiguration = CurrentMarket.Configuration.EnrollmentKits;
                }
                return _orderPacksConfiguration;
            }
        }
        public ExigoService.IOrderConfiguration _orderPacksConfiguration { get; set; }

        public ShoppingCartCheckoutPropertyBag PropertyBag
        {
            get
            {
                if (_propertyBag == null)
                {
                    _propertyBag = ExigoDAL.PropertyBags.Get<ShoppingCartCheckoutPropertyBag>(ShoppingCartName + "PropertyBag");
                }
                return _propertyBag;
            }
        }
        private ShoppingCartCheckoutPropertyBag _propertyBag;
        public ILogicProvider LogicProvider
        {
            get
            {
                if (_logicProvider == null)
                {
                    _logicProvider = new ShopifyCheckoutLogicProvider(this, PropertyBag);
                }
                return _logicProvider;
            }
        }
        private ILogicProvider _logicProvider;
        public ActionResult RedirectBackToShopCart(string shopUrl)
        {
            return Redirect("https://" + shopUrl + "/" + Identity.Owner.WebAlias + "/cart");
        }
        #endregion
        // GET: ShopifyCheckout/Home
        public async Task<ActionResult> ActivateAccountByToken(string token, string shopUrl, bool isBackoffice = false)
        {
            var model = new CustomerStatusViewModel();
            model.Log = new List<string>();
            var customerID = 0;
            // Decrypt the token
            if (isBackoffice)
            {
                var decryptedToken = Security.Decrypt(token);
                customerID = (int)decryptedToken.CustomerID;
            }
            else
            {
                var IV = GlobalSettings.EncryptionKeys.SilentLogins.IV;
                var key = GlobalSettings.EncryptionKeys.SilentLogins.Key;
                var decryptedToken = Security.AESDecrypt(token, key, IV);

                // Split the value and get the values
                var splitToken = decryptedToken.Split('|');
                customerID = Convert.ToInt32(splitToken[0]);
                var tokenExpirationDate = Convert.ToDateTime(splitToken[1]);
            }

            model.ExigoCustomer = ExigoService.ExigoDAL.GetCustomer(customerID);
            model.Log.Add("CustomerID in Exigo is: " + model.ExigoCustomer.CustomerID);
            var config = new TenantConfiguration().Get(20002);
            var shopify = new ShopifyApp.Services.ShopService.ShopifyDAL(config.Id);
            try
            {
                var customer = await new ShopifyApp.Services.ShopService.ShopifyDAL(config.Id).GetCustomerByEmail(model.ExigoCustomer.Email);
                model.Customer = new ShopifyApp.Models.Customer().GetByExigoId(model.ExigoCustomer.CustomerID);
                if (model.Customer == null)
                {
                    model.Log.Add("Customer does not found in plugin");

                    if (customer != null)
                    {
                        model.Log.Add("Customer already existed in shopify. ID: " + customer.Id.Value.ToString());
                        model.Customer = new ShopifyApp.Models.Customer(config, customer.Id.Value.ToString(), model.ExigoCustomer.CustomerID, model.ExigoCustomer.FullName, "Admin", model.ExigoCustomer.EnrollerID.Value, true, false);
                    }
                    else
                    {
                        customer = await shopify.CreateCustomer(new ShopifySharp.Customer
                        {
                            FirstName = model.ExigoCustomer.FirstName,
                            LastName = model.ExigoCustomer.LastName,
                            Email = model.ExigoCustomer.Email,
                            VerifiedEmail = true,
                            AcceptsMarketing = model.ExigoCustomer.IsOptedIn
                        });
                        model.Log.Add("Customer record created in Shopify. ID: " + customer.Id.Value.ToString());
                        model.Customer = new ShopifyApp.Models.Customer(config, customer.Id.Value.ToString(), model.ExigoCustomer.CustomerID, model.ExigoCustomer.FullName, "Admin", model.ExigoCustomer.EnrollerID.Value, true, false);
                        model.ActivationLink = await shopify.GetAccountActivationLink(customer.Id.Value);
                        model.Log.Add("Generated activation link for shopify: " + model.ActivationLink);
                    }
                    model.ShopifyCustomer = customer;
                    model.Customer.Create();
                    var created = await shopify.CreateSilentLoginToken(new ShopifyApp.Models.Customer
                    {
                        ExigoCustomerId = model.ExigoCustomer.CustomerID,
                        ShopCustomerId = customer.Id.Value.ToString()
                    });
                    model.Log.Add("Customer bridge created in plugin, silent login tokne: " + model.Customer.SilentLoginToken);
                    var sync = await ShopifyApp.Services.CheckoutService.SyncCustomerFromExigo(model.ExigoCustomer.CustomerID, model.ExigoCustomer.CustomerTypeID, config.Id);
                    model.Log.Add("Customerdata synced from exigo to shopify.");

                }
                else
                {
                    model.Log.Add("Customer existed in Shopify id: " + model.Customer.ShopCustomerId);
                    var appCustomer = await ShopifyApp.Services.CheckoutService.SyncCustomerFromExigo(model.ExigoCustomer.CustomerID, model.ExigoCustomer.CustomerTypeID, config.Id);
                    model.Log.Add("Customerdata synced from exigo to shopify.");
                    model.ShopifyCustomer = await new ShopifyApp.Services.ShopService.ShopifyDAL(config.Id).GetCustomer(model.Customer.ShopCustomerId);
                    model.Log.Add("Customer Account status in shopify: " + model.ShopifyCustomer.State);
                    if (model.ShopifyCustomer.State.ToLower() != "enabled")
                    {
                        model.ActivationLink = await shopify.GetAccountActivationLink(model.ShopifyCustomer.Id.Value);
                        model.Log.Add("Generated new activation link: " + model.ActivationLink);
                    }
                    var created = await shopify.CreateSilentLoginToken(model.Customer);
                    model.Customer.Update();
                    model.Log.Add("Silent login token refreshed: " + model.Customer.SilentLoginToken);

                }
                model.Log.Add("Updated customer Record with activation link.");
                if(isBackoffice)
                {
                    if (model.ActivationLink.IsNotNullOrEmpty())
                        return Redirect(model.ActivationLink);
                    else
                        return Redirect("https://" + shopUrl + "/account");
                }
                return View(model);
            }
            catch (Exception e)
            {
                model.Log.Add(e.Message);
                new Log(ShopifyApp.LogType.Information, $"Link not created error: {e.Message}", ShopifyApp.LogSection.Global, "APP").Create();
                return View(model);
            }
        }
        public async Task<ActionResult> ActivateAccountByEmail(int CustomerID, string shopUrl)
        {
            PropertyBag.Customer = ExigoDAL.GetCustomer(CustomerID);
            var link = await CreateShopAccount(PropertyBag.Customer, tenantConfig.Id);

            ExigoService.ExigoDAL.WebService().UpdateCustomer(new UpdateCustomerRequest
            {
                CustomerID = PropertyBag.Customer.CustomerID,
                Field14 = link
            });
            var request = new ExigoService.SendEmailRequest
            {
                Subject = $"Activation of account on {PropertyBag.Cart.ShopUrl}",
                Body = $"Here is your link to activate your account on {shopUrl}.<br/><br/></br> <a href='{link}'>Click here to activate account</a>",
                To = new string[] { PropertyBag.Customer.Email }, 
                ReplyTo = new string [] { GlobalSettings.Company.Email } ,
                From = GlobalSettings.Company.Email,
                IsHtml = true,
                UseExigoApi = true
            };
            ExigoDAL.SendEmail(request);
            return Redirect("https://" + PropertyBag.Cart.ShopUrl + "/cart");
        }
        public async Task<ActionResult> ActivateAccount(string token)
        {
            var decryptedToken = Security.Decrypt(token);
            var customerId = decryptedToken.customerId.Value;
            var configId = decryptedToken.tenantId.Value;
            var customer = ExigoDAL.GetCustomer(Convert.ToInt32(customerId));
            var link = await CreateShopAccount(customer, Convert.ToInt32(configId));
            ExigoService.ExigoDAL.WebService().UpdateCustomer(new UpdateCustomerRequest
            {
                CustomerID = customer.CustomerID,
                Field14 = link
            });
            return Redirect(link);
        }
        public ActionResult Index()
        {
            return LogicProvider.GetNextAction();
        }
        public ActionResult Account(string errorMessage = null)
        {
            if (PropertyBag.Cart == null)
                return RedirectBackToShopCart(GlobalSettings.ReplicatedSites.ShopUrl);
            var allowedCustomerTypes = new List<int>
            {
                CustomerTypes.Affiliate,
                CustomerTypes.Distributor
            };
            if (Identity.Customer != null)
                return LogicProvider.GetNextAction();
            //If this is a return from an error
            //Populate ViewModel
            var model = new ShopifyCheckoutViewModel(PropertyBag);
            //Populate error for display
            model.ErrorMessage = errorMessage;
            if (errorMessage.IsNotNullOrEmpty())
                //Populate address entered
                PropertyBag.Customer.MainAddress = PropertyBag.MainAddress;
            model.PropertyBag.AcceptsMarketing = true;
            model.PropertyBag.Customer.BirthDate = DateTime.Now;
            return View(model);
        }
        [HttpPost]
        public ActionResult Account(ShopifyCheckoutViewModel model)
        {
            PropertyBag.Customer.LoginName = model.PropertyBag.Customer.FirstName + model.PropertyBag.Customer.LastName;
            PropertyBag.Customer.Password = model.PropertyBag.Customer.Password;
            PropertyBag.AcceptsMarketing = model.PropertyBag.AcceptsMarketing;
            PropertyBag.Customer.BirthDate = new DateTime(model.PropertyBag.Customer.BirthDate.Year, model.PropertyBag.BirthMonth, model.PropertyBag.BirthDay);
            PropertyBag.MainAddress = model.PropertyBag.Customer.MainAddress;
            PropertyBag.Customer.FirstName = model.PropertyBag.Customer.FirstName;
            PropertyBag.Customer.LanguageID = model.PropertyBag.Customer.LanguageID;
            PropertyBag.Customer.LastName = model.PropertyBag.Customer.LastName;
            PropertyBag.Customer.PrimaryPhone = model.PropertyBag.Customer.PrimaryPhone;
            PropertyBag.Customer.Email = model.PropertyBag.Customer.Email;
            PropertyBag.AcceptsMarketing = model.PropertyBag.AcceptsMarketing;
            PropertyBag.ShippingAddressSameAsMain = model.PropertyBag.ShippingAddressSameAsMain;
            if (PropertyBag.MainAddress.Country == "US")
            {
                var verify = ExigoDAL.VerifyAddress(model.PropertyBag.Customer.MainAddress);
                if (verify.IsValid)
                {
                    PropertyBag.Customer.MainAddress = (ExigoService.Address)verify.VerifiedAddress;
                }
                else
                {
                    ExigoDAL.PropertyBags.Update(PropertyBag);
                    return RedirectToAction("Account", new { errorMessage = "Could not verify address" });
                }
            }
            else
            {
                if (PropertyBag.MainAddress.State.IsNullOrEmpty())
                    PropertyBag.MainAddress.State = PropertyBag.MainAddress.Country;
                PropertyBag.Customer.MainAddress = model.PropertyBag.Customer.MainAddress;
            }
            
            if (model.PropertyBag.ShippingAddressSameAsMain)
            {
                PropertyBag.ShippingAddress = new ExigoService.ShippingAddress(PropertyBag.Customer.MainAddress);
                PropertyBag.ShippingAddress.FirstName = PropertyBag.Customer.FirstName;
                PropertyBag.ShippingAddress.LastName = PropertyBag.Customer.LastName;
                PropertyBag.ShippingAddress.Email = PropertyBag.Customer.Email;
                PropertyBag.ShippingAddress.Phone = PropertyBag.Customer.PrimaryPhone;
                PropertyBag.Cart.ShippingAddress = true;
            }
            PropertyBag.Cart.Account = true;
            ExigoDAL.PropertyBags.Update(PropertyBag);
            if(PropertyBag.MainAddress.Country != CurrentMarket.CookieValue)
                GlobalUtilities.SetSelectedCountryCode(PropertyBag.MainAddress.Country);
            var calculated = CalculateCart(true);
            if (!calculated)
                return RedirectBackToShopCart(PropertyBag.Cart.ShopUrl);
            return LogicProvider.GetNextAction();
        }
        public ActionResult ShippingAddress(string errorMessage = null)
        {
            if (PropertyBag.Cart == null)
                return RedirectBackToShopCart(PropertyBag.Cart.ShopUrl);
            var model = new ShopifyCheckoutViewModel(PropertyBag);
            model.ErrorMessage = errorMessage;
            if (!model.PropertyBag.Cart.ShippingAddress)
            {
                model.PropertyBag.ShippingAddress.FirstName = PropertyBag.Customer.FirstName;
                model.PropertyBag.ShippingAddress.LastName = PropertyBag.Customer.LastName;
                model.PropertyBag.ShippingAddress.Phone = PropertyBag.Customer.PrimaryPhone;
                model.PropertyBag.ShippingAddress.Email = PropertyBag.Customer.Email;
            }
            if (PropertyBag.Customer.CustomerID != 0)
            {
                model.ShippingAddresses = new ShippingAddressesViewModel();
                var adresses = new List<ExigoService.ShippingAddress>();
                foreach(var address in PropertyBag.Customer.Addresses)
                {
                    adresses.Add(new ExigoService.ShippingAddress(address));
                }
                model.ShippingAddresses.Addresses = adresses.ToArray();
            }
            return View(model);
        }
        [HttpPost]
        public ActionResult ShippingAddress(ShopifyCheckoutViewModel model, ExigoService.ShippingAddress address = null)
        {

            address.FirstName = (address.FirstName.IsNullOrEmpty()) ? PropertyBag.Customer.FirstName : address.FirstName;
            address.LastName = (address.LastName.IsNullOrEmpty()) ? PropertyBag.Customer.LastName : address.LastName;
            address.Email = (address.Email.IsNullOrEmpty()) ? PropertyBag.Customer.Email : address.Email;
            address.Phone = (address.Phone.IsNullOrEmpty()) ? (PropertyBag.Customer.PrimaryPhone.IsNullOrEmpty()) ? PropertyBag.Customer.MobilePhone : PropertyBag.Customer.PrimaryPhone : address.Phone;
            if (address.Country == "US")
            {
                var verify = ExigoDAL.VerifyAddress(address);
                if (verify.IsValid)
                {
                    var shippingAddress = new ExigoService.ShippingAddress((ExigoService.Address)verify.VerifiedAddress);
                    shippingAddress.FirstName = address.FirstName;
                    shippingAddress.LastName = address.LastName;
                    shippingAddress.Email = address.Email;
                    shippingAddress.Phone = address.Phone;
                    PropertyBag.ShippingAddress = shippingAddress;
                }
                else
                {
                    ExigoDAL.PropertyBags.Update(PropertyBag);
                    return RedirectToAction("ShippingAddress", new { errorMessage = "Could not verify address" });
                }
            }
            else
            {
                if (address.State.IsNullOrEmpty())
                    address.State = address.Country;
                PropertyBag.ShippingAddress = address;
            }
            var _address = PropertyBag.ShippingAddress;
            // Save the address to the customer's account if applicable
            if (PropertyBag.Customer.CustomerID != 0 && _address.AddressType == AddressType.New)
            {
                ExigoDAL.SetCustomerAddressOnFile(PropertyBag.Customer.CustomerID, _address as ExigoService.Address);
            }

            PropertyBag.ShippingAddress = _address;
            PropertyBag.Cart.ShippingAddress = true;
            ExigoDAL.PropertyBags.Update(PropertyBag);
            var calculated = CalculateCart(true);
            if (!calculated)
                return RedirectBackToShopCart(PropertyBag.Cart.ShopUrl);
            return LogicProvider.GetNextAction();

        }
        public ActionResult EnrollmentInfo(string errorMessage = null)
        {
            if (PropertyBag.Cart == null)
                return RedirectBackToShopCart(PropertyBag.Cart.ShopUrl);
            var model = new ShopifyCheckoutViewModel(PropertyBag);
            model.ErrorMessage = errorMessage;
            return View(model);
        }
        [HttpPost]
        public ActionResult EnrollmentInfo(ShopifyCheckoutViewModel model)
        {
            if (PropertyBag.Cart == null)
                RedirectBackToShopCart(PropertyBag.Cart.ShopUrl);
            PropertyBag.Customer.NewLoginName = model.PropertyBag.Customer.NewLoginName;
            PropertyBag.Customer.WebAlias = PropertyBag.Customer.NewLoginName;
            PropertyBag.AcceptedEnroller = model.PropertyBag.AcceptedEnroller;
            PropertyBag.AcceptedTerms = model.PropertyBag.AcceptedTerms;
            PropertyBag.AcceptedWebsiteFee = model.PropertyBag.AcceptedWebsiteFee;
            PropertyBag.Customer.TaxID = model.PropertyBag.Customer.TaxID;
            var returnUrl = LogicProvider.GetNextAction();
            if (!model.PropertyBag.AcceptedTerms)
                returnUrl = RedirectToAction("EnrollmentInfo", new { errorMessage = "Terms must be accepted to continue" });
            else
                PropertyBag.Cart.EnrollmentInfo = true;
            ExigoDAL.PropertyBags.Update(PropertyBag);
            return returnUrl;
        }
        public ActionResult AutoOrder(string errorMessage = null, bool isDebug = false)
        {
            if (isDebug)
            {
                PropertyBag.IsDebug = true;
                ExigoDAL.PropertyBags.Update(PropertyBag);
            }
            if (PropertyBag.Cart.EditAutoOrderId > 0)
            {
                var autoOrder = ExigoDAL.GetCustomerAutoOrders(PropertyBag.Customer.CustomerID, PropertyBag.Cart.EditAutoOrderId).First();
                var details = new List<ExigoService.AutoOrderDetail>();
                foreach (var item in PropertyBag.Cart.Items.Where(c => c.Type == ShopifyApp.ShoppingCartItemType.AutoOrder))
                {
                    details.Add(new ExigoService.AutoOrderDetail
                    {
                        ItemCode = item.ItemCode,
                        Quantity = item.Quantity
                    });
                }
                autoOrder.Details = details;
                var autoOrderRequest = new CreateAutoOrderRequest(autoOrder)
                {
                    CustomerID = PropertyBag.Customer.CustomerID,
                    OverwriteExistingAutoOrder = true,
                    ExistingAutoOrderID = PropertyBag.Cart.EditAutoOrderId
                };
                var response = ExigoDAL.WebService().CreateAutoOrder(autoOrderRequest);
                PropertyBag.Cart.AutoOrderSaved = true;
                PropertyBag.Cart.AutoOrderFrequency = autoOrder.FrequencyTypeDescription;
                PropertyBag.Cart.AutoOrderStartDate = autoOrder.StartDate;
                ExigoDAL.PropertyBags.Update(PropertyBag);

                //If no orderitems exit from checkout
                if (!PropertyBag.Cart.OrderItems.Any())
                    //TODO: Send to autoorder page
                    return Redirect($"{GlobalSettings.Company.BaseBackofficeUrl}/manage-auto-order/" + PropertyBag.Cart.EditAutoOrderId);
            }
            if (PropertyBag.Cart == null)
                return RedirectBackToShopCart(PropertyBag.Cart.ShopUrl);
            var model = new ShopifyCheckoutViewModel();
            if (PropertyBag.Customer.CustomerID != 0)
            {
                var customerAutoOrder = ExigoDAL.GetCustomerAutoOrders(PropertyBag.Customer.CustomerID).ToList();
                model.CustomerAutoOrders = customerAutoOrder.Where(c => c.FrequencyTypeID != FrequencyTypes.Yearly).ToList();
                if (model.CustomerAutoOrders.Any())
                {
                    PropertyBag.ExisitingAutoOrder = true;
                    PropertyBag.AddToExistingAutoOrder = true;
                    PropertyBag.SelectedAutoOrderId = model.CustomerAutoOrders.First().AutoOrderID;
                }
            }
            if(PropertyBag.Cart.EditAutoOrderId > 0)
            {
                var auOrder = model.CustomerAutoOrders.Where(c => c.AutoOrderID == PropertyBag.Cart.EditAutoOrderId).First();
                PropertyBag.AutoOrderShippingAddress = auOrder.ShippingAddress;
                PropertyBag.AutoOrderFrequencyType = (FrequencyType)auOrder.FrequencyTypeID;
                PropertyBag.AutoOrderStartDate = auOrder.NextRunDate.Value;
                PropertyBag.AutoOrderPaymentMethod = auOrder.PaymentMethod;

            }
            else
            {
                PropertyBag.AutoOrderFrequencyType = FrequencyType.Monthly;
                PropertyBag.AutoOrderStartDate = DateTime.Now.AddDays(30);
                PropertyBag.AutoOrderAddressSameAsShipping = true;
            }
            ExigoDAL.PropertyBags.Update(PropertyBag);
            model.PropertyBag = PropertyBag;
            model.ErrorMessage = errorMessage;
            return View(model);
        }
        [HttpPost]
        public ActionResult AutoOrder(ShopifyCheckoutViewModel model)
        {
            if (PropertyBag.Cart == null)
                RedirectBackToShopCart(PropertyBag.Cart.ShopUrl);
            //if existing autoorder selected Save aoutoorder and remove items
            if (PropertyBag.Cart.EditAutoOrderId > 0)
            {
                var autoOrder = ExigoDAL.GetCustomerAutoOrders(PropertyBag.Customer.CustomerID, PropertyBag.Cart.EditAutoOrderId).First();
                autoOrder.ShippingAddress = model.PropertyBag.AutoOrderShippingAddress;
                var details = new List<ExigoService.AutoOrderDetail>();
                foreach (var item in PropertyBag.Cart.Items.Where(c => c.Type == ShopifyApp.ShoppingCartItemType.AutoOrder))
                {
                    details.Add(new ExigoService.AutoOrderDetail
                    {
                        ItemCode = item.ItemCode,
                        Quantity = item.Quantity
                    });
                }
                autoOrder.Details = details;
                autoOrder.FrequencyTypeID = (int)model.PropertyBag.AutoOrderFrequencyType;
                autoOrder.StartDate = model.PropertyBag.AutoOrderStartDate;
                var autoOrderRequest = new CreateAutoOrderRequest(autoOrder)
                {
                    CustomerID = PropertyBag.Customer.CustomerID,
                    OverwriteExistingAutoOrder = true,
                    ExistingAutoOrderID = PropertyBag.SelectedAutoOrderId
                };
                var response = ExigoDAL.WebService().CreateAutoOrder(autoOrderRequest);
                PropertyBag.Cart.AutoOrderSaved = true;
                PropertyBag.Cart.AutoOrderFrequency = autoOrder.FrequencyTypeDescription;
                PropertyBag.Cart.AutoOrderStartDate = autoOrder.StartDate;
                ExigoDAL.PropertyBags.Update(PropertyBag);

                //If no orderitems exit from checkout
                if (!PropertyBag.Cart.OrderItems.Any())
                    //TODO: Send to autoorder page
                    return Redirect(PropertyBag.Cart.ShopUrl + "/" + Identity.Owner.WebAlias + "/pages/clearcart");
            }
            else if (model.PropertyBag.AddToExistingAutoOrder && model.PropertyBag.SelectedAutoOrderId != 0)
            {
                PropertyBag.SelectedAutoOrderId = model.PropertyBag.SelectedAutoOrderId;
                var autoOrder = ExigoDAL.GetCustomerAutoOrders(PropertyBag.Customer.CustomerID, PropertyBag.SelectedAutoOrderId).First();
                foreach (var item in PropertyBag.Cart.Items.Where(c => c.Type == ShopifyApp.ShoppingCartItemType.AutoOrder))
                {
                    autoOrder.Details.Add(new ExigoService.AutoOrderDetail
                    {
                        ItemCode = item.ItemCode,
                        Quantity = item.Quantity
                    });
                }
                var autoOrderRequest = new CreateAutoOrderRequest(autoOrder)
                {
                    CustomerID = PropertyBag.Customer.CustomerID,
                    ExistingAutoOrderID = PropertyBag.SelectedAutoOrderId,
                    Frequency = (FrequencyType)autoOrder.FrequencyTypeID,
                    StartDate = model.PropertyBag.AutoOrderStartDate
                };
                ExigoDAL.WebService().CreateAutoOrder(autoOrderRequest);
                PropertyBag.Cart.AutoOrderSaved = true;
                PropertyBag.Cart.AutoOrderFrequency = autoOrder.FrequencyTypeDescription.ToString();
                PropertyBag.Cart.AutoOrderStartDate = model.PropertyBag.AutoOrderStartDate;
                ExigoDAL.PropertyBags.Update(PropertyBag);
                
                //If no orderitems exit from checkout
                if(!PropertyBag.Cart.OrderItems.Any())
                    //TODO: Send to autoorder page
                    return Redirect(Url.Action("OrderComplete", "ShopifyCheckout"));
            }
            else
            {
                PropertyBag.AddToExistingAutoOrder = false;
                PropertyBag.AutoOrderFrequencyType = model.PropertyBag.AutoOrderFrequencyType;
                PropertyBag.AutoOrderStartDate = model.PropertyBag.AutoOrderStartDate;
                PropertyBag.Cart.AutoOrderFrequency = PropertyBag.AutoOrderFrequencyType.ToString();
                PropertyBag.Cart.AutoOrderStartDate = PropertyBag.AutoOrderStartDate;
                if (model.PropertyBag.AutoOrderAddressSameAsShipping)
                {
                    if(!PropertyBag.ShippingAddress.IsComplete)
                    {
                        var Address = ExigoDAL.GetCustomerAddresses(PropertyBag.Customer.CustomerID).First(c => c.AddressType == AddressType.Main);
                        var Customer = ExigoDAL.GetCustomer(PropertyBag.Customer.CustomerID);
                        PropertyBag.AutoOrderShippingAddress = new ExigoService.ShippingAddress(Address);
                        PropertyBag.AutoOrderShippingAddress.Email = Customer.Email;
                        PropertyBag.AutoOrderShippingAddress.Phone = Customer.PrimaryPhone;
                    }
                    else
                    {
                        PropertyBag.AutoOrderShippingAddress = PropertyBag.ShippingAddress;
                    }
                }
                else
                {
                    if (PropertyBag.AutoOrderShippingAddress.Country == "US")
                    {
                        var verify = ExigoDAL.VerifyAddress(model.PropertyBag.AutoOrderShippingAddress);
                        if (verify.IsValid)
                        {
                            var shippingAddress = new ExigoService.ShippingAddress((ExigoService.Address)verify.VerifiedAddress);
                            shippingAddress.FirstName = model.PropertyBag.AutoOrderShippingAddress.FirstName;
                            shippingAddress.LastName = model.PropertyBag.AutoOrderShippingAddress.LastName;
                            shippingAddress.Email = model.PropertyBag.AutoOrderShippingAddress.Email;
                            shippingAddress.Phone = model.PropertyBag.AutoOrderShippingAddress.Phone;
                            model.PropertyBag.AutoOrderShippingAddress = shippingAddress;
                        }
                        else
                        {
                            return RedirectToAction("AutoOrder", new { errorMessage = "Could not verify address" });
                        }
                    }
                    else if (PropertyBag.AutoOrderShippingAddress.State.IsNullOrEmpty())
                    {
                        model.PropertyBag.AutoOrderShippingAddress.State = model.PropertyBag.AutoOrderShippingAddress.Country;
                    }
                    PropertyBag.AutoOrderShippingAddress = model.PropertyBag.AutoOrderShippingAddress;
                }
            }
            PropertyBag.Cart.AutoOrder = true;
            ExigoDAL.PropertyBags.Update(PropertyBag);
            return LogicProvider.GetNextAction();
        }
        public ActionResult Payment()
        {
            if (PropertyBag.NewOrderID != 0)
            {
                if(PropertyBag.IsSubmitting)
                    return RedirectToAction("OrderComplete", "ShopifyCheckout", new { success = false });
                var token = Security.Encrypt(new
                {
                    OrderID = PropertyBag.NewOrderID,
                    CustomerID = PropertyBag.Customer.CustomerID
                });
                return RedirectToAction("OrderPayment", "ShopifyCheckout", new { token = token });
            }
            if (PropertyBag.Cart == null)
                return RedirectBackToShopCart(PropertyBag.Cart.ShopUrl);
            var model = new PaymentMethodsViewModel();
            model.PropertyBag = PropertyBag;
            if (Identity.Customer != null)
            {
                model.PaymentMethods = ExigoDAL.GetCustomerPaymentMethods(new GetCustomerPaymentMethodsRequest
                {
                    CustomerID = Identity.Customer.CustomerID,
                    ExcludeIncompleteMethods = true,
                    ExcludeInvalidMethods = true
                });
            }
            #region Points Payments
            if (Identity.Customer != null && PropertyBag.Cart.OrderItems.Any())
            {
                var pointAccountID = Identity.Customer.CustomerTypeID == CustomerTypes.Distributor ? PointAccounts.PowerStartPoints : PointAccounts.BeautyInsiderPoints;
                model.PointAccount = ExigoDAL.GetCustomerPointAccount(Identity.Customer.CustomerID, pointAccountID);
                var TotalBalance = (model.PointAccount != null) ? model.PointAccount.Balance : 0;
                if (model.PointAccount != null && TotalBalance > 0)
                {
                    model.HasValidPointAccount = true;
                }
                model.UsePointsAsPayment = PropertyBag.UsePointsAsPayment;
                model.QuantityOfPointsToUse = model.UsePointsAsPayment ? PropertyBag.QuantityOfPointsToUse : 0;
            }
            #endregion
            #region Shipping
            var beginningShipMethodID = PropertyBag.ShipMethodID;
            // If this is the first time coming to the page, and the property bag's ship method has not been set, then set it to the default for the configuration
            if (PropertyBag.ShipMethodID == 0)
            {
                PropertyBag.ShipMethodID = OrderConfiguration.DefaultShipMethodID;
                beginningShipMethodID = PropertyBag.ShipMethodID;
                ExigoDAL.PropertyBags.Update(PropertyBag);
            }
            
            #endregion
            var calculated = CalculateCart(true);
            if (!calculated)
                return RedirectBackToShopCart(PropertyBag.Cart.ShopUrl);
            var viewModel = new ShopifyCheckoutViewModel(PropertyBag);
            viewModel.Payments = model;
            return View("Payment", viewModel);
        }


        public ActionResult OrderPayment(string token = "")
        {
            var decryptedToken = Security.Decrypt(token);
            var customerID = (int)decryptedToken.CustomerID;
            var orderID = (int)decryptedToken.OrderID;
            var order = ExigoDAL.GetCustomerOrders_SQL(new GetCustomerOrdersRequest
            {
                OrderID = orderID,
                CustomerID = customerID,
                IncludeOrderDetails = true
            }).Orders.First();

            if (order.OrderStatusID >= OrderStatuses.Accepted)
            {
                return RedirectToAction("Ordercomplete", "shopifyCheckout");
            }
            if (PropertyBag.NewOrderID != order.OrderID)
                ExigoDAL.PropertyBags.Delete(PropertyBag);
            PropertyBag.Customer = ExigoDAL.GetCustomer(customerID);
            PropertyBag.NewOrderID = order.OrderID;
            if (PropertyBag.Customer.MainAddress.Country != CurrentMarket.CookieValue)
                GlobalUtilities.SetSelectedCountryCode(PropertyBag.Customer.MainAddress.Country);
            PropertyBag.Cart = new Cart();
            PropertyBag.Cart.IsPaymentOfExistingOrder = true;
            foreach(var detail in order.Details)
            {
                var cartItem = new CartItem
                {
                    Sku = detail.ItemCode,
                    Product_Title = detail.ItemDescription,
                    Original_Line_Price = detail.PriceEach,
                    Quantity = detail.Quantity
                };
                PropertyBag.Cart.Items.Add(cartItem);
            }
            PropertyBag.Cart.ShippingTotal = order.ShippingTotal;
            PropertyBag.Cart.TaxTotal = order.TaxTotal;
            PropertyBag.Cart.PointsTotal = -PropertyBag.QuantityOfPointsToUse; 

            ExigoDAL.PropertyBags.Update(PropertyBag);
            var model = new PaymentMethodsViewModel();

            #region Points Payments
            if (PropertyBag.Customer.CustomerID != 0)
            {
                var pointAccountID = PropertyBag.Customer.CustomerTypeID == CustomerTypes.Distributor ? PointAccounts.PowerStartPoints : PointAccounts.BeautyInsiderPoints;
                model.PointAccount = ExigoDAL.GetCustomerPointAccount(PropertyBag.Customer.CustomerID, pointAccountID);
                var TotalBalance = (model.PointAccount != null) ? model.PointAccount.Balance : 0;
                if (model.PointAccount != null && TotalBalance > 0)
                {
                    model.HasValidPointAccount = true;
                }
                model.UsePointsAsPayment = PropertyBag.UsePointsAsPayment;
                model.QuantityOfPointsToUse = model.UsePointsAsPayment ? PropertyBag.QuantityOfPointsToUse : 0;
            }
            #endregion
            model.PropertyBag = PropertyBag;
            if (PropertyBag.Customer.CustomerID != 0)
            {
                model.PaymentMethods = ExigoDAL.GetCustomerPaymentMethods(new GetCustomerPaymentMethodsRequest
                {
                    CustomerID = PropertyBag.Customer.CustomerID,
                    ExcludeIncompleteMethods = true,
                    ExcludeInvalidMethods = true
                });
            }
            var viewModel = new ShopifyCheckoutViewModel(PropertyBag);
            viewModel.Payments = model;
            return View("OrderPayment", viewModel);
        }
        public async Task<ActionResult> PayOrder()
        {
            try
            {
                var order = ExigoDAL.GetCustomerOrders(new GetCustomerOrdersRequest
                {
                    OrderID = PropertyBag.NewOrderID,
                    CustomerID = PropertyBag.Customer.CustomerID,
                    IncludeOrderDetails = true
                }).First();
                // Set up variables for use later down the line, if point payments are in play
                decimal pointPaymentAmount = PropertyBag.QuantityOfPointsToUse;
                decimal orderAmount = order.Total;
                decimal regularPaymentAmount = orderAmount - pointPaymentAmount;
                // Create the payment request
                if (PropertyBag.PaymentMethod is CreditCard)
                {
                    var card = PropertyBag.PaymentMethod as CreditCard;
                    if (card.Type == CreditCardType.New)
                    {
                        // Test Credit Card, so no need to charge card
                        if (!card.IsTestCreditCard)
                        {
                            var chargeRequest = new ChargeCreditCardTokenRequest(card);
                            if (PropertyBag.UsePointsAsPayment)
                            {
                                chargeRequest.MaxAmount = regularPaymentAmount;
                            }
                            var response = ExigoDAL.WebService().ChargeCreditCardToken(chargeRequest); 
                            if (response.Result.Status == ResultStatus.Success)
                            {
                                var updateOrderRequest = new UpdateOrderRequest
                                {
                                    OrderID = order.OrderID,
                                    OrderStatus = (int)OrderStatusType.Accepted
                                };
                                ExigoDAL.WebService().UpdateOrder(updateOrderRequest);
                            }
                        }
                    }
                    else
                    {
                        if (orderAmount > 0)
                        {
                            var chargeRequest = new ChargeCreditCardTokenOnFileRequest(card);
                            if (PropertyBag.UsePointsAsPayment)
                            {
                                chargeRequest.MaxAmount = regularPaymentAmount;
                            }
                            var response = ExigoDAL.WebService().ChargeCreditCardTokenOnFile(chargeRequest);
                            if(response.Result.Status == ResultStatus.Success)
                            {
                                var updateOrderRequest = new UpdateOrderRequest
                                {
                                    OrderID = order.OrderID,
                                    OrderStatus = (int)OrderStatusType.Accepted
                                };
                                ExigoDAL.WebService().UpdateOrder(updateOrderRequest);
                            }
                        }
                    }
                }
                return new JsonNetResult(new
                {
                    success = true,
                    redirectUrl = Url.Action("Ordercomplete", "shopifyCheckout").ToString()
                });
            }
            catch(Exception e)
            {
                return new JsonNetResult(new
                {
                    success = false,
                    message = e.Message
                });
            }
        }
        public ActionResult OrderComplete(bool success = true, string token = "")
        {
            if(PropertyBag.Cart == null)
            {
                return Redirect(GlobalSettings.ReplicatedSites.ShopUrl + "?clearcart=true");
            }
            var model = new OrderCompleteViewModel();
            try
            {
                if (PropertyBag.NewCustomerID != 0)
                {
                    var activationToken = Security.Encrypt(new
                    {
                        tenantId = tenantConfig.Id,
                        customerId = PropertyBag.NewCustomerID
                    });
                    model.Token = activationToken;
                }
                else if (Identity.Customer == null && token.IsNotNullOrEmpty())
                {
                    var decryptedToken = Security.Decrypt(token);
                    var customerId = decryptedToken.CustomerID.Value;
                    var orderId = decryptedToken.OrderID.Value;
                    var activationToken = Security.Encrypt(new
                    {
                        tenantId = tenantConfig.Id,
                        customerId = customerId
                    });
                    model.Token = activationToken;
                }
                else if (Identity.Customer != null)
                {
                    FormsAuthentication.SignOut();
                }
            }
            catch
            {
                model.PropertyBag = PropertyBag;
                model.Success = false;
                ExigoDAL.PropertyBags.Delete(PropertyBag);
                return View(model);
            }

            model.PropertyBag = PropertyBag;
            model.Success = success;
            ExigoDAL.PropertyBags.Delete(PropertyBag);
            return View(model);
        }
        public bool CancelSubmission()
        {
            PropertyBag.IsSubmitting = false;
            ExigoDAL.PropertyBags.Update(PropertyBag);
            return true;
        }
        [HttpPost]
        public async Task<ActionResult> SubmitCheckout(bool isCitconWebSDK = false, string authorizationCode = "", string sdkPaymentMethod = "")
        {
            if (PropertyBag.Cart == null)
            {
                return new JsonNetResult(new
                {
                    success = true,
                    message = "Your checkout has expired, please refresh page"
                });
            }
            var redirectonCompleted = Url.Action("OrderComplete", "ShopifyCheckout");
            if (!PropertyBag.IsSubmitting)
            {
                PropertyBag.IsSubmitting = true;
                decimal pointPaymentAmount = 0;
                decimal regularPaymentAmount = 0;
                CustomerPointAccount pointAccount = new CustomerPointAccount();
                var newOrderID = 0;
                _propertyBag = ExigoDAL.PropertyBags.Update(PropertyBag);
                var isEnrollment = PropertyBag.Cart.CartType == ShopifyApp.CartType.Enrollment;
                try
                {
                    var details = new List<ApiRequest>();
                    var isLocal = Request.IsLocal;

                    // Guest Checkout Customer creation, if user not logged in
                    if (Identity.Customer == null && !PropertyBag.IsEmailOnly)
                    {
                        var address = PropertyBag.ShippingAddress;

                        var createCustomerRequest = new CreateCustomerRequest
                        {
                            FirstName = PropertyBag.Customer.FirstName,
                            LastName = PropertyBag.Customer.LastName,
                            Email = PropertyBag.Customer.Email,
                            Phone = PropertyBag.Customer.PrimaryPhone,
                            EntryDate = DateTime.Now.ToCST(),
                            LanguageID = PropertyBag.Customer.LanguageID,
                            MainAddress1 = PropertyBag.Customer.MainAddress.Address1,
                            MainAddress2 = PropertyBag.Customer.MainAddress.Address2,
                            MainCity = PropertyBag.Customer.MainAddress.City,
                            MainState = PropertyBag.Customer.MainAddress.State,
                            MainZip = PropertyBag.Customer.MainAddress.Zip,
                            MainCountry = PropertyBag.Customer.MainAddress.Country,
                            DefaultWarehouseID = OrderConfiguration.WarehouseID,
                            Field15 = PropertyBag.Customer.Field15,
                            CanLogin = true,
                            InsertEnrollerTree = true,
                            EnrollerID = Identity.Owner.CustomerID,
                            CustomerStatus = CustomerStatuses.Active
                        };
                        createCustomerRequest.LoginPassword = PropertyBag.Customer.Password;
                        createCustomerRequest.LoginName = PropertyBag.Customer.Email;
                        createCustomerRequest.CustomerType = PropertyBag.Cart.CustomerTypeId;
                        details.Add(createCustomerRequest);

                    } 
                    else if(PropertyBag.IsEmailOnly)
                    {
                        PropertyBag.NewCustomerID = ShopifyApp.Services.Exigo.CheckCustomerByEmail(PropertyBag.Customer.Email, tenantConfig);
                    }
                    
                    var cartItems = new ShoppingCartItemCollection();
                    foreach (var item in PropertyBag.Cart.Items.Where(c => c.Type == ShopifyApp.ShoppingCartItemType.Order || c.Type == ShopifyApp.ShoppingCartItemType.EnrollmentPack))
                    {
                        cartItems.Add(new ExigoService.ShoppingCartItem
                        {
                            ItemCode = item.ItemCode,
                            Quantity = item.Quantity,
                            Type = (item.Type == ShopifyApp.ShoppingCartItemType.Order) ? ShoppingCartItemType.Order : ShoppingCartItemType.EnrollmentPack
                        });
                    }
                    foreach (var item in PropertyBag.Cart.Items.Where(c => c.Type == ShopifyApp.ShoppingCartItemType.AutoOrder))
                    {
                        cartItems.Add(new ExigoService.ShoppingCartItem
                        {
                            ItemCode = item.ItemCode,
                            Quantity = item.Quantity,
                            Type = ShoppingCartItemType.AutoOrder
                        });
                    }
                    OrderConfiguration.PriceTypeID = PropertyBag.Cart.PriceTypeID;
                    

                    var orderItems = cartItems.Where(i => i.Type == ShoppingCartItemType.Order || i.Type == ShoppingCartItemType.EnrollmentPack);
                    var hasOrder = orderItems.Any();
                    var autoOrderItems = cartItems.Where(i => i.Type == ShoppingCartItemType.AutoOrder);
                    var hasAutoOrder = autoOrderItems.Any();
                    decimal orderAmount = 0;
                    if (hasOrder)
                    {
                        var calculateOrderRequest = new OrderCalculationRequest
                        {
                            Configuration = OrderConfiguration,
                            Items = orderItems,
                            Address = PropertyBag.ShippingAddress,
                            ShipMethodID = PropertyBag.ShipMethodID,
                            CustomerID = PropertyBag.Customer.CustomerID
                        };

                        var orderTotals = ExigoDAL.CalculateOrder(calculateOrderRequest, tenantConfig.SandBoxId);
                        // Set up variables for use later down the line, if point payments are in play
                        orderAmount = orderTotals.Total;
                        var orderRequest = new CreateOrderRequest(OrderConfiguration, PropertyBag.ShipMethodID, orderItems, PropertyBag.ShippingAddress);
                        orderRequest.PriceType = PropertyBag.Cart.PriceTypeID;
                        if(orderRequest.Details.Where(c => c.ItemCode == "Discount-FO").Any())
                        {
                            var itemprices = ShopifyApp.Services.Exigo.GetItemPrices(PropertyBag.Cart.OrderItems.Select(c => c.Sku).ToList(), (TenantOrderConfiguration)JsonConvert.SerializeObject(OrderConfiguration));
                            decimal other1 = 0; 
                            decimal other2 = 0;
                            foreach(var price in itemprices)
                            {
                                other1 = other1 + (price.OtherPrice1 * PropertyBag.Cart.OrderItems.First(c => c.ItemCode == price.ItemCode).Quantity);
                                other2 = other2 + (price.OtherPrice2 * PropertyBag.Cart.OrderItems.First(c => c.ItemCode == price.ItemCode).Quantity);
                            }
                            var discount = orderRequest.Details.Where(c => c.ItemCode == "Discount-FO").First();
                            discount.BusinessVolumeEachOverride = -((orderTotals.BusinessVolumeTotal * 10) / 100) / discount.Quantity;
                            discount.CommissionableVolumeEachOverride = -((orderTotals.CommissionsableVolumeTotal * 10) / 100) / discount.Quantity;
                            discount.Other1EachOverride = -((other1 * 10) / 100) / discount.Quantity;
                            discount.Other2EachOverride = -((other2 * 10) / 100) / discount.Quantity;
                        }
                        if (PropertyBag.IsEmailOnly)
                            orderRequest.CustomerID = PropertyBag.NewCustomerID;
                        else
                            orderRequest.CustomerID = (Identity.Customer == null) ? 0 : Identity.Customer.CustomerID;
                        details.Add(orderRequest);
                    }
                    pointPaymentAmount = PropertyBag.QuantityOfPointsToUse;
                    regularPaymentAmount = orderAmount - pointPaymentAmount;


                    if (hasAutoOrder && !PropertyBag.AddToExistingAutoOrder)
                    {
                        var autoOrderRequest = new CreateAutoOrderRequest(AutoOrderConfiguration, ExigoDAL.GetAutoOrderPaymentType(PropertyBag.PaymentMethod), PropertyBag.AutoOrderStartDate, PropertyBag.ShipMethodID, autoOrderItems, PropertyBag.AutoOrderShippingAddress)
                        {
                            CustomerID = (Identity.Customer != null) ? Identity.Customer.CustomerID : (PropertyBag.IsEmailOnly) ? PropertyBag.NewCustomerID : 0,
                            Frequency = (FrequencyType)PropertyBag.AutoOrderFrequencyTypeId
                        };
                        details.Add(autoOrderRequest);
                    }

                    // Create the payment request
                    if (regularPaymentAmount > 0 && PropertyBag.PaymentMethod is CreditCard)
                    {
                        var card = PropertyBag.PaymentMethod as CreditCard;
                        if (card.Type == CreditCardType.New)
                        {
                            // Test Credit Card, so no need to charge card
                            if (!card.IsTestCreditCard)
                            {
                                var chargeRequest = new ChargeCreditCardTokenRequest(card);
                                if (PropertyBag.UsePointsAsPayment)
                                {
                                    chargeRequest.MaxAmount = regularPaymentAmount;
                                }
                                details.Add(chargeRequest);
                            }
                            else
                            {
                                // Test Credit Card, so no need to charge card
                                if(hasOrder)
                                {
                                    ((CreateOrderRequest)details.Where(c => c is CreateOrderRequest).FirstOrDefault()).OrderStatus = GlobalUtilities.GetDefaultOrderStatusType();
                                }
                            }
                        }
                        else 
                        {
                            if (orderAmount > 0 && regularPaymentAmount > 0)
                            {
                                var chargeRequest = new ChargeCreditCardTokenOnFileRequest(card);
                                if (PropertyBag.UsePointsAsPayment)
                                {
                                    chargeRequest.MaxAmount = regularPaymentAmount;
                                }
                                details.Add(chargeRequest);
                            }
                        }
                    }


                    // Process the transaction
                    var transactionRequest = new TransactionalRequest();
                    transactionRequest.TransactionRequests = details.ToArray();
                    var transactionResponse = ExigoDAL.WebService(tenantConfig.SandBoxId).ProcessTransaction(transactionRequest);


                    if (transactionResponse.Result.Status == Common.Api.ExigoWebService.ResultStatus.Success)
                    {
                        foreach (var response in transactionResponse.TransactionResponses)
                        {
                            if (response is CreateOrderResponse)
                            {
                                var orderResponse = (CreateOrderResponse)response;
                                newOrderID = orderResponse.OrderID;
                                PropertyBag.NewOrderID = newOrderID;
                                LogCheckout(ShopifyApp.LogType.Success, $"Order {newOrderID} Created in Exigo ");
                            }
                            if (response is CreateAutoOrderResponse)
                            {
                                var orderResponse = (CreateAutoOrderResponse)response;
                                PropertyBag.NewAutoOrderID = orderResponse.AutoOrderID;
                                LogCheckout(ShopifyApp.LogType.Success, $"AutoOrder {orderResponse.AutoOrderID} Created in Exigo ");
                            }
                            if (response is CreateCustomerResponse)
                            {

                                var customerResponse = (CreateCustomerResponse)response;
                                PropertyBag.NewCustomerID = customerResponse.CustomerID;
                                PropertyBag.Customer.CustomerID = PropertyBag.NewCustomerID;
                                var newCustomerId = customerResponse.CustomerID;

                                CreateCustomerInShopify(PropertyBag.NewCustomerID, PropertyBag.Cart.CustomerTypeId);
                                LogCheckout(ShopifyApp.LogType.Success, $"Customer {newCustomerId} Created in Exigo ");
                            }
                        }
                    }
                    
                }
                catch (Exception exception)
                {
                    LogCheckout(ShopifyApp.LogType.Error, $"Something went wrong processing the checkout {PropertyBag.Cart.Token} message: {exception.Message}");
                    PropertyBag.OrderException = exception.Message;
                    PropertyBag.IsSubmitting = false;
                    ExigoDAL.PropertyBags.Update(PropertyBag);

                    return new JsonNetResult(new
                    {
                        success = false,
                        message = exception.Message
                    });
                }

                var customerId = (Identity.Customer == null) ? PropertyBag.NewCustomerID : Identity.Customer.CustomerID;

                //Upgrade Customer
                if (Identity.Customer != null && PropertyBag.Cart.CustomerTypeId > Identity.Customer.CustomerTypeID)
                {
                    try
                    {
                        var upgradeRequest = new UpdateCustomerRequest
                        {
                            CustomerID = customerId,
                            CustomerType = PropertyBag.Cart.CustomerTypeId
                        };
                        if (PropertyBag.Cart.CustomerTypeId == CustomerTypes.Distributor)
                            upgradeRequest.Date1 = DateTime.Now.ToCST();
                        ExigoDAL.WebService().UpdateCustomer(upgradeRequest);

                        LogCheckout(ShopifyApp.LogType.Success, $"Customer {customerId} Upgraded to {PropertyBag.Cart.CustomerTypeId}");
                        await ShopifyApp.Services.CheckoutService.SyncCustomerFromExigo(customerId, PropertyBag.Cart.CustomerTypeId, tenantConfig.Id);

                    }
                    catch (Exception e)
                    {
                        LogCheckout(ShopifyApp.LogType.Error, $"Something went wrong updating customer {customerId} from {Identity.Customer.CustomerTypeID} to {PropertyBag.Cart.CustomerTypeId} , message: {e.Message}");
                    }
                }
                //CustomerSIte
                if (isEnrollment)
                {
                    // Create customer site
                    var customerSiteRequest = new SetCustomerSiteRequest(PropertyBag.Customer);
                    customerSiteRequest.CustomerID = customerId;
                    customerSiteRequest.WebAlias = PropertyBag.Customer.WebAlias;
                    try
                    {
                        ExigoDAL.WebService().SetCustomerSite(customerSiteRequest);
                    }
                    catch (Exception e)
                    {
                        LogCheckout(ShopifyApp.LogType.Error, $"Something went wrong creating CustomerSite {customerId}, message: {e.Message}");
                    }
                }
                if (regularPaymentAmount > 0 && PropertyBag.PaymentMethod is CreditCard)
                {
                    var card = PropertyBag.PaymentMethod as CreditCard;
                    if (card.Type == CreditCardType.New)
                    {

                        try
                        {
                            ExigoDAL.SetCustomerCreditCard(customerId, card);
                        }
                        catch (Exception e)
                        {
                            LogCheckout(ShopifyApp.LogType.Error, $"Something went wrong saving creditcard {customerId}, message: {e.Message}");
                        }
                    }
                }
                ExigoDAL.PropertyBags.Update(PropertyBag);
                return new JsonNetResult(new
                {
                    success = true,
                    redirectUrl = redirectonCompleted
                });
            }
            else
            {
                if (PropertyBag.NewOrderID > 0)
                {
                    return new JsonNetResult(new
                    {
                        success = true
                    });
                }
                else
                {
                    return new JsonNetResult(new
                    {
                        success = false,
                        message = Resources.Common.YourOrderIsSubmitting
                    });
                }
            }
        }

        #region other post requests
        [HttpPost]
        public ActionResult CustomerLookup(string email)
        {
            var customerId = ShopifyApp.Services.Exigo.CheckCustomerByEmail(email, tenantConfig);
            if(customerId != 0 )
            {

                var isEmailOnly = ShopifyApp.Services.Exigo.CheckIfCustomerIsEmailOnly(customerId, tenantConfig);
                if(isEmailOnly)
                {
                    PropertyBag.IsEmailOnly = true;
                    ExigoDAL.PropertyBags.Update(PropertyBag);
                    return new JsonNetResult(new
                    {
                        success = false
                    });
                }
                else
                {
                    var model = new CheckoutAccountModel();
                    model.CustomerID = customerId;
                    model.ShopUrl = PropertyBag.Cart.ShopUrl;
                    model.HasExigoAccount = true;
                    var shopCustomer = new ShopifyApp.Models.Customer().GetByExigoId(customerId);
                    if (shopCustomer != null)
                        model.HasShopAccount = true;
                    return new JsonNetResult(new
                    {
                        success = true,
                        html = this.RenderPartialViewToString("Partials/_AccountModal", model)
                    });
                }
            }
            else
            {
                return new JsonNetResult(new
                {
                    success = false
                });
            }
        }
        [HttpPost]
        public JsonNetResult GetDistributors(string query)
        {
            try
            {
                // assemble a list of customers who match the search criteria
                var enrollerCollection = new List<SearchResult>();
                var isCustomerID = query.CanBeParsedAs<int>();

                var nodeDataRecords = new List<dynamic>();
                if (isCustomerID)
                {
                    using (var context = ExigoDAL.Sql())
                    {
                        nodeDataRecords = context.Query(@"
                                SELECT
                                    cs.CustomerID, cs.FirstName, cs.LastName, cs.WebAlias, c.Company
                                    c.MainCity, c.MainState, c.MainCountry
                                FROM CustomerSites cs
                                INNER JOIN Customers c
                                ON cs.CustomerID = c.CustomerID
                                WHERE c.CustomerTypeID = @customertypeid
                                And ISNULL(cs.Webalias, '') <> ''
                                AND cs.CustomerID = @customerid
                                AND (c.Field2 = 0 or c.Field2 = '')
                        ", new
                        {
                            customertypeid = CustomerTypes.Distributor,
                            customerid = query
                        }).ToList();
                    }
                }
                else
                {
                    using (var context = ExigoDAL.Sql())
                    {
                        nodeDataRecords = context.Query(@"
                                SELECT
                                    c.CustomerID, c.FirstName, c.LastName, cs.WebAlias, c.Company,
                                    c.MainCity, c.MainState, c.MainCountry
                                FROM Customers c
                                LEFT JOIN CustomerSites cs
                                ON cs.CustomerID = c.CustomerID
                                WHERE c.CustomerTypeID = @customertypeid
                                And ISNULL(cs.Webalias, '') <> ''
                                AND (c.Field2 = 0 or c.Field2 = '')
                                AND (c.FirstName LIKE @queryValue OR c.LastName LIKE @queryvalue OR c.Company LIKE @queryValue OR cs.FirstName LIKE @queryValue OR cs.LastName LIKE @queryValue OR c.MainCity LIKE @queryValue or c.MainState LIKE @queryValue)
                        ", new
                        {
                            customertypeid = CustomerTypes.Distributor,
                            queryValue = "%" + query + "%"
                        }).ToList();
                    }
                }

                if (nodeDataRecords.Count() > 0)
                {
                    foreach (var record in nodeDataRecords)
                    {
                        var node = new SearchResult();
                        node.CustomerID = record.CustomerID;
                        node.FirstName = record.FirstName;
                        node.LastName = record.LastName;
                        node.MainCity = record.MainCity;
                        node.MainState = record.MainState;
                        node.MainCountry = record.MainCountry;
                        node.WebAlias = record.WebAlias;
                        enrollerCollection.Add(node);
                    }
                }
                return new JsonNetResult(new
                {
                    success = true,
                    enrollers = enrollerCollection
                });
            }
            catch (Exception ex)
            {
                return new JsonNetResult(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }
        [HttpGet]
        public ActionResult PostCart()
        {
            var host = HttpContext.Request.Url.Host;
            var index = host.IndexOf(".");
            if (index < 0)
            {
                return Redirect("https://olbali-development.myshopify.com" + HttpContext.Request.Url.AbsolutePath + "?ref=corporphan");
            }

            var webalias = host.Substring(0, index);
            return RedirectPermanent("https://olbali-development.myshopify.com" + HttpContext.Request.Url.AbsolutePath + "?ref=" + webalias);
        }
        [HttpPost]
        public ActionResult PostCart(string cart, string countryCode = null, string shopUrl = null, string cultureCode = null, string token = "")
        {
            PropertyBag.NewOrderID = 0;
            PropertyBag.IsSubmitting = false;
            Response.Cookies.Add(SetShopifyUrlCookie(shopUrl));
            try
            {
                PropertyBag.Cart = new Cart(cart);
                if (shopUrl.IsNotNullOrEmpty())
                    PropertyBag.Cart.ShopUrl = shopUrl;
            } 
            catch (Exception e)
            {
                LogCheckout(ShopifyApp.LogType.Error, $"Not able to set up cart {shopUrl} message: {e.Message}");
                return RedirectBackToShopCart(shopUrl);
            }
            if (countryCode.IsNotNullOrEmpty())
                GlobalUtilities.SetSelectedCountryCode(countryCode);
            if (cultureCode.IsNotNullOrEmpty())
                GlobalUtilities.SetCurrentCulture(cultureCode);
            if (token.IsNotNullOrEmpty())
            {
                try
                {
                    //var decryptedToken = Security.Decrypt(token);
                    var exigoCustomerId = 10;// Convert.ToInt32(decryptedToken.ExigoCustomerId);
                    var service = new IdentityService();
                    if(Identity.Customer != null && exigoCustomerId != Identity.Customer.CustomerID)
                    {
                        FormsAuthentication.SignOut();
                        service.SignIn(exigoCustomerId);
                    }
                    else if (Identity.Customer == null && exigoCustomerId != 0)
                    {
                        service.SignIn(exigoCustomerId);
                    }

                    PropertyBag.Customer = ExigoDAL.GetCustomer(exigoCustomerId);
                    if (PropertyBag.Customer.PrimaryPhone.IsNullOrEmpty())
                        if (PropertyBag.Customer.MobilePhone.IsNotNullOrEmpty())
                            PropertyBag.Customer.PrimaryPhone = PropertyBag.Customer.MobilePhone;
                    PropertyBag.Cart.CustomerId = PropertyBag.Customer.CustomerID;
                    if (PropertyBag.Customer.MainAddress.Country != CurrentMarket.CookieValue)
                        GlobalUtilities.SetSelectedCountryCode(PropertyBag.Customer.MainAddress.Country);
                }
                catch (Exception e)
                {
                    LogCheckout(ShopifyApp.LogType.Error, $"Token not valid for logged in customer, message: {e.Message}");
                    return RedirectBackToShopCart(shopUrl);
                }
            }
            else
            {
                if(Identity.Customer != null)
                    FormsAuthentication.SignOut();
                PropertyBag.Customer = new ExigoService.Customer();
            }
            SetCartCustomerType();
            PropertyBag.WebAlias = Identity.Owner.WebAlias;
            PropertyBag.Customer.EnrollerID = Identity.Owner.CustomerID;
            //CustomerTypeItemCleanup();
            CalculateCart();
            return LogicProvider.GetNextAction();
        }
        [HttpPost]
        public ActionResult ChangePointAccountPayment(bool payWithPoints, decimal pointAmount)
        {
            PropertyBag.UsePointsAsPayment = payWithPoints;
            PropertyBag.QuantityOfPointsToUse = (payWithPoints) ? pointAmount : 0;
            PropertyBag.Cart.PointsTotal = (payWithPoints) ? -pointAmount : 0;
            ExigoDAL.PropertyBags.Update(PropertyBag);
            return RedirectToAction("ReturnCartTotals");
        }
        [HttpPost]
        public ActionResult ChangeShipMethod(int shipMethodId)
        {
            PropertyBag.ShipMethodID = shipMethodId;
            ExigoDAL.PropertyBags.Update(PropertyBag);
            return RedirectToAction("ReturnCartTotals");
        }
        #endregion

        #region paymentMethods
        [HttpPost]
        public ActionResult UseCreditCardOnFile(CreditCardType type, string existingcardcvv)
        {
            var paymentMethod = ExigoDAL.GetCustomerPaymentMethods(new GetCustomerPaymentMethodsRequest
            {
                CustomerID = PropertyBag.Customer.CustomerID,
                ExcludeIncompleteMethods = true,
                ExcludeInvalidMethods = true
            }).Where(c => c is CreditCard && ((CreditCard)c).Type == type).FirstOrDefault();

            var card = paymentMethod as CreditCard;

            card.CVV = existingcardcvv;

            return UsePaymentMethod(paymentMethod);
        }
        [HttpPost]
        public ActionResult UseBankAccountOnFile(ExigoService.BankAccountType type)
        {
            var paymentMethod = ExigoDAL.GetCustomerPaymentMethods(new GetCustomerPaymentMethodsRequest
            {
                CustomerID = PropertyBag.Customer.CustomerID,
                ExcludeIncompleteMethods = true,
                ExcludeInvalidMethods = true
            }).Where(c => c is BankAccount && ((BankAccount)c).Type == type).FirstOrDefault();

            return UsePaymentMethod(paymentMethod);
        }

        [HttpPost]
        public ActionResult UseCreditCard(CreditCard newCard, bool billingSameAsShipping = false)
        {
            if (billingSameAsShipping)
            {
                var address = PropertyBag.ShippingAddress;

                newCard.BillingAddress = new ExigoService.Address
                {
                    Address1 = address.Address1,
                    Address2 = address.Address2,
                    City = address.City,
                    State = (address.State.IsNotNullOrEmpty()) ? address.State : address.Country,
                    Zip = address.Zip,
                    Country = address.Country
                };
            }
            else
            {
                newCard.BillingAddress.State = (newCard.BillingAddress.State.IsNotNullOrEmpty()) ? newCard.BillingAddress.State : newCard.BillingAddress.Country;
            }


            // Verify that the card is valid
            if (!newCard.IsValid)
            {
                return new JsonNetResult(new
                {
                    success = false,
                    message = "This card is invalid, please try again"
                });
            }
            else
            {
                return UsePaymentMethod(newCard);
            }
        }

        [HttpPost]
        public ActionResult UseBankAccount(BankAccount newBankAccount, bool billingSameAsShipping = false)
        {
            if (billingSameAsShipping)
            {
                var address = PropertyBag.ShippingAddress;

                newBankAccount.BillingAddress = new ExigoService.Address
                {
                    Address1 = address.Address1,
                    Address2 = address.Address2,
                    City = address.City,
                    State = address.State,
                    Zip = address.Zip,
                    Country = address.Country
                };
            }

            // Verify that the card is valid
            if (!newBankAccount.IsValid)
            {
                return new JsonNetResult(new
                {
                    success = false
                });
            }
            else
            {
                // Save the bank account to the customer's account if applicable                
                var paymentMethodsOnFile = ExigoDAL.GetCustomerPaymentMethods(new GetCustomerPaymentMethodsRequest
                {
                    CustomerID = Identity.Customer.CustomerID,
                    ExcludeIncompleteMethods = true,
                    ExcludeInvalidMethods = true,
                }).Where(c => c is BankAccount).Select(c => c as BankAccount);

                if (paymentMethodsOnFile.FirstOrDefault() == null)
                {
                    ExigoDAL.SetCustomerBankAccount(Identity.Customer.CustomerID, newBankAccount);
                }
            }

            return UsePaymentMethod(newBankAccount);
        }

        [HttpPost]
        public ActionResult UsePaymentMethod(IPaymentMethod paymentMethod)
        {
            try
            {
                PropertyBag.PaymentMethod = paymentMethod;
                ExigoDAL.PropertyBags.Update(PropertyBag);

                return new JsonNetResult(new
                {
                    success = true
                });
            }
            catch (Exception ex)
            {
                return new JsonNetResult(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }
        #endregion
        public ActionResult ReturnCartTotals()
        {
            if(!PropertyBag.Cart.IsPaymentOfExistingOrder)
                CalculateCart(true);
            var autoOrderSubtotal = PropertyBag.Cart.AutoOrderTotal + PropertyBag.Cart.ShippingTotal;
            return new JsonNetResult(new
            {
                success = true,
                orderTotal = PropertyBag.Cart.OrderTotal.ToString("c"),
                shippingTotal = PropertyBag.Cart.ShippingTotal.ToString("c"),
                taxTotal = PropertyBag.Cart.TaxTotal.ToString("c"),
                pointsTotal = PropertyBag.Cart.PointsTotal.ToString("c"),
                autoOrderSubTotal = autoOrderSubtotal.ToString("c")
            });
        }
        private bool CalculateCart(bool calculateOrder = false)
        {
            try
            {
                var hasOrder = PropertyBag.Cart.Items.Where(c => c.Type == ShopifyApp.ShoppingCartItemType.Order || c.Type == ShopifyApp.ShoppingCartItemType.EnrollmentPack).Any();
                PropertyBag.Cart.IsCalculated = false;
                SetPriceType();
                PropertyBag.Cart.CartType = (PropertyBag.Cart.EnrollmentPackItems.Any()) ? ShopifyApp.CartType.Enrollment : ShopifyApp.CartType.Shopping;
                OrderPackConfiguration.PriceTypeID = PropertyBag.Cart.PriceTypeID;
                OrderConfiguration.PriceTypeID = PropertyBag.Cart.PriceTypeID;
                AutoOrderConfiguration.PriceTypeID = PropertyBag.Cart.PriceTypeID;

                if (calculateOrder)
                {
                    var cartItems = new ShoppingCartItemCollection();
                    foreach (var item in PropertyBag.Cart.Items.Where(c => c.Type == ShopifyApp.ShoppingCartItemType.Order || c.Type == ShopifyApp.ShoppingCartItemType.EnrollmentPack))
                    {
                        cartItems.Add(new ExigoService.ShoppingCartItem
                        {
                            ItemCode = item.ItemCode,
                            Quantity = item.Quantity,
                            Type = (item.Type == ShopifyApp.ShoppingCartItemType.Order) ? ShoppingCartItemType.Order : ShoppingCartItemType.EnrollmentPack,
                            PriceTypeID = PropertyBag.Cart.PriceTypeID
                        });
                    }
                    if(!hasOrder)
                    {
                        foreach (var item in PropertyBag.Cart.Items.Where(c => c.Type == ShopifyApp.ShoppingCartItemType.AutoOrder))
                        {
                            cartItems.Add(new ExigoService.ShoppingCartItem
                            {
                                ItemCode = item.ItemCode,
                                Quantity = item.Quantity,
                                Type = ShoppingCartItemType.AutoOrder,
                                PriceTypeID = PropertyBag.Cart.PriceTypeID
                            });
                        }
                    }
                    var orderCalcRequest = new OrderCalculationRequest
                    {
                        CustomerID = (Identity.Customer != null) ? Identity.Customer.CustomerID : 0,
                        Address = (hasOrder) ? PropertyBag.ShippingAddress : PropertyBag.AutoOrderShippingAddress,
                        Configuration = OrderConfiguration,
                        PartyID = PropertyBag.PartyID,
                        Items = cartItems,
                        ShipMethodID = PropertyBag.ShipMethodID,
                        ReturnShipMethods = true,
                        

                    };
                    var orderTotals = ExigoDAL.CalculateOrder(orderCalcRequest);
                    if(orderTotals.ShipMethods != null)
                        PropertyBag.Shipmethods = orderTotals.ShipMethods;
                    else
                    {
                        var shipMethod = new ShipMethod {
                            ShipMethodID = ShopifyApp.Settings.DefaultShipMethodId,
                            Price = 0,
                            Selected = true,
                            ShipMethodDescription = "Standard Shipping"
                        };
                        PropertyBag.Shipmethods = new List<ShipMethod> { shipMethod };
                    }
                    PropertyBag.Cart.ShippingCalculated = true;
                    PropertyBag.Cart.ShippingTotal = orderTotals.Shipping;
                    PropertyBag.Cart.TaxTotal = orderTotals.Tax;
                    if ((PropertyBag.Cart.OrderTotal + PropertyBag.QuantityOfPointsToUse) < orderTotals.Total)
                        PropertyBag.Cart.InternationalShippingFee = orderTotals.Total - PropertyBag.Cart.OrderTotal;
                    PropertyBag.Cart.IsCalculated = true;
                }
                if (PropertyBag.Cart.EnrollmentPackItems.Any())
                {
                    var orderItemsPrices = ShopifyApp.Services.Exigo.GetItemPrices(PropertyBag.Cart.EnrollmentPackItems.Select(c => c.Sku).ToList(), (TenantOrderConfiguration)JsonConvert.SerializeObject(OrderPackConfiguration));

                    foreach (var item in PropertyBag.Cart.EnrollmentPackItems)
                    {
                        var shopItem = orderItemsPrices.FirstOrDefault(c => c.ItemCode == item.Sku);
                        item.Original_Line_Price = shopItem.Price;
                    }
                }
                if (PropertyBag.Cart.OrderItems.Where(c=> !c.IsDiscountItem).Any())
                {
                    var orderItemsPrices = ShopifyApp.Services.Exigo.GetItemPrices(PropertyBag.Cart.OrderItems.Select(c => c.Sku).ToList(), (TenantOrderConfiguration)JsonConvert.SerializeObject(OrderConfiguration));

                    foreach (var item in PropertyBag.Cart.OrderItems.Where(c => !c.IsDiscountItem))
                    {
                        var shopItem = orderItemsPrices.FirstOrDefault(c => c.ItemCode == item.Sku);
                        var itemPrice = shopItem.Price;
                        item.Original_Line_Price = itemPrice;
                    }
                }
                if (PropertyBag.Cart.AutoOrderItems.Any())
                {
                    var orderItemsPrices = ShopifyApp.Services.Exigo.GetItemPrices(PropertyBag.Cart.AutoOrderItems.Select(c => c.Sku).ToList(), (TenantOrderConfiguration)JsonConvert.SerializeObject(AutoOrderConfiguration));

                    foreach (var item in PropertyBag.Cart.AutoOrderItems)
                    {
                        var shopItem = orderItemsPrices.FirstOrDefault(c => c.ItemCode == item.Sku);
                        item.Original_Line_Price = shopItem.Price;
                    }
                }
                CalculateDiscount();
                ExigoDAL.PropertyBags.Update(PropertyBag);
                return true;
            }
            catch (Exception e)
            {
                foreach(var item in PropertyBag.Cart.Items)
                {
                    item.Original_Line_Price = 0;
                }
                LogCheckout(ShopifyApp.LogType.Error, $"Not able to calculate cart: {PropertyBag.Cart.Token} message: {e.Message}");
                ExigoDAL.PropertyBags.Update(PropertyBag);
                return false;
            }
            
        }
        private void recalculateAutoOrder()
        {
            var autoOrder = ExigoDAL.GetCustomerAutoOrders(PropertyBag.Customer.CustomerID, PropertyBag.SelectedAutoOrderId).First();
            foreach(var item in autoOrder.Details)
            {
                if(PropertyBag.Cart.Items.Where(c => c.Type == ShopifyApp.ShoppingCartItemType.AutoOrder && c.ItemCode == item.ItemCode).Any())
                {
                    var original = PropertyBag.Cart.Items.Where(c => c.Type == ShopifyApp.ShoppingCartItemType.AutoOrder && c.ItemCode == item.ItemCode).First().Quantity;
                    var newQuantity = original + item.Quantity;
                    PropertyBag.Cart.Items.Where(c => c.Type == ShopifyApp.ShoppingCartItemType.AutoOrder && c.ItemCode == item.ItemCode).First().Quantity = (int)newQuantity;
                }
                else
                {
                    var newItem = new CartItem();
                    newItem.Properties = new Dictionary<string, string>();
                    newItem.Quantity = (int)item.Quantity;
                    newItem.Sku = item.ItemCode;
                    newItem.Properties.Add("ordertype", "autoorder");
                    newItem.Featured_Image = new CartItem.FeaturedImage() { Url = "" };
                    PropertyBag.Cart.Items.Add(newItem);
                }
            }
        }
        public void UsePointAccount(int newOrderID, decimal pointPaymentAmount, CustomerPointAccount pointAccount)
        {
            while (pointPaymentAmount > 0)
            {
                if (pointAccount.Balance >= pointPaymentAmount)
                {
                    CreatePointAccountPayment(newOrderID, pointAccount.PointAccountID, pointPaymentAmount);
                    pointPaymentAmount = 0;
                }
                else if (pointAccount.Balance > 0 && pointAccount.Balance < pointPaymentAmount)
                {
                    CreatePointAccountPayment(newOrderID, pointAccount.PointAccountID, pointAccount.Balance);
                    pointPaymentAmount = pointPaymentAmount - pointAccount.Balance;
                }

                if (pointPaymentAmount > 0)
                {
                    pointPaymentAmount = 0;
                    //throw new Exception(Resources.Common.PointPaymentError3);
                }
            }
        }
        [HttpPost]
        public JsonNetResult CreatePendingCitconTransaction(string accessToken, string reference)
        {

            return new JsonNetResult(new
            {

            });
        }
        public void CreatePointAccountPayment(int orderID, int PointAccountID, decimal total)
        {
            try
            {
                var pointPaymentRequest = new CreatePaymentPointAccountRequest()
                {
                    OrderID = orderID,
                    PointAccountID = PointAccountID,
                    PaymentDate = DateTime.Now,
                    Amount = total
                };
                var pointPaymentResponse = ExigoDAL.WebService().CreatePaymentPointAccount(pointPaymentRequest);

                ExigoDAL.WebService().UpdateOrder(new UpdateOrderRequest
                {
                    OrderID = orderID,
                    OrderStatus = OrderStatuses.Accepted
                });
            }
            catch (Exception ex)
            {
                //throw new Exception(Resources.Common.PointPaymentError4);
            }
        }
        private HttpCookie SetShopifyUrlCookie(string shopUrl)
        {
            HttpCookie StudentCookies = new HttpCookie("LatestCheckoutShopifyUrl");
            StudentCookies.Value = shopUrl;
            StudentCookies.Expires = DateTime.Now.AddYears(1);
            return StudentCookies;
        }
        private void LogCheckout(ShopifyApp.LogType type, string message)
        {
            new ShopifyApp.Models.Log(type, message, ShopifyApp.LogSection.Checkout, null).Create();
        }
        private async Task<string> CreateShopAccount(ExigoService.Customer customer, int configId)
        {
            try
            {
                var config = new TenantConfiguration().Get(configId);
                var shopify = new ShopifyApp.Services.ShopService.ShopifyDAL(config.Id);
                var isCustomer = new ShopifyApp.Models.Customer().GetByExigoId(customer.CustomerID);
                if(isCustomer == null)
                {
                    var newCustomer = await shopify.CreateCustomer(new ShopifySharp.Customer
                    {
                        FirstName = customer.FirstName,
                        LastName = customer.LastName,
                        Email = customer.Email,
                        VerifiedEmail = true,
                        AcceptsMarketing = customer.IsOptedIn
                    });
                    var created = await shopify.CreateSilentLoginToken(new ShopifyApp.Models.Customer
                    {
                        ExigoCustomerId = customer.CustomerID,
                        ShopCustomerId = newCustomer.Id.Value.ToString()
                    });
                    new ShopifyApp.Models.Customer(config, newCustomer.Id.Value.ToString(), customer.CustomerID, customer.FullName, "Checkout", customer.EnrollerID.Value, true, false).Create();
                    var sync = await ShopifyApp.Services.CheckoutService.SyncCustomerFromExigo(customer.CustomerID, customer.CustomerTypeID, config.Id);
                    LogCheckout(ShopifyApp.LogType.Success, $"Customer created in shopify for {customer.CustomerID} CustomerId: {newCustomer.Id.Value} ");
                    var link = await shopify.GetAccountActivationLink(newCustomer.Id.Value);
                    return link + "?clearcart=true";
                }
                else
                {
                    var sync = await ShopifyApp.Services.CheckoutService.SyncCustomerFromExigo(customer.CustomerID, customer.CustomerTypeID, config.Id);
                    var link = await shopify.GetAccountActivationLink((long)Convert.ToDecimal(isCustomer.ShopCustomerId));
                    return link + "?clearcart=true";
                }
            }
            catch (Exception e)
            {
                LogCheckout(ShopifyApp.LogType.Error, $"Customer not created in Shopify {customer.CustomerID} Created in Exigo, message: {e.Message} ");
                return "";
            }
        }

        public bool CreateCustomerInShopify(int customerId, int customerTypeId = 0)
        {
            using (var client = new HttpClient())
            {
                var data = new StringContent(JsonConvert.SerializeObject(new
                {
                    ExigoCustomerId = customerId,
                    CustomerType = customerTypeId,
                    Email = PropertyBag.Customer.Email,
                    Password = PropertyBag.Customer.Password,
                    FirstName = PropertyBag.Customer.FirstName,
                    LastName = PropertyBag.Customer.LastName,
                    ShopUrl = PropertyBag.Cart.ShopUrl,
                    Address = PropertyBag.Customer.MainAddress
                }));
                data.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                var result = client.PostAsync(ShopifyApp.Settings.PluginApiUrl + "/api/checkout/CreateCustomerShopify", data).Result;
                if (result.IsSuccessStatusCode)
                    return true;
                else
                    return false;
            }
        }
        #region client specific calculations
        private void SetPriceType()
        {
            PropertyBag.Cart.PriceTypeID = PriceTypes.Retail;
        }
        private void CalculateDiscount()
        {
            if (PropertyBag.IsEmailOnly && PropertyBag.Cart.CustomerTypeId == CustomerTypes.RetailCustomer && PropertyBag.Cart.Items.Where(c => c.ItemCode == "Discount-FO").Count() == 0)
            {
                var sum = PropertyBag.Cart.OrderItems.Sum(c => c.Final_Line_Price);
                var discount = sum * 10 / 100;
                PropertyBag.Cart.Items.Add(new CartItem
                {
                    Sku = "Discount-FO",
                    Original_Line_Price = -1,
                    Quantity = (decimal)discount,
                    IsDiscountItem = true,
                    Product_Title = "First Order Discount",
                    Properties = new Dictionary<string, string>()
                });
            }
            else if(PropertyBag.Cart.CustomerTypeId == CustomerTypes.AffiliateCustomer && PropertyBag.Cart.Items.Where(c => c.ItemCode == "Discount-FO").Count() == 0)
            {
                var sum = PropertyBag.Cart.OrderItems.Sum(c => c.Final_Line_Price);
                var discount = sum * 10 / 100;
                PropertyBag.Cart.Items.Add(new CartItem
                {
                    Sku = "Discount-FO",
                    Original_Line_Price = -1,
                    Quantity = (decimal)discount,
                    IsDiscountItem = true,
                    Product_Title = "Referral Discount",
                    Properties = new Dictionary<string, string>()
                });
            }
        }
        private void SetCartCustomerType()
        {
            if (PropertyBag.Cart.EnrollmentPackItems.Any())
                PropertyBag.Cart.CustomerTypeId = CustomerTypes.Distributor;
            else if (PropertyBag.Cart.AutoOrderItems.Count() > 0)
                PropertyBag.Cart.CustomerTypeId = CustomerTypes.PreferredCustomer;
            else if(Identity.Owner.IsAffiliate)
                PropertyBag.Cart.CustomerTypeId = CustomerTypes.AffiliateCustomer;
            else
                PropertyBag.Cart.CustomerTypeId = CustomerTypes.RetailCustomer;
        }
        private void CustomerTypeItemCleanup()
        {
            if (PropertyBag.Customer.CustomerID != 0 && PropertyBag.Customer.CustomerTypeID == CustomerTypes.Distributor)
            {
                var items = PropertyBag.Cart.Items.Where(c => c.Type == ShopifyApp.ShoppingCartItemType.EnrollmentPack).ToList();
                foreach (var item in items)
                {  
                    PropertyBag.Cart.Items.Remove(item);
                }
            }
        }
        #endregion

    }
}