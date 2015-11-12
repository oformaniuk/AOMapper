using System.Collections.Generic;
using System.Diagnostics;
using AOMapper.Interfaces.Keys;

namespace AOMapper.Data.Keys
{
    [DebuggerDisplay("{Value}")]    
    public struct StringKey : IKey<string>
                            //AbstractKey<string>
    {
        private readonly int HashCode;

        private StringKey(string value)
            :this()//: base(value)
        {
            Value = value;
            HashCode = value.GetHashCode();
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="System.String"/> to <see cref="StringKey"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static implicit operator StringKey(string value)
        {            
            return new StringKey(value);
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="StringKey"/> to <see cref="System.String"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        /// The result of the conversion.
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
            return HashCode == obj.GetHashCode();
        }

        public override int GetHashCode()
        {
            return HashCode;
        }        

        public string Value { get; private set; }
    }
}