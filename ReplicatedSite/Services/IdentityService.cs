﻿using System;
using System.Linq;
using System.Web;
using System.Web.Security;
using ReplicatedSite.Models;
using Common;
using ExigoService;
using Common.Services;
using Dapper;
using ReplicatedSite.Providers;
using ShopifyApp.Models;
using ShopifyApp;
using System.Web.Mvc;
using System.Security.Principal;

namespace ReplicatedSite.Services
{
    public class IdentityService
    {        
        readonly IReplicatedSiteIdentityAuthenticationProvider _authProvider = new SqlIdentityAuthenticationProvider();        

        public IdentityService() { }
        public IdentityService(IReplicatedSiteIdentityAuthenticationProvider authProvider)
        {
            _authProvider = authProvider;
        }


        // Owner Identities
        public ReplicatedSiteIdentity GetIdentity(string webAlias)
        {
            webAlias = webAlias.ToUpper();
            var cacheKey = string.Format("{0}-OwnerIdentity-{1}", GlobalSettings.Exigo.Api.CompanyKey, webAlias);
            var identity = HttpContext.Current.Cache[cacheKey] as ReplicatedSiteIdentity;

            if (identity == null)
            {
                try
                {
                    identity = _authProvider.GetSiteOwnerIdentity(webAlias);

                    // Save the identity
                    HttpContext.Current.Cache.Insert(cacheKey,
                        identity,
                        null,
                        DateTime.Now.AddMinutes(GlobalSettings.ReplicatedSites.IdentityRefreshInterval),
                        System.Web.Caching.Cache.NoSlidingExpiration,
                        System.Web.Caching.CacheItemPriority.Normal,
                        null);
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("Default user missing"))
                    {
                        throw ex;
                    }
                    return null;
                }
            }

            return identity;
        }


        public LoginResponse SignIn(string loginname, string password)
        {
            var response = new LoginResponse();

            try
            {
                // Authenticate the customer
                var customerID = _authProvider.AuthenticateCustomer(loginname, password);
                if (customerID == 0)
                {
                    response.Fail("Unable to authenticate");
                    return response;
                }

                // Get the customer
                var identity = GetIdentity(customerID);
                if (identity == null)
                {
                    response.Fail("Customer not found");
                    return response;
                }

                // Get the redirect URL (for silent logins) or create the forms ticket
                response.RedirectUrl = GetSilentLoginRedirect(identity);
                if (response.RedirectUrl.IsEmpty()) CreateFormsAuthenticationTicket(customerID);

                // Mark the response as successful
                response.Success();
            }
            catch (Exception ex)
            {
                response.Fail(ex.Message);
            }

            return response;
        }
        // Customer Identities
        public LoginResponse SignInApp(string loginname, string password)
        {
            var response = new LoginResponse();
            int userId = 0;
            try
            {
                User user = null;
                // Authenticate the customer
                var _password = ShopifyApp.Extensions.Encrypt(password);
                user = new User().GetByUserNameAndPassword(loginname, _password);
                if (user == null)
                {
                    response.Fail("Unable to authenticate");
                    return response;
                }
                else
                {
                    userId = user.Id;
                }

                // Get the customer 

                var identity = GetAppIdentity(userId);
                if (identity == null)
                {
                    response.Fail("User not found");
                    return response;
                }

                CreateFormsAuthenticationTicket(userId, true);
                // Mark the response as successful
                response.Success();
            }

            catch (Exception ex)
            {
                response.Fail(ex.Message);
            }

            return response;
        }
        public LoginResponse SignIn(int customerid, bool noredirect = false, string shop = null)
        {
            var response = new LoginResponse();

            try
            {
                // Authenticate the customer
                var customerID = _authProvider.AuthenticateCustomer(customerid);
                if (customerID == 0)
                {
                    response.Fail("Unable to authenticate");
                    return response;
                }

                // Get the customer
                var identity = GetIdentity(customerID);
                if (identity == null)
                {
                    response.Fail("Customer not found");
                    return response;
                }

                CreateFormsAuthenticationTicket(customerID, false, shop);

                // Mark the response as successful
                response.Success();
            }
            catch (Exception ex)
            {
                response.Fail(ex.Message);
            }

            return response;
        }
        public LoginResponse SignIn(string silentLoginToken)
        {
            var response = new LoginResponse();

            try
            {
                // Decrypt the token
                var decryptedToken = Security.Decrypt(silentLoginToken);

                // Split the value and get the values
                var customerID = Convert.ToInt32(decryptedToken.CustomerID);
                var tokenExpirationDate = Convert.ToDateTime(decryptedToken.ExpirationDate);

                // Return the expiration status of the token and the sign in response
                if (tokenExpirationDate < DateTime.Now)
                {
                    response.Fail("Token expired");
                    return response;
                }

                // Sign the customer in with their customer ID
                response = SignIn(customerID);

                // Mark the response as successful
                response.Success();
            }
            catch (Exception ex)
            {
                response.Fail(ex.Message);
            }

            return response;
        }
        public LoginResponse SignInShopify(string silentLoginToken, string shop)
        {
            var response = new LoginResponse();

            try
            {
                // Decrypt the token
                var decryptedToken = Security.Decrypt(silentLoginToken);

                // Split the value and get the values
                var exigoCustomerId = Convert.ToInt32(decryptedToken.ExigoCustomerId);

                // Sign the customer in with their customer ID
                response = SignIn(exigoCustomerId, true, shop);

                // Mark the response as successful
                response.Success();
            }
            catch (Exception ex)
            {
                response.Fail(ex.Message);
            }

            return response;
        }
        public LoginResponse AdminSilentLogin(string token)
        {
            var response = new LoginResponse();

            try
            {
                // Decrypt the token
                var IV = GlobalSettings.EncryptionKeys.SilentLogins.IV;
                var key = GlobalSettings.EncryptionKeys.SilentLogins.Key;
                var decryptedToken = Security.AESDecrypt(token, key, IV);

                // Split the value and get the values
                var splitToken = decryptedToken.Split('|');
                var customerID = Convert.ToInt32(splitToken[0]);
                var tokenExpirationDate = Convert.ToDateTime(splitToken[1]);

                // Return the expiration status of the token and the sign in response
                if (tokenExpirationDate < DateTime.Now)
                {
                    response.Fail("Token expired");
                    return response;
                }

                // Sign the customer in with their customer ID
                response = SignIn(customerID);

                // Mark the response as successful
                response.Success();
            }
            catch (Exception ex)
            {
                response.Fail(ex.Message);
            }

            return response;
        }
        public void SignOut()
        {
            FormsAuthentication.SignOut();
        }

