using System;

namespace jetMapperTests
{
    public class Customer
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime DateOfBirth { get; set; }
        public int NumberOfOrders { get; set; }

        public CustomerSubClass Sub { get; set; }   

        protected bool Equals(Customer other)
        {
            return string.Equals(FirstName, other.FirstName) && string.Equals(LastName, other.LastName) && DateOfBirth.Equals(other.DateOfBirth) && NumberOfOrders == other.NumberOfOrders;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Customer) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (FirstName != null ? FirstName.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (LastName != null ? LastName.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ DateOfBirth.GetHashCode();
                hashCode = (hashCode*397) ^ (Sub != null ? Sub.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ NumberOfOrders;
                return hashCode;
            }
        }
    }
}