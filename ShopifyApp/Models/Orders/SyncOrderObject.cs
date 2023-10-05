using ShopifyApp.Api.ExigoWebservice;
using ShopifyApp.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ShopifyApp.Models
{
    public class SyncOrderObject
    {
        public SyncOrderObject()
        {

        }
        public SyncOrderObject(ShopifySharp.Order order, Webhook hook = null)
        {
            ConvertOrderToSyncOrder(order, hook);
        }
        public int ShopOrderId { get; set; }
        public string ShopOrderReference { get; set; }
        public long ShopCustomerId { get; set; }
        public int ShipMethodId { get; set; }
        public decimal TotalDiscount { get; set; }
        public decimal SubTotal { get; set; }
        public decimal OrderTotal { get; set; }
        public decimal TaxTotal { get; set; }
        public decimal ShippingTotal { get; set; }
        public bool IsPaid { get; set; }
        public bool IsShipped { get; set; }
        public string TrackingNumber { get; set; }
        public List<ShopifySharp.LineItem> LineItems { get; set; }
        public string ReferralWebalias { get; set; }
        public List<ShopifySharp.NoteAttribute> Notes { get; set; }
        public CustomerResponse Customer { get; set; }
        public List<Refunds> OrderRefunds { get; set; }
        public bool IsFlaggedFraud { get; set; }
        public bool IsReplacementOrder { get; set; }
        public List<ShopifySharp.DiscountCode> DiscountCodes { get; set; }
        public List<ShopifySharp.DiscountApplication> DiscountApplications { get; set; }
        public List<ShoppingCartItem> Items { get; set; }
        public SyncShippingAddress ShippingAddress { get; set; }
        public string WebhookId { get; set; }
        public bool IsDefaultCustomer { get; set; }
        public string WebhookBody { get; set; }
        public decimal TaxRateOverride { get; set; }

        private void ConvertOrderToSyncOrder(ShopifySharp.Order order, Models.Webhook hook = null)
        {;
            GetRefunds(order);
            OrderItems(order);
            GetCustomer(order);
            GetPayments(order);
            GetFulfilments(order);
            GetNotes(order);
            HandleTags(order);
            TaxRateOverride = CustomService.TaxRateOverride(order.TaxLines);
            CalculateShipping(order);
            SubTotal = order.SubtotalPrice.Value;
            ShopOrderId = order.OrderNumber.Value;
            DiscountCodes = order.DiscountCodes.ToList();
            DiscountApplications = order.DiscountApplications.ToList();
            ShopOrderReference = order.Id.Value.ToString();
            ShopCustomerId = (IsDefaultCustomer) ? Settings.DefaultShopCustomerID : order.Customer.Id.Value;
            ShippingAddress = new SyncShippingAddress(order);
            LineItems = order.LineItems.ToList();
            ShipMethodId = Settings.DefaultShipMethodId;
            WebhookId = (hook != null) ? hook.WebhookId : "";
            WebhookBody = (hook != null) ? hook.WebhookBody : "";
            OrderTotal = (order.TotalPrice.HasValue) ? order.TotalPrice.Value : 0;
            TaxTotal = (order.TotalTax.HasValue) ? order.TotalTax.Value : 0;
            TotalDiscount = (order.TotalDiscounts.HasValue) ? order.TotalDiscounts.Value : 0;
        }
        private void CalculateShipping(ShopifySharp.Order order)
        {
            decimal shippingRate = (order.ShippingLines.Any()) ? order.ShippingLines.ToList().Sum(c => c.Price.Value) : 0;
            ShippingTotal = shippingRate;
        }
        private void GetNotes(ShopifySharp.Order order)
        {
            if(order.NoteAttributes.Where(c => c.Name == "Referral").Count() != 0)
            {
                Notes = order.NoteAttributes.ToList();
                ReferralWebalias = Notes.First(c => c.Name == "Referral").Value.ToString();
            }
            else
            {
                ReferralWebalias = Settings.DefaultWebalias;
            }
        }
        private void GetPayments(ShopifySharp.Order order)
        {
            IsPaid = (order.FinancialStatus == "paid");
        }
        private void GetFulfilments(ShopifySharp.Order order)
        {
            if (order.Fulfillments.Count() != 0)
            {
                IsShipped = true;
                if (order.Fulfillments.First().TrackingNumber != null)
                    TrackingNumber = order.Fulfillments.First().TrackingNumber;
            }
        }
        private void GetRefunds(ShopifySharp.Order order)
        {
            OrderRefunds = new List<Refunds>();

            //If orderobject contains refunds
            if (order.Refunds != null)
            {
                foreach (var refund in order.Refunds)
                {
                    //Check if refund inclues shipping 
                    decimal refundShipping = 0;
                    decimal refundTax = 0;
                    if (refund.OrderAdjustments.Any())
                    {
                        foreach (var adj in refund.OrderAdjustments)
                        {
                            if (adj.Kind == "shipping_refund")
                            {
                                refundShipping = adj.Amount.Value;
                            }
                            refundTax = adj.TaxAmount.Value;
                        }
                    }
                    var refundLineItems = new List<ShopifySharp.RefundLineItem>();
                    //if there is items in the refund, andthe refund has not been processed before
                    if (refund.RefundLineItems != null)
                    {
                        //fill the information to create a new refund
                        foreach (var refundItem in refund.RefundLineItems)
                        {
                            refundLineItems.Add(refundItem);
                            refundTax = refundTax + refundItem.TotalTax.Value;
                        }
                    }
                    //Add the refund of order refunds
                    OrderRefunds.Add(new Refunds
                    {
                        ShopRefundId = refund.Id.Value.ToString(),
                        Amount = refund.Transactions.Sum(c => c.Amount.Value) * -1,
                        RefundedShipping = refundShipping,
                        RefundedTax = refundTax * -1,
                        RefundLineItems = refundLineItems
                    });
                }
            }
        }
        private void OrderItems(ShopifySharp.Order order)
        {
            Items = new List<ShoppingCartItem>();
            foreach (var item in order.LineItems)
            {
                var newItem = new ShoppingCartItem
                {
                    ItemCode = item.SKU,
                    Quantity = item.Quantity.Value,
                    Type = ShoppingCartItemType.Order
                };
                Items.Add(newItem);
            }
        }
        private void GetCustomer(ShopifySharp.Order order)
        {
            if(order.Customer == null && order.Tags.Contains("toktokpos"))
            {
                IsDefaultCustomer = true;
            }
            else
            {
                Customer = new CustomerResponse
                {
                    FirstName = order.Customer.FirstName,
                    LastName = order.Customer.LastName,
                    Email = order.Customer.Email,
                    Phone = order.Customer.Phone,
                    MainAddress1 = order.Customer.DefaultAddress.Address1,
                    MainAddress2 = order.Customer.DefaultAddress.Address2,
                    MainCity = order.Customer.DefaultAddress.City,
                    MainState = order.Customer.DefaultAddress.ProvinceCode,
                    MainZip = order.Customer.DefaultAddress.Zip,
                    MainCountry = order.Customer.DefaultAddress.CountryCode,
                };
            }
        }
        private void HandleTags(ShopifySharp.Order order)
        {
            if(order.Tags.ToLower().Contains("nofraud_fail"))
            {
                IsFlaggedFraud = true;
            }
            if(order.Tags.ToLower().Contains("replacement"))
            {
                IsReplacementOrder = true;
            }
        }
    }
}