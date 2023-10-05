using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ShopifyApp.Models
{
    public class ShopTemplateSettings
    {
        public string CookieName { get; set; }
        public string CartTokenCookieName { get; set; }
        public string DefaultWebalias { get; set; }
        public string ReferralQueryString { get; set; }
        public string AppUrl { get; set; }
        public bool UseShopifyBackoffice { get; set; }
        public string BackofficeUrl { get; set; }
    }
}