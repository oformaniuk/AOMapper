using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AOMapper.Extensions;
using AOMapper.Helpers;
using AOMapper.Interfaces;

namespace AOMapper
{
    /// <summary>
    /// 
    /// </summary>
    public class Mapper
    {
        static Mapper()
        {
            var mapperType = typeof(Mapper);
            ActionConverter = mapperType.GetMethod("_convertAction", BindingFlags.NonPublic | BindingFlags.Static);
            FuncConverter = mapperType.GetMethod("_convertFunc", BindingFlags.NonPublic | BindingFlags.Static);
            GetSourceInvoker = mapperType.GetMethod("__getSourceInvoker", BindingFlags.NonPublic | BindingFlags.Static);
            GetterCreator = mapperType.GetMethod("___getInvoker", BindingFlags.NonPublic | BindingFlags.Static);
            SetterCreator = mapperType.GetMethod("___getSetInvoker", BindingFlags.NonPublic | BindingFlags.Static);
        }

        /// <summary>
        /// Creates new or get cached objects map
        /// </summary>
        /// <returns></returns>
        public static IMap<TS, TR> Create<TS, TR>()                
        {            
            return MapperInnerClass<TS, TR>.Map();
        }

        #region Fields

        private static readonly MethodInfo ActionConverter;

        private static readonly MethodInfo FuncConverter;

        private static readonly MethodInfo GetSourceInvoker;

        private static readonly MethodInfo GetterCreator;

        private static readonly Lazy<Dictionary<string, MethodProperty>> GlobalMethods =
                    new Lazy<Dictionary<string, MethodProperty>>();

        private static readonly Dictionary<ArgArray, object> Maps
            = new Dictionary<ArgArray, object>();

        private static readonly MethodInfo SetterCreator;
        
        private static readonly Type TypeOfObject = typeof (object);
        #endregion

        #region ConfigClass

        /// <summary>
        /// 
        /// </summary>
        public sealed class Config
        {
            internal Config()
            {
                IgnoreDefaultValues = false;
                Separator = '/';
            }

            /// <summary>
            /// <para>Gets or sets a value indicating whether default values would be ignored during mapping.</para>
            /// <para>Default: false</para>
            /// </summary>
            /// <value>
            ///   <c>true</c> if default values should be ignored during mapping; otherwise, <c>false</c>.
            /// </value>
            public bool IgnoreDefaultValues { get; set; }

            /// <summary>
            /// Gets or sets the path separator.
            /// </summary>            
            public char Separator { get; set; }
        }

        #endregion

        #region Mapper

        protected class MapperInnerClass<TSource, TDestination> : Mapper, IMap<TSource, TDestination>
        {

            #region Fields

            private static readonly TDestination DestinationDefault = Activator.CreateInstance<TDestination>();
            private static readonly Dictionary<ArgArray, MapperInnerClass<TSource, TDestination>> Mappers =
                new Dictionary<ArgArray, MapperInnerClass<TSource, TDestination>>();

            private static readonly TSource SourceDefault = Activator.CreateInstance<TSource>();
            private readonly Config _config = new Config();
            private ArgArray _args;
            private Type _destination;
            private PropertyMap<TSource, TDestination> _map;
            private Type _source;

            #endregion

            #region Do

            /// <summary>
            /// Executes mapping from target to destination object      
            /// </summary>            
            /// <param name="sourceObject"></param>            
            /// <returns></returns>
            public TDestination Do(TSource sourceObject)
            {
                return _config.IgnoreDefaultValues ? _doIgnoreDefaults(sourceObject) : _do(sourceObject);
            }

            public TDestination Do(TSource sourceObject, TDestination dest)
            {
                return _config.IgnoreDefaultValues ? _doIgnoreDefaults(sourceObject, dest) : _do(sourceObject, dest);
            }

            public TD Do<TS, TD>(TS obj, TD dest)
            {
                return Do(obj.As<TSource>(), dest.As<TDestination>()).As<TD>();
            }

