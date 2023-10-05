using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using ShopifyApp.Models;
using System;
using ShopifyApp;
using ReplicatedSite.Areas.Shopify.Filters;

namespace ReplicatedSite.Areas.Shopify.Controllers
{
    [Filters.Authorize]
    [PermissionRequired(UserRole.Admin)]
    public class TenantOrderConfigurationsController : BaseController
    {

        // GET: TenantOrderConfigurations/Create
        public ActionResult Create()
        {
            var model = new TenantOrderConfiguration();
            return View(model);
        }

        // POST: TenantOrderConfigurations/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind(Include = "Id,TenantId,ConnectionString,WarehouseID,PriceTypeID,CurrencyCode,LanguageID,DefaultCountryCode,DefaultShipMethodID,CategoryID,FeaturedCategoryID")] TenantOrderConfiguration tenantOrderConfiguration)
        {
            tenantOrderConfiguration.CreatedBy = Identity.Current.Id;
            tenantOrderConfiguration.Created = DateTime.Now;
            tenantOrderConfiguration.ModifiedBy = Identity.Current.Id;
            tenantOrderConfiguration.Modified = DateTime.Now;
            if (ModelState.IsValid)
            {
                tenantOrderConfiguration.Create();
                new Log(LogType.Information, $"Client Market {tenantOrderConfiguration.Id} Created by {Identity.Current.FullName}").Create();
                return RedirectToAction("Index", "Home");
            }

            return View(tenantOrderConfiguration);
        }

        // GET: TenantOrderConfigurations/Edit/5
        public async Task<ActionResult> Edit(int id)
        {
            TenantOrderConfiguration tenantOrderConfiguration = new TenantOrderConfiguration().Get(id);
            if (tenantOrderConfiguration == null)
            {
                return HttpNotFound();
            }
            return View(tenantOrderConfiguration);
        }

        // POST: TenantOrderConfigurations/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind(Include = "Id,TenantId,ConnectionString,WarehouseID,PriceTypeID,CurrencyCode,LanguageID,DefaultCountryCode,DefaultShipMethodID,CategoryID,FeaturedCategoryID,Created,CreatedBy")] TenantOrderConfiguration tenantOrderConfiguration)
        {
            tenantOrderConfiguration.ModifiedBy = Identity.Current.Id;
            tenantOrderConfiguration.Modified = DateTime.Now;
            if (ModelState.IsValid)
            {
                tenantOrderConfiguration.Update();
                new Log(LogType.Information, $"Client Market {tenantOrderConfiguration.Id} Modified by {Identity.Current.FullName}").Create();
                return RedirectToAction("Index", "Home");
            }
            return View(tenantOrderConfiguration);
        }

        // GET: TenantOrderConfigurations/Delete/5
        public async Task<ActionResult> Delete(int id)
        {
            TenantOrderConfiguration tenantOrderConfiguration = new TenantOrderConfiguration().Get(id);
            if (tenantOrderConfiguration == null)
            {
                return HttpNotFound();
            }
            return View(tenantOrderConfiguration);
        }

        // POST: TenantOrderConfigurations/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            TenantOrderConfiguration tenantOrderConfiguration = new TenantOrderConfiguration().Get(id);
            tenantOrderConfiguration.Delete();
            new Log(LogType.Information, $"Client Market {tenantOrderConfiguration.Id} Deleted by {Identity.Current.FullName}").Create();
            return RedirectToAction("Index");
        }
    }
}
