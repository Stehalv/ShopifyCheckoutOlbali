using Common;
using System;

namespace ReplicatedSite
{
    public static partial class Settings
    {
        public static bool RememberLastWebAliasVisited = false;
        public static bool AllowOrphans = true;

        public static string PreferredCustomerSubscriptionItemCode = "TEST1";

        // Triggers modal to ask how a user was referred if they land on the Product list page without an Enroller
        public static bool ShowChooseEnrollerInShopping = false;
        public static bool ShowChooseEnrollerInCehcking = true;

        public static string ContactUsEmailAddress = "noreply@exigo.com";
        public static string ContactUsReplyAddress = "noreply@exigo.com";

        // To assist with cache busting on new deployments
        public static int StyleVersionNumber = DateTime.Now.Millisecond;
    }
    public static class UXSettings
    {
        public static string TermsOfUseLink = "";
        public static string PrivacyPolicyLink = "";
        public static string ShippingPolicyLink = "";
        public static string RefundPolicyLink = "";
        public static string LogoLink = "https://olbali.com/FoolproofBody/CompanyLogo/b7b0fd53-93dc-428c-8aaa-185520b9656815-09-2023T11-03-15-68-cropped.png";
    }

    public enum EnrollmentType
    {
        Distributor = 1,
        PreferredCustomer = 2
    }

    public enum SearchType
    {
        webaddress = 1,
        distributorID = 2,
        distributorInfo = 3,
        zipcode = 4,
        eventcode = 5,
        eventname = 6
    }

}