using System.Collections.Generic;

namespace ExigoService
{
    public class OrderCalculationResponse
    {
        public decimal Subtotal { get; set; }
        public decimal Shipping { get; set; }
        public decimal Tax { get; set; }
        public decimal Discount { get; set; }
        public decimal Total { get; set; }
        public decimal BusinessVolumeTotal { get; set; }
        public decimal CommissionsableVolumeTotal { get; set; }
        public decimal Other1Total { get; set; }
        public decimal Other2Total { get; set; }

        public IEnumerable<ShipMethod> ShipMethods { get; set; }
    }
}