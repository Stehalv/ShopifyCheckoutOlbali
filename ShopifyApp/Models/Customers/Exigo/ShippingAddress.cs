using System.ComponentModel.DataAnnotations;

namespace ShopifyApp.Models
{
    public class ShippingAddress : Address
    {
        public ShippingAddress() { }
        public ShippingAddress(Address address)
        {
            AddressType = address.AddressType;
            Address1    = address.Address1;
            Address2    = address.Address2;
            City        = address.City;
            State       = address.State;
            Zip         = address.Zip;
            Country     = address.Country;
        }
        public ShippingAddress(string firstName, string lastName)
        {
            FirstName = firstName;
            LastName = lastName;
        }

        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
        public string Company { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }

        public string Notes { get; set; }

        public string FullName
        {
            get { return string.Join(" ", this.FirstName, this.LastName); }
        }
    }
}