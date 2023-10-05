using ShopifyApp.Services.Jobs;
using ShopifyApp.Services.Scheduler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ShopifyApp.Models
{
    public class Tenant
    { 
        public Tenant(bool install = false)
        {
            if(!install)
            {
                Configurations = new TenantConfiguration().GetAll();
                OrderConfigurations = new TenantOrderConfiguration().GetAll();
            }
        }
        public string ExigoCompanyKey => Settings.ExigoCompanyKey;
        public string ExigoApiKey => Settings.ExigoApiKey;
        public string ExigoApiSecret => Settings.ExigoApiSecret;
        public List<TenantConfiguration> Configurations { get; set; }
        public List<TenantOrderConfiguration> OrderConfigurations { get; set; }
        public string ExigoReportingDBConnectionString => Settings.ConnectionString;
    }
}