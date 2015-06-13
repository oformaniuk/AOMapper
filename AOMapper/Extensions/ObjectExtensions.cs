using System;
using System.Collections.Generic;

namespace AOMapper.Extensions
{    
    public static class ObjectExtensions
    {
        /// <summary>
        /// <para>The same as to do (T)object but in more clear manner</para>
        /// <para>Doing in this way allows us to continue 'dotting' instead of doing something like ((T)obj).property</para>
        /// </summary>
        public static T As<T>(this object obj)
        {
            return (T)obj;
        }

        /// <summary>
        /// Allows to manipulate the object in the chain manner.
        /// </summary>
        /// <returns></returns>
        public static T Apply<T>(this T source, Action<T> action) where T : class
        {
            action(source);
            return source;
        }

        /// <summary>
        /// Determines whether the specified collection has more them one element.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection"></param>
        /// <returns></returns>
        public static bool IsMultiple<T>(this IEnumerable<T> collection)
        {
            bool result;
            using (var enumerator = collection.GetEnumerator())
            {
                result = enumerator.MoveNext() && enumerator.MoveNext();
            }
            return result;
        }
    }
}
