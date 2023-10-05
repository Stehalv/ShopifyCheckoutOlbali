
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using ShopifyApp.Models;
using ReplicatedSite.Areas.Shopify.ViewModels;
using System;
using ShopifyApp.Data;
using Dapper;
using Common.Api.ExigoWebService;
using Common;

namespace ReplicatedSite.Areas.Shopify.Controllers
{
    [Filters.Authorize]
    public class CustomerController : BaseController
    {
        // GET: Customer
        public ActionResult Index(int currentPage = 1, int pageSize = 50, int total = 0)
        {
            var customer = new Customer();
            var model = new CustomersViewModel();
            model.Tenant = tenant;
            model.CurrentPage = currentPage;
            model.PageSize = pageSize;
            model.Total = (total == 0) ? customer.CustomerCount() : total;
            model.StartRowNumber = (currentPage - 1) * pageSize + 1;
            model.EndRowNumber = (currentPage * pageSize > model.Total) ? model.Total : currentPage * pageSize;
            model.Customers = customer.LoadPaginatedCustomers(currentPage, pageSize, model.StartRowNumber, model.EndRowNumber);
            return View(model);
        }
        public ActionResult Search(string query = null)
        {
            var customer = new Customer();
            var model = new CustomersViewModel();
            model.Customers = customer.Search(query);
            model.Total = model.Customers.Count();
            return View("Index", model);
        }
        public async Task<ActionResult> CustomerDetails(int id)
        {
            if(id != 0)
            {
                var model = new CustomerDetailViewModel(id);
                await model.Populate();
                return View(model);
            }
            var _model = new CustomerDetailViewModel();
            return View(_model);
        }
        public async Task<ActionResult> CustomerDetailsExigoId(int id, int tenantConfigId)
        {
            var newId = new Customer().GetCustomerIDByExigoCustomerId(id, tenantConfigId);
            if (newId == 0 && tenantConfigId != 0)
            {
                var order = new Order().GetByExigoCustomerId(id, tenantConfigId).FirstOrDefault();
                var model = new CustomerDetailViewModel();
                await model.PopulateExigoId(order.ExigoCustomerId, tenantConfigId);
                return View("CustomerDetails", model);
            }
            return RedirectToAction("CustomerDetails", new { id = newId});
        }
        public async Task<bool> UpdateCustomer(int customerId, int tenantConfigID = 20002)
        {
            var customer = new Customer().GetById(customerId);
            await customer.CreateSilentLoginToken();
            return await customer.UpdateSHopify();
        }
        public async Task<bool> createSilentLoginTokenByShopifyId(long id)
        {
            var shopify = new ShopifyApp.Services.ShopService.ShopifyDAL(20002);
            var exigoCustomerID = new Customer().GetByShopId(id.ToString(), false).ExigoCustomerId;
            var created = await shopify.CreateSilentLoginToken(new ShopifyApp.Models.Customer
            {
                ExigoCustomerId = exigoCustomerID,
                ShopCustomerId = id.ToString()
            });
            return true;
        }
        public async Task<int> SyncActivationLinks(int configId)
        {
            int count = 0;
            using (var sql = SQLContext.Sql())
            {

                var customers = sql.Query<int>(@"select CustomerID from customers 
                    where field14 = '' and 
                    customertypeid in (2,3)
                ").ToList();
                foreach(var id in customers)
                {
                    var result = await ActivateAccount(id, configId);
                    if(result)
                    {
                        count++;
                    }
                }
            }
            return count;
        }
        public async Task<bool> ActivateAccount(int id, int configId)
        {
            var customer = ExigoService.ExigoDAL.GetCustomer(id);
            var config = new TenantConfiguration().Get(configId);
            var shopify = new ShopifyApp.Services.ShopService.ShopifyDAL(config.Id);
            string link;
            try
            {
                var isCustomer = new ShopifyApp.Models.Customer().GetByExigoId(customer.CustomerID);
                if (isCustomer == null)
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
                    new Customer(config, newCustomer.Id.Value.ToString(), customer.CustomerID, customer.FullName, "Checkout", customer.EnrollerID.Value, true, false).Create();
                    var sync = await ShopifyApp.Services.CheckoutService.SyncCustomerFromExigo(customer.CustomerID, customer.CustomerTypeID, config.Id);
                    link = await shopify.GetAccountActivationLink(newCustomer.Id.Value);

                }
                else
                {
                    var sync = await ShopifyApp.Services.CheckoutService.SyncCustomerFromExigo(customer.CustomerID, customer.CustomerTypeID, config.Id);
                    link = await shopify.GetAccountActivationLink((long)Convert.ToDecimal(isCustomer.ShopCustomerId));
                }
                ExigoService.ExigoDAL.WebService().UpdateCustomer(new UpdateCustomerRequest { 
                    CustomerID = customer.CustomerID,
                    Field14 = link
                });
                return true;
            }
            catch (Exception e)
            {
                new Log(ShopifyApp.LogType.Information, $"Link not created error: {e.Message}", ShopifyApp.LogSection.Global, "APP").Create();
                return false;
            }
        }

        public ActionResult SendActivationEmailToUser()
        {
            var count = 0;
            var errors = "";
            var delivered = "";

            using (var sql = SQLContext.Sql())
            {
                var customers = sql.Query<ExigoService.Customer>($"Select * from Customers where customertypeid in (2,3)").ToList();
                foreach(var customer in customers)
                {
                    try
                    {
                        var text = $@"<p>We are excited to launch a new shopping experience for Lorde + Belle and RealHer! To integrate these two stores we have an entirely new(and improved!) shopping experience.To start shopping, click the link below and set your new password.Moving forward your log in will be your email address and the new password you set using the link below.</p>
                                <a href ='{customer.Field14}'> Activate account </a>
                                <p>If you already have logged in to your account on this new shopping experience, you can ignore this email.</p>
                                <p><strong>If you click this link again after you’ve already logged in, you will get an error message. You can just log in, or press forgot password if you don’t remember what password you entered on activation.</strong></p>";
                        var request = new ExigoService.SendEmailRequest
                        {
                            Subject = $"Login to Lorde + Belle’s New Shopping Experience!",
                            Body = text,
                            To = new string[] { customer.Email },
                            ReplyTo = new string[] { GlobalSettings.Company.Email },
                            From = GlobalSettings.Company.Email,
                            IsHtml = true,
                            UseExigoApi = true
                        };
                        ExigoService.ExigoDAL.SendEmail(request);
                        delivered += customer.CustomerID.ToString() + ",";
                        count++;
                    }
                    catch
                    {
                        errors += customer.CustomerID.ToString() + ",";
                    }
                }

            }
            return new JsonNetResult(new
            {
                success = true,
                count = count,
                errors = errors,
                delivered = delivered
            });
        }
    }
}