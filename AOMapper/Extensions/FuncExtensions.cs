using System;
using System.Reflection;
using AOMapper.Helpers;

namespace AOMapper.Extensions
{
    public static partial class FuncExtensions
    {
        public static Linker<T, TR, Func<T, TR>> Link<T, TR>(this Func<T, TR> f)
        {
            return new Linker<T, TR, Func<T, TR>>(f);
        }

        public static Linker<T, TR, Action<T, object>> Link<T, TR>(this Action<T, object> f)
        {
            return new Linker<T, TR, Action<T, object>>(f);
        }

        public  static Func<TNew, TRNew> Convert<T, TR, TNew, TRNew>(this Func<T, TR> f)
        {
            return arg => f(arg.As<T>()).As<TRNew>();
        }        

        public static Action<TNew, TRNew> Convert<T, TR, TNew, TRNew>(this Action<T, TR> f)
        {
            return (o, o1) => f(o.As<T>(), o1.As<TR>());
        }

        public static Func<TRNew> Convert<TR, TRNew>(this Func<TR> f)
        {
            return () => f().As<TRNew>();
        }

        public static Action<TNew> Convert<T, TNew>(this Action<T> f)
        {
            return @new => f(@new.As<T>());
        }

        //public static Action<TNew, TRNew> Convert<T, TR, TNew, TRNew>(this Action<T, TR> f)
        //{
        //    return (@new, rNew) => f(@new.As<T>(), rNew.As<TR>());//@new => f(@new.As<T>());
        //}

        public  static Func<TX, TZ> Compose<TX, TY, TZ>(this Func<TX, TY> f, Func<TY, TZ> g)
        {
            return x => g(f(x));
        }        

        public static Func<TX, TZ> Compose<TX, TY, TZ>(this Func<TX, TY> f, Delegate g)
        {
            return x => ((Func<TY, TZ>)g)(f(x));
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