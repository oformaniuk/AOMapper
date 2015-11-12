using System;

namespace AOMapper.Extensions
{
    public static class TypeExtensions
    {
        public static object GetDefault(this Type type)
        {
            return type.IsValueType ? Activator.CreateInstance(type) : null;
        }

        public static object GetDefault<T>(this T obj)
        {
            if (obj == null) return null;
            var type = obj.GetType();
            return type.IsValueType ? Activator.CreateInstance(type) : null;
        }
    }
}