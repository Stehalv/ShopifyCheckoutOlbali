using System;
using System.Threading.Tasks;
using System.Web.Mvc;
using ShopifyApp;
using ShopifyApp.Models;
using ReplicatedSite.Areas.Shopify.Filters;

namespace ReplicatedSite.Areas.Shopify.Controllers
{
    [Filters.Authorize]
    [PermissionRequired(UserRole.SystemAdmin)]
    public class AdminUsersController : BaseController
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
        public async Task<ActionResult> Create([Bind(Include = "Id,FirstName,LastName,Email,Password")] User user)
        {
            user.UserRole = (int)UserRole.SystemAdmin;
            user.CreatedBy = Identity.Current.Id;
            user.Created = DateTime.Now;
            user.ModifiedBy = Identity.Current.Id;
            user.Modified = DateTime.Now;

            if (ModelState.IsValid)
            {
                user.Password = ShopifyApp.Extensions.Encrypt(user.Password);
                user.Create();
                new Log(LogType.Information, $"Systemuser {user.Id} Created by {Identity.Current.FullName}").Create();
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
        public async Task<ActionResult> Edit([Bind(Include = "Id,FirstName,LastName,Email,Password,Created,CreatedBy")] User user)
        {
            user.UserRole = (int)UserRole.SystemAdmin;
            if (user.Password.IsNotNullOrEmpty())
            {
                user.Password = ShopifyApp.Extensions.Encrypt(user.Password);
            }
            else
            {
                var oldPassword = user.GetPassword();
                user.Password = oldPassword;
            }
            user.ModifiedBy = Identity.Current.Id;
            if (ModelState.IsValid)
            {
                user.Update();
                new Log(LogType.Information, $"Systemuser {user.Id} modified by {Identity.Current.FullName}").Create();
                return RedirectToAction("Index");
            }
            return View(user);
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
            new Log(LogType.Information, $"Systemuser {id} Deleted by {Identity.Current.FullName}").Create();
            return RedirectToAction("Index");
        }
    }
}
