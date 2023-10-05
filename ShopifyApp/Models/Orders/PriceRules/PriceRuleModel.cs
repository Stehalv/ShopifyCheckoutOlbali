using Newtonsoft.Json;
using ShopifySharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ShopifyApp.Models
{
    public class PriceRuleModel :ShopifySharp.PriceRule
    {
        public int TenantConfigId { get; set; }
        public List<CustomerSavedSearch> SavedSearches { get; set; }
        public new bool OncePerCustomer { get; set; }
        public List<SelectListItem> TargetSelections
        {
            get
            {
                var list = new List<SelectListItem>
                {
                    new SelectListItem{ Text = "Select", Value = "" },
                    new SelectListItem{ Text = "All", Value = "all" },
                    new SelectListItem{ Text = "Pr Product", Value = "entitled" }
                };
                return list;
            }
        }
        public List<SelectListItem> ValueTypes
        {
            get
            {
                var list = new List<SelectListItem>
                {
                    new SelectListItem{ Text = "Select", Value = "" },
                    new SelectListItem{ Text = "Fixed Amount", Value = "fixed_amount" },
                    new SelectListItem{ Text = "Percent", Value = "percent" }
                };
                return list;
            }
        }
        public List<SelectListItem> TargetTypes
        {
            get
            {
                var list = new List<SelectListItem>
                {
                    new SelectListItem{ Text = "Select", Value = "" },
                    new SelectListItem{ Text = "Line Item", Value = "line_item" },
                    new SelectListItem{ Text = "Shipping", Value = "shipping_line" }
                };
                return list;
            }
        }
        public List<SelectListItem> AllocationMethods
        {
            get
            {
                var list = new List<SelectListItem>
                {
                    new SelectListItem{ Text = "Select", Value = "" },
                    new SelectListItem{ Text = "Pr LineItem", Value = "each" },
                    new SelectListItem{ Text = "All", Value = "across" }
                };
                return list;
            }
        }
        public List<SelectListItem> CustomerSelections
        {
            get
            {
                var list = new List<SelectListItem>
                {
                    new SelectListItem{ Text = "Select", Value = "" },
                    new SelectListItem{ Text = "All", Value = "all" },
                    new SelectListItem{ Text = "List", Value = "rerequisite" }
                };
                return list;
            }
        }

        public List<SelectListItem> SavedSearchesList
        {
            get
            {
                var list = new List<SelectListItem>();
                if(SavedSearches != null)
                {
                    foreach(var s in SavedSearches)
                    {
                        list.Add(new SelectListItem { Text = s.Name, Value = s.Id.ToString() });
                    }
                }
                return list;
            }
        }
    }
}