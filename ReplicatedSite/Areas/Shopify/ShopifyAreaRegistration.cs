using System.Web.Mvc;

namespace ReplicatedSite.Areas.Shopify
{
    public class ShopifyRegistration : AreaRegistration 
    {
        public override string AreaName 
        {
            get 
            {
                return "Shopify";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context) 
        {
            context.MapRoute(
                "Shopify_default",
                "Shopify/{controller}/{action}/{id}",
                new { action = "Index", controller = "home", id = UrlParameter.Optional },
                new string[] { "ReplicatedSite.Areas.Shopify.Controllers" }
            );
        }
    }
}