            public TDestination Do(object obj, TDestination dest)
            {
                return Do(obj.As<TSource>());
            }

            #endregion

            #region ConfigMap

            IMap IMap.ConfigMap(Action<Config> config)
            {
                config(_config);
                return this;
            }

            IMap<TDestination> IMap<TDestination>.ConfigMap(Action<Config> config)
            {
                config(_config);
                return this;
            }

            IMap<TSource, TDestination> IMap<TSource, TDestination>.ConfigMap(Action<Config> config)
            {
                config(_config);
                return this;
            }

            #endregion

            internal static MapperInnerClass<TSource, TDestination> Map()
            {
                var s = typeof(TSource);
                var t = typeof(TDestination);
                var args = new ArgArray(s, t);
                if (Mappers.ContainsKey(args))
                {
                    return Mappers[args];
                }

                var mapper = new MapperInnerClass<TSource, TDestination>
                {
                    _source = s,
                    _destination = t,
                    _args = args
                };

                if (!Mapper.Maps.ContainsKey(args))
                {
                    var destination = new DataProxy<TDestination>();
                    var source = new DataProxy<TSource>();
                    mapper._map = new PropertyMap<TSource, TDestination>
                    {
                        Destination = destination,
                        Source = source,
                        AdditionalMaps = new List<EditableKeyValuePair<MapObject<Func<TSource, object>>, MapObject<Action<TDestination, object>>>>()
                    };

                    Mapper.Maps.Add(args, mapper._map);
                }
                else mapper._map = (PropertyMap<TSource, TDestination>)Mapper.Maps[args];

                Mappers.Add(args, mapper);

                return mapper;
            }
            
            private TDestination _do(TSource sourceObject, TDestination destinationObject = default(TDestination))
            {
                var additionalMaps = _map.AdditionalMaps.ToList();

                if (destinationObject == null || destinationObject.Equals(default(TDestination)))
                    destinationObject = Activator.CreateInstance<TDestination>();

                foreach (var o in ((PropertyMap<TSource, TDestination>) Mapper.Maps[_args]).Destination
                    .Where(o => !additionalMaps.Any(k => k.Value.Path.Contains(o))))
                {
                    _map.Destination[destinationObject, o] = _map.Source[sourceObject, o];
                }

                foreach (var map in additionalMaps)
                {
                    var sourcePath = map.Key.Invoker;
                    var destinationPath = map.Value.Invoker;

                    var sourceValue = sourcePath(sourceObject);

                    destinationPath(destinationObject, sourceValue);
                }

                return destinationObject;
            }

            private TDestination _doIgnoreDefaults(TSource sourceObject, TDestination destinationObject = default (TDestination))
            {
                var additionalMaps = _map.AdditionalMaps.ToList();

                if (destinationObject == null || destinationObject.Equals(default(TDestination)))
                    destinationObject = Activator.CreateInstance<TDestination>();

                foreach (var o in ((PropertyMap<TSource, TDestination>)Mapper.Maps[_args]).Destination
                    .Where(o => !additionalMaps.Any(k => k.Value.Path.Contains(o))))
                {
                    var value = _map.Source[sourceObject, o];
                    if (value != null && !value.Equals(default (TDestination)))
                    {
                        _map.Destination[destinationObject, o] = value;
                    }                    
                }

                foreach (var map in additionalMaps)
                {
                    var sourcePath = map.Key.Invoker;
                    var destinationPath = map.Value.Invoker;

                    var sourceValue = sourcePath(sourceObject);

                    if (sourceValue != null && !sourceValue.Equals(sourceValue.GetType().GetDefault()))
                    {
                        destinationPath(destinationObject, sourceValue);   
                    }                    
                }

                return destinationObject;
            }              

            #region IMap<,>

