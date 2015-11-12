using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace AOMapper.Data.Keys
{
    [DebuggerDisplay("{Value}")]
    public class TypeKey : AbstractKey<Type>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TypeKey"/> class.
        /// </summary>
        /// <param name="value">The value.</param>
        private TypeKey(Type value) : base(value)
        {                
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="Type"/> to <see cref="TypeKey"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static implicit operator TypeKey(Type value)
        {            
            return new TypeKey(value);
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="TypeKey"/> to <see cref="Type"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static implicit operator Type(TypeKey value)
        {
            return value.Value;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return HashCode == obj.GetHashCode();
        }
    }
}