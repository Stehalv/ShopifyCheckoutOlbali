using ShopifyApp.Models;
using ShopifyApp.Services.ShopService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ReplicatedSite.Areas.Shopify.ViewModels
{
    public class ProductDetailViewModel
    {
        public ProductDetailViewModel(long id, int configId)
        {
            _configId = configId;
            ProductId = id;
            Variants = new List<AppProductVariant>();
        }
        private int _configId { get; set; }
        public long ProductId { get; set; }
        public ShopifySharp.Product Product { get; set; }
        public List<AppProductVariant> Variants { get; set; }
        public async void Get()
        {
            Product = await new ShopifyDAL(_configId).GetProduct(ProductId);
            foreach (var variant in Product.Variants)
            {
                Variants.Add(new AppProductVariant(variant, _configId));
            }
        }
    }
}