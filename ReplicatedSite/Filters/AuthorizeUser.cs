using Common.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
    public class AuthorizedReadAttribute : System.Web.Http.Filters.ActionFilterAttribute
    {
        public AuthorizedReadAttribute()
        { }
        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            var userToken = actionContext.Request.Headers.GetValues("token");
            var decryptedToken = Security.Decrypt(userToken.First());
            var requestCustomerId = actionContext.ActionArguments["customerId"];
            actionContext.Request.Properties.Add(new KeyValuePair<string, object>("ExigoCustomerId", decryptedToken.ExigoCustomerId));
            if (decryptedToken.ShopCustomerId.Value != requestCustomerId.ToString())
                actionContext.Response = new HttpResponseMessage(HttpStatusCode.Unauthorized);
            try
            {
                var siteToken = actionContext.Request.Headers.GetValues("sitetoken");
                var decryptedSiteToken = Security.Decrypt(siteToken.First());
                var config = new ShopifyApp.Models.TenantConfiguration().GetByDomain(decryptedSiteToken.shopUrl.Value);
                actionContext.Request.Properties.Add(new KeyValuePair<string, object>("TenantConfiguration", config));
            }
            catch
            {
                actionContext.Response = new HttpResponseMessage(HttpStatusCode.Unauthorized);
            }
        }
    }
}