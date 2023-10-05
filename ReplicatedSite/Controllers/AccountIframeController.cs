using Common;
using Common.Api.ExigoWebService;
using Common.Services;
using ExigoService;
using ReplicatedSite.Models;
using ReplicatedSite.Services;
using ReplicatedSite.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Web.Mvc;
using System.Web.Security;
using Common.Filters;
using Dapper;
using Serilog;
using Newtonsoft.Json;
using ReplicatedSite.Filters;

namespace ReplicatedSite.Controllers
{
    [AllowAnonymous]
    [RoutePrefix("{webalias}/accountiframe")]
    public class AccountIFrameController : BaseController
    {

        #region Overview
        [Route("settings")]
        [HttpPost]
        public ActionResult GetCustomer()
        {
            // Split the value and get the values
            var exigoCustomerId = CustomerID;
            var model = new AccountSummaryViewModel();
            var customer = Customers.GetCustomer(exigoCustomerId);

            model.Customer = customer;

            return new JsonNetResult(new
            {
                success = true,
                json = model.Customer
            });
        }

        [HttpParamAction]
        public JsonNetResult UpdateEmailAddress(Customer customer)
        {
            ExigoDAL.UpdateCustomer(new UpdateCustomerRequest
            {
                CustomerID = Identity.Customer.CustomerID,
                Email = customer.Email
            });

            ExigoDAL.SendEmailVerification(Identity.Customer.CustomerID, customer.Email);

            var html = string.Format("{0}", customer.Email);

            return new JsonNetResult(new
            {
                success = true,
                action = "UpdateEmailAddress",
                html = html
            });
        }

        [HttpParamAction]
        public JsonNetResult UpdateNotifications(Customer customer)
        {
            var html = string.Empty;

            try
            {
                var token = Security.Encrypt(new
                {
                    CustomerID = Identity.Customer.CustomerID,
                    Email = customer.Email
                });

                if (customer.IsOptedIn)
                {
                    ExigoDAL.SendEmailVerification(Identity.Customer.CustomerID, customer.Email);
                    html = string.Format("{0}", Resources.Common.PendingOptedInStatus);
                }
                else
                {
                    ExigoDAL.OptOutCustomer(Identity.Customer.CustomerID);
                    html = string.Format("{0}", Resources.Common.OptedOutStatus);
                }
            }
            catch
            {

            }

            return new JsonNetResult(new
            {
                success = true,
                action = "UpdateNotifications",
                html = html
            });
        }

        [HttpParamAction]
        public JsonNetResult UpdateName(Customer customer)
        {
            ExigoDAL.UpdateCustomer(new UpdateCustomerRequest
            {
                CustomerID = Identity.Customer.CustomerID,
                FirstName = customer.FirstName,
                LastName = customer.LastName
            });

            var html = string.Format("{0} {1}, {3}# {2}", customer.FirstName, customer.LastName, Identity.Customer.CustomerID, Resources.Common.ID);

            return new JsonNetResult(new
            {
                success = true,
                action = "UpdateName",
                html = html
            });
        }

        [HttpParamAction]
        public JsonNetResult UpdateLoginName(Customer customer)
        {
            ExigoDAL.UpdateCustomer(new UpdateCustomerRequest
            {
                CustomerID = Identity.Customer.CustomerID,
                LoginName = customer.LoginName
            });

            var html = string.Format("{0}", customer.LoginName);

            return new JsonNetResult(new
            {
                success = true,
                action = "UpdateLoginName",
                html = html
            });
        }
        public JsonResult IsValidLoginName(string loginname)
        {
            var isValid = ExigoDAL.IsLoginNameAvailable(loginname, Identity.Customer.CustomerID);

            if (isValid) return Json(true, JsonRequestBehavior.AllowGet);
            else return Json(string.Format(Resources.Common.LoginNameNotAvailable, loginname), JsonRequestBehavior.AllowGet);
        }

        [HttpParamAction]
        public JsonNetResult UpdatePassword(string password)
        {
            ExigoDAL.UpdateCustomer(new UpdateCustomerRequest
            {
                CustomerID = Identity.Customer.CustomerID,
                LoginPassword = password
            });

            var html = "********";

            return new JsonNetResult(new
            {
                success = true,
                action = "UpdatePassword",
                html = html
            });
        }

