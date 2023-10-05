using Common.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Mvc;
using System.Web.Routing;


namespace ReplicatedSite.Filters
{
    public class AuthorizedRequestAttribute : System.Web.Http.Filters.ActionFilterAttribute
    {
        public AuthorizedRequestAttribute()
        { }
        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            try
            {
                var siteToken = actionContext.Request.Headers.GetValues("sitetoken");
                var decryptedSiteToken = Security.Decrypt(siteToken.First());
                var config = new ShopifyApp.Models.TenantConfiguration().GetByDomain(decryptedSiteToken.shopUrl.Value);
                actionContext.Request.Properties.Add(new KeyValuePair<string, object>("TenantConfiguration", config));
            }
            catch
            {
                base.OnActionExecuting(actionContext);
            }
        }
    }
}