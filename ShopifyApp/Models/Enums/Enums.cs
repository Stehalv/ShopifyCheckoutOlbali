using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ShopifyApp
{
    public enum UserRole
    {
        User = 1,
        Admin = 2,
        SystemAdmin = 3,
        App = 4
    }
    public enum PriceTypes
    {
        retail = 1,
        preferred = 2,
        wholesale = 3
    }
    public enum MasterItemRecord
    {
        Shopify = 1,
        Exigo = 2
    }
    public enum WebhookStatus
    {
        Saved = 1,
        Processed = 2,
        Error = 3,
        None = 0
    }
    public enum LogSection
    {
        Orders = 1,
        Webhooks = 2,
        Customers = 3,
        Tenant = 4,
        Global = 5,
        Checkout = 6
    }
    public enum LogType
    {
        Information = 1,
        Success = 2,
        Error = 3
    }
    public enum AddressType
    {
        New = 0,
        Main = 1,
        Mailing = 2,
        Other = 3
    }
    public enum ShoppingCartItemType
    {
        Order = 0,
        AutoOrder = 1,
        WishList = 2,
        Coupon = 3,
        EnrollmentPack = 4,
        EnrollmentAutoOrderPack = 5,
        InternationalShippingFee = 6
    }
    public enum CustomerTypes
    {
        RetailCustomer = 1,
        PreferredCustomer = 2,
        Distributor = 3,
        Newsletter = 4
    }
    public enum CartType
    {
        Shopping = 1,
        Enrollment = 2,
        AutoOrder = 3
    }
}