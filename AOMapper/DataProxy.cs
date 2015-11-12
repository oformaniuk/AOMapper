using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using AOMapper.Data;
using AOMapper.Data.Keys;
using AOMapper.Extensions;
#if NET35
using AOMapper.Helpers;
#endif
using AOMapper.Interfaces;

namespace AOMapper
{
    public abstract class DataProxy
    {
        #region Fields

        protected readonly static Type _enumerableType = typeof(IEnumerable);

        protected static readonly Lazy<Dictionary<TypeKey, Dictionary<StringKey, IAccessObject>>> AccessObjects =
            new Lazy<Dictionary<TypeKey, Dictionary<StringKey, IAccessObject>>>();

        protected static readonly Lazy<Dictionary<TypeKey, Dictionary<StringKey, MethodProperty>>> MethodsDictionary =
            new Lazy<Dictionary<TypeKey, Dictionary<StringKey, MethodProperty>>>();

        public Dictionary<StringKey, object> RawView { get; protected set; }

        #endregion

        #region Create

        /// <summary>
        /// Creates the data access proxy object.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static DataProxy<TEntity> Create<TEntity>(TEntity entity)
        {
            return new DataProxy<TEntity>(entity);
        }

        /// <summary>
        /// Creates the data access proxy object.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <returns></returns>
        public static DataProxy<TEntity> Create<TEntity>()
        {
            return new DataProxy<TEntity>();
        }

        /// <summary>
        /// Creates the data access proxy object.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static DataProxy<object> Create(Type type)
        {
            return new DataProxy<object>(type);
        }

        #endregion        
    }

