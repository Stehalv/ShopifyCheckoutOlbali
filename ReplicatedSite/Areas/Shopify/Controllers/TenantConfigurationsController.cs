using System.Threading.Tasks;
using System.Web.Mvc;
using ShopifyApp.Models;
using System.Collections.Generic;
using ShopifyApp.Services.ShopService;
using System.Linq;
using System;
using ShopifyApp;
using ReplicatedSite.Areas.Shopify.Filters;
using ReplicatedSite.Areas.Shopify.ViewModels;

namespace ReplicatedSite.Areas.Shopify.Controllers
{
    [Filters.Authorize]
    [PermissionRequired(UserRole.Admin)]
    public class TenantConfigurationsController : BaseController
    {

        // GET: TenantConfigurations/Details/5
        public async Task<ActionResult> Details(int id)
        {
            TenantConfiguration tenantConfiguration = new TenantConfiguration().Get(id);
            if (tenantConfiguration == null)
            {
                return HttpNotFound();
            }
            var model = new TenantConfigurationViewModel();
            model.Config = tenantConfiguration;
            ViewBag.Id = id;
            return View(model);
        }

        public async Task<ActionResult> Webhooks(int id)
        {
            TenantConfiguration tenantConfiguration = new TenantConfiguration().Get(id);
            if (tenantConfiguration == null)
            {
                return HttpNotFound();
            }
            var model = new TenantConfigurationViewModel();
            var webhooks = new WebHooksViewModel();
            webhooks.ConfigWebhooks = new List<ConfigWebhooks>();
            webhooks.AvailableWebhooks = new AvailableWebhook().GetAll();
            var config = new ConfigWebhooks();
            config.TenantConfigId = tenantConfiguration.Id;
            config.ShopifyUrl = tenantConfiguration.ShopUrl;
            var shop = new ShopifyDAL(tenantConfiguration);
            config.Webhooks = await shop.ListWebhooksAsync();
            webhooks.ConfigWebhooks.Add(config);
            model.Webhooks = webhooks;
            ViewBag.Id = id;
            return View(model);
        }
        public async Task<ActionResult> PriceRules(int id)
        {
            TenantConfiguration tenantConfiguration = new TenantConfiguration().Get(id);
            if (tenantConfiguration == null)
            {
                return HttpNotFound();
            }
            var model = new TenantConfigurationPriceRuleViewModel();
            model.Config = tenantConfiguration;
            model.Rules = await new ShopifyDAL(tenantConfiguration).GetPricerules();
            ViewBag.Id = id;
            return View(model);
        }
        public async Task<ActionResult> Advanced(int id)
        {
            TenantConfiguration tenantConfiguration = new TenantConfiguration().Get(id);
            if (tenantConfiguration == null)
            {
                return HttpNotFound();
            }
            var model = new TenantConfigurationViewModel();
            model.Config = tenantConfiguration;
            ViewBag.Id = id;
            return View(model);
        }
        // GET: TenantConfigurations/Create
        public ActionResult Create()
        {
            var model = new TenantConfiguration();
            return View(model);
        }

        // POST: TenantConfigurations/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind(Include = "Id,IntegrationType,TenantId,ExigoReportingDBConnectionString,ExigoCompanyKey,ExigoApiKey,ExigoApiSecret,UseSandbox,SandBoxId,ShopApiKey,ShopSecret,ShopUrl,LocationId,DefaultEnrollerID,DefaultEnrollerWebAlias")] TenantConfiguration tenantConfiguration)
        {
            tenantConfiguration.CreatedBy = Identity.Current.Id;
            tenantConfiguration.Created = DateTime.Now;
            tenantConfiguration.ModifiedBy = Identity.Current.Id;
            tenantConfiguration.Modified = DateTime.Now;
            if (ModelState.IsValid)
            {
                tenantConfiguration.Create();
                new Log(LogType.Information, $"Client Shop {tenantConfiguration.ShopUrl} Created by {Identity.Current.FullName}").Create();
                try
                {
                    var locations = await new ShopifyDAL(tenantConfiguration).GetLocations();
                    if (tenantConfiguration.LocationId == 0)
                    {
                        tenantConfiguration.LocationId = locations.FirstOrDefault().Id.Value;
                        tenantConfiguration.Update();
                    }
                }
                catch { }
                return RedirectToAction("Index", "Home");
            }

            return View(tenantConfiguration);
        }

        // GET: TenantConfigurations/Edit/5
        public async Task<ActionResult> Edit(int id)
        {
            TenantConfiguration tenantConfiguration = new TenantConfiguration().Get(id);
            var locations = await new ShopifyDAL(tenantConfiguration).GetLocations();
            if(tenantConfiguration.LocationId == 0)
            {
                tenantConfiguration.LocationId = locations.FirstOrDefault().Id.Value;
            }
            if (tenantConfiguration == null)
            {
                return HttpNotFound();
            }
            return View(tenantConfiguration);
        }

        // POST: TenantConfigurations/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind(Include = "Id,IntegrationType,TenantId,ExigoReportingDBConnectionString,ExigoCompanyKey,ExigoApiKey,ExigoApiSecret,UseSandbox,SandBoxId,ShopApiKey,ShopSecret,ShopUrl,LocationId,DefaultEnrollerID,DefaultEnrollerWebAlias,Created,CreatedBy")] TenantConfiguration tenantConfiguration)
        {
            tenantConfiguration.ModifiedBy = Identity.Current.Id;
            tenantConfiguration.Modified = DateTime.Now;
            if (ModelState.IsValid)
            {
                tenantConfiguration.Update();
                new Log(LogType.Information, $"Client Shop {tenantConfiguration.ShopUrl} Modified by {Identity.Current.FullName}").Create();
                return RedirectToAction("Index", "Home");
            }
            return View(tenantConfiguration);
        }

        // GET: TenantConfigurations/Delete/5
        public async Task<ActionResult> Delete(int id)
        {
            TenantConfiguration tenantConfiguration = new TenantConfiguration().Get(id);
            if (tenantConfiguration == null)
            {
                return HttpNotFound();
            }
            return View(tenantConfiguration);
        }

        // POST: TenantConfigurations/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            TenantConfiguration tenantConfiguration = new TenantConfiguration().Get(id);
            tenantConfiguration.Delete();
            new Log(LogType.Information, $"Client Shop {tenantConfiguration.ShopUrl} Deleted by {Identity.Current.FullName}").Create();
            return RedirectToAction("Index");
        }
        public ActionResult CleanInstance(int configId)
        {
            var config = new TenantConfiguration().Get(configId);
            config.CleanInstall();
            return RedirectToAction("Details", new { id = configId });
        }
    }
}