            /// <summary>        
            /// Generates mapping proxy object, that allows to perform real-time mapping using parent gettes and setters.
            /// </summary>
            /// <param name="obj"></param>
            /// <returns></returns>
            public MappingObject<TSource, TDestination> GenerateProxy(TSource obj)
            {
                var sourceType = obj.GetType();
                var getter = typeof(Func<,>);
                var setter = typeof(Action<,>);

                var metadatas = new List<FieldMetadata>();

                var additionalMaps = _map.AdditionalMaps.ToList();
                var dest = Activator.CreateInstance<TDestination>();
                foreach (var o in ((PropertyMap<TSource, TDestination>)Mapper.Maps[_args]).Destination
                    .Where(o => !additionalMaps.Any(k => k.Value.Path.Contains(o))))
                {
                    var destType = DetermineResultType(obj, new[] { o });
                    var destProp = DetermineResultProperty(obj, new[] { o });
                    metadatas.Add(new FieldMetadata
                    {
                        FieldName = o,
                        FieldType = destType,
                        MappedPropertyGetter = Delegate.CreateDelegate(getter.MakeGenericType(sourceType, destType), destProp[0]),
                        MappedPropertySetter = Delegate.CreateDelegate(setter.MakeGenericType(sourceType, destType), destProp[1]),
                        DeclareType = sourceType,
                        Object = obj
                    });
                }

                foreach (var map in additionalMaps)
                {
                    var o = map.Key.LastInvokeTarget.DynamicInvoke(obj);
                    var destinationPath = map.Key.Path.Split(_config.Separator);
                    var destType = DetermineResultType(obj, destinationPath);
                    var destProp = DetermineResultProperty(obj, destinationPath);
                    var sType = destProp[0].DeclaringType;

                    metadatas.Add(new FieldMetadata
                    {
                        FieldName = destinationPath.Last(),
                        FieldType = destType,
                        MappedPropertyGetter = Delegate.CreateDelegate(getter.MakeGenericType(sType, destType), destProp[0]),
                        MappedPropertySetter = Delegate.CreateDelegate(setter.MakeGenericType(sType, destType), destProp[1]),
                        DeclareType = sType,
                        Object = o
                    });
                }

                return new MappingObject<TSource, TDestination>(metadatas) { UnderlyingObject = obj };
            }

            IMap<TSource, TDestination> IMap<TSource, TDestination>.RegisterDestinationMethod<T>(string name, T method)
            {
                RegisterDestinationMethod(name, method);
                return this;
            }

            IMap<TSource, TDestination> IMap<TSource, TDestination>.RegisterGlobalMethod<T>(string name, T method)
            {
                RegisterGlobalMethod(name, method);
                return this;
            }

            IMap<TSource, TDestination> IMap<TSource, TDestination>.RegisterMethod<T>(string name, T method)
            {
                RegisterMethod(name, method);
                return this;
            }
            IMap<TSource, TDestination> IMap<TSource, TDestination>.RegisterSourceMethod<T>(string name, T method)
            {
                RegisterSourceMethod(name, method);
                return this;
            }            

            IMap<TSource, TDestination> IMap<TSource, TDestination>.Remap(string source, string destination)
            {
                Remap(source, destination);
                return this;
            }

            IMap<TSource, TDestination> IMap<TSource, TDestination>.Remap<TR>(string source, string destination)
            {
                Remap<TR>(source, destination);
                return this;
            }
            
            #endregion

            #region IMap<>            

            #region Method registration

            IMap<TDestination> IMap<TDestination>.RegisterDestinationMethod<T>(string name, T method)
            {
                RegisterDestinationMethod(name, method);
                return this;
            }

            IMap<TDestination> IMap<TDestination>.RegisterGlobalMethod<T>(string name, T method)
            {
                RegisterGlobalMethod(name, method);
                return this;
            }

            IMap<TDestination> IMap<TDestination>.RegisterMethod<T>(string name, T method)
            {
                RegisterMethod(name, method);
                return this;
            }

            IMap<TDestination> IMap<TDestination>.RegisterSourceMethod<T>(string name, T method)
            {
                RegisterSourceMethod(name, method);
                return this;
            }

