using Dapper;
using ShopifyApp.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Web;

namespace ShopifyApp.Data
{
    public static class SQLContext
    {
        public static SqlConnection Sql(bool isSandbox = false)
        {
            if(isSandbox)
                return new SqlConnection(Settings.ConnectionString);
            else
                return new SqlConnection(Settings.ConnectionString);

        }

    }
}