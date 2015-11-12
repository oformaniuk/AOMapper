using System;

namespace AOMapperTests.Helpers
{
    public class CustomerSubClass
    {
        public string Name {get;set;}

        //public CustomerSubClass SubClass { get; set; }

        protected bool Equals(CustomerSubClass other)
        {
            return string.Equals(Name, other.Name);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((CustomerSubClass) obj);
        }

        public override int GetHashCode()
        {
            return (Name != null ? Name.GetHashCode() : 0);
        }
    }

    public class CustomerSubClass2 : CustomerSubClass
    {
        public DateTime[] DateTimes2 { get; set; }
    }
}