using Dapper;
using ShopifyApp.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ShopifyApp.Models
{
    public class Warehouse
    {
        public Warehouse()
        {

        }
        public Warehouse(int itemId)
        {
            ItemID = itemId;
        }
        public int ItemID { get; set; }
        public int WarehouseID { get; set; }
        public string WarehouseDescription { get; set; }
        public string CurrencyCode
        {
            get
            {
                if(WarehouseID != 0)
                {
                    using (var sql = SQLContext.Sql())
                    {
                        return sql.Query<string>($"Select CurrencyCode from dbo.WarehouseCurrencies where WarehouseID = {WarehouseID}").FirstOrDefault();
                    }
                }
                return "";

            }
        }
        public bool IsAvailable
        {
            get
            {
                if (WarehouseID != 0 && ItemID != 0)
                {
                    using (var sql = SQLContext.Sql())
                    {
                        var id = sql.Query<int>($"Select ItemID from dbo.ItemWarehouses where WarehouseID = {WarehouseID} and ItemID = {ItemID}").FirstOrDefault();
                        if (id != 0)
                            return true;
                    }
                }
                return false;
            }
        }
    }
}