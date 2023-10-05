using Dapper;
using ShopifyApp.Data;
using ShopifyApp.Models;
using ShopifyApp.Services;
using ShopifyApp.Services.ShopService;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Web;
using System.Web.Configuration;

namespace ShopifyApp.Models
{
    public class DatabaseSetup
    {
        public DatabaseSetup()
        {
            Get(); 
        }
        public List<DatabaseTable> Tables { get; set; }
        public string DatabaseContext { get; set; }
        public bool AdminUserCreated { get; set; }
        public bool whitelisted { get; set; }
        public string IP { get; set; }
        public void Update()
        {
            Get();
            if(Tables.Where(c => c.Exists).Count() == 0)
            {
                InstallService.CreateContext(DatabaseContext);
            }
        }
        public void Get()
        {
            var configuration = WebConfigurationManager.OpenWebConfiguration("~/Areas/Shopify/");
            var app = (AppSettingsSection)configuration.GetSection("appSettings");
            DatabaseContext = app.Settings["DatabaseContext"].Value;
            try
            {
                Tables = InstallService.CheckTables(DatabaseContext);
                whitelisted = true;
            }
            catch
            {
                whitelisted = false; 
                var host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        IP = ip.ToString();
                    }
                }
            }
        }
        public void CreateEntity(string name)
        {
            InstallService.CreateEntity(name);
        }
    }
}