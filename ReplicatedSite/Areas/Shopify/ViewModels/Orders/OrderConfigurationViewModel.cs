using ShopifyApp.Models;
using ShopifyApp.Services;
using System.Collections.Generic;
using System.Web.Mvc;

namespace ReplicatedSite.Areas.Shopify.ViewModels
{
    public class OrderConfigurationViewModel
    {
        public OrderConfigurationViewModel()
        {
            var tenant = new Tenant();
            Countries = ShopifyApp.Services.Exigo.GetAvailableCountries();
            Webacts = ShopifyApp.Services.Exigo.GetAvailableWebCats();
            Currencies = ShopifyApp.Services.Exigo.GetAvailableCurrencies();
            Languages = ShopifyApp.Services.Exigo.GetAvailableLanguages();
            PriceTypes = ShopifyApp.Services.Exigo.GetAvailablePriceTypes();
            Wareohuses = ShopifyApp.Services.Exigo.GetAvailableWarehouses();
        }
        public OrderConfigurationViewModel(TenantOrderConfiguration config)
        {
            var tenant = new Tenant();
            Countries = ShopifyApp.Services.Exigo.GetAvailableCountries();
            Webacts = ShopifyApp.Services.Exigo.GetAvailableWebCats();
            Currencies = ShopifyApp.Services.Exigo.GetAvailableCurrencies();
            Languages = ShopifyApp.Services.Exigo.GetAvailableLanguages();
            PriceTypes = ShopifyApp.Services.Exigo.GetAvailablePriceTypes();
            Wareohuses = ShopifyApp.Services.Exigo.GetAvailableWarehouses();
            ShipMethods = ShopifyApp.Services.Exigo.GetAvailableShipMethhods(config.WarehouseID);
        }
        public List<SelectListItem> Countries { get; set; }
        public List<SelectListItem> Webacts { get; set; }
        public List<SelectListItem> Currencies { get; set; }
        public List<SelectListItem> Languages { get; set; }
        public List<SelectListItem> PriceTypes { get; set; }
        public List<SelectListItem> ShipMethods { get; set; }
        public List<SelectListItem> Wareohuses { get; set; }
    }
}