using ReplicatedSite.Areas.Shopify.ViewModels;
using ShopifyApp.Models;
using ShopifyApp.Services;
using ShopifyApp.Services.ShopService;
using ShopifySharp.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace ReplicatedSite.Areas.Shopify.Controllers
{
    [Filters.Authorize]
    public class ProductController : Controller
    {
        // GET: Product
        public async Task<ActionResult> Index(int tenantConfigId, string pageInfo = null, int pageSize = 40)
        {
            var filter = new ListFilter<ShopifySharp.Product>(pageInfo, pageSize);
            var result = await new ShopifyDAL(tenantConfigId).GetProductListResult(filter);
            var model = new ProductListViewModel(); 
            model.Items = result.Items.ToList();
            if (result.HasNextPage)
                model.NextFilter = result.GetNextPageFilter().PageInfo;
            if (result.HasPreviousPage)
                model.PreviousFilter = result.GetPreviousPageFilter().PageInfo;
            ViewBag.ConfigId = tenantConfigId;
            return View(model);
        }
        public async Task<ActionResult> ProductDetails(long id, int configId)
        {
            var config = new TenantConfiguration().Get(configId);
            var model = new AppProduct(id, config);
            await model.GetByShopifyId();
            return View(model);
        }
        public async Task<ActionResult> SyncAllShopPricing(int id)
        {
            var result = await new SyncService().SyncAllProductPrices(id);
            return RedirectToAction("Index", "Product", new { tenantConfigId = id });
        }
    }
}