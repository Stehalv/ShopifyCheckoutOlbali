using Newtonsoft.Json.Serialization;
using System.Linq;
using System.Web.Http;
using System.Web.Http.Cors;

namespace ReplicatedSite
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            //EnableCrossSiteRequests(config);
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{action}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );
        }

        private static void EnableCrossSiteRequests(HttpConfiguration config)
        {
            var cors = new EnableCorsAttribute(
                origins: "*",
                headers: "Content-Type, Authorization, X-Requested-With, Token",
                methods: "GET, POST, PUT, DELETE, OPTIONS");
            config.EnableCors(cors);
        }
    }

    /// <summary>
    /// Resolves property names returned in JSON responses to be formatted in lowercase.
    /// </summary>
    internal class LowerCasePropertyNamesContractResolver : DefaultContractResolver
    {
        public LowerCasePropertyNamesContractResolver()
            : base()
        {
        }
        protected override string ResolvePropertyName(string propertyName)
        {
            return propertyName.ToLowerInvariant();
        }
    }
}
