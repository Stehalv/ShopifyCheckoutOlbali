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
        #region Referral
        public static List<dynamic> SearchForEnroller(string query)
        {
            var isCustomerID = query.CanBeParsedAs<int>();
            var nodeDataRecords = new List<dynamic>();
            if (isCustomerID)
            {
                using (var context = SQLContext.Sql())
                {
                    nodeDataRecords = context.Query(@"
                                    SELECT
                                        cs.CustomerID, cs.FirstName, cs.LastName, cs.WebAlias, c.Company,
                                        c.MainCity, c.MainState, c.MainCountry
                                    FROM CustomerSites cs
                                    INNER JOIN Customers c
                                    ON cs.CustomerID = c.CustomerID
                                    WHERE c.CustomerTypeID = @customertypeid
                                    AND cs.CustomerID = @customerid
                                    And ISNULL(cs.Webalias, '') <> ''
                                    AND (c.Field2 = 0 or c.Field2 = '')
                            ", new
                    {
                        customertypeid = (int)CustomerTypes.Distributor,
                        customerid = query
                    }).ToList();
                    return nodeDataRecords;
                }
            }
            else
            {
                using (var context = SQLContext.Sql())
                {
                    nodeDataRecords = context.Query(@"
                                    SELECT
                                        c.CustomerID, c.FirstName, c.LastName, cs.WebAlias, c.Company,
                                        c.MainCity, c.MainState, c.MainCountry
                                    FROM Customers c
                                    LEFT JOIN CustomerSites cs
                                    ON cs.CustomerID = c.CustomerID
                                    WHERE c.CustomerTypeID = @customertypeid
                                    And ISNULL(cs.Webalias, '') <> ''
                                    AND (c.FirstName + ' ' + c.LastName LIKE @queryValue OR c.FirstName LIKE @queryValue OR c.LastName LIKE @queryvalue OR c.Company LIKE @queryValue OR cs.FirstName LIKE @queryValue OR cs.LastName LIKE @queryValue OR c.MainCity LIKE @queryValue or c.MainState LIKE @queryValue)
                            ", new
                    {
                        customertypeid = (int)CustomerTypes.Distributor,
                        queryValue = "%" + query + "%"
                    }).ToList();
                    return nodeDataRecords;
                }
            }
        }
        #endregion

    }
}