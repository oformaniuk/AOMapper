using System;
using System.Reflection;

namespace AOMapper.Extensions
{
    internal static partial class FuncExtensions
    {        
        public  static Func<TNew, TRNew> Convert<T, TR, TNew, TRNew>(this Func<T, TR> f) 
            where TNew : T
        {
            return arg => (TRNew)(f(arg) as object);
        }

        public static Action<TNew, TRNew> Convert<T, TR, TNew, TRNew>(this Action<T, TR> f)
            where TNew : T            
        {
            return (o, o1) => f(o, (TR)(o1 as object));
        }

        public static Func<TRNew> Convert<TR, TRNew>(this Func<TR> f)
            where TRNew : TR
        {
            return () => (TRNew)f();
        }

        public static Action<TNew> Convert<T, TNew>(this Action<T> f) where TNew : T
        {
            return @new => f(@new);
        }        

        public  static Func<TX, TZ> Compose<TX, TY, TZ>(this Func<TX, TY> f, Func<TY, TZ> g)
        {
            return x => g(f(x));
        }        

        public static Func<TX, TZ> Compose<TX, TY, TZ>(this Func<TX, TY> f, Delegate g)
        {
            return x => (g as Func<TY, TZ>)(f(x));
        }

        public static Action<TX, TZ> Compose<TX, TY, TZ>(this Func<TX, TY> f, Action<TY, TZ> g)
        {
            return (x, o) => g(f(x), o);
        }

        internal static MethodInfo MakeGeneric(this MethodInfo method, params Type[] types)
        {
            return method.MakeGenericMethod(types);
        }
    }
}