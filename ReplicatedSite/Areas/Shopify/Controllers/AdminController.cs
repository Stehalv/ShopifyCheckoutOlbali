using System.Threading.Tasks;
using System.Web.Mvc;
using ShopifyApp;
using ReplicatedSite.Areas.Shopify.Filters;
using ShopifyApp.Services.Jobs;
using System.Linq;

namespace ReplicatedSite.Areas.Shopify.Controllers
{
    [Filters.Authorize]
    [PermissionRequired(UserRole.SystemAdmin)]
    public class AdminController : BaseController
    {
        // GET: Admin
        public ActionResult Index()
        {
            return View(tenant);
        }
        public ActionResult Markets()
        {
            return View(tenant);
        }
        public ActionResult Integrations()
        {
            if (tenant.Configurations.Count() == 1)
                return RedirectToAction("Details", "TenantConfigurations", new { id = tenant.Configurations.First().Id });
            return View(tenant);
        }
        public ActionResult Jobs()
        {
            return View(tenant);
        }
        public async Task<ActionResult> RunJob(string name)
        {
            JobScheduler.RunJob(name);
            return View("index", tenant);
        }
        public async Task<ActionResult> StartJobs()
        {
            JobScheduler.Start();
            return View("index", tenant);
        }
    }
}