using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShopifyApp.Models
{
    public class SyncShippingAddress : ShippingAddress
    {
        public SyncShippingAddress(ShopifySharp.Order order)
        {
            if (order.ShippingAddress == null && order.BillingAddress != null)
                order.ShippingAddress = order.BillingAddress;
            if (order.ShippingAddress != null)
            {
                FirstName = order.ShippingAddress.FirstName;
                LastName = order.ShippingAddress.LastName;
                Address1 = order.ShippingAddress.Address1;
                Address2 = order.ShippingAddress.Address2;
                Zip = order.ShippingAddress.Zip;
                City = order.ShippingAddress.City;
                State = order.ShippingAddress.ProvinceCode;
                Country = order.ShippingAddress.CountryCode;
                Phone = order.ShippingAddress.Phone;
                Email = order.Customer.Email;
            }
        }
        public SyncShippingAddress(WooCommerceNET.WooCommerce.v3.Order order, string email)
        {
            FirstName = order.shipping.first_name;
            LastName = order.shipping.last_name;
            Address1 = order.shipping.address_1;
            Address2 = order.shipping.address_2;
            Zip = order.shipping.postcode;
            City = order.shipping.city;
            State = order.shipping.state;
            Country = order.shipping.country;
            //Phone = order.shipping.Phone;
            Email = email;
        }
    }
}
