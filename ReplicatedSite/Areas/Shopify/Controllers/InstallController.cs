using ShopifyApp.Models;
using ShopifyApp.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ShopifyApp;

namespace ReplicatedSite.Areas.Shopify.Controllers
{
    [Filters.Authorize]
    public class InstallController : Controller
    {
        // GET: Install
        public ActionResult Index(string message = null)
        {
            var model = new SettingsModel();
            model.ErrorMessage = message;
            return View(model);
        }
        [HttpPost]
        public ActionResult UpdateConnectionString(SettingsModel model)
        {
            var _model = new SettingsModel();
            if (!model.DatabaseContext.Contains("."))
            {
                model.DatabaseContext += ".";
            }
            _model.CompanyName = model.CompanyName;
            _model.Update();
            return RedirectToAction("DataBaseSetup");
        }
        public ActionResult DatabaseSetup()
        {
            //if(!InstallService.CheckApiConnection())
            //{
            //    return RedirectToAction("Index", new { message = "Could not establish API connection, please verify credentials" });
            //}
            var model = new DatabaseSetup();
            var userCount = new User().GetAll().Count();
            if(userCount != 0)
            {
                model.AdminUserCreated = true;
            }
            model.Get();
            return View(model);
        }
        public ActionResult CreateEntity(string name)
        {
            var model = new DatabaseSetup();
            model.CreateEntity(name);
            if (name.ToLower() == "users")
                CreateAdminUser();
            return RedirectToAction("DatabaseSetup");
        }
        public ActionResult CreateAdminUser()
        {

            var user = new User
            {
                FirstName = "Admin",
                LastName = "Admin",
                UserRole = 3,
                Email = "steinar@teqnavi.com",
                Password = ShopifyApp.Extensions.Encrypt("2Wofrik"),
                CreatedBy = 0,
                ModifiedBy = 0
            };
            user.Create();
            return RedirectToAction("index");
        }
    }
}