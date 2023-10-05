using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ShopifyApp.Models
{
    public class WebCategory
    {
        public int WebCategoryID { get; set; }
        public string WebCategoryDescription { get; set; }
        public int ParentID { get; set; }
        public int NestedLevel { get; set; }
    }
}