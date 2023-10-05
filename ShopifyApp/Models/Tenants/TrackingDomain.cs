using Dapper;
using ShopifyApp.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ShopifyApp.Models
{
    public class TrackingDomain : IEntity
    {
        public int id { get; set; }
        public int TenantConfigurationId { get; set; }
        public string Domain { get; set; }
        public int CreatedBy { get; set; }
        public int ModifiedBy { get; set; }
        public Nullable<DateTime> Created { get; set; }
        public Nullable<DateTime> Modified { get; set; }

        public TrackingDomain Get(int id)
        {
            using (var sql = SQLContext.Sql())
            {
                return sql.Query<TrackingDomain>($"Select * from {Settings.DatabaseContext}TrackingDomains Where Id = {id}").FirstOrDefault();
            }
        }
        public List<TrackingDomain> GetByConfig(int id)
        {
            using (var sql = SQLContext.Sql())
            {
                return sql.Query<TrackingDomain>($"Select * from {Settings.DatabaseContext}TrackingDomains Where TenantConfigurationId = {id}").ToList();
            }
        }
        public TrackingDomain GetByDomain(string domain)
        {
            using (var sql = SQLContext.Sql())
            {
                return sql.Query<TrackingDomain>($"Select * from {Settings.DatabaseContext}TrackingDomains Where Domain = '{domain}'").FirstOrDefault();
            }
        }
        public void Create()
        {
            using (var sql = SQLContext.Sql())
            {
                sql.Query($"INSERT INTO {Settings.DatabaseContext}TrackingDomains (TenantConfigurationId, Domain, CreatedBy, ModifiedBy, Created) VALUES ({TenantConfigurationId}, '{Domain}', {CreatedBy}, {ModifiedBy}, GetDate())");

            }
        }
        public void Delete(int id)
        {
            using (var sql = SQLContext.Sql())
            {
                sql.Query($"Delete from {Settings.DatabaseContext}TrackingDomains Where Id = {id}");
            }
        }
        public void CreateTable(string context)
        {
            context = context.Replace(".", "");
            var sql1 = @"SET ANSI_NULLS ON";

            var sql2 = @"SET QUOTED_IDENTIFIER ON";

            var sql3 = @"CREATE TABLE [" + context + "].[TrackingDomains](" +
                "[Id][int] IDENTITY(1, 1) NOT NULL," +
                "[Domain] [nvarchar](4000) NULL," +
                "[TenantConfigurationId] [int] NOT NULL," +
                "[CreatedBy] [int] NOT NULL," +
                "[ModifiedBy] [int] NOT NULL," +
                "[Created] [datetime] NULL," +
                "[Modified] [nvarchar](4000) NULL," +
                "[RowGuid] [uniqueidentifier] NOT NULL," +
                "[RowVersion] [bigint] NOT NULL," +
                "CONSTRAINT [PK_TrackingDomains] PRIMARY KEY CLUSTERED " +
                "([Id] ASC)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]) ON [PRIMARY]";

            var sql4 = $"ALTER TABLE [{context}].[TrackingDomains] ADD  DEFAULT (newsequentialid()) FOR [RowGuid]";

            var sql5 = $"ALTER TABLE [{context}].[TrackingDomains] ADD  DEFAULT ((1)) FOR [RowVersion]";

            using (var con = SQLContext.Sql())
            {
                var exists = con.Query<object>($"SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = '{context}' AND  TABLE_NAME = 'TrackingDomains'").FirstOrDefault();
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