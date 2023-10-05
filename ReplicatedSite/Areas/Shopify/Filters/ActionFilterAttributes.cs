
using ShopifyApp;
using System.Web.Mvc;

namespace ReplicatedSite.Areas.Shopify.Filters
{
    public class AllowCrossSiteJsonAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            filterContext.RequestContext.HttpContext.Response.AddHeader("Access-Control-Allow-Origin", "*");
            base.OnActionExecuting(filterContext);
        }
    }
    public class PermissionRequiredAttribute : ActionFilterAttribute
    {
        private readonly UserRole _role;
        public PermissionRequiredAttribute(UserRole role)
        {
            _role = role;
        }
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            UrlHelper url = new UrlHelper(filterContext.RequestContext);
            if (_role == UserRole.SystemAdmin && !ShopifyApp.Settings.IsSystemAdmin(Identity.Current.UserRole))
            {
                filterContext.HttpContext.Response.Redirect(url.Action("Index", "Home"), true);
            }
            if (_role == UserRole.Admin && !ShopifyApp.Settings.IsAdmin(Identity.Current.UserRole))
            {
                filterContext.HttpContext.Response.Redirect(url.Action("Index", "Home"), true);
            }
            base.OnActionExecuting(filterContext);
        }
    }
}