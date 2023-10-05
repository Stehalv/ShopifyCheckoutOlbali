using Dapper;
using ShopifyApp.Data;
using ShopifyApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using ShopifyApp.Api.ExigoWebservice;

namespace ShopifyApp.Services
{
    public static partial class Exigo
    {

        private static Tenant tenant;
        public static ExigoApi WebService(TenantConfiguration tenantConfig)
        {
            return CreateWebServiceContext(tenantConfig);
        }
        public static ExigoApi WebServiceNoConfig()
        {
            tenant = new Tenant(true);
            var urlFormat = "https://{0}.exigo.com/3.0/ExigoApi.asx";
            var cname = $"{tenant.ExigoCompanyKey}-api";

            string url = string.Format(urlFormat, cname);
            // Create the context

            return new ExigoApi
            {
                ApiAuthenticationValue = new ApiAuthentication
                {
                    LoginName = tenant.ExigoApiKey,
                    Password = tenant.ExigoApiSecret,
                    Company = tenant.ExigoCompanyKey
                },
                Url = url
            };
        }
        private static ExigoApi CreateWebServiceContext(TenantConfiguration tenantConfig)
        {
            // Determine which URL we should use
            var url = GetWebServiceUrl(tenantConfig);
            // Create the context
            return new ExigoApi
            {
                ApiAuthenticationValue = new ApiAuthentication
                {
                    LoginName = tenant.ExigoApiKey,
                    Password = tenant.ExigoApiSecret,
                    Company = tenant.ExigoCompanyKey
                },
                Url = url
            };
        }

        private static string GetWebServiceUrl(TenantConfiguration tenantConfig)
        {
            tenant = new Tenant(true);
            var urlFormat = "https://{0}.exigo.com/3.0/ExigoApi.asmx";
            var cname = $"{tenant.ExigoCompanyKey}-api";

            if (tenantConfig.UseSandbox && tenantConfig.SandBoxId > 0)
            {
                cname = "sandboxapi" + tenantConfig.SandBoxId;
            }

            return string.Format(urlFormat, cname);
        }

    }
}