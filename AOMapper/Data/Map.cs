using System.Collections.Generic;

namespace AOMapper.Data
{
    internal class Map<TKey, TValue>
    {
        public TKey Key { get; set; }
        public TValue Value { get; set; }

        public Map(TKey key, TValue value)
        {
            Key = key;
            Value = value;
        }

        protected bool Equals(Map<TKey, TValue> other)
        {
            return EqualityComparer<TKey>.Default.Equals(Key, other.Key) && EqualityComparer<TValue>.Default.Equals(Value, other.Value);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Map<TKey, TValue>) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (EqualityComparer<TKey>.Default.GetHashCode(Key)*397) ^ EqualityComparer<TValue>.Default.GetHashCode(Value);
            }
        }
    }
}