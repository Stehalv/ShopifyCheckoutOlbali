
using ShopifyApp.Models;
using ShopifyApp.Services;
using ShopifyApp.Services.ShopService;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using ShopifyApp;
using ReplicatedSite.Areas.Shopify.ViewModels;

namespace ReplicatedSite.Areas.Shopify.Controllers
{
    [Filters.Authorize]
    public class OrderController : BaseController
    {
        // GET: Orders
        public ActionResult Index(int currentPage = 1, int pageSize = 50, int totalOrders = 0)
        {
            var order = new Order();
            var model = new OrdersViewModel();
            model.Tenant = tenant;
            model.CurrentPage = currentPage;
            model.PageSize = pageSize;
            model.TotalOrders = (totalOrders == 0) ? order.OrderCount() : totalOrders;
            model.StartRowNumber = (currentPage - 1) * pageSize + 1;
            model.EndRowNumber = (currentPage * pageSize > model.TotalOrders) ? model.TotalOrders : currentPage * pageSize;
            model.Orders = order.LoadPaginatedOrders(currentPage, pageSize, model.StartRowNumber, model.EndRowNumber);
            return View(model);
        }
        public async Task<ActionResult> OrderDetails(int id)
        {
            var model = new OrderDetailsViewModel(id);
            await model.Populate();
            return View(model);
        }
    }
}