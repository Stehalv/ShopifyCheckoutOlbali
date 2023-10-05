using Dapper;
using Newtonsoft.Json;
using ShopifyApp.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ShopifyApp.Models
{
    public class TenantOrderConfiguration : IOrderConfiguration,  IEntity
    {
        public int Id { get; set; }
        public int WarehouseID { get; set; }
        public int PriceTypeID { get; set; }
        public string CurrencyCode { get; set; }
        public int LanguageID { get; set; }
        public string DefaultCountryCode { get; set; }
        public int DefaultShipMethodID { get; set; }
        public int FreeShippingId { get; set; }
        public int CategoryID { get; set; }
        public int FeaturedCategoryID { get; set; }
        public List<int> AvailableShipMethods { get; set; }
        public int CreatedBy { get; set; }
        public int ModifiedBy { get; set; }
        public Nullable<DateTime> Created { get; set; }
        public Nullable<DateTime> Modified { get; set; }
        public string ConnectionString => Settings.ConnectionString;
        public static explicit operator TenantOrderConfiguration(string c)
        {

            TenantOrderConfiguration a = JsonConvert.DeserializeObject<TenantOrderConfiguration>(c);
            return a;
        }
        public TenantOrderConfiguration Get(int id)
        {
            using (var sql = SQLContext.Sql())
            {
                var config = sql.Query<TenantOrderConfiguration>($"Select * from {Settings.DatabaseContext}TenantOrderConfigurations Where Id = {id}").FirstOrDefault();
                return config;
            }
        }
        public List<TenantOrderConfiguration> GetAll()
        {
            using (var sql = SQLContext.Sql())
            {
                return sql.Query<TenantOrderConfiguration>($"Select * from {Settings.DatabaseContext}TenantOrderConfigurations").ToList();
            }
        }
        public void Create()
        {
            using (var sql = SQLContext.Sql())
            {
                sql.Query($"INSERT INTO {Settings.DatabaseContext}TenantOrderConfigurations (WarehouseID, PriceTypeID, CurrencyCode, LanguageID, DefaultCountryCode, DefaultShipMethodID, FreeShippingId, CategoryID, FeaturedCategoryID, CreatedBy, ModifiedBy, Created) VALUES ({WarehouseID}, {PriceTypeID}, '{CurrencyCode}', {LanguageID}, '{DefaultCountryCode}', {DefaultShipMethodID}, {FreeShippingId}, {CategoryID}, {FeaturedCategoryID}, {CreatedBy}, {CreatedBy}, GetDate())");

            }
        }
        public void Update()
        {
            using (var sql = SQLContext.Sql())
            {
                sql.Query($"Update {Settings.DatabaseContext}TenantOrderConfigurations SET WarehouseID = {WarehouseID}, PriceTypeID = {PriceTypeID}, CurrencyCode = '{CurrencyCode}', LanguageID = {LanguageID}, DefaultCountryCode = '{DefaultCountryCode}', DefaultShipMethodID = {DefaultShipMethodID}, FreeShippingId = {FreeShippingId}, CategoryID = {CategoryID}, FeaturedCategoryID = {FeaturedCategoryID}, ModifiedBy = {ModifiedBy}, Modified = GetDate()  WHERE Id = '{Id}'");
            }
        }
        public void Delete()
        {
            using (var sql = SQLContext.Sql())
            {
                sql.Query<TenantOrderConfiguration>($"Delete from {Settings.DatabaseContext}TenantOrderConfigurations Where Id = {Id}");
            }
        }
        public void CreateTable(string context)
        {
            context = context.Replace(".", "");
            var sql1 = @"SET ANSI_NULLS ON";

            var sql2 = "SET QUOTED_IDENTIFIER ON";

            var sql3 = $"CREATE TABLE [{context}].[TenantOrderConfigurations](" +
                        "[Id][int] IDENTITY(1, 1) NOT NULL," +
                        "[WarehouseID] [int] NOT NULL," +
                        "[PriceTypeID] [int] NOT NULL," +
                        "[CurrencyCode] [nvarchar](4000) NULL," +
                        "[LanguageID] [int] NOT NULL," +
                        "[DefaultCountryCode] [nvarchar](4000) NULL," + 
                        "[DefaultShipMethodID] [int] NOT NULL," +
                        "[CategoryID] [int] NOT NULL," +
                        "[FeaturedCategoryID] [int] NOT NULL," +
                        "[CreatedBy] [int] NOT NULL," +
                        "[ModifiedBy] [int] NOT NULL," +
                        "[Created] [datetime] NULL," +
                        "[Modified] [datetime] NULL," +
                        "[FreeShippingId] [int] NOT NULL," +
                        "[RowGuid] [uniqueidentifier] NOT NULL," +
                        "[RowVersion] [bigint] NOT NULL," +
                        "CONSTRAINT [PK_TenantOrderConfigurations] PRIMARY KEY CLUSTERED " +
                        "(" +
                            "[Id] ASC" +
                        ")WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]" +
                        ") ON [PRIMARY]";

            var sql4 = $"ALTER TABLE [{context}].[TenantOrderConfigurations] ADD  DEFAULT (newsequentialid()) FOR [RowGuid]";

            var sql5 = $"ALTER TABLE [{context}].[TenantOrderConfigurations] ADD  DEFAULT ((1)) FOR [RowVersion]";
            using (var con = SQLContext.Sql())
            {
                var exists = con.Query<object>($"SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = '{context}' AND  TABLE_NAME = 'TenantOrderConfigurations'").FirstOrDefault();
                if (exists == null)
                {
                    con.Query(sql1);
                    con.Query(sql2);
                    con.Query(sql3);
                    con.Query(sql4);
                    con.Query(sql5);
                }
            }
        }
    }
}