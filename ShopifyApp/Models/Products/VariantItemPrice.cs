using Dapper;
using ShopifyApp.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ShopifyApp.Models
{
    public class VariantItemPrice
    {
        public VariantItemPrice()
        {

        }
        public VariantItemPrice(string sku)
        {
            SKU = sku;
        }
        public int Id { get; set; }
        public string PriceType { get; set; }
        public string SKU { get; set; }
        public long ShopVariantId { get; set; }
        public decimal ExigoPrice { get; set; }
        public decimal ShopifyPrice { get; set; }
        public decimal BV { get; set; }
        public decimal CV { get; set; }
        public decimal QV { get; set; }
        public decimal ShopifyBV { get; set; }
        public decimal ShopifyCV { get; set; }
        public decimal ShopifyQV { get; set; }
        public bool Insync
        {
            get
            {
                return ExigoPrice == ShopifyPrice;
            }
        }
        public int TenantConfigId { get; set; }
        public List<VariantItemPrice> GetAllForTenantConfig(int tenantConfigId)
        {
            using (var sql = SQLContext.Sql())
            {
                return sql.Query<VariantItemPrice>($"Select * from {Settings.DatabaseContext}VariantPrices where TenantConfigId = {tenantConfigId}").ToList();
            }
        }
        public List<VariantItemPrice> GetItemPricesBySku()
        {
            using (var sql = SQLContext.Sql())
            {
                return sql.Query<VariantItemPrice>($"Select * from {Settings.DatabaseContext}VariantPrices Where SKU = '{SKU}'").ToList();
            }
        }
        public List<VariantItemPrice> GetItemPricesById()
        {
            using (var sql = SQLContext.Sql())
            {
                return sql.Query<VariantItemPrice>($"Select * from {Settings.DatabaseContext}VariantPrices Where ShopVariantId = {ShopVariantId}").ToList();
            }
        }
        public void Create()
        {
            using (var sql = SQLContext.Sql())
            {
                sql.Query($"INSERT INTO {Settings.DatabaseContext}VariantPrices (PriceType, ShopifyPrice, ExigoPrice, SKU, ShopVariantId, TenantConfigId, CreatedDate, ModifiedDate, ShopifyBV, ShopifyCV, ShopifyQV, BV, CV, QV) VALUES ('{PriceType}', {ShopifyPrice}, {ExigoPrice}, '{SKU}', {ShopVariantId}, {TenantConfigId}, getDate(), GetDate(), {ShopifyBV}, {ShopifyCV}, {ShopifyQV}, {BV}, {CV}, {QV})");

            }
        }
        public void UpdateExigoPrice()
        {
            using (var sql = SQLContext.Sql())
            {
                sql.Query($"Update {Settings.DatabaseContext}VariantPrices SET ExigoPrice = {ExigoPrice}, BV = {BV}, CV = {CV}, QV = {QV} WHERE Id = '{Id}'");
            }
        }
        public void UpdateShopifyPrice()
        {
            using (var sql = SQLContext.Sql())
            {
                sql.Query($"Update {Settings.DatabaseContext}VariantPrices SET ShopifyPrice = {ShopifyPrice}, ShopifyBV = {ShopifyBV}, ShopifyCV = {ShopifyCV}, ShopifyQV = {ShopifyQV} WHERE Id = '{Id}'");
            }
        }
    }
}