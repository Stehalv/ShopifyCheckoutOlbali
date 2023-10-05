using System;
using System.Web.Mvc;

namespace ReplicatedSite.Areas.Shopify.Filters
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = true)]
    public class AuthorizeAttribute : System.Web.Mvc.AuthorizeAttribute
    {
         public override void OnAuthorization(AuthorizationContext filterContext)
         {
             if (filterContext.HttpContext.Request.IsAuthenticated)
             {
                 var identity = filterContext.HttpContext.User.Identity as CustomerIdentity;
                 if (identity == null || identity.Id == 0)
                 {
                     base.OnAuthorization(filterContext);
                     return;
                 }
             }
             else
             {
                 base.OnAuthorization(filterContext);
             }
         }
    }
}