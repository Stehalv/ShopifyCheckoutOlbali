using System;
using System.Collections.Generic;
using System.Linq;

namespace ShopifyApp.Models
{
    public class ShoppingCartItem 
    {
        public ShoppingCartItem()
        {
            ID                  = Guid.NewGuid();
            ItemCode            = string.Empty;
            Quantity            = 0;
            GroupMasterItemCode = string.Empty;
            Type                = ShoppingCartItemType.Order;
        }

        public Guid ID
        {
            get
            {
                if (_id == null)
                {
                    _id = Guid.NewGuid();
                }
                return _id;
            }
            set
            {
                _id = value;
            }
        }
        private Guid _id;

        public string ItemCode { get; set; }
        public decimal Quantity { get; set; }
        public string GroupMasterItemCode { get; set; }
        public ShoppingCartItemType Type { get; set; }
    }
}