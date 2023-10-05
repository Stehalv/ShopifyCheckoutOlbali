using System;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Web;
using System.Web.Mvc;
using ShopifyApp.Models;
using ShopifyApp;
using ReplicatedSite.Areas.Shopify.Filters;

namespace ReplicatedSite.Areas.Shopify.Controllers
{
    [Filters.Authorize]
    [PermissionRequired(UserRole.Admin)]
    public class UsersController : BaseController
    {

        // GET: Users
        public async Task<ActionResult> Index()
        {
            return View(new User().GetAll());
        }
        // GET: Users/Details/5
        public async Task<ActionResult> Details(int id)
        {
            User user = new User().Get(id);
            if (user == null)
            {
                return HttpNotFound();
            }
            return View(user);
        }

        // GET: Users/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Users/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind(Include = "Id,FirstName,LastName,Email,Password,UserRole")] User user)
        {
            user.CreatedBy = Identity.Current.Id;
            user.Created = DateTime.Now;
            user.ModifiedBy = Identity.Current.Id;
            user.Modified = DateTime.Now;

            if (ModelState.IsValid)
            {
                user.Password = ShopifyApp.Extensions.Encrypt(user.Password);
                user.Create();
                new Log(LogType.Information, $"User {user.Id} Created by {Identity.Current.FullName}").Create();
                return RedirectToAction("Index");
            }

            return View(user);
        }

        // GET: Users/Edit/5
        public async Task<ActionResult> Edit(int id)
        {
            User user = new User().Get(id);
            user.Password = "";
            if (user == null)
            {
                return HttpNotFound();
            }
            return View(user);
        }

        // POST: Users/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind(Include = "Id,FirstName,LastName,Email,Password,UserRole,Created,CreatedBy")] User user)
        {
            if (user.Password.IsNotNullOrEmpty())
            {
                user.Password = ShopifyApp.Extensions.Encrypt(user.Password);
            }
            else
            {
                user.Password = user.GetPassword();
            }
            user.ModifiedBy = Identity.Current.Id;
            user.Modified = DateTime.Now;
            user.Update();
            new Log(LogType.Information, $"User {user.Id} modeified by {Identity.Current.FullName}").Create();
            return RedirectToAction("Index");
        }

        // GET: Users/Delete/5
        public async Task<ActionResult> Delete(int id)
        {
            User user = new User().Get(id);
            if (user == null)
            {
                return HttpNotFound();
            }
            return View(user);
        }

        // POST: Users/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            new User().Delete(id);
            new Log(LogType.Information, $"User {id} Deleted by {Identity.Current.FullName}").Create();
            return RedirectToAction("Index");
        }
    }
}
