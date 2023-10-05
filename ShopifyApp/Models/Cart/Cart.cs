using Dapper;
using Newtonsoft.Json;
using ShopifyApp.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ShopifyApp.Models
{
    public class Cart
    {
        public Cart() 
        {
            Items = new List<CartItem>();
        }
        public Cart(string json)
        {
            Json = json;
            Populate();
        }
        #region steps
        public bool Account { get; set; }
        public bool ShippingAddress { get; set; }
        public bool EnrollmentInfo { get; set; }
        public bool AutoOrder { get; set; }
        public bool Completed { get; set; }
        #endregion

        public int Id { get; set; }
        public bool IsPaymentOfExistingOrder { get; set; }
        public int TenantConfigId { get; set; }
        public string CheckoutToken { get; set; }
        public bool AutoOrderSaved { get; set; }
        public bool IsCalculated { get; set; }
        public string AutoOrderFrequency { get; set; }
        public DateTime AutoOrderStartDate { get; set; }
        public int CustomerId { get; set; }
        public int CustomerTypeId { get; set; }
        public string WebAlias { get; set; }
        public string TaxId { get; set; }
        public CartType CartType { get; set; }
        public int PriceTypeID { get; set; }
        public string ShopUrl { get; set; }
        public string Currency { get; set; }
        public string Token { get; set; }
        public string Referral { get; set; }
        public string Json { get; set; }
        public string Note { get; set; }
        public decimal PointsTotal { get; set; }
        public decimal TaxTotal { get; set; }
        public bool ShippingCalculated { get; set; }
        public decimal ShippingTotal { get; set; }
        public decimal InternationalShippingFee { get; set; } 
        public int EditAutoOrderId { 
            get
            {
                if (Items.Where(c => c.EditAutoOrderId > 0).Any())
                {
                    return Items.Where(c => c.EditAutoOrderId > 0).First().EditAutoOrderId;
                }
                else
                    return 0;
            } 
        }
        public decimal SubTotal
        {
            get
            {
                var itemstotal = OrderItems.Sum(c => c.Final_Line_Price);
                itemstotal = itemstotal + EnrollmentPackItems.Sum(c => c.Final_Line_Price);
                return itemstotal;
            }
        }
        public decimal OrderTotal
        {
            get
            {
                var itemstotal = SubTotal;
                itemstotal = itemstotal + TaxTotal;
                itemstotal = itemstotal + ShippingTotal;
                itemstotal = itemstotal + PointsTotal;
                return itemstotal;
            }
        }
        public decimal AutoOrderTotal
        {
            get
            {
                return AutoOrderItems.Sum(c => c.Final_Line_Price);
            }
        }
        public List<CartItem> Items { get; set; }
        public List<CartItem> OrderItems
        {
            get
            {
                if (Items.Any())
                    return Items.Where(c => c.Type == ShoppingCartItemType.Order).ToList();
                else
                    return new List<CartItem>();
            }
        }
        public List<CartItem> DiscountItems
        {
            get
            {
                if (Items.Any())
                    return Items.Where(c => c.IsDiscountItem).ToList();
                else
                    return new List<CartItem>();
            }
        }
        public List<CartItem> AutoOrderItems
        {
            get
            {
                if (Items.Any())
                    return Items.Where(c => c.Type == ShoppingCartItemType.AutoOrder).ToList();
                else
                    return new List<CartItem>();
            }
        }
        public List<CartItem> EnrollmentPackItems
        {
            get
            {
                if (Items.Any())
                    return Items.Where(c => c.Type == ShoppingCartItemType.EnrollmentPack).ToList();
                else
                    return new List<CartItem>();
            }
        }
        public void Populate()
        {
            var cart = JsonConvert.DeserializeObject<Cart>(Json);
            Token = cart.Token;
            Note = cart.Note;
            Items = cart.Items;

        }
    }
    public class CartItem
    {
        public decimal Quantity { get; set; }
        public bool IsDiscountItem { get; set; }
        public string Sku { get; set; }
        public string Product_Title { get; set; }
        public string Variant_Title { get; set; }
        public string Total_Discount { get; set; }
        public decimal Original_Line_Price { get; set; }
        public string OriginalPriceString
        {
            get
            {
                return "$" + Original_Line_Price.ToString("n2");
            }
        }
        public decimal Final_Line_Price
        {
            get
            {
                return Original_Line_Price * Quantity;
            }
        }
        public string FinalPriceString
        {
            get
            {
                return "$" + Final_Line_Price.ToString("n2");
            }
        }
        public string Url { get; set; }
        public Dictionary<string, string> Properties { get; set; }
        public FeaturedImage Featured_Image { get; set; }
        public ShoppingCartItemType Type
        {
            get
            {
                ShoppingCartItemType type = ShoppingCartItemType.Order;
                if (Properties != null && Properties.Any())
                {
                    if(Properties.Values.Contains("autoorder"))
                        type = ShoppingCartItemType.AutoOrder;
                    if (Properties.Values.Contains("enrollmentpack"))
                        type = ShoppingCartItemType.EnrollmentPack;
                }
                return type;
            }
        }
        public int EditAutoOrderId
        {
            get
            {
                if (Properties != null && Properties.Any())
                {
                    if (Properties.Where(c => c.Key == "autoorderid").Count() > 0)
                        return Convert.ToInt32(Properties.Where(c => c.Key == "autoorderid").First().Value);
                    else
                        return 0;
                }
                else
                    return 0;
            }
        }
        public string ItemCode
        {
            get
            {
                return Sku;
            }
        }
        public class FeaturedImage
        {
            public string Url { get; set; }

        }
    }
}