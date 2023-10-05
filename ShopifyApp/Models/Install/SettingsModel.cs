using Dapper;
using ShopifyApp.Data;
using ShopifyApp.Models;
using ShopifyApp.Services;
using ShopifyApp.Services.ShopService;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Configuration;

namespace ShopifyApp.Models
{
    public class SettingsModel
    {
        public SettingsModel()
        {
            Get();
        }
        public string ErrorMessage { get; set; }
        public string AppUrl { get; set; }
        //public bool DeleteProcessedWebhooks { get; set; }
        //public int DefaultTenantConfig { get; set; }
        public string CompanyName { get; set; }
        public string DatabaseContext { get; set; }
        //public int DefaultEnrollerID { get; set; }
        //public string DefaultWebalias { get; set; }
        public void Update()
        {
            var configuration = WebConfigurationManager.OpenWebConfiguration("~/Areas/Shopify");
            var conn = (ConnectionStringsSection)configuration.GetSection("connectionStrings");
            var app = (AppSettingsSection)configuration.GetSection("appSettings");
            app.Settings["DatabaseContext"].Value = DatabaseContext;
            app.Settings["CompanyName"].Value = CompanyName;
            //app.Settings["DeleteProcessedWebhooks"].Value = DeleteProcessedWebhooks.ToString();
            //app.Settings["DefaultTenantConfig"].Value = DefaultTenantConfig.ToString();
            //app.Settings["DefaultEnrollerID"].Value = DefaultEnrollerID.ToString();
            //app.Settings["DefaultWebalias"].Value = DefaultWebalias;
            //app.Settings["Shopify.AppUrl"].Value = AppUrl;
            configuration.Save();
        }
        public void Get()
        {  
            var configuration = WebConfigurationManager.OpenWebConfiguration("~/Areas/Shopify");
            var app = (AppSettingsSection)configuration.GetSection("appSettings");
            DatabaseContext = app.Settings["DatabaseContext"].Value;
            CompanyName = app.Settings["CompanyName"].Value;
            //DeleteProcessedWebhooks = Convert.ToBoolean(app.Settings["DeleteProcessedWebhooks"].Value);
            //DefaultTenantConfig = Convert.ToInt32(app.Settings["DefaultTenantConfig"].Value);
            //DefaultEnrollerID = Convert.ToInt32(app.Settings["DefaultEnrollerID"].Value);
            //DefaultWebalias = app.Settings["DefaultWebalias"].Value;
            //AppUrl = app.Settings["Shopify.AppUrl"].Value;
        }
    }
}