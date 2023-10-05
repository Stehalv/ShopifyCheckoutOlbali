
using ShopifyApp.Models;
using System.Linq;
using System.Web.Mvc;

namespace ReplicatedSite.Areas.Shopify.Controllers
{
    [Filters.Authorize]
    public class LogController : BaseController
    {
        // GET: Log
        public ActionResult Index()
        {
            return View(new Log().GetAll());
        }
        public ActionResult ClearLog()
        {
            new Log().DeleteAll();
            return RedirectToAction("Index");
        }
    }
}