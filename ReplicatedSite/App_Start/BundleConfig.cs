using Common.Bundles;
using System.Web.Optimization;

namespace ReplicatedSite
{
    public class BundleConfig
    {
        public static void RegisterBundles(BundleCollection bundles)
        {
            // Enable bundling optimizations, even when the site is in debug mode or local.
            BundleTable.EnableOptimizations = true;


            // Bundle the Handlebars plugins
            var handlebarsScripts = new ScriptBundle("~/bundles/scripts/handlebars");
            handlebarsScripts.Include(
                "~/Content/scripts/vendor/handlebars.js",
                "~/Content/scripts/vendor/handlebars.extended.js");
            handlebarsScripts.Orderer = new NonOrderingBundleOrderer();

            bundles.Add(handlebarsScripts);



            // Bundle the vendor's styles
            var vendorStyles = new StyleBundle("~/bundles/styles/vendor");   
            bundles.Add(vendorStyles);
            bundles.Add(new ScriptBundle("~/Areas/Shopify/bundles/jquery").Include(
                        "~/Areas/Shopify/Scripts/jquery-{version}.js"));

            bundles.Add(new ScriptBundle("~/Areas/Shopify/bundles/jqueryval").Include(
                        "~/Areas/Shopify/Scripts/jquery.validate*"));

            // Use the development version of Modernizr to develop with and learn from. Then, when you're
            // ready for production, use the build tool at https://modernizr.com to pick only the tests you need.
            bundles.Add(new ScriptBundle("~/Areas/Shopify/bundles/modernizr").Include(
                        "~/Areas/Shopify/Scripts/modernizr-*"));

            bundles.Add(new ScriptBundle("~/Areas/Shopify/bundles/bootstrap").Include(
                      "~/Areas/Shopify/Scripts/bootstrap.js"));

            bundles.Add(new StyleBundle("~/Areas/Shopify/Content/css").Include(
                      "~/Areas/Shopify/Content/bootstrap.css",
                      "~/Areas/Shopify/Content/site.css"));

        }
    }
}