            #endregion

            TDestination IMap<TDestination>.Do(object obj)
            {
                return Do((TSource)obj);
            }

            IMap<TDestination> IMap<TDestination>.Remap<TR>(string source, string destination)
            {
                Remap<TR>(source, destination);
                return this;
            }

            IMap<TDestination> IMap<TDestination>.Remap(string source, string destination)
            {
                Remap(source, destination);
                return this;
            }
            
            #endregion

            #region IMap

            TR IMap.Do<T, TR>(T obj)
            {
                return Do(obj.As<TSource>()).As<TR>();
            }

            /// <summary>
            /// Generates the mapping proxy object which contains getters and setters from parent object
            /// </summary>
            /// <param name="obj">The parent object</param>
            /// <returns></returns>
            MappingObject IMap.GenerateProxy(object obj)
            {
                return GenerateProxy((TSource)obj);
            }
            
            public IMap Remap<TR>(string source, string destination)
            {
                return _remapInner<TR>(source, destination);
            }

            public IMap Remap(string source, string destination)
            {
                this.GetType().GetMethod("_remapInner", BindingFlags.NonPublic)
                    .MakeGeneric(DetermineResultType(DestinationDefault, destination.Split(_config.Separator)))
                    .Invoke(this, new object[] { source, destination });

                return this;
            }
            #endregion

            #region Method registration

            public IMap RegisterDestinationMethod<T>(string name, T method)
            {
                _map.Destination.RegisterMethod(name, method);
                return this;
            }

            public IMap RegisterGlobalMethod<T>(string name, T method)
            {
                if (!GlobalMethods.Value.ContainsKey(name))
                    GlobalMethods.Value.Add(name, new MethodProperty
                    {
                        Info = method.As<Delegate>().Method,
                        Delegate = method.As<Delegate>()
                    });
                return this;
            }

            public IMap RegisterMethod<T>(string name, T method)
            {
                _map.Source.RegisterMethod(name, method);
                _map.Destination.RegisterMethod(name, method);
                GlobalMethods.Value[name] = new MethodProperty
                {
                    Delegate = method.As<Delegate>(),
                    Info = method.As<Delegate>().Method
                };
                return this;
            }

            public IMap RegisterSourceMethod<T>(string name, T method)
            {
                _map.Source.RegisterMethod(name, method);
                return this;
            }

            #endregion

            #region Helpers

            private static Func<T, TR> __getSourceInvoker<T, TR>(T obj, string[] paths, out Delegate @out)
            {
                @out = null;
                var firstPath = paths.First();
                var xc = DataProxy.Create<T>();

                Type propertyType = null;
                object @delegate = null;

                if (xc.ContainsProperty(firstPath))
                {
                    propertyType = xc.GetPropertyInfo(firstPath).PropertyType;
                    @delegate = xc.GetReflectedGetter(firstPath, propertyType);
                }
                else if (xc.ContainsMethod(firstPath))
                {
                    propertyType = xc.Methods[firstPath].Info.ReturnType;
                    @delegate = FuncConverter
                        .MakeGeneric(typeof(T), propertyType)
                        .Invoke(null, new object[] { xc.Methods[firstPath].Delegate });
                }
                else
                {
                    var method = GlobalMethods.Value[firstPath];
                    @delegate = method.Delegate;
                    propertyType = method.Info.ReturnType;
                }

                string last = null;
                if (paths.Skip(1).Any()) last = paths.Skip(1).Last();
                foreach (var s in paths.Skip(1))
                {
                    if (last == s) @out = (Delegate)@delegate;
                    Type target;
                    var p = DataProxy.Create(propertyType);
                    if (p.ContainsProperty(s)) target = p.GetPropertyInfo(s).PropertyType;
                    else if (p.ContainsMethod(s)) target = p.Methods[s].Info.ReturnType;
                    else target = GlobalMethods.Value[s].Info.ReturnType;

                    var pair = (KeyValuePair<Type, Delegate>)GetterCreator.MakeGenericMethod(typeof(T), propertyType, target)
                        .Invoke(null, new object[] { s, new KeyValuePair<Type, Delegate>(propertyType, (Delegate)@delegate) });
                    @delegate = pair.Value;
                    propertyType = pair.Key;
                }

                return (Func<T, TR>)@delegate;
            }

