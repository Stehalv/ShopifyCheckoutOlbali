﻿using Common.Routes;
using System.Web.Mvc;
using System.Web.Mvc.Routing;
using System.Web.Routing;

namespace ReplicatedSite
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");
            routes.IgnoreRoute("bundles/{*pathInfo}");
            routes.IgnoreRoute("content/{*pathInfo}");
            routes.IgnoreRoute("scripts/{*pathInfo}");
            routes.IgnoreRoute("areas/shopify/bundles/{*pathInfo}");
            routes.IgnoreRoute("areas/shopify/content/{*pathInfo}");
            routes.IgnoreRoute("areas/shopify/scripts/{*pathInfo}");


            // Attribute routing
            var constraintsResolver = new DefaultInlineConstraintResolver();
            constraintsResolver.ConstraintMap.Add("hasroutevalue", typeof(RouteValuePresentConstraint));
            constraintsResolver.ConstraintMap.Add("values", typeof(ValuesConstraint));
            routes.MapMvcAttributeRoutes(constraintsResolver);
            
            // Standard routing
            routes.MapRouteLowerCase(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { action = "index", id = UrlParameter.Optional },
                constraints: new { controller = @"(app|dev|error)" }
            );
            routes.MapRouteLowerCase(
                name: "Replicated",
                url: "{webalias}/{controller}/{action}/{id}",
                defaults: new { webalias = ShopifyApp.Settings.DefaultWebalias, controller = "shopifycheckout", action = "postcart", id = UrlParameter.Optional },
                namespaces: new string[] { "ReplicatedSite.Controllers" },
                constraints: new { controller = @"(shopifycheckout)" }
            );
            routes.Add(new SubdomainRoute());
        }
    }
}