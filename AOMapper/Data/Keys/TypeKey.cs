using System;
using System.Diagnostics;
using AOMapper.Interfaces.Keys;

namespace AOMapper.Data.Keys
{
    [DebuggerDisplay("{Value}")]
    public struct TypeKey : IKey<Type>, IEquatable<TypeKey>, IEquatable<Type>
    {
        private readonly int HashCode;

        /// <summary>
        ///     Initializes a new instance of the <see cref="TypeKey" /> class.
        /// </summary>
        /// <param name="value">The value.</param>
        private TypeKey(Type value) : this() //base(value)
        {
            Value = value;
            HashCode = value == null ? 0 : value.GetHashCode();
        }

        /// <summary>
        ///     Performs an implicit conversion from <see cref="Type" /> to <see cref="TypeKey" />.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        ///     The result of the conversion.
        /// </returns>
        public static implicit operator TypeKey(Type value)
        {
            return new TypeKey(value);
        }

        /// <summary>
        ///     Performs an implicit conversion from <see cref="TypeKey" /> to <see cref="Type" />.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        ///     The result of the conversion.
        /// </returns>
        public static implicit operator Type(TypeKey value)
        {
            return value.Value;
        }

        public override string ToString()
        {
            return Value.ToString();
        }

        public bool Equals(TypeKey other)
        {
            if (HashCode != other.HashCode) return false;
            return Value == other.Value;
        }

        public bool Equals(Type other)
        {
            if (ReferenceEquals(null, other)) return false;
            return Value == other;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (HashCode != obj.GetHashCode()) return false;
            var type = obj as Type;
            if (type != null) return Equals(type);

            if (obj is TypeKey) return Equals((TypeKey) obj);

            return false;
        }

        public override int GetHashCode()
        {
            return HashCode;
        }

        public Type Value { get; private set; }
    }
}