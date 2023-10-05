using Common;
using Dapper;
using ExigoService;
using ReplicatedSite.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ReplicatedSite.Services
{
    public static class ReportingService
    {
        public static dynamic MyCustomersList(int customerId)
        {
            var customerID = customerId;
            var preferredCustomerTypes = new List<int> { CustomerTypes.RetailCustomer, CustomerTypes.PreferredCustomer };

            using (var context = ExigoDAL.Sql())
            {
                return context.Query(@"
                        SELECT 
	                          c.CustomerID
	                        , c.FirstName
	                        , c.LastName
	                        , CAST(CreatedDate as date) AS 'CreatedDate'
	                        , c.Email
	                        , c.Phone
                            , c.CustomerTypeID
                            , cs.CustomerTypeDescription
                        ,ISNULL(cpa.PointBalance, 0) as 'PointBalance'
                        FROM
                            EnrollerDownline ud
                        INNER JOIN Customers c 
	                            ON c.CustomerID = ud.CustomerID
                                AND c.CustomerTypeID IN @customertypes
                        Left Join CustomerPointAccounts cpa
                                on cpa.CustomerID = c.CustomerID
                                and cpa.PointAccountID = 1
                        Left Join CustomerTypes cs on cs.CustomerTypeID = c.CustomerTypeID
                        WHERE 
                            ud.DownlineCustomerID = @customerid
                            AND ud.Level = 1
                        ", new
                {
                    customertypes = preferredCustomerTypes,
                    customerid = customerID
                });
            }
        }

        public static dynamic DownlineOrdersList(int customerId, string TreeToUse = "UnilevelDownline")
        {

            var maxDayRange = 90;
            var currentcustomerID = customerId;
            // Fetch the data
            using (var context = ExigoDAL.Sql())
            {
                var Rows = context.Query($@"
                    SELECT 
                          ud.CustomerID		                
                        , CurrencyCode = o.CurrencyCode
		                , o.OrderID
                        , o.OrderStatusID
		                , o.Total
                        , os.OrderStatusDescription
	                    , o.FirstName
	                    , o.LastName 
                        , o.BusinessVolumeTotal
		                , OrderDate = CAST(o.OrderDate AS date)
                    FROM
	                    Orders o
	                    INNER JOIN {TreeToUse} ud
		                    ON ud.CustomerID = o.CustomerID
                        LEFT JOIN OrderStatuses os on os.OrderSTatusID = o.OrderStatusID
                    WHERE
	                    ud.DownlineCustomerID = @downlinecustomerid
                        AND o.OrderDate > GETDATE() - @maxdayrange
                        AND o.OrderStatusID >= 7
                        AND ud.CustomerID <> @customerid
                ", new
                {
                    downlinecustomerid = currentcustomerID,
                    maxdayrange = maxDayRange,
                    customerid = currentcustomerID
                }).ToList();
                return Rows;
            }
        }

        public static dynamic PersonallyEnrolledList(int customerId)
        {
            var customerID = 3;
            var distributorCustomerTypes = new List<int> { (int)CustomerTypes.Distributor, (int)CustomerTypes.RetailCustomer, (int)CustomerTypes.PreferredCustomer };
            using (var context = ExigoDAL.Sql())
            {
                return context.Query($@"
                    SELECT 
                         c.CustomerID
                        ,c.FirstName
                        ,c.LastName
                        ,CAST(c.CreatedDate as date) AS 'CreatedDate'
                        ,c.Email
                        ,c.Phone
                        ,c.CustomerTypeID
                        ,ct.CustomerTypeDescription
                        ,case when c.CustomerTypeID in (1,2) then ISNULL(cpa1.PointBalance, 0) else ISNULL(cpa1.PointBalance, 0) end  as 'PointBalance'

                    FROM EnrollerDownline ed
                        INNER JOIN Customers c 
	                        ON c.CustomerID = ed.CustomerID
                            AND c.CustomerTypeID IN @customertypes
                      Left Join CustomerPointAccounts cpa
                        on cpa.CustomerID = c.CustomerID
                        and cpa.PointAccountID = 2
                      Left Join CustomerPointAccounts cpa1
                        on cpa1.CustomerID = c.CustomerID
                        and cpa1.PointAccountID = 1
                      Left Join CustomerTypes ct on c.CustomerTypeID = ct.CustomerTypeID
                    WHERE ed.DownlineCustomerID = @CustomerID
                        AND c.EnrollerID = @customerid
            ", new
                {
                    customertypes = distributorCustomerTypes,
                    customerid = customerID
                });
            }
        }

        public static List<dynamic> GetRecentDownlineOrders(int customerId)
        {

            var maxDayRange = 30;
            var currentcustomerID = customerId;
            // Fetch the data
            using (var context = ExigoDAL.Sql())
            {
                return context.Query<dynamic>($@"
                    SELECT 
                          ud.CustomerID		                
                        , CurrencyCode = o.CurrencyCode
                        , CountryCode = o.Country
	                    , c.FirstName
	                    , c.LastName
		                , o.OrderID
		                , o.Total
		                , o.OrderStatusID
		                , o.BusinessVolumeTotal
		                , OrderDate = CAST(o.OrderDate AS date)
                    FROM
	                    Orders o
	                    INNER JOIN EnrollerDownline ud
		                    ON ud.CustomerID = o.CustomerID
                            AND ud.Level < 2
	                    INNER JOIN Customers c
		                    ON c.CustomerID = ud.CustomerID
                    WHERE
	                    ud.DownlineCustomerID = @downlinecustomerid
                        AND o.OrderDate > GETDATE() - @maxdayrange
                        AND o.OrderStatusID >= 7
                    ORDER BY o.OrderDate DESC
                ", new
                {
                    downlinecustomerid = currentcustomerID,
                    maxdayrange = maxDayRange
                }).ToList();
            }
        }

        public static PointAccountTransactionsViewModel GetPointTransactions(int customerId)
        {
            var model = new PointAccountTransactionsViewModel();
            model.PointAccountDescription = "";

            using (var context = ExigoDAL.Sql())
            {
                model.Transactions = context.Query<PointAccountTransaction>(@"
                SELECT PointTransactionID
                    , TransactionDate
                    , Amount AS Points
                    , Reference AS Reason
                FROM PointTransactions
                WHERE CustomerID = @customerid
                    AND PointAccountID = @pointAccountID
                ORDER BY TransactionDate desc
                ", new
                {
                    customerId,
                    pointAccountID = PointAccounts.PowerStartPoints
                }).ToList();
            }

            if (model.Transactions.Count() > 0)
            {
                model.PointAccountBalance = model.Transactions.Sum(t => t.Points);
            }
            model.Transactions = model.Transactions.Take(5).ToList();
            return model;
        }

        public static IEnumerable<CustomerWallItem> GetRecentActivity(int customerId)
        {
            return ExigoDAL.GetCustomerRecentActivity(new GetCustomerRecentActivityRequest
            {
                CustomerID = customerId,
                Page = 1,
                RowCount = 50
            });
            //}).Tokenize();

        }
        public static GetCustomerRankQualificationsResponse GetRankAdvancementCard(int customerId, int rankid)
        {
            var ranks = RankService.GetRanks().ToList();
            var customerID = customerId;

            GetCustomerRankQualificationsResponse model = null;

            // Check to ensure that the rank we are checking is not the last rank.
            // If so, return a null. Our view will take care of nulls specially.
            if (ranks.Last().RankID != rankid)
            {
                var nextRankID = ranks.OrderBy(c => c.RankID).Where(c => c.RankID > rankid).FirstOrDefault().RankID;
                model = RankQualificationService.GetCustomerRankQualifications(new GetCustomerRankQualificationsRequest
                {
                    CustomerID = customerID,
                    PeriodTypeID = PeriodTypes.Default,
                    RankID = nextRankID
                });
            }
            return model;
        }
        #region customerspecific reports
        public static List<dynamic> GetPowerStart(int customerId)
        {
            var currentcustomerID = customerId;
            var periodID = ExigoDAL.GetCurrentPeriod(PeriodTypes.Default).PeriodID;

            // Fetch the data
            using (var context = ExigoDAL.Sql())
            {
                return context.Query<dynamic>($@"
                SELECT 
	                c.CreatedDate
	                , pv.Volume20 AS FastStartPersonalQualificationVolume
	                , pv.Volume8 AS CurrentAmount
	                , pa.PointBalance AS PointAccountBalance
	                , SUM(pt.Amount) AS PointAccountTransactionBalance
                FROM PeriodVolumes pv
                INNER JOIN Customers c
	                ON pv.CustomerID = c.CustomerID
                LEFT JOIN CustomerPointAccounts pa
	                ON pa.CustomerID = c.CustomerID
	                AND pa.PointAccountID = 2
                LEFT JOIN PointTransactions pt
	                ON pt.CustomerID = pv.CustomerID
	                AND pt.PointAccountID = 2
	                AND pt.Amount > 0
                WHERE pv.CustomerID = @customerID
                AND pv.PeriodTypeID = 1
                AND pv.PeriodID = @periodID
                GROUP BY c.CreatedDate, pv.Volume20, pv.Volume8, pa.PointBalance
            ", new
                {
                    customerID = currentcustomerID,
                    periodID
                }).FirstOrDefault();
            }
        }
        #endregion
    }
}