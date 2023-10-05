using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace ReplicatedSite
{
    public class SubdomainRoute : RouteBase
    {
        public override RouteData GetRouteData(HttpContextBase httpContext)
        {
            if (httpContext.Request == null || httpContext.Request.Url == null)
            {
                return null;
            }
            var host = httpContext.Request.Url.Host;
            var index = host.IndexOf(".");
            string[] segments = httpContext.Request.Url.PathAndQuery.TrimStart('/').Split('/');
            var subdomain = GlobalSettings.ReplicatedSites.DefaultWebAlias;
            string controller = "shopifycheckout";
            string action = "PostCart";
            if (index < 0)
            {
                subdomain = GlobalSettings.ReplicatedSites.DefaultWebAlias;
            }
            else
            {
                subdomain = host.Substring(0, index);
            }
            if (subdomain == "olbali-checkout-shopify" || host == "localhost")
            {
                subdomain = segments[0];
                controller = (segments.Length > 1) ? segments[1] : "shopifycheckout";
                action = (segments.Length > 2) ? segments[2] : "PostCart";
            }
            string[] blacklist = {};

            if (blacklist.Contains(subdomain))
            {
                subdomain = GlobalSettings.ReplicatedSites.DefaultWebAlias;
            }

            var routeData = new RouteData(this, new MvcRouteHandler());
            routeData.Values.Add("controller", controller); //Goes to the relevant Controller  class
            routeData.Values.Add("action", action); //Goes to the relevant action method on the specified Controller
            routeData.Values.Add("webalias", subdomain); //pass subdomain as argument to action method
            return routeData;
        }

        public override VirtualPathData GetVirtualPath(RequestContext requestContext, RouteValueDictionary values)
        {
            //Implement your formating Url formating here
            return null;
        }
    }
}