using System;
using System.Collections.Generic;
using System.Linq;

namespace AOMapperTests.Helpers
{
    public class Customer4 : Customer
    {
        public Dictionary<int, DateTime> DateTimes { get; set; } 
    }

    public class Customer5 : Customer
    {
        public List<DateTime> DateTimes { get; set; }

        public CustomerSubClass2 SubClass2 { get; set; }
    }

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

    public class Customer6 : Customer
    {
        public ConsoleColor Color { get; set; }
        public int Cast { get; set; }
    }

    public class Customer2 : Customer
    {
        public SimpleObject[] ViewItems { get; set; } 
    }

    public class SimpleObject
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime Date { get; set; }
        public List<SimpleObjectInner> Inners { get; set; }

        protected bool Equals(SimpleObject other)
        {
            return Id == other.Id && string.Equals(Name, other.Name) && Date.Equals(other.Date) && Inners.SequenceEqual(other.Inners);//Equals(Inners, other.Inners);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((SimpleObject) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Id;
                hashCode = (hashCode*397) ^ (Name != null ? Name.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ Date.GetHashCode();
                hashCode = (hashCode*397) ^ (Inners != null ? Inners.GetHashCode() : 0);
                return hashCode;
            }
        }
    }

    public class SimpleObjectInner
    {
        public string Inner {get;set;}
    }
}