        public void RefreshIdentity()
        {
            CreateFormsAuthenticationTicket(Identity.Customer.CustomerID);
        }

        public CustomerIdentity GetIdentity(int customerID)
        {
            return _authProvider.GetCustomerIdentity(customerID);
        }
        public CustomerIdentity GetAppIdentity(int userId)
        {
            dynamic identity = null;
            try
            {
                var user = new User().Get(userId);
                identity = new CustomerIdentity(user);
            }
            catch (Exception ex)
            {
                new Log(LogType.Error, $"Login Error UserId: {userId} Error Getting Identity: {ex.Message}").Create();
                return null;
            }

            return identity;
        }
        public string GetSilentLoginRedirect(CustomerIdentity identity, string shop = null)
        {
            System.Collections.Generic.List<int> distributorTypes = new System.Collections.Generic.List<int>
	        {
		        Common.CustomerTypes.Distributor		       
	        };

            if (distributorTypes.Contains(identity.CustomerTypeID) && identity.CustomerStatusID == Common.CustomerStatuses.Active)
            {
                var token = Security.Encrypt(new
                {
                    CustomerID = identity.CustomerID,
                    ExpirationDate = DateTime.Now.AddHours(1)
                });
                var redirectUrl = GlobalSettings.Backoffices.SilentLogins.DistributorBackofficeUrl.FormatWith(token);
                if (shop != null)
                    redirectUrl += "&shop=" + shop;
                return redirectUrl;
            }

            return string.Empty;
        }
        public bool CreateFormsAuthenticationTicket(int customerID, bool isApp = false, string shop = null)
        {
            // If we got here, we are authorized. Let's attempt to get the identity.
            if(!isApp)
            {
                var identity = GetIdentity(customerID) as CustomerIdentity;
                if (shop != null)
                    identity.Shop = shop;
                else
                {
                    try
                    {
                        identity.Shop = new TenantConfiguration().Get(new ShopifyApp.Models.Customer().GetByExigoId(identity.CustomerID).TenantConfigId).ShopUrl;
                    }
                    catch { }
                }
                if (identity == null) return false; // Create the ticket
                FormsAuthenticationTicket ticket = new FormsAuthenticationTicket(
                    1,
                    identity.CustomerID.ToString(),
                    DateTime.Now,
                    DateTime.Now.AddMinutes(GlobalSettings.ReplicatedSites.SessionTimeout),
                    false,
                    identity.SerializeProperties());


                // Encrypt the ticket
                string encTicket = FormsAuthentication.Encrypt(ticket);


                // Create the cookie.
                HttpCookie cookie = HttpContext.Current.Request.Cookies[FormsAuthentication.FormsCookieName]; //saved user
                if (cookie == null)
                {
                    cookie = new HttpCookie(FormsAuthentication.FormsCookieName, encTicket);
                    cookie.HttpOnly = true;
                    cookie.SameSite = SameSiteMode.None;
                    cookie.Secure = true;

                    HttpContext.Current.Response.Cookies.Add(cookie);
                }
                else
                {
                    cookie.Value = encTicket;
                    HttpContext.Current.Response.Cookies.Set(cookie);
                }


                // Add the customer ID to the items in case we need this in the same request later on.
                // We need this because we don't have access to the Identity.Current in this same request later on.
                HttpContext.Current.Items.Add("CustomerID", customerID);
            }
            else
            {
                var identity = GetAppIdentity(customerID);

                if (identity == null) return false; // Create the ticket
                FormsAuthenticationTicket ticket = new FormsAuthenticationTicket(
                    1,
                    identity.Id.ToString(),
                    DateTime.Now,
                    DateTime.Now.AddMinutes(GlobalSettings.ReplicatedSites.SessionTimeout),
                    false,
                    identity.SerializeProperties());


                // Encrypt the ticket
                string encTicket = FormsAuthentication.Encrypt(ticket);


                // Create the cookie.
                HttpCookie cookie = HttpContext.Current.Request.Cookies[FormsAuthentication.FormsCookieName]; //saved user
                if (cookie == null)
                {
                    cookie = new HttpCookie(FormsAuthentication.FormsCookieName, encTicket);
                    cookie.HttpOnly = true;

                    HttpContext.Current.Response.Cookies.Add(cookie);
                }
                else
                {
                    cookie.Value = encTicket;
                    HttpContext.Current.Response.Cookies.Set(cookie);
                }


                // Add the customer ID to the items in case we need this in the same request later on.
                // We need this because we don't have access to the Identity.Current in this same request later on.
                HttpContext.Current.Items.Add("CustomerID", customerID);
            }



           


            return true;
        }
    }
}