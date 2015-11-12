using System.Collections.Generic;
using System.Linq;
using AOMapper.Data.Keys;

namespace AOMapper.Extensions
{
    public static class StringExtensions
    {
        public static IEnumerable<StringKey> ToStringKeys(this IEnumerable<string> enumerable)
        {
            return enumerable.Select(o => (StringKey) o);
        }
    }
}