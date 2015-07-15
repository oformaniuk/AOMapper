using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AOMapper.Extensions;
using AOMapper.Helpers;
using AOMapper.Interfaces;

namespace AOMapper
{
    public abstract class DataProxy
    {
        #region Fields

        protected static readonly Lazy<Dictionary<Type, Dictionary<string, IAccessObject>>> AccessObjects =
            new Lazy<Dictionary<Type, Dictionary<string, IAccessObject>>>();

        protected static readonly Lazy<Dictionary<Type, Dictionary<string, MethodProperty>>> MethodsDictionary =
            new Lazy<Dictionary<Type, Dictionary<string, MethodProperty>>>();

        public Dictionary<string, object> RawView { get; protected set; }

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

        #region Helpers

        //protected static Action<T, object> ___getSetInvoker<T>(DataProxy<T> proxy, string name)
        //{
        //    return proxy.GetSetter(name);
        //}

        //protected static Action<TNew, TRNew> _convertAction<T, TR, TNew, TRNew>(Action<T, TR> f)
        //{
        //    return (o, o1) => f(o.As<T>(), o1.As<TR>());
        //}

        //protected static Func<T, TR> _convertFunc<T, TR>(Func<T, object> f)
        //{
        //    return arg => (TR) f(arg);
        //}

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

        private readonly Dictionary<string, IAccessObject> _accessObjects;
        private readonly Type _type;
        private Dictionary<string, MethodProperty> _methods;
        private static readonly MethodInfo BuildAccessorsMethod = typeof(DataProxy<TEntity>).GetMethod("BuildAccessors", BindingFlags.Static | BindingFlags.NonPublic);
        private readonly Dictionary<string, object> _virtualProperties = new Dictionary<string, object>();

        #endregion

        #region ctor's

        public DataProxy(TEntity entity)
        {
            var proxy = entity as DataProxy<TEntity>;
            _type = proxy != null ? proxy.UnderlyingObject.GetType() : entity.GetType();           

            RawView = new Dictionary<string, object>();
            if (!AccessObjects.Value.ContainsKey(_type)) 
                BuildAccessorsMethod.MakeGenericMethod(_type).Invoke(null, new object[] {_type, this});
            _accessObjects = AccessObjects.Value[_type];

            UnderlyingObject = entity;

            if (!MethodsDictionary.Value.ContainsKey(_type))
                MethodsDictionary.Value.Add(_type, new Dictionary<string, MethodProperty>());

            _methods = new Dictionary<string, MethodProperty>(MethodsDictionary.Value[_type]);

            Count = _accessObjects.Keys.Count;
        }

        public DataProxy(Type type)
        {
            _type = type;                                        

            RawView = new Dictionary<string, object>();
            _type = type;
            if (!AccessObjects.Value.ContainsKey(_type))
                BuildAccessorsMethod.MakeGenericMethod(_type).Invoke(null, new object[] {_type, this});
            _accessObjects = AccessObjects.Value[_type];               

            UnderlyingObject = default(TEntity);

            if (!MethodsDictionary.Value.ContainsKey(_type))
                MethodsDictionary.Value.Add(_type, new Dictionary<string, MethodProperty>());

            _methods = new Dictionary<string, MethodProperty>(MethodsDictionary.Value[_type]);

            Count = _accessObjects.Keys.Count;
        }

        public DataProxy()
        {
            _type = typeof (TEntity);            

            RawView = new Dictionary<string, object>();
            if (!AccessObjects.Value.ContainsKey(_type)) 
                BuildAccessorsMethod.MakeGenericMethod(_type).Invoke(null, new object[] {_type, this});
            _accessObjects = AccessObjects.Value[_type];

            if (!MethodsDictionary.Value.ContainsKey(_type))
                MethodsDictionary.Value.Add(_type, new Dictionary<string, MethodProperty>());

            _methods = new Dictionary<string, MethodProperty>(MethodsDictionary.Value[_type]);

            Count = _accessObjects.Keys.Count;
        }

        #endregion

        #region Properties
        
        public Dictionary<string, MethodProperty> Methods
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
                return AccessObjects.Value[_type].Keys.AsEnumerable();
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

        #region Type generation
#if !PORTABLE

        /// <summary>
        /// Generates a new object from specified meta-data, including all properties and virtual properties.
        /// </summary>
        /// <returns></returns>
        public object Generate(bool fillValues = true)
        {
            List<FieldMetadata> metadatas =
                _virtualProperties.Select(o => new FieldMetadata
                {
                    FieldName = o.Key,
                    FieldType = o.Value.GetType()
                }).ToList();

            var type = TypeGenerator.GetResultType(_type, metadatas);
            if (!fillValues) return Activator.CreateInstance(type);

            var result = Create(Activator.CreateInstance(type));
            foreach (var o in result)
            {
                result[o] = this[o];
            }

            return result.UnderlyingObject;
        }

