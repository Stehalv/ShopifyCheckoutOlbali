using System;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System.Net.Http;
using System.Net;
using System.Web.Mvc;
using ReplicatedSite.Areas.Shopify.Filters;
using ShopifyApp.Models;
using ShopifyApp.Services.ShopService;
using ShopifyApp;
using ReplicatedSite.Areas.Shopify.ViewModels;
using Common;

namespace ReplicatedSite.Areas.Shopify.Controllers
{
    [Filters.Authorize]
    public class WebHookController : BaseController
    {
        private ShopifyDAL shop;
        public WebHookController()
        {
        }
        #region Webhook logs
        [PermissionRequired(UserRole.Admin)]
        public ActionResult WebhookBacklog(WebhookStatus status = WebhookStatus.Saved)
        {
            return View(new Webhook().GetAll((WebhookStatus)status));
        }
        [PermissionRequired(UserRole.Admin)]
        public async Task<ActionResult> WebhookDetail(string webhookId)
        {
            var model = new WebhookViewModel(webhookId);
            return View(model);
        }
        #endregion

        #region setup

        [PermissionRequired(UserRole.Admin)]
        public async Task<ActionResult> CreateWebhook(string topic, int tenantConfigId)
        {
            var config = new TenantConfiguration().Get(tenantConfigId);
            shop = new ShopifyDAL(config);
            await Task.Run(() => shop.CreateWebHook(topic));
            return RedirectToAction("Details", "TenantConfigurations", new { id = tenantConfigId });
        }
        #endregion
    }
}
