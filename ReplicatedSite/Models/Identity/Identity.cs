using System;
using System.Web;
using ReplicatedSite.Services;
using ShopifyApp.Models;

namespace ReplicatedSite
{
    public class Identity
    {
        public static CustomerIdentity Customer
        {
            get
            {
                var identity = HttpContext.Current.User.Identity as CustomerIdentity;
                if (identity!= null && identity.Id != 0)
                    return null;
                return identity;
            }
        }
        public static CustomerIdentity Current
        {
            get
            {
                var identity = HttpContext.Current.User.Identity as CustomerIdentity;
                if (identity != null && identity.CustomerID != 0)
                    return null;
                return identity;
            }
        }
        public static ReplicatedSiteIdentity Owner
        {
            get
            {
                var identity = (HttpContext.Current.Items["OwnerWebIdentity"] as ReplicatedSiteIdentity);

                if (identity == null && Settings.RememberLastWebAliasVisited)
                {
                    var lastWebAlias = ShopifyApp.Settings.DefaultWebalias;

                    var cookie = HttpContext.Current.Request.Cookies["LastWebAlias"];
                    if (cookie != null && cookie.Value.IsNotNullOrEmpty())
                    {
                        lastWebAlias = cookie.Value;
                    }

                    var identityService = new IdentityService();
                    identity = identityService.GetIdentity(lastWebAlias);
                    HttpContext.Current.Items["OwnerWebIdentity"] = identity;
                }

                return identity;
            }
        }
    }
}