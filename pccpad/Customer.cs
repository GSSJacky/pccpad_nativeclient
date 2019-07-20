
namespace pccpad
{
    public class Customer
    {
        // A default constructor is required for reflection based autoserialization
        public Customer() { }

        public string Name { get; set; }
        public string Email { get; set; }
        public int Id { get; set; }
        public string TelephoneNumber { get; set; }
        public string Address { get; set; }

        public Customer(int intId, string strName, string strEmail, string strTelephoneNumber, string strAddress)
        {
            Name = strName;
            Email = strEmail;
            Id = intId;
            TelephoneNumber = strTelephoneNumber;
            Address = strAddress;
        }

        public override string ToString()
        {
            return "Customer: [" + Name + ", " + Email + ", " + Id + ", " + TelephoneNumber + ", " + Address + "]";
        }

    }
}
