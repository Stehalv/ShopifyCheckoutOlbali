using Dapper;
using ShopifyApp.Data;
using ShopifyApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using ShopifyApp.Api.ExigoWebservice;

namespace ShopifyApp.Services
{
    public static partial class Exigo
    {
        #region Products
        public static List<Item> GetAllExigoItems()
        {
            using (var context = SQLContext.Sql())
            {
                return context.Query<Item>($"Select ItemCode, ItemDescription, ItemID from Items").ToList();
            }
        }

        public static Item GetItem(string itemCode)
        {
            var item = GetItemInformation(itemCode);
            PopulateGroupMembers(item);
            return item;
        }

        private static void PopulateGroupMembers(Item item)
        {
            // ensure max recursion is not exceeded
            var _maxDepth = 1;
            // if recursion is exceeded, stop
            if (_maxDepth <= 0) { return; }

            // if no group masters, stop here
            if (!item.IsGroupMaster) { return; }
            // Get a list of group member items for all the group master items
            var itemGroupMemberItemCodes = new List<string>();

            using (var context = SQLContext.Sql())
            {
                itemGroupMemberItemCodes = context.Query<string>(@"
                    SELECT 
	                    mi.ItemCode                  AS MasterItemCode
                    FROM 
                        ItemGroupMembers AS igm
	                        LEFT JOIN Items AS mi
		                        ON mi.ItemID = igm.MasterItemID
	                        LEFT JOIN Items AS ci
		                        ON ci.ItemID = igm.ItemID 
                    WHERE
                        mi.ItemCode = @masterItemcode
                ", new
                {
                    masterItemCode = item.ItemCode
                }).ToList();
            }
            // if no group members, stop here
            if (!itemGroupMemberItemCodes.Any()) { return; }


            // get the item details
            // Items are returned based on their availability 
            item.GroupMembers = GetItemsInformation(itemGroupMemberItemCodes);
        }

