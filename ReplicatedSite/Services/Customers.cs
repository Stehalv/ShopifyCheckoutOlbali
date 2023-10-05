using ExigoService;
using System.Collections.Generic;

namespace ReplicatedSite
{
    public static partial class Customers
    {
        public static Customer GetCustomer(int customerID)
        {
            // If we are getting the report for the current user, than we will want to get it in real time
            return ExigoDAL.GetCustomer(customerID);
        }

        public static IEnumerable<Address> GetCustomerAddresses(int customerID)
        {
            CustomerIdentity ident = null;
            try { ident = Identity.Customer; } catch { } //null-safe for tests
            return ExigoDAL.GetCustomerAddresses(customerID, (ident != null ? (customerID == ident.CustomerID) : false));
        }
        public static bool RefreshCustomerTag(long customerId)
        {
            var appCustomer = new ShopifyApp.Models.Customer().GetByShopId(customerId.ToString(), false);
            return true;
        }
    }
}