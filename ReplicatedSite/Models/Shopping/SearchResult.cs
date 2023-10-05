using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ReplicatedSite.Models
{
    public class SearchResult
    {
        public int CustomerID { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string FullName
        {
            get { return this.FirstName + " " + this.LastName; }
        }
        public string AvatarURL { get; set; }
        public string WebAlias { get; set; }
        public string ReplicatedSiteUrl
        {
            get
            {
                if (string.IsNullOrEmpty(this.WebAlias)) return "";
                else return GlobalSettings.ReplicatedSites.GetFormattedUrl(WebAlias);
            }
        }

        public string MainState { get; set; }
        public string MainCity { get; set; }
        public string MainCountry { get; set; }
    }
}