using ReplicatedSite.Models;
using ExigoService;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Common;
 
namespace ReplicatedSite.ViewModels
{
    public class PaymentMethodsViewModel : IShoppingViewModel
    { 
        public IEnumerable<IPaymentMethod> PaymentMethods { get; set; }
        public IEnumerable<Address> Addresses { get; set; }
        public ShoppingCartCheckoutPropertyBag PropertyBag { get; set; }
        public CustomerPointAccount PointAccount { get; set; }
        public bool HasValidPointAccount { get; set; }
        public bool UsePointsAsPayment { get; set; }
        public decimal QuantityOfPointsToUse { get; set; }

        public OrderCalculationResponse OrderTotals { get; set; }

        [Required, Display(Name = "CVV", ResourceType = typeof(Common.Resources.Models)), RegularExpression(GlobalSettings.RegularExpressions.CVV, ErrorMessageResourceName = "IncorrectCVV", ErrorMessageResourceType = typeof(Common.Resources.Models))]
        public string ExistingCardCVV { get; set; }

        public string[] Errors { get; set; }
    }
}