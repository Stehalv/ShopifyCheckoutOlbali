using Common;
using System.Web.Mvc;

namespace ReplicatedSite.Controllers
{
    public class ErrorController : BaseController
    {
        public ActionResult Index(string aspxerrorpath = "/")
        {
            return UnexpectedError(aspxerrorpath);
        }

        public ActionResult UnexpectedError(string aspxerrorpath = "/")
        {
            var host = HttpContext.Request.Url.Host;
            var index = host.IndexOf(".");
            if (index < 0)
            {
                return RedirectPermanent("https://olbali-development.myshopify.com" + aspxerrorpath + "?ref=" + GlobalSettings.ReplicatedSites.DefaultWebAlias);
            }

            var webalias = host.Substring(0, index);
            if (webalias == "olbali-checkout-shopify")
                webalias = GlobalSettings.ReplicatedSites.DefaultWebAlias;
            return RedirectPermanent("https://olbali-development.myshopify.com" + aspxerrorpath + "?ref=" + webalias);
        }
        public ActionResult NotFound(string aspxerrorpath = "/")
        {
            var host = HttpContext.Request.Url.Host;
            var index = host.IndexOf(".");
            if (index < 0)
            {
                return RedirectPermanent("https://olbali-development.myshopify.com" + aspxerrorpath + "?ref=" + GlobalSettings.ReplicatedSites.DefaultWebAlias);
            }

            var webalias = host.Substring(0, index);
            if (webalias == "olbali-checkout-shopify")
                webalias = GlobalSettings.ReplicatedSites.DefaultWebAlias;
            return RedirectPermanent("https://olbali-development.myshopify.com" + aspxerrorpath + "?ref=" + webalias);
        }
        public ActionResult WebAliasRequired()
        {
            return View();
        }
        public ActionResult InvalidWebAlias(string aspxerrorpath = "/")
        {
            var host = HttpContext.Request.Url.Host;
            var index = host.IndexOf(".");
            if (index < 0)
            {
                return RedirectPermanent("https://olbali-development.myshopify.com" + aspxerrorpath + "?ref=" + GlobalSettings.ReplicatedSites.DefaultWebAlias);
            }

            var webalias = host.Substring(0, index);
            if (webalias == "olbali-checkout-shopify")
                webalias = GlobalSettings.ReplicatedSites.DefaultWebAlias;
            return RedirectPermanent("https://olbali-development.myshopify.com" + aspxerrorpath + "?ref=" + webalias);
        }
    }
}
