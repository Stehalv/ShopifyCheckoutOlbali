using ReplicatedSite.Models;
using Common;
using ExigoService;
using System.Collections.Generic;

namespace ReplicatedSite.ViewModels
{
    public class ShopifyCheckoutViewModel
    {
        public ShopifyCheckoutViewModel()
        {
            CustomerAutoOrders = new List<AutoOrder>();
        }

        public ShopifyCheckoutViewModel(ShoppingCartCheckoutPropertyBag propertyBag)
        {
            PropertyBag = propertyBag;
        }
        public ShoppingCartCheckoutPropertyBag PropertyBag { get; set; }
        public ShippingAddressesViewModel ShippingAddresses { get; set; }
        public PaymentMethodsViewModel Payments { get; set; }
        public List<AutoOrder> CustomerAutoOrders { get; set; }
        public string ErrorMessage { get; set; }

    }
}