        /// <summary>
        /// Generates a new object from specified meta-data, including all properties and virtual properties.
        /// </summary>
        /// <returns></returns>
        public T Generate<T>(bool fillValues = true)
            where T : TEntity
        {
            List<FieldMetadata> metadatas =
                _virtualProperties.Select(o => new FieldMetadata
                {
                    FieldName = o.Key,
                    FieldType = o.Value.GetType()
                }).ToList();

            var type = TypeGenerator.GetResultType(typeof (T), metadatas);
            if (!fillValues) return (T) Activator.CreateInstance(type);

            var result = Create(Activator.CreateInstance(type));
            foreach (var o in result)
            {
                result[o] = this[o];
            }

            return (T) result.UnderlyingObject;
        }

        /// <summary>
        /// Generates a new object from specified meta-data, including all properties and virtual properties.
        /// </summary>
        /// <returns></returns>
        public object Generate(TEntity obj)
        {
            List<FieldMetadata> metadatas =
                _virtualProperties.Select(o => new FieldMetadata
                {
                    FieldName = o.Key,
                    FieldType = o.Value.GetType()
                }).ToList();

            var type = TypeGenerator.GetResultType(_type, metadatas);
            var result = Create(Activator.CreateInstance(type));

            foreach (var o in result)
            {
                result[o] = this[obj, o];
            }

            return result.UnderlyingObject;
        }        

        /// <summary>
        /// Generates a new object from specified meta-data, including all properties and virtual properties.
        /// </summary>
        /// <returns></returns>
        public T Generate<T>(T obj)
            where T : TEntity
        {
            List<FieldMetadata> metadatas =
                _virtualProperties.Select(o => new FieldMetadata
                {
                    FieldName = o.Key,
                    FieldType = o.Value.GetType()
                }).ToList();

            var type = TypeGenerator.GetResultType(typeof (T), metadatas);
            var result = Create(Activator.CreateInstance(type));

            foreach (var o in result)
            {
                result[o] = this[obj, o];
            }

            return (T) result.UnderlyingObject;
        }
#endif
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

        internal object GetReflectedGetter(string name, Type type)
        {
            return this.GetType().GetMethod("GetGetterGeneric", BindingFlags.NonPublic | BindingFlags.Instance)
                .MakeGeneric(type)
                .Invoke(this, new object[] {name});
        }

        internal Func<TEntity, TR> GetGetterGeneric<TR>(string name)
        {
            return _accessObjects[name].GetGetter<TEntity, TR>();
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

        private static void BuildAccessors<T>(Type type, DataProxy<TEntity> obj)
        {
            if (AccessObjects.Value.ContainsKey(type)) return;

            AccessObjects.Value.Add(type, new Dictionary<string, IAccessObject>());
            var buildAccessor = typeof(DataProxy<TEntity>).GetMethod("BuildAccessor", BindingFlags.NonPublic | BindingFlags.Static);

            foreach (var o in type.GetProperties().Where(o => o.CanRead && o.CanWrite))
            {                
                buildAccessor.MakeGeneric(type, o.PropertyType)
                    .Invoke(null, new object[]{type, o});
            }
        }

        private static void BuildAccessor<T, TR>(Type type, PropertyInfo o)
        {
            if (AccessObjects.Value[type].ContainsKey(o.Name) && o.DeclaringType == type)
            {
                AccessObjects.Value[type][o.Name] = new AccessObject<T, TR>
                {
                    PropertyInfo = o,
                    Getter = o.CanRead ? GetValueGetter<T, TR>(o, type) : null,
                    Setter = o.CanWrite ? GetValueSetter<T, TR>(o, type) : null,
                    CanCreate = o.PropertyType.GetConstructor(new Type[0]) != null
                };
            }
            else if (!AccessObjects.Value[type].ContainsKey(o.Name))
            {
                AccessObjects.Value[type].Add(o.Name, new AccessObject<T, TR>
                {
                    PropertyInfo = o,
                    Getter = o.CanRead ? GetValueGetter<T, TR>(o, type) : null,
                    Setter = o.CanWrite ? GetValueSetter<T, TR>(o, type) : null,
                    CanCreate = o.PropertyType.GetConstructor(new Type[0]) != null
                });
            }
        }

        private static Func<T, TR> GetValueGetter<T, TR>(PropertyInfo propertyInfo, Type type)
        {            
            if (!(type == propertyInfo.DeclaringType || type == propertyInfo.ReflectedType))
            {
                throw new Exception();                
            }

            return (Func<T, TR>)Delegate.CreateDelegate(typeof(Func<T, TR>), propertyInfo.GetGetMethod());
        }

        private static Action<T, TR> GetValueSetter<T, TR>(PropertyInfo propertyInfo, Type type)
        {            
            if (!(type == propertyInfo.DeclaringType || type == propertyInfo.ReflectedType))
            {
                throw new Exception();                
            }

            return (Action<T, TR>)Delegate.CreateDelegate(typeof(Action<T, TR>), propertyInfo.GetSetMethod());
        }        

        private static void InitObjects<T>(ref Dictionary<string, AccessObject<T, object>> dictionary)
        {
            var type = typeof(T);
            foreach (var o in AccessObjects.Value[type].As<Dictionary<string, object>>())
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
    }
}
