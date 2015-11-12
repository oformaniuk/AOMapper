using System;
using System.Collections.Generic;
using System.Diagnostics;
using AOMapper.Interfaces.Keys;

namespace AOMapper.Data.Keys
{
    [DebuggerDisplay("{Value}")]
    public class AbstractKey<T> : IKey<T>
    {
        public T Value { get; private set; }        

        protected readonly int HashCode;

        protected AbstractKey(T value)
        {
            Value = value;
            HashCode = ReferenceEquals(default(T), Value) ? 0 : Value.GetHashCode();            
        }

        public bool Equals(AbstractKey<T> obj)
        {            
            if (ReferenceEquals(Value, obj.Value)) return true;
            if (HashCode != obj.HashCode) return false;
            return Value.Equals(obj.Value);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (HashCode != obj.GetHashCode()) return false;
            return Value.Equals((AbstractKey<T>)obj);            
        }

        public override int GetHashCode()
        {
            return HashCode;
        }

        public override string ToString()
        {
            return Value.ToString();
        }

        #region == && !=

        public static bool operator ==(AbstractKey<T> right, AbstractKey<T> left)
        {
            if (ReferenceEquals(null, right)) return false;
            if (ReferenceEquals(null, left)) return false;
            return right.Equals(left);
        }

        public static bool operator !=(AbstractKey<T> right, AbstractKey<T> left)
        {
            if (ReferenceEquals(null, right)) return false;
            if (ReferenceEquals(null, left)) return true;
            return !right.Equals(left);
        }

        public static bool operator ==(AbstractKey<T> right, object left)
        {
            if (ReferenceEquals(null, right)) return false;
            if (ReferenceEquals(null, left)) return false;
            return right.Equals(left);
        }

        public static bool operator !=(AbstractKey<T> right, object left)
        {
            if (ReferenceEquals(null, right)) return false;
            if (ReferenceEquals(null, left)) return true;
            return !right.Equals(left);
        }

        public static bool operator ==(object right, AbstractKey<T> left)
        {
            if (ReferenceEquals(null, left)) return false;
            if (ReferenceEquals(null, right)) return false;
            return left.Equals(right);
        }

        public static bool operator !=(object right, AbstractKey<T> left)
        {
            if (ReferenceEquals(null, left)) return false;
            if (ReferenceEquals(null, right)) return true;
            return !left.Equals(right);
        }

        #endregion

    }
}