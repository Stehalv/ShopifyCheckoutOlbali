using Common.Services;
using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ReplicatedSite.Areas.Shopify.Controllers
{
    [AllowAnonymous]
    public class ReferralController : Controller
    {
        public JsonNetResult EnrollerSearch(string query)
        {
            var result = ShopifyApp.Services.Exigo.SearchForEnroller(query);
            return new JsonNetResult(new
            {
                success = true,
                json = result
            });
        }
        public JsonNetResult GetEnroller(string webalias)
        {
            var enroller = ShopifyApp.Services.Exigo.GetEnrollerByWebalias(webalias);
            return new JsonNetResult(new
            {
                success = true,
                json = enroller
            });
        }
        public ActionResult GoToShopifyCustomer(string token)
        {

            var IV = Common.GlobalSettings.EncryptionKeys.SilentLogins.IV;
            var key = Common.GlobalSettings.EncryptionKeys.SilentLogins.Key;
            var decryptedToken = Security.AESDecrypt(token, key, IV);
            var splitToken = decryptedToken.Split('|');
            var customerID = Convert.ToInt32(splitToken[0]);
            var customer = new ShopifyApp.Models.Customer().GetByExigoId(customerID);
            return Redirect("https://lorde-and-belle.myshopify.com/admin/customers/" + customer.ShopCustomerId);
        }
    }
}