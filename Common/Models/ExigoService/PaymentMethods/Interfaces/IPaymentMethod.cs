using System.Web.Mvc;

namespace ExigoService
{
    [ModelBinder(typeof(IPaymentMethodModelBinder))]
    public interface IPaymentMethod
    {
        bool TestMode { get; set; }
        bool IsComplete { get; }
        bool IsValid { get; }
    }
}