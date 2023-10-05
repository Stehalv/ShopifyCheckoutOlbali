using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ReplicatedSite.Models.Reporting
{
    public class ReportModel
    {
        public string name { get; set; }
        public string[] TableHeaders { get; set; }
        public List<TableRow> TableRows { get; set; }
        public string[] Columns { get; set; }
        public string Table { get; set; }
        public List<KeyValuePair<string, dynamic>> Parameters { get; set; }
    }
    public class TableRow
    {
        public string name { get; set; }
        public string index { get; set; }
        public int width { get; set; }
        public bool sortable { get; set; }
    }
}