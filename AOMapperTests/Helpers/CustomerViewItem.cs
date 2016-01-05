using System;
using System.Collections.Generic;
using System.Linq;

namespace AOMapperTests.Helpers
{
    public class CustomerViewItem
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime DateOfBirth { get; set; }
        //}

        public virtual string SubName { get; set; }

        public virtual CustomerSubViewItem SubSubItem { get; set; }

        public int NumberOfOrders { get; set; }

        protected bool Equals(CustomerViewItem other)
        {
            return string.Equals(FirstName, other.FirstName) && string.Equals(LastName, other.LastName) && DateOfBirth.Equals(other.DateOfBirth) && NumberOfOrders == other.NumberOfOrders;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((CustomerViewItem)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (FirstName != null ? FirstName.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (LastName != null ? LastName.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ DateOfBirth.GetHashCode();
                hashCode = (hashCode*397) ^ (SubName != null ? SubName.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ NumberOfOrders;
                return hashCode;
            }
        }
    }

    public struct CustomerViewItem2
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime DateOfBirth { get; set; }
        //}

        public string SubName { get; set; }

        public CustomerSubViewItem SubSubItem { get; set; }

        public int NumberOfOrders { get; set; }

        public bool Equals(CustomerViewItem other)
        {
            return string.Equals(FirstName, other.FirstName) && string.Equals(LastName, other.LastName) && DateOfBirth.Equals(other.DateOfBirth) && NumberOfOrders == other.NumberOfOrders;
        }

        //public override bool Equals(object obj)
        //{
        //    if (ReferenceEquals(null, obj)) return false;
        //    if (ReferenceEquals(this, obj)) return true;
        //    if (obj.GetType() != this.GetType()) return false;
        //    return Equals((CustomerViewItem)obj);
        //}

        //public override int GetHashCode()
        //{
        //    unchecked
        //    {
        //        var hashCode = (FirstName != null ? FirstName.GetHashCode() : 0);
        //        hashCode = (hashCode * 397) ^ (LastName != null ? LastName.GetHashCode() : 0);
        //        hashCode = (hashCode * 397) ^ DateOfBirth.GetHashCode();
        //        hashCode = (hashCode * 397) ^ (SubName != null ? SubName.GetHashCode() : 0);
        //        hashCode = (hashCode * 397) ^ NumberOfOrders;
        //        return hashCode;
        //    }
        //}

        public SimpleObjectViewItem[] ViewItems { get; set; }

        public bool Equals(CustomerViewItem2 other)
        {
            return base.Equals(other) && ViewItems.SequenceEqual(other.ViewItems);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((CustomerViewItem2) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (base.GetHashCode()*397) ^ (ViewItems != null ? ViewItems.GetHashCode() : 0);
            }
        }
    }

    public class SimpleObjectViewItem
    {
        public string Name { get; set; }
        public DateTime Date { get; set; }
        public List<SimpleObjectViewItemInner> Inners { get; set; }

        protected bool Equals(SimpleObjectViewItem other)
        {
            return string.Equals(Name, other.Name) && Date.Equals(other.Date) && Inners.SequenceEqual(other.Inners);//Equals(Inners, other.Inners);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((SimpleObjectViewItem) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Name != null ? Name.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ Date.GetHashCode();
                hashCode = (hashCode*397) ^ (Inners != null ? Inners.GetHashCode() : 0);
                return hashCode;
            }
        }
    }

    public class SimpleObjectViewItemInner
    {
        public string Inner { get; set; }

        protected bool Equals(SimpleObjectViewItemInner other)
        {
            return string.Equals(Inner, other.Inner);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((SimpleObjectViewItemInner) obj);
        }

        public override int GetHashCode()
        {
            return (Inner != null ? Inner.GetHashCode() : 0);
        }
    }   
}