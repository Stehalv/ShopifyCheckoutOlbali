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

namespace ReplicatedSite.Controllers
{
    [Authorize]
    [RoutePrefix("{webalias}/account")]
    public class AccountController : BaseController
    {

        [Route("silentlogin")]
        [AllowAnonymous]
        public ActionResult SilentLogin(string token)
        {
            var service = new IdentityService();
            var response = service.SignIn(token);

            if (response.Status)
            {
                return RedirectToAction("Index");
            }
            else
            {
                return RedirectToAction("Login");
            }
        }
        [Route("silentloginShopify")]
        [AllowAnonymous]
        public ActionResult SilentLoginShopify(string token, string carttoken = null, string returnUrl = null, string shop = null)
        {
            var service = new IdentityService();
            var response = service.SignInShopify(token, shop);

            if (response.Status)
            {
                if (returnUrl.IsNullOrEmpty())
                {
                    if (response.RedirectUrl.IsNotNullOrEmpty())
                    {
                        return Redirect(response.RedirectUrl);
                    }
                    else if (carttoken.IsNotNullOrEmpty())
                        return RedirectToAction("index", "ShopifyCheckout", new { cartToken = carttoken });
                    return RedirectToAction("Index", "AccountIFrame");
                }
                else
                    return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction("Login");
            }
        }

        [Route("~/adminlogin")]
        [AllowAnonymous]
        public ActionResult AdminLogin(string token)
        {
            var service = new IdentityService();
            var response = service.AdminSilentLogin(token);

            if (response.Status)
            {
                if (response.RedirectUrl.IsNullOrEmpty())
                {
                    return RedirectToAction("Index", "Account");
                }
                else
                {
                    return Redirect(response.RedirectUrl);
                }
            }
            else
            {
                return RedirectToAction("Login");
            }
        }
    }
}