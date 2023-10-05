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
        #region Setup
        public static List<SelectListItem> GetAvailableCurrencies()
                {
                    try
                    {
                        using (var context = SQLContext.Sql())
                        {
                            return context.Query<SelectListItem>($"Select Text = CurrencyDescription, Value = CurrencyCode from Currencies").ToList();
                        }
                    }
                    catch
                    {
                        return new List<SelectListItem>();
                    }
                }
                public static List<SelectListItem> GetAvailableWarehouses()
                {
                    try
                    {
                        using (var context = SQLContext.Sql())
                        {
                            return context.Query<SelectListItem>($"Select Text = WarehouseDescription, Value = WarehouseID from Warehouses").ToList();
                        }
                    }
                    catch
                    {
                        return new List<SelectListItem>();
                    }
                }
                public static List<SelectListItem> GetAvailableCountries()
                {
                    try
                    {
                        using (var context = SQLContext.Sql())
                        {
                            return context.Query<SelectListItem>($"Select Text = CountryDescription, Value = CountryCode from Countries").ToList();
                        }
                    }
                    catch
                    {
                        return new List<SelectListItem>();
                    }
                }
                public static List<SelectListItem> GetAvailablePriceTypes()
                {
                    try
                    {
                        using (var context = SQLContext.Sql())
                        {
                            return context.Query<SelectListItem>($"Select Text = PriceTypeDescription, Value = PriceTypeID from PriceTypes").ToList();
                        }
                    }
                    catch
                    {
                        return new List<SelectListItem>();
                    }
                }
                public static List<SelectListItem> GetAvailableLanguages()
                {
                    try
                    {
                        using (var context = SQLContext.Sql())
                        {
                            return context.Query<SelectListItem>($"Select Text = LanguageDescription, Value = LanguageID from Languages").ToList();
                        }
                    }
                    catch
                    {
                        return new List<SelectListItem>();
                    }
                }
                public static List<SelectListItem> GetAvailableShipMethhods(int warehouseId)
                {
                    try
                    {
                        using (var context = SQLContext.Sql())
                        {
                            return context.Query<SelectListItem>($"Select Text = ShipMethodDescription, Value = ShipMethodID from ShipMethods where WarehouseID = {warehouseId}").ToList();
                        }
                    }
                    catch
                    {
                        return new List<SelectListItem>();
                    }
                }
                public static List<SelectListItem> GetAvailableWebCats()
                {
                    try
                    {
                        using (var context = SQLContext.Sql())
                        {

                            var cats = context.Query<Models.WebCategory>($"SELECT WebCategoryID, WebCategoryDescription, ParentID, NestedLevel FROM WebCategories").ToList();
                            var parentId = 0;
                            var list = cascadeWebCats(cats, parentId);

                            return list;
                        }
                    }
                    catch
                    {
                        return new List<SelectListItem>();
                    }
                }
                private static List<SelectListItem> cascadeWebCats(List<Models.WebCategory> webCats, int parentId, string name = "")
                {
                    var list = new List<SelectListItem>();
                    var subCats = webCats.Where(c => c.ParentID == parentId).ToList();
                    var newName = "";
                    foreach (var subCat in subCats)
                    { 
                        newName = name + "/" + subCat.WebCategoryDescription;
                        list.Add(new SelectListItem
                        {
                            Text = newName,
                            Value = subCat.WebCategoryID.ToString()
                        });
                        parentId = subCat.WebCategoryID;
                        list.AddRange(cascadeWebCats(webCats, parentId, newName));
                    }
                    return list;
                }
                #endregion

    }
}