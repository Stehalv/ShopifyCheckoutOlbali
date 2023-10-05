using ReplicatedSite.Models;
using Common;
using Common.Providers;
using ExigoService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace ReplicatedSite.Providers
{
    public class ShopifyCheckoutLogicProvider : BaseLogicProvider
    {
        #region Constructors
        public ShopifyCheckoutLogicProvider() : base() {  }
        public ShopifyCheckoutLogicProvider(Controller controller, ShoppingCartCheckoutPropertyBag propertyBag)
        {
            Controller  = controller;
            PropertyBag = propertyBag;
        }
        #endregion

        #region Properties
        public ShoppingCartCheckoutPropertyBag PropertyBag { get; set; }
        #endregion 

        #region Logic
        public override CheckLogicResult CheckLogic()
        {
            if(Identity.Customer == null && PropertyBag.Cart.CartType == ShopifyApp.CartType.AutoOrder && !PropertyBag.Cart.AutoOrder)
            {
                return CheckLogicResult.Failure(RedirectToAction("AutoOrder"));
            }
            else
            {
                if (PropertyBag.Cart == null)
                {
                    return CheckLogicResult.Failure(RedirectToAction("RedirectBackToShopCart"));
                }

                if (Identity.Customer == null && !PropertyBag.Cart.Account)
                {
                    return CheckLogicResult.Failure(RedirectToAction("Account"));
                }

                if ((PropertyBag.Cart.OrderItems.Any() || PropertyBag.Cart.EnrollmentPackItems.Any()) && !PropertyBag.Cart.ShippingAddress)
                {
                    return CheckLogicResult.Failure(RedirectToAction("ShippingAddress"));
                }

                if (PropertyBag.Cart.AutoOrderItems.Any() && !PropertyBag.Cart.AutoOrder)
                {
                    return CheckLogicResult.Failure(RedirectToAction("AutoOrder"));
                }

                if (PropertyBag.Cart.CartType == ShopifyApp.CartType.Enrollment && !PropertyBag.Cart.EnrollmentInfo)
                {
                    return CheckLogicResult.Failure(RedirectToAction("EnrollmentInfo"));
                }
            }

            return CheckLogicResult.Success(RedirectToAction("Payment"));
        }   

        #endregion
    }
}