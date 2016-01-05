using System;
using System.Diagnostics;
using AOMapper.Interfaces.Keys;

namespace AOMapper.Data.Keys
{
    [DebuggerDisplay("{Value}")]
    public struct StringKey :
        IKey<string>,
        IEquatable<string>,
        IEquatable<StringKey>
    {
        private readonly int _hashCode;

        private StringKey(string value)
            : this()
        {
            Value = value;
            _hashCode = value.GetHashCode();
        }

        /// <summary>
        ///     Performs an implicit conversion from <see cref="System.String" /> to <see cref="StringKey" />.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        ///     The result of the conversion.
        /// </returns>
        public static implicit operator StringKey(string value)
        {
            return new StringKey(value);
        }

        /// <summary>
        ///     Performs an implicit conversion from <see cref="StringKey" /> to <see cref="System.String" />.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        ///     The result of the conversion.
        /// </returns>
        public static implicit operator string(StringKey value)
        {
            return value.Value;
        }

        public override string ToString()
        {
            return Value;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            var s = obj as string;
            if (s != null) return Equals(s);
            if (obj is StringKey) return Equals((StringKey) obj);

            return false;
        }

        public bool Equals(string obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (_hashCode != obj.GetHashCode()) return false;
            return Value == obj;
        }

        public bool Equals(StringKey obj)
        {
            if (_hashCode != obj._hashCode) return false;
            return Value == obj.Value;
        }

        public override int GetHashCode()
        {
            return _hashCode;
        }

        /// <summary>
        ///     Gets the value that is stored inside of the key object.
        /// </summary>
        public string Value { get; private set; }
    }
}