    /// <summary>
    /// <para>Represents data access proxy object.</para>
    /// <para>Designed to simplify get/set properties of the object using their names.</para>
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    public class DataProxy<TEntity> : DataProxy, IEnumerable<string>
    {
        #region Fields

        private readonly Dictionary<StringKey, IAccessObject> _accessObjects;
        private readonly TypeKey _type;
        private Dictionary<StringKey, MethodProperty> _methods;        
        private static readonly MethodInfo BuildAccessorsMethod = typeof(DataProxy<TEntity>).GetMethod("BuildAccessors", BindingFlags.Static | BindingFlags.NonPublic);
        private readonly Dictionary<StringKey, object> _virtualProperties = new Dictionary<StringKey, object>();
        private readonly int _hashCode;

        #endregion

        #region ctor's

        public DataProxy(TEntity entity)
        {
            var proxy = entity as DataProxy<TEntity>;
            _type = proxy != null ? proxy.UnderlyingObject.GetType() : entity.GetType();

            _hashCode = (_type != null ? _type.GetHashCode() : 0);

            RawView = new Dictionary<StringKey, object>();
            if (!AccessObjects.Value.ContainsKey(_type)) 
                BuildAccessorsMethod.MakeGenericMethod(_type).Invoke(null, new object[] {_type, this});
            _accessObjects = AccessObjects.Value[_type];

            UnderlyingObject = entity;

            if (!MethodsDictionary.Value.ContainsKey(_type))
                MethodsDictionary.Value.Add(_type, new Dictionary<StringKey, MethodProperty>());

            _methods = new Dictionary<StringKey, MethodProperty>(MethodsDictionary.Value[_type]);

            Count = _accessObjects.Keys.Count;
        }

        public DataProxy(Type type)
        {
            _type = type;

            _hashCode = (_type != null ? _type.GetHashCode() : 0);

            RawView = new Dictionary<StringKey, object>();
            _type = type;
            if (!AccessObjects.Value.ContainsKey(_type))
                BuildAccessorsMethod.MakeGenericMethod(_type).Invoke(null, new object[] {_type, this});
            _accessObjects = AccessObjects.Value[_type];               

            UnderlyingObject = default(TEntity);

            if (!MethodsDictionary.Value.ContainsKey(_type))
                MethodsDictionary.Value.Add(_type, new Dictionary<StringKey, MethodProperty>());

            _methods = new Dictionary<StringKey, MethodProperty>(MethodsDictionary.Value[_type]);

            Count = _accessObjects.Keys.Count;
        }

        public DataProxy()
        {
            _type = typeof (TEntity);

            _hashCode = (_type != null ? _type.GetHashCode() : 0);

            RawView = new Dictionary<StringKey, object>();
            if (!AccessObjects.Value.ContainsKey(_type)) 
                BuildAccessorsMethod.MakeGenericMethod(_type).Invoke(null, new object[] {_type, this});
            _accessObjects = AccessObjects.Value[_type];

            if (!MethodsDictionary.Value.ContainsKey(_type))
                MethodsDictionary.Value.Add(_type, new Dictionary<StringKey, MethodProperty>());

            _methods = new Dictionary<StringKey, MethodProperty>(MethodsDictionary.Value[_type]);

            Count = _accessObjects.Keys.Count;
        }

        #endregion

        #region Properties

        public Dictionary<StringKey, MethodProperty> Methods
        {
            get { return _methods; }
            private set { _methods = value; }
        }

        /// <summary>
        /// Gets or sets the underlying object.
        /// </summary>
        /// <value>
        /// The underlying object.
        /// </value>
        public TEntity UnderlyingObject { get; set; }

        /// <summary>
        /// Gets the names of object properties.
        /// </summary>        
        public IEnumerable<string> Properties
        {
            get
            {
                return AccessObjects.Value[_type].Keys.Select(o => o.Value);
            }
        }

        /// <summary>
        /// Gets the count of properties.
        /// </summary>        
        public int Count
        {
            get;
            private set;
        }

        public Type Type
        {
            get { return _type; }
        }

        #endregion                

        #region Indexers

        /// <summary>
        /// Gets or sets the property with the specified name.
        /// </summary>        
        /// <param name="name">Property name.</param>
        /// <returns></returns>
        public object this[string name]
        {
            get
            {
                return RawView.ContainsKey(name)
                    ? RawView[name]
                    : (RawView[name] = (AccessObjects.Value[_type][name]).GetGeneric<TEntity, object>(UnderlyingObject));
            }
            set
            {
                try
                {
                    RawView[name] = value;
                    if (_accessObjects.ContainsKey(name) && _accessObjects[name].CanSet)
                        (_accessObjects[name]).SetGeneric(UnderlyingObject, value);
                    else
                    {
                        if (!_virtualProperties.ContainsKey(name))
                            _virtualProperties.Add(name, value);
                        else _virtualProperties[name] = value;                        
                    }                     
                }
                catch (InvalidCastException)
                {                    
                    var t = UnderlyingObject.GetType();
                    var p = t.GetProperty(name);
                    p.SetValue(UnderlyingObject, value, null);              
                }
            }
        }

        /// <summary>
        /// Gets or sets the property with the specified name of the specified object.
        /// </summary>        
        /// <param name="obj"></param>
        /// <param name="name">Property name.</param>
        /// <returns></returns>
        public object this[TEntity obj, string name]
        {
            get { return _accessObjects[name].GetGeneric<TEntity, object>(obj); }
            set { _accessObjects[name].SetGeneric(obj, value); }
        }

        /// <summary>
        /// Gets or sets the property with the specified name of the specified object.
        /// </summary>        
        /// <param name="obj"></param>
        /// <param name="name">Property name.</param>
        /// <returns></returns>
        public object this[object obj, string name]
        {
            get { return _accessObjects[name].Get((TEntity) obj); }
            set { _accessObjects[name].Set(obj, value); }
        }

        #endregion
        
        #region Contains

        /// <summary>
        /// Determines whether object contains method with specified name.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool ContainsMethod(string name)
        {
            return _methods.ContainsKey(name);
        }

        /// <summary>
        /// Determines whether object contains property with specified name.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool ContainsProperty(string name)
        {
            return _accessObjects.ContainsKey(name);
        }

        #endregion        

        #region IEnumerable

        public IEnumerator<string> GetEnumerator()
        {
            return Properties.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion        

        #region Gettes of the additional info

        /// <summary>
        /// Gets the getter delegate.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public Func<TEntity, object> GetGetter(string name)
        {
            return _accessObjects[name].GetGetter<TEntity, object>();
        }

        internal object GetReflectedGetter(StringKey name, Type type)
        {
            return this.GetType().GetMethod("GetGetterGeneric", BindingFlags.NonPublic | BindingFlags.Instance)
                .MakeGeneric(type)
                .Invoke(this, new object[] {name});
        }

        internal object GetReflectedSetter(StringKey name, Type type)
        {
            return this.GetType().GetMethod("GetSetterGeneric", BindingFlags.NonPublic | BindingFlags.Instance)
                .MakeGeneric(type)
                .Invoke(this, new object[] { name });
        }

        internal object GetReflectedConvertedGetter(StringKey name, Type type)
        {
            return this.GetType().GetMethod("GetGetterConverted", BindingFlags.NonPublic | BindingFlags.Instance)
                .MakeGeneric(type)
                .Invoke(this, new object[] { name });
        }

        internal object GetReflectedConvertedSetter(StringKey name, Type type)
        {
            return this.GetType().GetMethod("GetSetterConverted", BindingFlags.NonPublic | BindingFlags.Instance)
                .MakeGeneric(type)
                .Invoke(this, new object[] { name });
        }

        internal Func<TEntity, TR> GetGetterGeneric<TR>(StringKey name)
        {
            return _accessObjects[name].GetGetter<TEntity, TR>();
        }

        internal Action<TEntity, TR> GetSetterGeneric<TR>(StringKey name)
        {
            return _accessObjects[name].GetSetter<TEntity, TR>();
        }

        internal Action<TEntity, object> GetSetterConverted<TR>(StringKey name)
        {
            return _accessObjects[name].GetSetter<TEntity, TR>().Convert<TEntity, TR, TEntity, object>();
        }

        internal Func<TEntity, object> GetGetterConverted<TR>(StringKey name)
        {
            return _accessObjects[name].GetGetter<TEntity, TR>().Convert<TEntity, TR, TEntity, object>();
        }

        /// <summary>
        /// Gets the property information.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public PropertyInfo GetPropertyInfo(string name)
        {
            return (_accessObjects[name]).PropertyInfo;
        }

        /// <summary>
        /// Gets the property information.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public MemberInfo GetMemberInfo(string name)
        {
            return (_accessObjects[name]).MemberInfo;
        }

        /// <summary>
        /// Gets the property information.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public Type GetMemberType(string name)
        {
            return (_accessObjects[name]).MemberType;
        }

        /// <summary>
        /// Gets the setter delegate.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public Action<TEntity, object> GetSetter(string name)
        {
            return _accessObjects[name].GetSetter<TEntity, object>();
        }

        /// <summary>
        /// Gets the setter delegate.
        /// </summary>
        /// <typeparam name="TR"></typeparam>
        /// <param name="name"></param>
        /// <returns></returns>
        public Action<TEntity, TR> GetSetter<TR>(string name)
        {
            return _accessObjects[name].GetSetter<TEntity, TR>();
        }

        public bool CanGet(string name)
        {
            return _accessObjects[name].CanGet;
        }

        public bool CanSet(string name)
        {
            return _accessObjects[name].CanSet;
        }

        public bool CanCreate(string name)
        {
            return _accessObjects[name].CanCreate;
        }

        public bool IsEnumerable(string name)
        {
            var propertyType = _accessObjects[name].PropertyInfo.PropertyType;
            return _enumerableType.IsAssignableFrom(propertyType) && propertyType != typeof(string);
        }

        #endregion                 
        
        #region Helpers

        internal DataProxy<TEntity> RegisterMethod<T>(string name, T method)
        {
            MethodsDictionary.Value[_type][name] = new MethodProperty
            {
                Delegate = method.As<Delegate>(),
                Info = method.As<Delegate>().Method
            };
            return this;
        }

        private static void BuildAccessors<T>(TypeKey type, DataProxy<TEntity> obj)
        {
            if (AccessObjects.Value.ContainsKey(type)) return;

            AccessObjects.Value.Add(type, new Dictionary<StringKey, IAccessObject>());
            var buildAccessor = typeof(DataProxy<TEntity>).GetMethod("BuildPropertyAccessor", BindingFlags.NonPublic | BindingFlags.Static);

            foreach (var o in type.Value.GetProperties().Where(o => o.CanRead && o.CanWrite))
            {                
                buildAccessor.MakeGeneric(type, o.PropertyType)
                    .Invoke(null, new object[]{type, o});
            }


            // TODO: Implement fields support
            //buildAccessor = typeof(DataProxy<TEntity>).GetMethod("BuildFieldAccessor", BindingFlags.NonPublic | BindingFlags.Static);
            //foreach (var o in type.Value.GetFields().Where(o => !o.IsSpecialName && !o.IsInitOnly && !o.IsStatic))
            //{                               
            //    buildAccessor.MakeGeneric(type, o.FieldType)
            //        .Invoke(null, new object[] { type, o });
            //}
        }

        private static void BuildPropertyAccessor<T, TR>(TypeKey type, PropertyInfo o)
        {
            if (o.GetIndexParameters().Any()) return; // indexers are not supported

            if (AccessObjects.Value[type].ContainsKey(o.Name) && o.DeclaringType == type)
            {
                AccessObjects.Value[type][o.Name] = new AccessObject<T, TR>
                {
                    PropertyInfo = o,
                    Getter = o.CanRead ? GetValueGetter<T, TR>(o, type) : null,
                    Setter = o.CanWrite ? GetValueSetter<T, TR>(o, type) : null,
                    CanCreate = o.PropertyType.GetConstructor(new Type[0]) != null,
                    FieldInfo = null
                };
            }
            else if (!AccessObjects.Value[type].ContainsKey(o.Name))
            {
                AccessObjects.Value[type].Add(o.Name, new AccessObject<T, TR>
                {
                    PropertyInfo = o,
                    Getter = o.CanRead && o.GetGetMethod() != null ? GetValueGetter<T, TR>(o, type) : null,
                    Setter = o.CanWrite && o.GetSetMethod() != null ? GetValueSetter<T, TR>(o, type) : null,
                    CanCreate = o.PropertyType.GetConstructor(new Type[0]) != null,
                    FieldInfo = null
                });
            }
        }

        private static void BuildFieldAccessor<T, TR>(TypeKey type, FieldInfo o)
        {
            //if (o.GetIndexParameters().Any()) return; // indexers are not supported

            if (AccessObjects.Value[type].ContainsKey(o.Name) && o.DeclaringType == type)
            {
                AccessObjects.Value[type][o.Name] = new AccessObject<T, TR>
                {
                    PropertyInfo = null,
                    Getter = arg => (TR)o.GetValue(arg), //o.CanRead ? GetValueGetter<T, TR>(o, type) : null,
                    Setter = (arg1, r) => o.SetValue(arg1, r),
                    CanCreate = o.FieldType.GetConstructor(new Type[0]) != null,
                    FieldInfo = o
                };
            }
            else if (!AccessObjects.Value[type].ContainsKey(o.Name))
            {
                AccessObjects.Value[type].Add(o.Name, new AccessObject<T, TR>
                {
                    PropertyInfo = null,
                    Getter = arg => (TR)o.GetValue(arg),
                    Setter = (arg1, r) => o.SetValue(arg1, r),
                    CanCreate = o.FieldType.GetConstructor(new Type[0]) != null,
                    FieldInfo = o
                });
            }
        }

        internal static Func<T, TR> GetValueGetter<T, TR>(PropertyInfo propertyInfo, Type type)
        {            
            if (!(type == propertyInfo.DeclaringType || type == propertyInfo.ReflectedType))
            {
                //throw new InvalidOperationException("Property does not belong to the type");     
                return null;
            }

            if(type.IsClass)
                return Delegate.CreateDelegate(typeof(Func<T, TR>), propertyInfo.GetGetMethod()) as Func<T, TR>;

            ParameterExpression paramExpression = Expression.Parameter(type, "value");

            Expression propertyGetterExpression = Expression.Property(paramExpression, propertyInfo.Name);
            
            return Expression.Lambda<Func<T, TR>>(propertyGetterExpression, paramExpression).Compile();            
        }

        internal static Action<T, TR> GetValueSetter<T, TR>(PropertyInfo propertyInfo, Type type)
        {
            Contract.Requires(type != null);
            if (!(type == propertyInfo.DeclaringType || type == propertyInfo.ReflectedType))
            {
                throw new Exception();                
            }

            if (type.IsClass)
                return (Action<T, TR>)Delegate.CreateDelegate(typeof(Action<T, TR>), propertyInfo.GetSetMethod());

            ParameterExpression paramExpression = Expression.Parameter(type);

            ParameterExpression paramExpression2 = Expression.Parameter(propertyInfo.PropertyType, propertyInfo.Name);

            MemberExpression propertyGetterExpression = Expression.Property(paramExpression, propertyInfo.Name);

            return Expression.Lambda<Action<T, TR>>
            (
                Expression.Assign(propertyGetterExpression, paramExpression2), paramExpression, paramExpression2
            ).Compile();
        }        

        private static void InitObjects<T>(ref Dictionary<StringKey, AccessObject<T, object>> dictionary)
        {
            var type = typeof(T);
            foreach (var o in AccessObjects.Value[type].As<Dictionary<StringKey, object>>())
            {
                dictionary.Add(o.Key, o.Value.As<AccessObject<T, object>>());
            }
        }

        #endregion

        #region operators

        /// <summary>
        /// Performs an implicit conversion from <see cref="TEntity"/> to <see cref="DataProxy{TEntity}"/>.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static implicit operator DataProxy<TEntity>(TEntity obj)
        {
            return new DataProxy<TEntity>(obj);
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="DataProxy{TEntity}"/> to <see cref="TEntity"/>.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static implicit operator TEntity(DataProxy<TEntity> obj)
        {
            return obj.UnderlyingObject;
        }
        #endregion

        #region General overloads

        protected bool Equals(DataProxy<TEntity> other)
        {
            return _hashCode == other._hashCode;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals(obj as DataProxy<TEntity>);
        }

        public override int GetHashCode()
        {
            return _hashCode;
        }

        #endregion
    }
}
