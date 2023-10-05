using ShopifyApp.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;

namespace ShopifyApp.Services
{
    public static class CustomService
    {
        public static int SetPricetype(SyncOrderObject order, TenantOrderConfiguration orderConfiguration)
        {
            return orderConfiguration.PriceTypeID;
        }
        public static decimal TaxRateOverride(IEnumerable<ShopifySharp.TaxLine> taxLines)
        {
            if (taxLines.Any())
            {
                decimal rate = taxLines.Sum(c => c.Rate.Value);
                return rate * 100;
            }
            else
                return 0;
        }

        public static Item CalculatevolumeDiscount(string sku, ShopifySharp.DiscountApplication code, TenantOrderConfiguration config)
        {
            var item = Exigo.GetItemVolumes(sku, config);
            if(item != null)
            {
                if (code.Type == "percentage")
                {;
                    var amount = decimal.Parse(code.Value, CultureInfo.InvariantCulture);
                    var overrideBV = item.BV - (item.BV * (amount / 100));
                    var overrideCV = item.CV - (item.CV * (amount / 100));
                    item.BV = (decimal)overrideBV;
                    item.CV = (decimal)overrideCV;
                }
            }
            return item;
        }

        public static int NewCustomerWithOrderTypeID(SyncOrderObject order)
        {
            return Settings.DefaultCustomerTypeId;
        }
        public static int NewCustomerWithoutOrderTypeID(ShopifySharp.Customer customer)
        {
            return Settings.DefaultCustomerTypeId;
        }
    }
}