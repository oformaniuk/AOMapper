using System;
using System.Reflection;
using AOMapper.Extensions;
using AOMapper.Interfaces;

namespace AOMapper.Data
{
    internal struct AccessObject<T, TResult> : IAccessObject
    {
        public bool CanGet
        {
            get { return Getter != null; }
        }

        public bool CanCreate { get; set; }

        public bool CanSet
        {
            get { return Setter != null; }
        }        

        public PropertyInfo PropertyInfo { get; set; }
        public Func<T, TResult> Getter { get; set; }
        public Action<T, TResult> Setter { get; set; }

        object IAccessObject.Get(object obj)
        {
            return Getter((T)obj);
        }

        void IAccessObject.Set(object obj, object value)
        {
            Setter((T)obj, (TResult)value);
        }

        public Func<T1, TR> GetGetter<T1, TR>()            
        {
            return Getter.As<Func<T1, TR>>();           
        }

        public Action<T1, TR> GetSetter<T1, TR>()            
        {
            return Setter.As<Action<T1, TR>>();
        }

        public Delegate GetterDelegate
        {
            get { return Getter; }
        }

        public Delegate SetterDelegate
        {
            get { return Setter; }
        }

        public TR GetGeneric<T1, TR>(T1 obj)             
        {
            return Getter(obj.As<T>()).As<TR>();
        }

        public void SetGeneric<T1, TR>(T1 obj, TR value)            
        {
            Setter(obj.As<T>(), value.As<TResult>());
        }        
    }    
}
