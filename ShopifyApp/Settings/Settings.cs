using ShopifyApp.Models;
using ShopifyApp.Services;
using ShopifyApp.Services.Jobs;
using ShopifyApp.Services.Scheduler;
using ShopifyApp.Services.ShopService;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Configuration;

namespace ShopifyApp
{
    public static class Settings
    {
        public static string ConnectionString = ConfigurationManager.ConnectionStrings["Api.Sql.ConnectionStrings.SqlReporting"].ConnectionString;
        public static string AppUrl = ConfigurationManager.AppSettings["Company.BaseReplicatedUrl"] + "/Shopify";
        public static string ExigoCompanyKey = ConfigurationManager.AppSettings["Api.CompanyKey"];
        public static string ExigoApiKey = ConfigurationManager.AppSettings["Api.LoginName"];
        public static string ExigoApiSecret = ConfigurationManager.AppSettings["Api.Password"];
        public static bool DeleteProcessedWebhooks = Convert.ToBoolean(GetAppSettings("DeleteProcessedWebhooks"));
        public static int DefaultTenantConfig = Convert.ToInt32(GetAppSettings("DefaultTenantConfig"));
        public static string CompanyName = GetAppSettings("CompanyName");
        public static string DatabaseContext = GetAppSettings("DatabaseContext");
        public static int DefaultEnrollerID = Convert.ToInt32(GetAppSettings("DefaultEnrollerID"));
        public static string DefaultWebalias = GetAppSettings("DefaultWebalias");
        public static string DefaultExigoDiscountItemCode = GetAppSettings("DefaultDiscountCode");
        public static bool SyncFulfillmentFromShopify = Convert.ToBoolean(GetAppSettings("FulfillmentinShopify"));
        public static string TrackingUrl = GetAppSettings("TrackingUrl");
        public static int DefaultCustomerTypeId = Convert.ToInt32(GetAppSettings("DefaultCustomerTypeId"));
        public static int CustomerEmailSignupStatus = CustomerStatuses.Deleted;
        public static int WebhookLogTimeDays = 14;
        public static int WebhookCleanupIntervalHours = 24;
        public static int SyncPricesInterval = 3;
        public static int SyncFulfillmentInterval = Convert.ToInt32(GetAppSettings("SyncFulfillmentInterval"));
        public static int SessionTimeout = 60;
        public static string EncryptionKey = "SDCLKJYAFS654ASF321FP87K";
        public static bool PlaceBinaryTree = false;
        public static bool AllowMultipleAutoOrders = true;
        public static bool AllowCustomerReferral = false;
        public static bool UseShopifyAbandonedCheckouts = true;
        public static bool UseUsernameBlacklist = false;
        public static bool AllowPointPayments = true;
        public static int DefaultShipMethodId = 6;
        public static string BeautyInsiderItemCode = "BI-02";
        public static long DefaultShopCustomerID = 0;
        public static long DefaultExigoCustomerID = 2;
        public static string CitconPaymentToken = "";

        //ENrollment logic
        public static string EnrollmentTermsLink = "https://Testing.com";
        public static string CreateCustomerAccountLink = "";

        //pricetypes logic
        public static bool AutoshipGivesPreferred = true;
        public static bool DistributorRequiresKit = true;
        public static string CompanyCheckoutLogo = "https://olbali.com/FoolproofBody/CompanyLogo/b7b0fd53-93dc-428c-8aaa-185520b9656815-09-2023T11-03-15-68-cropped.png";
        public static string PluginApiUrl = "https://shopplugin.azurewebsites.net";

        public static int GetCustomerPriceType(int customerTypeId)
        {

            switch (customerTypeId)
            {
                case (int)CustomerTypes.RetailCustomer:
                    return (int)PriceTypes.retail;
                case (int)CustomerTypes.PreferredCustomer:
                    return (int)PriceTypes.preferred;
                case (int)CustomerTypes.Distributor:
                    return (int)PriceTypes.wholesale;
                default:
                    return (int)PriceTypes.retail;
            }
        }
        private static string GetAppSettings(string name)
        {
            var config = WebConfigurationManager.OpenWebConfiguration("~/Areas/Shopify");
            return config.AppSettings.Settings[name].Value;
        }
        private static string GetConnectionString(string name)
        {
            var config = WebConfigurationManager.OpenWebConfiguration("~/Areas/Shopify");
            return config.ConnectionStrings.ConnectionStrings[name].ConnectionString;
        }
        public static List<IJobExt> Jobs => new List<IJobExt>
        {
            new PriceSyncJob()
        };
        public static bool DiscountCVonCuponCode = Convert.ToBoolean(ConfigurationManager.AppSettings["DiscountCVonCuponCode"]);

        public static List<DatabaseTable> Tables = new List<DatabaseTable>
                {
                    new DatabaseTable("Customers", false, new Customer()),
                    new DatabaseTable("Logs", false, new Log()),
                    new DatabaseTable("Orders", false, new Order()),
                    new DatabaseTable("Refunds", false, new Refunds()),
                    new DatabaseTable("TenantConfigurations", false, new TenantConfiguration()),
                    new DatabaseTable("TenantOrderConfigurations", false, new TenantOrderConfiguration()),
                    new DatabaseTable("Users", false, new User()),
                    new DatabaseTable("Webhooks", false, new Webhook()),
                    new DatabaseTable("UnfulfilledOrders", false, new UnfulfilledOrder())
                };

        public static List<UserType> UserTypes = new List<UserType>
        {
            new UserType{ Name = UserRole.User.ToString(), Value = Convert.ToInt32(UserRole.User) },
            new UserType{ Name = UserRole.Admin.ToString(), Value = Convert.ToInt32(UserRole.Admin) },
            new UserType{ Name = UserRole.SystemAdmin.ToString(), Value = Convert.ToInt32(UserRole.SystemAdmin) },
            new UserType{ Name = UserRole.App.ToString(), Value = Convert.ToInt32(UserRole.App) }
        };
        public static bool IsAdmin(UserRole role)
        {
            return (role == UserRole.Admin || role == UserRole.SystemAdmin || role == UserRole.App);
        }
        public static bool IsSystemAdmin(UserRole role)
        {
            return (role == UserRole.SystemAdmin);
        }
    }
    public class ShippingCountry
    {
        public string Name { get; set; }
        public string CountryCode { get; set; }
    }
}