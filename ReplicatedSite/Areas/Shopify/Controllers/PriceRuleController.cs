using ReplicatedSite.Areas.Shopify.ViewModels;
using ShopifyApp.Models;
using ShopifyApp.Services.ShopService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace ReplicatedSite.Areas.Shopify.Controllers
{
    public class PriceRuleController : Controller
    {
        // GET: Shopify/PriceRule
        public ActionResult Index()
        {
            return View();
        }
        public async Task<ActionResult> Create(int tenantConfigId)
        {
            var model = new ShopifyApp.Models.PriceRuleModel();
            model.TenantConfigId = tenantConfigId;
            model.SavedSearches = await new ShopifyDAL(tenantConfigId).GetCustomerSavedSearches();
            return View(model);
        }
        [HttpPost]
        public async Task<ActionResult> Create(PriceRuleModel model)
        {
            var tenantConfig = new TenantConfiguration().Get(model.TenantConfigId);
            var rule = (ShopifySharp.PriceRule)model;
            rule.StartsAt = DateTime.Now;
            await new ShopifyDAL(tenantConfig).CreatePriceRule(rule);
            return RedirectToAction("PriceRules", "TenantConfigurations", new { id = model.TenantConfigId });
        }
    }
}