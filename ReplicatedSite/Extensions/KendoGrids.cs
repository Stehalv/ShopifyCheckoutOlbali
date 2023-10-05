using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common.Services;
using Common.Helpers;

namespace ExigoWeb.Kendo
{
    public static class KendoGridResponseExtensions
    {
        public static KendoGridResponse Tokenize(this KendoGridResponse response, int customerId, params string[] properties)
        {
            var results = new List<dynamic>();

            foreach (var item in response.Data)
            {
                var newItem = item as IDictionary<String, object>;
                foreach (var prop in properties)
                {
                    var value = newItem[prop];
                    var token = Security.Encrypt(value, customerId);

                    newItem = DynamicHelper.AddProperty(item, prop + "Token", token);
                }

                results.Add(newItem);
            }

            response.Data = results;

            return response;
        }
    }
}