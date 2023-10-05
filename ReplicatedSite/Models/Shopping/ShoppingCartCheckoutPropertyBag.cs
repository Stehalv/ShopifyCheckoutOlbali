﻿using System;
using ExigoService;
using Common.Api.ExigoWebService;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ReplicatedSite.Models
{
    public class ShoppingCartCheckoutPropertyBag : BasePropertyBag
    {
        private string version = "3.0.0";
        private int expires = 61;

        #region Constructors
        public ShoppingCartCheckoutPropertyBag()
        {
            CustomerID = (Identity.Customer != null) ? Identity.Customer.CustomerID : 0;
            Expires = expires;
            ShippingAddress = new ShippingAddress();
        }
        #endregion

        #region Shared Properties
        public bool IsDebug { get; set; }
        public int CustomerID { get; set; }
        public bool IsEmailOnly { get; set; }
        public int NewCustomerID { get; set; }
        public int NewOrderID { get; set; }
        //public List<ShopifyApp.Models.EnrollmentCheckboxes> EnrollmentCheckboxes { get; set; }
        public ShopifyApp.Models.Cart Cart { get; set; }
        public Customer Customer { get; set; }
        public int BirthDay { get; set; }
        public int BirthMonth { get; set; }
        public string WebAlias { get; set; }
        public Address MainAddress { get; set; }
        public bool AcceptsMarketing { get; set; }
        public bool AcceptedTerms { get; set; }
        public bool AcceptedWebsiteFee { get; set; }
        public bool AcceptedEnroller { get; set; }
        public ShippingAddress ShippingAddress { get; set; }
        public bool ShippingAddressSameAsMain { get; set; }
        public ShippingAddress AutoShipAddress { get; set; }
        public int ShipMethodID { get; set; }
        public IEnumerable<IShipMethod> Shipmethods { get; set; }
        public IPaymentMethod PaymentMethod { get; set; }
        public bool BillingAddressSameAsShipping { get; set; }
        public bool UsePointsAsPayment { get; set; }
        public decimal QuantityOfPointsToUse { get; set; }


        public bool IsSubmitting { get; set; }
        public string OrderException { get; set; }

        public string SelectedDistributor { get; set; }
        public int PartyID { get; set; }
        public int GuestID { get; set; }
        #endregion

        #region Shopping With Auto Order Only Properties
        public int NewAutoOrderID { get; set; }
        public FrequencyType AutoOrderFrequencyType { get; set; }
        public int AutoOrderFrequencyTypeId
        {
            get
            {
                return (int)AutoOrderFrequencyType;
            }
        }
        public DateTime AutoOrderStartDate { get; set; }
        public ShippingAddress AutoOrderShippingAddress { get; set; }
        public ShippingAddress AutoOrderBillingAddress { get; set; }
        public IPaymentMethod AutoOrderPaymentMethod { get; set; }
        public bool AutoOrderAddressSameAsShipping { get; set; }
        public bool AutoOrderBillingSameAsShipping { get; set; }
        public int AutoOrderShipMethodID { get; set; }
        public bool ExisitingAutoOrder { get; set; }
        public bool AddToExistingAutoOrder { get; set; }
        public int SelectedAutoOrderId { get; set; }


        public bool HasProvidedNewCustomerLoginName { get; set; }
        public string NewCustomerLoginName { get; set; }
        public string NewCustomerPassword { get; set; }
        #endregion

        #region Methods
        public override T OnBeforeUpdate<T>(T propertyBag)
        {
            propertyBag.Version = version;

            return propertyBag;
        }
        public override bool IsValid()
        {
            var currentCustomerID = (Identity.Customer != null) ? Identity.Customer.CustomerID : 0;
            return this.Version == version && (CustomerID == 0 || CustomerID == currentCustomerID);
        }
        public bool HasValidParty()
        {
            return this.PartyID > 0;
        }
        public bool IsGuestCheckout()
        {
            return this.GuestID > 0;
        }
        #endregion
    }
}