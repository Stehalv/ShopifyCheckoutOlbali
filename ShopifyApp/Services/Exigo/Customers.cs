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
        #region Customer
        public static int GetCustomerType(TenantConfiguration tenantConfig, int customerId)
        {
            using (var context = SQLContext.Sql())
            {
                return context.Query<int>($"Select CustomerTypeID from Customers Where CustomerID = {customerId}").First();
            }
        }
        public static CustomerSite GetEnrollerByWebalias(string webalias)
        {
            using (var context = SQLContext.Sql())
            {
                return context.Query<CustomerSite>($"Select c.FirstName, c.LastName, c.Email, c.CustomerID, CustomerTypeID, cs.WebAlias from CustomerSites cs Inner join Customers c on c.CustomerID = cs.CustomerID Where Webalias = @webalias", new
                {
                    webalias
                }).First();
            }
        }
        public static int CheckCustomerByEmail(string email,TenantConfiguration config, bool realtime = false)
        {
            try
            {
                if (!realtime)
                {
                    using (var context = SQLContext.Sql(config.UseSandbox))
                    {
                        var customerId = context.Query<int>($"Select CustomerID from Customers where Email = @email", new
                        {
                            email
                        }).FirstOrDefault();
                        if (customerId == 0)
                        {
                            return 0;
                        }
                        else
                        {
                            return customerId;
                        }
                    }
                }
                else
                {
                    var customerId = Exigo.WebService(config).GetCustomers(new GetCustomersRequest
                    {
                        Email = email
                    }).Customers.First().CustomerID;
                    return customerId;
                }
            }
            catch
            {
                return 0;
            }
        }
        public static bool CheckIfCustomerIsEmailOnly(int customerId, TenantConfiguration config, bool realtime = false)
        {
            try
            {
                if (!realtime)
                {
                    using (var context = SQLContext.Sql(config.UseSandbox))
                    {
                        var customerType = context.Query<int>($"Select CustomerTypeId from Customers where CustomerId = @customerId", new
                        {
                            customerId
                        }).FirstOrDefault();
                        if (customerType == (int)CustomerTypes.Newsletter)
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                }
                else
                {
                    var customerType = Exigo.WebService(config).GetCustomers(new GetCustomersRequest
                    {
                        CustomerID = customerId
                    }).Customers.First().CustomerType;

                    if (customerType == (int)CustomerTypes.Newsletter)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            catch
            {
                return false;
            }
        }
        #endregion

    }
}