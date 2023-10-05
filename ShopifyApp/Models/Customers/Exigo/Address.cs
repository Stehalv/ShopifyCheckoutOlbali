using System.ComponentModel.DataAnnotations;
using System.Web.WebPages;

namespace ShopifyApp.Models
{
    public class Address 
    {
        public Address()
        {
            this.AddressType = AddressType.New;
        }
        public Address(string country, string state) : this()
        {
            this.Country = country;
            this.State = state;
        }

        public Address(ShippingAddress shippingAddress)
        {
            AddressType = shippingAddress.AddressType;
            Address1 = shippingAddress.Address1;
            Address2 = shippingAddress.Address2;
            Address3 = shippingAddress.Address3;
            City = shippingAddress.City;
            State = shippingAddress.State;
            Zip = shippingAddress.Zip;
            County = shippingAddress.County;
            Country = shippingAddress.Country;
        }

        [Required]
        public AddressType AddressType { get; set; }
        public string Address1 { get; set; }

        public string Address2 { get; set; }

        public string Address3 { get; set; }

        public string City { get; set; }

        public string State { get; set; }

        public string Zip { get; set; }

        public string Country { get; set; }

        public string County { get; set; }

        public string AddressDisplay
        {
            get { return this.Address1 + ((!this.Address2.IsEmpty()) ? " {0}".FormatWith(this.Address2) : ""); }
        }
        public bool IsComplete
        {
            get
            {
                return
                    !string.IsNullOrEmpty(Address1) &&
                    !string.IsNullOrEmpty(City) &&
                    !string.IsNullOrEmpty(State) &&
                    !string.IsNullOrEmpty(Zip) &&
                    !string.IsNullOrEmpty(Country);
            }
        }


        public override string ToString()
        {
            return this.Address1 + " " + (this.Address2.IsNotNullOrEmpty() ? this.Address2 + " " : string.Empty) + this.City + " " + this.State + " " + this.Zip;
        }
    }
}