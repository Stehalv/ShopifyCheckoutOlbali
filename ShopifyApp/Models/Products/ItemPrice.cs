using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ShopifyApp.Models
{
    public class ItemPrice
    {
        public string ItemCode { get; set; }
        public int PriceTypeId { get; set; }
        public string CurrencyCode { get; set; }
        public decimal Price { get; set; }
        public decimal BusinessVolume { get; set; }
        public decimal CommissionableVolume { get; set; }
        public decimal BV { get { return BusinessVolume; } }
        public decimal CV { get { return CommissionableVolume; } }
        public decimal QV { get { return Other2Price; } }
        public decimal TaxablePrice { get; set; }
        public decimal ShippingPrice { get; set; }
        public decimal Other1Price { get; set; }
        public decimal Other2Price { get; set; }
        public decimal Other3Price { get; set; }
        public decimal Other4Price { get; set; }
        public decimal Other5Price { get; set; }
        public decimal Other6Price { get; set; }
        public decimal Other7Price { get; set; }
        public decimal Other8Price { get; set; }
        public decimal Other9Price { get; set; }
        public decimal Other10Price { get; set; }
    }
}