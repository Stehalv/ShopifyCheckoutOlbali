
using ShopifyApp.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using ShopifyApp;
using ReplicatedSite.ViewModels;
using ReplicatedSite.Services;
using System.Web.Security;

namespace ReplicatedSite.Areas.Shopify.Controllers
{
    [Filters.Authorize]
    public class HomeController : BaseController
    {
        public ActionResult Index()
        {
            return View(tenant);
        }

        [AllowAnonymous]
        public ActionResult Login()
        {
            var model = new LoginViewModel();

            return View(model);
        }

        [AllowAnonymous]
        [HttpPost]
        public ActionResult Login(LoginViewModel model)
        {
            var service = new IdentityService();
            var response = service.SignInApp(model.LoginName, model.LoginPassword);
            if (!response.Status)
            {
                return RedirectToAction("Login");
            }
            UrlHelper u = new UrlHelper(HttpContext.Request.RequestContext);
            var url = u.Action("Index", "Home", new { area = "Shopify" });
            return Redirect(url);
        }
        public ActionResult LogOut()
        {
            FormsAuthentication.SignOut();
            return RedirectToAction("Login");
        }
    }
}