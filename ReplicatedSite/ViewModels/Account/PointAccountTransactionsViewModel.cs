using System;
using System.Collections.Generic;

namespace ReplicatedSite.ViewModels
{
    public class PointAccountTransactionsViewModel
    {
        public PointAccountTransactionsViewModel()
        {
            Transactions = new List<PointAccountTransaction>();    
        }

        public string PointAccountDescription { get; set; }
        public decimal PointAccountBalance { get; set; }
        public List<PointAccountTransaction> Transactions { get; set; }
    }

    public class PointAccountTransaction
    {
        public DateTime TransactionDate { get; set; }
        public decimal Points { get; set; }
        public string RefType { get; set; }
        public int RefID { get; set; }
        public string Reason { get; set; }
        public string CreatedBy { get; set; }
    }
}