        [HttpParamAction]
        public JsonNetResult UpdateLanguagePreference(Customer customer)
        {
            var language = GlobalUtilities.GetLanguage(customer.LanguageID, CurrentMarket);

            ExigoDAL.SetCustomerPreferredLanguage(Identity.Customer.CustomerID, language.LanguageID);

            GlobalUtilities.SetCurrentUICulture(language.CultureCode);

            var html = language.LanguageDescription;

            // Refresh the identity in case the country changed
            new IdentityService().RefreshIdentity();

            return new JsonNetResult(new
            {
                success = true,
                action = "UpdateLanguagePreference",
                html = html
            });
        }

        [HttpParamAction]
        public JsonNetResult UpdatePhoneNumbers(Customer customer)
        {
            ExigoDAL.UpdateCustomer(new UpdateCustomerRequest
            {
                CustomerID = Identity.Customer.CustomerID,
                Phone = customer.PrimaryPhone,
                Phone2 = customer.SecondaryPhone
            });

            var html = string.Format(@"
                " + Resources.Common.Primary + @": <strong>{0}</strong><br />
                " + Resources.Common.Secondary + @": <strong>{1}</strong>
                ", customer.PrimaryPhone, customer.SecondaryPhone);

            return new JsonNetResult(new
            {
                success = true,
                action = "UpdatePhoneNumbers",
                html = html
            });
        }
        #endregion

        #region Addresses
        [Route("addresses")]
        public ActionResult AddressList()
        {
            var model = Customers.GetCustomerAddresses(Identity.Customer.CustomerID).Where(c => c.IsComplete).ToList();

            return View(model);
        }

        [Route("addresses/edit/{type:alpha}")]
        public ActionResult ManageAddress(AddressType type)
        {
            var model = Customers.GetCustomerAddresses(Identity.Customer.CustomerID).Where(c => c.AddressType == type).FirstOrDefault();

            return View("ManageAddress", model);
        }

        [Route("addresses/new")]
        public ActionResult AddAddress()
        {
            var model = new Address();
            model.AddressType = AddressType.New;
            model.Country = CurrentMarket.MainCountry;

            return View("ManageAddress", model);
        }

        [Route("deleteaddress")]
        public ActionResult DeleteAddress(AddressType type)
        {
            ExigoDAL.DeleteCustomerAddress(Identity.Customer.CustomerID, type);

            return RedirectToAction("AddressList");
        }

        [Route("setprimaryaddress")]
        public ActionResult SetPrimaryAddress(AddressType type)
        {
            ExigoDAL.SetCustomerPrimaryAddress(Identity.Customer.CustomerID, type);

            return RedirectToAction("AddressList");
        }

        [Route("saveaddress")]
        [HttpPost]
        public ActionResult SaveAddress(Address address, bool? makePrimary)
        {
            address = ExigoDAL.SetCustomerAddressOnFile(Identity.Customer.CustomerID, address);

            if (makePrimary != null && ((bool)makePrimary) == true)
            {
                ExigoDAL.SetCustomerPrimaryAddress(Identity.Customer.CustomerID, address.AddressType);
            }

            return RedirectToAction("AddressList");
        }
        #endregion

        #region Payment Methods
        [Route("paymentmethods")]
        public ActionResult PaymentMethodList()
        {
            var model = ExigoDAL.GetCustomerPaymentMethods(new GetCustomerPaymentMethodsRequest
            {
                CustomerID = Identity.Customer.CustomerID,
                ExcludeIncompleteMethods = true
            });

            return View(model);
        }

        #region Credit Cards
        [Route("paymentmethods/creditcards/edit/{type:alpha}")]
        public ActionResult ManageCreditCard(CreditCardType type)
        {
            var model = ExigoDAL.GetCustomerPaymentMethods(Identity.Customer.CustomerID)
                .Where(c => c is CreditCard && ((CreditCard)c).Type == type)
                .FirstOrDefault();

            // Clear out the card number
            ((CreditCard)model).CardNumber = "";

            return View("ManageCreditCard", model);
        }

        [Route("paymentmethods/creditcards/new")]
        public ActionResult AddCreditCard()
        {
            var model = new CreditCard();
            model.Type = CreditCardType.New;
            model.BillingAddress = new Address()
            {
                Country = CurrentMarket.MainCountry
            };

            return View("ManageCreditCard", model);
        }

        [Route("deletecreditcard")]
        public ActionResult DeleteCreditCard(CreditCardType type)
        {
            ExigoDAL.DeleteCustomerCreditCard(Identity.Customer.CustomerID, type);

            return RedirectToAction("PaymentMethodList");
        }

        [Route("savecreditcard")]
        [HttpPost]
        public ActionResult SaveCreditCard(CreditCard card)
        {
            card = ExigoDAL.SetCustomerCreditCard(Identity.Customer.CustomerID, card);

            return RedirectToAction("PaymentMethodList");
        }
        #endregion

        #region Bank Accounts
        [Route("paymentmethods/bankaccounts/edit/{type:alpha}")]
        public ActionResult ManageBankAccount(ExigoService.BankAccountType type)
        {
            var model = ExigoDAL.GetCustomerPaymentMethods(Identity.Customer.CustomerID)
                .Where(c => c is BankAccount && ((BankAccount)c).Type == type)
                .FirstOrDefault();


            // Clear out the account number
            ((BankAccount)model).AccountNumber = "";


            return View("ManageBankAccount", model);
        }

        [Route("paymentmethods/bankaccounts/new")]
        public ActionResult AddBankAccount()
        {
            var model = new BankAccount();
            model.Type = ExigoService.BankAccountType.New;
            model.BillingAddress = new Address()
            {
                Country = Identity.Customer.Market.MainCountry
            };

            return View("ManageBankAccount", model);
        }

        [Route("deletebankaccount")]
        public ActionResult DeleteBankAccount(ExigoService.BankAccountType type)
        {
            ExigoDAL.DeleteCustomerBankAccount(Identity.Customer.CustomerID, type);

            return RedirectToAction("PaymentMethodList");
        }

        [Route("savebankaccount")]
        [HttpPost]
        public ActionResult SaveBankAccount(BankAccount account)
        {
            account = ExigoDAL.SetCustomerBankAccount(Identity.Customer.CustomerID, account);

            return RedirectToAction("PaymentMethodList");
        }
        #endregion

        #endregion

        #region Models and Enums

        public class SearchResult
        {
            public int CustomerID { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string FullName
            {
                get { return this.FirstName + " " + this.LastName; }
            }
            public string AvatarURL { get; set; }
            public string WebAlias { get; set; }
            public string ReplicatedSiteUrl
            {
                get
                {
                    if (string.IsNullOrEmpty(this.WebAlias)) return "";
                    else return GlobalSettings.ReplicatedSites.GetFormattedUrl(WebAlias);
                }
            }

            public string MainState { get; set; }
            public string MainCity { get; set; }
            public string MainCountry { get; set; }
        }

        #endregion

        #region Point Account Transactions
        [Route("point-account-transactions")]
        public ActionResult PointAccountTransactions()
        {
            var model = new PointAccountTransactionsViewModel();
            model.PointAccountDescription = Resources.Common.PointAccount_01;
            var customerid = Identity.Customer.CustomerID;

            using (var context = ExigoDAL.Sql())
            {
                model.Transactions = context.Query<PointAccountTransaction>(@"
                    SELECT  PointTransactionID
                        , TransactionDate
                        , Amount AS Points
                        , Reference AS Reason
                    FROM PointTransactions
                    WHERE CustomerID = @customerid
                        AND PointAccountID = @pointAccountID
                    ORDER BY TransactionDate DESC
            ", new
                {
                    customerid,
                    pointAccountID = PointAccounts.BeautyInsiderPoints
                }).ToList();
            }

            if (model.Transactions.Count() > 0)
            {
                model.PointAccountBalance = model.Transactions.Sum(t => t.Points);
            }

            return View(model);
        }
        #endregion
    }
}