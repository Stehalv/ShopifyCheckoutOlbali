using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ReplicatedSite.Models
{
    public class AutoOrderDetail
    {
        public int AutoOrderId { get; set; }
        public long id { get; set; }
        public int quantity { get; set; }
        public properties properties
        {
            get
            {
                return new properties { ordertype = "autoorder", autoorderid = AutoOrderId };
            }
        }
    }
    public class properties
    {
        public string ordertype { get; set; }
        public int autoorderid { get; set; }
    }
}