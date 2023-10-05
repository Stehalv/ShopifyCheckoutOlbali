using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShopifyApp.Models
{
    public interface IEntity
    {
        void CreateTable(string context);
    }
}
