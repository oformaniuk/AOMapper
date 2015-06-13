using System;
using System.Collections.Generic;
using System.Linq;
using AOMapper.Extensions;

namespace AOMapper
{
    public class Linker<T, TR, TF>
    {
        private readonly TF _action;

        public TF Action
        {
            get { return _action; }
        }

        internal Linker(Func<T, TR> func) { _action = func.As<TF>(); }
        internal Linker(Action<T, object> func) { _action = func.As<TF>(); }

        public Linker<T, TRNew, Func<T, TRNew>> Link<TRNew>(Func<TR, TRNew> func)
        {
            return new Linker<T, TRNew, Func<T, TRNew>>(_action.As<Func<T, TR>>().Compose<T, TR, TRNew>(func));                       
        }

        public Linker<T, TRNew, Action<T, object>> Link<TRNew>(Action<TR, object> func)
        {
            return new Linker<T, TRNew, Action<T, object>>(_action.As<Func<T, TR>>().Compose<T, TR, object>(func));
        }        
    }

    public class Linker<T>
    {
        readonly LinkedList<Func<T, T>> _list = new LinkedList<Func<T, T>>();

        public Linker(Func<T, T> first)
        {
            if (first != null)
                _list.AddLast(first);
        }

        public Func<T, T> Do
        {
            get { return _action; }
        }         

        private Func<T, T> _action
        {
            get
            {
                if (!_list.Any()) return arg => arg;
                return _list.Aggregate((func, func1) => func.Compose(func1));
            }
        } 

        public Linker<T> AddLast(Func<T, T> right)
        {
            _list.AddLast(right);
            return this;
        }

        public Linker<T> AddFirst(Func<T, T> right)
        {
            _list.AddFirst(right);
            return this;
        }

        #region Operators

        public static implicit operator Func<T, T>(Linker<T> proxy)
        {
            return proxy._action;
        }

        public static implicit operator LinkedList<Func<T, T>>(Linker<T> proxy)
        {
            return proxy._list;
        }

        public static Linker<T> operator <(Linker<T> left, Func<T, T> right)
        {
            left.AddFirst(right);
            return left;
        }

        public static Linker<T> operator >(Linker<T> left, Func<T, T> right)
        {
            left.AddLast(right);
            return left;
        }

        public static T operator <(Linker<T> left, T obj)
        {
            return left.Do(obj);
        }

        public static T operator >(Linker<T> left, T obj)
        {
            return left.Do(obj);
        }
        #endregion
    }
}