        private static List<Item> GetItemsInformation(List<string> itemCodes)
        {
            using (var context = SQLContext.Sql())
            {
                var sql = $@"
                	SELECT
	                    i.ItemID
	                    , i.ItemCode
	                    , ItemDescription = 
		                    CASE 
			                    WHEN i.ItemTypeID = 0 AND i.IsGroupMaster = 1 
                                THEN COALESCE( NULLIF(i.GroupDescription,''), NULLIF(il.ItemDescription,''), i.ItemDescription)
			                    ELSE COALESCE( NULLIF(il.ItemDescription, ''), i.ItemDescription)
		                    END
	                    , i.Weight
	                    , i.ItemTypeID
	                    , TinyImageUrl  = i.TinyImageName
	                    , SmallImageUrl = i.SmallImageName
	                    , LargeImageUrl = i.LargeImageName
	                    , ShortDetail1  = COALESCE( NULLIF(il.ShortDetail ,''), i.ShortDetail)
	                    , ShortDetail2  = COALESCE( NULLIF(il.ShortDetail2,''), i.ShortDetail2)
	                    , ShortDetail3  = COALESCE( NULLIF(il.ShortDetail3,''), i.ShortDetail3)
	                    , ShortDetail4  = COALESCE( NULLIF(il.ShortDetail4,''), i.ShortDetail4)
	                    , i.IsVirtual
	                    , i.AllowOnAutoOrder
	                    , IsGroupMaster = CAST(CASE WHEN i.ItemTypeID = 0 AND i.IsGroupMaster = 1 THEN 1 ELSE 0 END AS BIT)
                        , i.GroupMembersDescription
	                    , IsDynamicKitMaster = CAST(CASE WHEN i.ItemTypeID = 0 THEN 1 ELSE 0 END AS BIT)
                        , OtherCheck1  = i.OtherCheck1
	                    , OtherCheck2  = i.OtherCheck2
	                    , OtherCheck3  = i.OtherCheck3
	                    , OtherCheck4  = i.OtherCheck4
	                    , OtherCheck5  = i.OtherCheck5
	                    , ip.Price
                        , ip2.Price as BeautyInsiderPrice
	                    , ip.CurrencyCode
	                    , BV           = ip.BusinessVolume
	                    , CV           = ip.CommissionableVolume
                    FROM Items AS i
						LEFT JOIN ItemLanguages AS il
		                    ON il.ItemID = i.ItemID
						    AND il.LanguageID = 0
					WHERE 
                        i.ItemCode in @itemCodes";
                var apiItems = context.Query<Item>(sql
                    , new
                    {
                        itemCodes = itemCodes
                    }).ToList();
                return apiItems;
            }
        }
        private static Item GetItemInformation(string itemCode)
        {
            using (var context = SQLContext.Sql())
            {
                var sql = $@"
                	SELECT
	                    i.ItemID
	                    , i.ItemCode
	                    , ItemDescription = 
		                    CASE 
			                    WHEN i.ItemTypeID = 0 AND i.IsGroupMaster = 1 
                                THEN COALESCE( NULLIF(i.GroupDescription,''), NULLIF(il.ItemDescription,''), i.ItemDescription)
			                    ELSE COALESCE( NULLIF(il.ItemDescription, ''), i.ItemDescription)
		                    END
	                    , i.Weight
	                    , i.ItemTypeID
	                    , TinyImageUrl  = i.TinyImageName
	                    , SmallImageUrl = i.SmallImageName
	                    , LargeImageUrl = i.LargeImageName
	                    , ShortDetail1  = COALESCE( NULLIF(il.ShortDetail ,''), i.ShortDetail)
	                    , ShortDetail2  = COALESCE( NULLIF(il.ShortDetail2,''), i.ShortDetail2)
	                    , ShortDetail3  = COALESCE( NULLIF(il.ShortDetail3,''), i.ShortDetail3)
	                    , ShortDetail4  = COALESCE( NULLIF(il.ShortDetail4,''), i.ShortDetail4)
	                    , i.IsVirtual
	                    , i.AllowOnAutoOrder
	                    , IsGroupMaster = CAST(CASE WHEN i.ItemTypeID = 0 AND i.IsGroupMaster = 1 THEN 1 ELSE 0 END AS BIT)
                        , i.GroupMembersDescription
	                    , IsDynamicKitMaster = CAST(CASE WHEN i.ItemTypeID = 0 THEN 1 ELSE 0 END AS BIT)
                        , OtherCheck1  = i.OtherCheck1
	                    , OtherCheck2  = i.OtherCheck2
	                    , OtherCheck3  = i.OtherCheck3
	                    , OtherCheck4  = i.OtherCheck4
	                    , OtherCheck5  = i.OtherCheck5
                    FROM Items AS i
						LEFT JOIN ItemLanguages AS il
		                    ON il.ItemID = i.ItemID
						    AND il.LanguageID = 0
					WHERE 
                        i.ItemCode = @itemCode";
                var apiItems = context.Query<Item>(sql
                    , new
                    {
                        itemCode = itemCode
                    }).FirstOrDefault();
                return apiItems;
            }
        }
        public static IEnumerable<Item> GetItemPrices(List<string> itemCodes, IOrderConfiguration configuration)
        {

            #region Items
            // get the item information             
            var items = GetItemPriceInformation(itemCodes, configuration);
            #endregion

            // Return the data
            foreach (var item in items)
            {
                yield return item;
            }
        }
        private static List<Item> GetItemPriceInformation(List<string> itemCodes, IOrderConfiguration configuration)
        {
            using (var context = SQLContext.Sql())
            {
                var sql = $@"
                	SELECT
	                    i.ItemID
	                    , i.ItemCode
                        , Price = ip.Price
                        , OtherPrice1 = ip.Other1Price
                        , OtherPrice2 = ip.Other2Price";
                sql += @"
                    FROM Items AS i
	                    LEFT JOIN ItemPrices AS ip
		                    ON ip.ItemID = i.ItemID
					WHERE 
                        i.ItemCode in @itemCodes
                        AND ip.PriceTypeID = @priceTypeID
						AND ip.CurrencyCode = @currencyCode";
                var apiItems = context.Query<Item>(sql
                    , new
                    {
                        warehouse = configuration.WarehouseID,
                        priceTypeID = configuration.PriceTypeID,
                        currencyCode = configuration.CurrencyCode,
                        itemCodes = itemCodes
                    }).ToList();
                return apiItems;
            }
        }
        #endregion
    }
}