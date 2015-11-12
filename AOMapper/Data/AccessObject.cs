using System;
using System.Reflection;
using AOMapper.Extensions;
using AOMapper.Helpers;
using AOMapper.Interfaces;

namespace AOMapper.Data
{
    internal class AccessObject<T, TResult> : IAccessObject
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

        public MemberInfo MemberInfo { get { return PropertyInfo as MemberInfo ?? FieldInfo as MemberInfo; } }
        public Type MemberType { get { return PropertyInfo != null ? PropertyInfo.PropertyType : FieldInfo.FieldType; } }
        public FieldInfo FieldInfo { get; set; }
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

        public TR GetGeneric<T1, TR>(T1 obj) where TR : class           
        {
            //return Getter((T)(obj as object)) as TR;
            return Getter(CastTo<T>.From(obj)) as TR;
        }

        public void SetGeneric<T1, TR>(T1 obj, TR value)          
        {
            //Setter((T)(obj as object), (TResult)(value as object));
            Setter(CastTo<T>.From(obj), CastTo<TResult>.From(value));
        }        
    }    
}