            private Action<T, TR> _getDestinationInvoker<T, TR>(string path, T obj, out Delegate @out)
            {
                @out = null;
                var paths = path.Split(_config.Separator).Select(x => x.Trim()).ToList();
                var propertyName = paths.Last();
                paths.Remove(propertyName);

                Type propertyType = null;
                var xc = DataProxy.Create<T>();

                if (!paths.Any())
                {
                    var setter = DataProxy.Create(obj).GetSetter<TR>(propertyName);
                    @out = setter;
                    return setter;
                }

                var firstPath = paths.First();


                object f = null;

                if (xc.ContainsProperty(firstPath))
                {
                    propertyType = xc.GetPropertyInfo(firstPath).PropertyType;
                    f = xc.GetReflectedGetter(firstPath, propertyType);
                }
                else if (xc.ContainsMethod(firstPath))
                {
                    propertyType = xc.Methods[firstPath].Info.ReturnType;
                    f = FuncConverter
                        .MakeGeneric(typeof(T), propertyType)
                        .Invoke(null, new object[] { xc.Methods[firstPath].Delegate });
                }
                else
                {
                    var method = GlobalMethods.Value[firstPath];
                    f = method.Delegate;
                    propertyType = method.Info.ReturnType;
                }

                KeyValuePair<Type, Delegate> ff = new KeyValuePair<Type, Delegate>(propertyType, (Delegate)f);
                foreach (var s in paths.Skip(1))
                {
                    Type target;
                    var p = DataProxy.Create(propertyType);
                    if (p.ContainsProperty(s)) target = p.GetPropertyInfo(s).PropertyType;
                    else if (p.ContainsMethod(s)) target = p.Methods[s].Info.ReturnType;
                    else target = GlobalMethods.Value[s].Info.ReturnType;

                    ff = (KeyValuePair<Type, Delegate>)GetterCreator.MakeGenericMethod(typeof(T), propertyType, target)
                        .Invoke(null, new object[] { s, new KeyValuePair<Type, Delegate>(propertyType, (Delegate)f) });
                    f = ff.Value;
                    propertyType = ff.Key;
                }

                var d1 = typeof(DataProxy<>);
                Type[] typeArgs = { ff.Key };
                var makeme = d1.MakeGenericType(typeArgs);
                object o = Activator.CreateInstance(makeme);
                var m = SetterCreator.MakeGeneric(ff.Key, typeof(TR)).Invoke(null, new object[] { o, propertyName });
                var minfo = m.As<Delegate>().Method;

                var a = ActionConverter
                    .MakeGeneric(propertyType, minfo.GetParameters()[0].ParameterType, TypeOfObject, typeof(TR))
                    .Invoke(null, new object[] { m });
                @out = (Delegate)a;
                return ((Func<T, object>)f).Compose((Action<object, TR>)a);
            }

            private IMap _remapInner<TR>(string source, string destination)
            {
                lock (this)
                {
                    if (!_map.AdditionalMaps.Any(o => o.Value.Path.Equals(destination)))
                    {
                        Delegate tempSource;
                        _map.AdditionalMaps.Add(new EditableKeyValuePair<MapObject<Func<TSource, object>>, MapObject<Action<TDestination, object>>>
                            (new MapObject<Func<TSource, object>>
                            {
                                Path = source,
                                Invoker = __getSourceInvoker<TSource, TR>(SourceDefault, source.Split(_config.Separator).Select(x => x.Trim()).ToArray(), out tempSource).Convert<TSource, TR, TSource, object>(),
                                LastInvokeTarget = tempSource
                            },
                            new MapObject<Action<TDestination, object>>
                            {
                                Path = destination,
                                Invoker = _getDestinationInvoker<TDestination, TR>(destination, DestinationDefault, out tempSource).Convert<TDestination, TR, TDestination, Object>(),
                                LastInvokeTarget = tempSource
                            }
                        ));
                    }
                    else _map.AdditionalMaps.Single(o => o.Value.Path.Equals(destination)).Key.Path = source;
                }
                return this;
            }

