
using ShopifyApp.Models;
using System.Linq;
using System.Web.Mvc;
using System.Web;
using System;
using ShopifyApp;

namespace ReplicatedSite.Areas.Shopify.Controllers
{
    public class BaseController : Controller
    {
        public Tenant tenant = new Tenant();
    }
}