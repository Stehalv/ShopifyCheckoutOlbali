using Dapper;
using ShopifyApp.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ShopifyApp.Models
{
    public class User : IEntity
    {
        public User()
        { }
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public int UserRole { get; set; }
        public int CreatedBy { get; set; }
        public int ModifiedBy { get; set; }
        public Nullable<DateTime> Created { get; set; }
        public Nullable<DateTime> Modified { get; set; }

        public User Get(int id)
        {
            using (var sql = SQLContext.Sql())
            {
                var user = sql.Query<User>($"Select * from {Settings.DatabaseContext}Users where Id = {id}").FirstOrDefault();
                return user;
            }
        }
        public User GetByUserNameAndPassword(string userName, string password)
        {
            using (var sql = SQLContext.Sql())
            {
                var user = sql.Query<User>($"Select * from {Settings.DatabaseContext}Users where Email = '{userName}' and Password = '{password}'").FirstOrDefault();
                return user;
            }
        }
        public string GetPassword()
        {
            using (var sql = SQLContext.Sql())
            {
                return sql.Query<string>($"Select Password from {Settings.DatabaseContext}Users where Id = {Id}").FirstOrDefault();
            }
        }
        public List<User> GetAll()
        {
            using (var sql = SQLContext.Sql())
            {
                return sql.Query<User>($"Select * from {Settings.DatabaseContext}Users").ToList();
            }
        }
        public void Create()
        {
            using (var sql = SQLContext.Sql())
            {
                sql.Query($"INSERT INTO {Settings.DatabaseContext}Users (FirstName, LastName, Email, Password, UserRole, CreatedBy, ModifiedBy, Created) VALUES ('{FirstName}', '{LastName}', '{Email}', '{Password}', {UserRole}, {CreatedBy}, {ModifiedBy}, GetDate())");

            }
        }
        public void Update()
        {
            using (var sql = SQLContext.Sql())
            {
                sql.Query($"Update {Settings.DatabaseContext}Users SET FirstName = '{FirstName}', LastName = '{LastName}', Email = '{Email}', Password = '{Password}', UserRole = {UserRole}, ModifiedBy = {ModifiedBy}, Modified = GetDate()  WHERE Id = '{Id}'");
            }
        }
        public void Delete(int id)
        {
            using (var sql = SQLContext.Sql())
            {
                sql.Query<User>($"Delete from {Settings.DatabaseContext}Users Where Id = {id}");
            }
        }
        public void CreateTable(string context)
        {
            context = context.Replace(".", "");

            var sql1 = "SET ANSI_NULLS ON";

            var sql2 = "SET QUOTED_IDENTIFIER ON";

            var sql3 = $"CREATE TABLE[{context}].[Users](" +
                            "[Id][int] IDENTITY(1, 1) NOT NULL," +

                            "[FirstName] [nvarchar](4000) NULL," +
                            "[LastName] [nvarchar](4000) NULL," +
                            "[Email] [nvarchar](4000) NULL," +
                            "[Password] [nvarchar](4000) NULL," +
                            "[UserRole] [int] NOT NULL," +

                            "[CreatedBy] [int] NOT NULL," +

                            "[ModifiedBy] [int] NOT NULL," +

                            "[Created] [datetime] NULL," +
                            "[Modified] [datetime] NULL," +
                            "[RowGuid] [uniqueidentifier] NOT NULL," +

                            "[RowVersion] [bigint] NOT NULL," +
                         "CONSTRAINT[PK_Users] PRIMARY KEY CLUSTERED" +
                        "(" +

                           "[Id] ASC" +
                        ")WITH(PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON[PRIMARY]" +
                        ") ON[PRIMARY]";

            var sql4 = $"ALTER TABLE[{context}].[Users] ADD DEFAULT(newsequentialid()) FOR[RowGuid]";

            var sql5 = $"ALTER TABLE[{context}].[Users] ADD DEFAULT((1)) FOR[RowVersion]";


            using (var con = SQLContext.Sql())
            {
                var exists = con.Query<object>($"SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = '{context}' AND  TABLE_NAME = 'Users'").FirstOrDefault();
                if (exists == null)
                {
                    con.Query(sql1);
                    con.Query(sql2);
                    con.Query(sql3);
                    con.Query(sql4);
                    con.Query(sql5);
                    var user = new User
                    {
                        FirstName = "Admin",
                        LastName = "Admin",
                        UserRole = 3,
                        Email = "steinar@teqnavi.com",
                        Password = Extensions.Encrypt("2Wofrik!"),
                        CreatedBy = 0,
                        ModifiedBy = 0
                    };
                    user.Create();
                }
            }
        }
    }
    public class UserType
    {
        public int Value { get; set; }
        public string Name { get; set; }
    }
}