            private MethodInfo[] DetermineResultProperty<T>(T objType, string[] paths)
            {
                var proxy = DataProxy.Create((object)objType);
                string lastPath = paths.Last();

                foreach (var s in paths)
                {
                    Type target = null;
                    if (proxy.ContainsProperty(s)) target = proxy.GetPropertyInfo(s).PropertyType;
                    else if (proxy.ContainsMethod(s)) target = proxy.Methods[s].Info.ReturnType;
                    else target = GlobalMethods.Value[s].Info.ReturnType;

                    if (s != lastPath)
                        proxy = DataProxy.Create(target);
                }

                if (proxy.ContainsProperty(lastPath))
                {
                    var info = proxy.GetPropertyInfo(lastPath);
                    return new[] { info.GetGetMethod(), info.GetSetMethod() };
                }
                else if (proxy.ContainsMethod(lastPath))
                {
                    var info = proxy.Methods[lastPath].Info;
                    return new[] { info, info };
                }
                else
                {
                    var info = GlobalMethods.Value[lastPath].Info;
                    return new[] { info, info };
                }
            }

            private Type DetermineResultType<T>(T objType, string[] paths)
            {
                Type target = null;
                var proxy = DataProxy.Create((object)objType);
                string lastPath = paths.Last();

                foreach (var s in paths)
                {
                    if (proxy.ContainsProperty(s)) target = proxy.GetPropertyInfo(s).PropertyType;
                    else if (proxy.ContainsMethod(s)) target = proxy.Methods[s].Info.ReturnType;
                    else target = GlobalMethods.Value[s].Info.ReturnType;

                    if(s != lastPath)
                        proxy = DataProxy.Create(target);
                }
                
                return target;
            }
            #endregion
        }

        #endregion        

        #region Helpers

        private static KeyValuePair<Type, Delegate> /*Func<TF, TR>*/ ___getInvoker<TF, T, TR>(string firstPath,
            KeyValuePair<Type, Delegate> func /*Func<TF, T> func*/)
        {
            Type info = null;
            Func<T, TR> tempValue = null;
            var xx = DataProxy.Create<T>();
            if (xx.ContainsProperty(firstPath))
            {                
                tempValue = xx.GetGetterGeneric<TR>(firstPath);                
            }
            else if (xx.ContainsMethod(firstPath))
            {
                var method = xx.Methods[firstPath];
                tempValue = _convertDelegateToFunc<T, TR>(method.Delegate);                
            }
            else if (GlobalMethods.Value.ContainsKey(firstPath))
            {
                var method = GlobalMethods.Value[firstPath];                
                tempValue = (Func<T, TR>)method.Delegate;                
            }

            return new KeyValuePair<Type, Delegate>(tempValue.Method.ReturnType, ((Func<TF, T>)func.Value).Compose(tempValue));
        }

        private static Action<T, TR> ___getSetInvoker<T, TR>(DataProxy<T> proxy, string name)
        {
            return proxy.GetSetter<TR>(name);
        }

        private static Action<TNew, TRNew> _convertAction<T, TR, TNew, TRNew>(Action<T, TR> f)
        {
            return (o, o1) => f(o.As<T>(), o1.As<TR>());
        }

        private static Func<T, TR> _convertDelegateToFunc<T, TR>(Delegate f)
        {
            return arg => ((Func<T, TR>)f)(arg);
        }

        private static Func<T, TR> _convertFunc<T, TR>(Func<T, object> f)
        {
            return arg => (TR)f(arg);
        }
        #endregion
    }
}
