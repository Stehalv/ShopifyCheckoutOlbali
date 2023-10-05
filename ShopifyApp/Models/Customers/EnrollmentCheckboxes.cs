using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ShopifyApp.Models
{
    public class EnrollmentCheckboxes
    {
        public string Name { get; set; }
        public string Text { get; set; }
        public bool Value { get; set; }
        public bool Required { get; set; }
        public bool ErrorMessage { get; set; }
        public int StringValues { get; set; }
        public string StringValueNames { get; set; }
    }
}