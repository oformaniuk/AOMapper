using System.Collections.Generic;
using System.Diagnostics;

namespace AOMapper.Data.Keys
{
    [DebuggerDisplay("{Value}")]    
    public class StringKey : AbstractKey<string>
    {        
        private StringKey(string value) : base(value)
        {            
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
    }
}