using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using AOMapper.Data;
using AOMapper.Extensions;
using AOMapper.Interfaces;
using AOMapper.Resolvers;

namespace AOMapper
{
    /// <summary>
    /// 
    /// </summary>
    public class Mapper
    {
        static Mapper()
        {
            TypeOfObject = typeof(object);
            var mapperType = typeof (Mapper);
            GlobalMethods = new Lazy<Dictionary<string, MethodProperty>>();
            Maps = new Dictionary<ArgArray, object>();
            ActionConverter = mapperType.GetMethod("_convertAction", BindingFlags.NonPublic | BindingFlags.Static);
            FuncConverter = mapperType.GetMethod("_convertFunc", BindingFlags.NonPublic | BindingFlags.Static);
            GetSourceInvoker = mapperType.GetMethod("GetSourceInvokeChain", BindingFlags.NonPublic | BindingFlags.Static);
            GetterCreator = mapperType.GetMethod("GetterBuilder", BindingFlags.NonPublic | BindingFlags.Static);
            InitCreator = mapperType.GetMethod("InitBuilder", BindingFlags.NonPublic | BindingFlags.Static);
            LoopCreator = mapperType.GetMethod("LoopBuilder", BindingFlags.NonPublic | BindingFlags.Static);
            SetterCreator = mapperType.GetMethod("___getSetInvoker", BindingFlags.NonPublic | BindingFlags.Static);
            CreatedMappers = new List<object>();
        }

        /// <summary>
        /// Creates new or get cached objects map
        /// </summary>
        /// <returns></returns>
        public static IMap<TS, TR> Create<TS, TR>() where TR : new()
        {
            return MapperInnerClass<TS, TR>.Map();
        }

        public static void Clear()
        {
            foreach (var map in CreatedMappers) map.As<IDisposable>().Dispose(); 
            CreatedMappers.Clear();
            GlobalMethods.Value.Clear();            
            Maps.Clear();
        }

        #region Fields

        private static readonly MethodInfo ActionConverter;

        private static readonly MethodInfo FuncConverter;

        private static readonly MethodInfo GetSourceInvoker;

        private static readonly MethodInfo GetterCreator;

        private static readonly MethodInfo InitCreator;

        private static readonly MethodInfo LoopCreator;

        private static readonly Lazy<Dictionary<string, MethodProperty>> GlobalMethods;

        private static readonly Dictionary<ArgArray, object> Maps;           

        private static readonly MethodInfo SetterCreator;

        private static readonly Type TypeOfObject;

        private const string InitMethodName = "________InIt";
        private const string LoopMethodName = "________Loop";

        protected static readonly List<object> CreatedMappers;  

        protected object InitializeValue(object obj, string prop)
        {
            var main = DataProxy.Create(obj.GetType());

            if (main[obj, prop] == null && main.CanCreate(prop))
                main[obj, prop] = Activator.CreateInstance(main.GetPropertyInfo(prop).PropertyType);

            return obj;
        }

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
                InitialyzeNullValues = true;
            }

            /// <summary>
            /// <para>Gets or sets a value indicating whether default values would be ignored during mapping.</para>
            /// <para>Default: False</para>
            /// </summary>
            /// <value>
            ///   <c>true</c> if default values should be ignored during mapping; otherwise, <c>false</c>.
            /// </value>
            public bool IgnoreDefaultValues { get; set; }

            /// <summary>
            /// Gets or sets the path separator.
            /// </summary>            
            public char Separator { get; set; }

            /// <summary>
            /// <para>Gets or sets a value indicating whether properties with null values should be initialized during mapping.</para>
            /// <para>Default: True</para>
            /// </summary>
            /// <value>
            /// <c>true</c> if null values should be initialized during mapping; otherwise, <c>false</c>.
            /// </value>
            public bool InitialyzeNullValues { get; set; }
        }

        #endregion

        #region Mapper

        protected class MapperInnerClass<TSource, TDestination> : Mapper, IMap<TSource, TDestination>, IPathProvider, IDisposable where TDestination : new()
        {
            #region Fields

            private static readonly TDestination DestinationDefault = Activator.CreateInstance<TDestination>();

            private static readonly Dictionary<ArgArray, MapperInnerClass<TSource, TDestination>> Mappers =
                new Dictionary<ArgArray, MapperInnerClass<TSource, TDestination>>();

            private static readonly TSource SourceDefault = Activator.CreateInstance<TSource>();
            private readonly Config _config = new Config();
            private PropertyMap<TSource, TDestination> _map;

            private Dictionary<string, object> _destinationCodeTree;
            private Dictionary<string, object> _sourceCodeTree;

            private Dictionary<string, IMap> _complexMaps;

            private Func<TSource, TDestination, TDestination> _compiledMap; 

            #endregion

            #region auto-mapping            

            private MapperInnerClass<TSource, TDestination> Auto()
            {
                _destinationCodeTree = new Dictionary<string, object>();
                GenerateCodeTree<TDestination>(_destinationCodeTree, _config.InitialyzeNullValues);
                _sourceCodeTree = new Dictionary<string, object>();
                GenerateCodeTree<TSource>(_sourceCodeTree, false);                

                if (_destinationCodeTree.Values.Any(o => o.ToString().StartsWith(LoopMethodName)) &&
                    _sourceCodeTree.Values.Any(o => o.ToString().StartsWith(LoopMethodName)))
                {

                    _complexMaps = new Dictionary<string, IMap>();
                    CreateComplexMapping<TDestination, TSource>();
                }

                CreateMapping(_destinationCodeTree, _sourceCodeTree);

                return this;
            }

            public IMap Compile()
            {
                TSource sObject = default(TSource);
                var dObject = default(TDestination);                
                _compiledMap = (sourceObject, destinationObject) =>
                {
                    sObject = sourceObject;
                    if (destinationObject == null || destinationObject.Equals(default(TDestination)))
                    {
                        var ddObject = new TDestination();
                        dObject = ddObject;
                        destinationObject = ddObject;
                        return destinationObject;
                    }                        
                    return dObject = destinationObject;                    
                };

                var destination = _map.Destination;
                var source = _map.Source;

                var nonRemapedDests = _map.DestinationNonReMapedProperties;
                for (int index = 0; index < nonRemapedDests.Length; index++)
                {                    
                    var o = nonRemapedDests[index];
                    var m = _compiledMap;
                    var setter = (Action<TDestination, object>)destination.GetReflectedConvertedSetter(o, destination.GetPropertyInfo(o).PropertyType);
                    var getter = (Func<TSource, object>)source.GetReflectedConvertedGetter(o, source.GetPropertyInfo(o).PropertyType);
                    
                    _compiledMap = (sourceObject, destinationObject) =>
                    {
                        destinationObject = m(sourceObject, destinationObject);                        
                        setter(dObject, getter(sObject));

                        return destinationObject;
                    };
                }

                var additionalMaps = _map.AdditionalMaps;
                for (int index = 0; index < additionalMaps.Count; index++)
                {                    
                    var map = additionalMaps[index];
                    var m = _compiledMap;
                    var keyInvoker = map.Key.Invoker;
                    var valueInvoker = map.Value.Invoker;
                    
                    _compiledMap = (sourceObject, destinationObject) =>
                    {
                        destinationObject = m(sourceObject, destinationObject);

                        var sourceValue = keyInvoker(sObject);
                        valueInvoker(dObject, sourceValue);
                        return destinationObject;
                    };
                }

                if (_complexMaps == null) return this;

                foreach (var complexMap in _complexMaps)
                {
                    var map = complexMap;
                    var m = _compiledMap;                    

                    var valType = source.GetPropertyInfo(complexMap.Key).PropertyType;
                    var destType = destination.GetPropertyInfo(map.Key).PropertyType;

                    var resolver = Resolver.Create(typeof(ArrayResolver<,>), valType, destType, map.Value);                                                             
                    
                    var o = map.Key;
                    var setter = (Action<TDestination, object>)destination.GetReflectedConvertedSetter(o, destination.GetPropertyInfo(o).PropertyType);
                    var getter = (Func<TSource, object>)source.GetReflectedConvertedGetter(o, source.GetPropertyInfo(o).PropertyType);

                    _compiledMap = (sourceObject, destinationObject) =>
                    {
                        destinationObject = m(sourceObject, destinationObject);

                        var val = getter(sObject);
                        if (val == null) return destinationObject;

                        var valList = (IList)val;
                        IList destList = null;
                        destList = destType.IsArray ? 
                            Array.CreateInstance(destType.GetElementType(), valList.Count) : 
                            destType.Create<IList>();
                        
                        resolver.Resolve(valList, ref destList);
#if !PORTABLE
                        setter(destinationObject, Convert.ChangeType(destList, destType)); 
#else
                        setter(destinationObject, Convert.ChangeType(destList, destType, null));
#endif
                        return destinationObject;
                    };
                }

                return this;
            }

            IMap<TSource, TDestination> IMap<TSource, TDestination>.Auto()
            {
                return Auto();
            }

            IMap<TDestination> IMap<TDestination>.Auto()
            {
                return Auto();
            }

            IMap IMap.Auto()
            {
                return Auto();
            }

            private void CreateMapping(Dictionary<string, object> destinationMap, Dictionary<string, object> sourceMap)
            {
                foreach (var dest in destinationMap)
                {
                    foreach (var source in sourceMap)
                    {                        
                        if (dest.Key.Equals(source.Key) && !dest.Value.Equals(source.Value) ||
                            (source.Value.As<string>().Contains(_config.Separator.ToString()) &&
                             dest.Key.Equals(source.Value.As<string>().Replace(_config.Separator.ToString(), string.Empty))))
                        {
                            Remap(source.Value.As<string>(), dest.Value.As<string>());
                        }
                    }
                }                
            }

            private void CreateComplexMapping<TFirst, TSecond>()
            {                
                var destType = DataProxy.Create<TFirst>();                

                var sType = DataProxy.Create<TSecond>();                

                List<KeyValuePair<string, KeyValuePair<Type, Type>>> pairss = new List<KeyValuePair<string, KeyValuePair<Type, Type>>>();
                foreach (var pair in _destinationCodeTree.Where(o => o.Value.ToString().StartsWith(LoopMethodName)))
                {
                    foreach (var valuePair in _sourceCodeTree.Where(o => o.Value.ToString().StartsWith(LoopMethodName)))
                    {
                        if (pair.Key != valuePair.Key) continue;
                        var name = pair.Key;
                        var destPropType = destType.GetPropertyInfo(name).PropertyType;
                        var destArg = destPropType.IsArray ? destPropType.GetElementType() : destPropType.GetGenericArguments().Single();

                        var sPropType = sType.GetPropertyInfo(name).PropertyType;
                        var sPropArg = sPropType.IsArray ? sPropType.GetElementType() : sPropType.GetGenericArguments().Single();

                        var p = new KeyValuePair<string, KeyValuePair<Type, Type>>(name,
                            new KeyValuePair<Type, Type>(destArg, sPropArg));
                        pairss.Add(p);
                    }
                }


                for (int i = 0; i < pairss.Count; i++ )
                {                    
                    var types = pairss[i].Value;                        
                    var map = (IMap)typeof(Mapper).GetMethod("Create", BindingFlags.Static | BindingFlags.Public)
                        .MakeGeneric(types.Value, types.Key)
                        .Invoke(null, null);
                    map.Auto().Compile();
                    _complexMaps.Add(pairss[i].Key, map);                    
                }                                                 
            }

            private void GenerateCodeTree<T>(Dictionary<string, object> dictionary, bool init)
            {
                var main = DataProxy.Create<T>();
                var builder = new StringBuilder();

                var generalRule = main.Where(o => main.CanGet(o) && main.CanSet(o)).ToArray();
                foreach (var prop in generalRule.Where(o => !main.IsEnumerable(o)))
                {
                    dictionary.Add(prop, prop);

                    builder.Append(prop);
                    var propProxy = DataProxy.Create(main.GetPropertyInfo(prop).PropertyType);
                    if (propProxy.Any())
                    {
                        if (init)
                            builder.Replace(prop, string.Format("{0}{1}{2}", InitMethodName, _config.Separator, prop));
                        BuildTree(propProxy, builder, dictionary, init);
                    }
                    builder.Clear();
                }

                foreach (var prop in generalRule.Where(main.IsEnumerable))
                {                    
                    if (!dictionary.ContainsKey(prop))
                        dictionary.Add(prop, string.Format("{0}({1})", LoopMethodName, prop));
                    else
                        dictionary[prop] = string.Format("{0}({1})", LoopMethodName, prop);
                    
                    builder.Clear();
                }
            }

            private void BuildTree<T>(DataProxy<T> main, StringBuilder builder, Dictionary<string, object> dictionary,
                bool init)
            {
                var generalRule = main.Where(o => main.CanGet(o) && main.CanSet(o)).ToArray();
                foreach (var prop in generalRule.Where(o => !main.IsEnumerable(o)))
                {
                    builder.Append(_config.Separator);
                    builder.Append(prop);
                    dictionary.Add(prop, builder.ToString());
                    var propProxy = DataProxy.Create(main.GetPropertyInfo(prop).PropertyType);

                    if (propProxy.Any())
                    {
                        if (init)
                            builder.Replace(prop, string.Format("{0}{1}{2}", InitMethodName, _config.Separator, prop));
                        BuildTree(propProxy, builder, dictionary, init);
                    }
                }

                foreach (var prop in generalRule.Where(main.IsEnumerable))
                {                    
                    builder.Append(_config.Separator);
                    builder.Append(prop);
                    if (!dictionary.ContainsKey(prop))
                        dictionary.Add(builder.ToString(), string.Format("{0}({1})", LoopMethodName, builder.ToString()));
                    else
                        dictionary[builder.ToString()] = string.Format("{0}({1})", LoopMethodName, builder.ToString());                    
                }
            }

            #endregion

            #region Do            

            /// <summary>
            /// Executes mapping from target to destination object      
            /// </summary>            
            /// <param name="sourceObject"></param>            
            /// <returns></returns>
            public TDestination Do(TSource sourceObject)
            {
                if (_compiledMap == null)
                    return _config.IgnoreDefaultValues
                        ? PerformMappingIgnoreDefaults(sourceObject, new TDestination())
                        : PerformMapping(sourceObject, new TDestination());
                return _compiledMap(sourceObject, default(TDestination));
            }

            public TDestination Do(TSource sourceObject, TDestination dest)
            {
                return _config.IgnoreDefaultValues
                    ? PerformMappingIgnoreDefaults(sourceObject, dest)
                    : PerformMapping(sourceObject, dest);
            }

            public TD Do<TS, TD>(TS obj, TD dest)
            {
                return Do(obj.As<TSource>(), dest.As<TDestination>()).As<TD>();
            }

            public TDestination Do(object obj, TDestination dest)
            {
                return Do(obj.As<TSource>());
            }

            public object Do(object source, object destination)
            {
                return _config.IgnoreDefaultValues
                    ? PerformAnonymousMappingIgnoreDefaults(source, destination)
                    : PerformAnonymousMapping(source, destination);
            }

            public object Do(object source)
            {
                if(_compiledMap == null)
                    return _config.IgnoreDefaultValues
                        ? PerformAnonymousMappingIgnoreDefaults(source)
                        : PerformAnonymousMapping(source);
                return _compiledMap((TSource)source, new TDestination());
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
                var s = typeof (TSource);
                var t = typeof (TDestination);
                var args = new ArgArray(s, t);
                if (Mappers.ContainsKey(args))
                {
                    return Mappers[args];
                }

                var mapper = new MapperInnerClass<TSource, TDestination>();

                if (!Mapper.Maps.ContainsKey(args))
                {
                    var destination = new DataProxy<TDestination>();
                    var source = new DataProxy<TSource>();
                    mapper._map = new PropertyMap<TSource, TDestination>
                    {
                        Destination = destination,
                        Source = source,
                        AdditionalMaps =
                            new List
                                <
                                    EditableKeyValuePair
                                        <MapObject<Func<TSource, object>>, MapObject<Action<TDestination, object>>>>()
                    };

                    Mapper.Maps.Add(args, mapper._map.Apply(o => o.Calculate()));
                }
                else mapper._map = (PropertyMap<TSource, TDestination>) Mapper.Maps[args];

                Mappers.Add(args, mapper);
                CreatedMappers.Add(mapper);

                return mapper;
            }           

            private TDestination PerformMapping(TSource sourceObject,
                TDestination destinationObject = default(TDestination))
            {
                if (destinationObject == null || destinationObject.Equals(default(TDestination)))
                    destinationObject = new TDestination();

                var destination = _map.Destination;
                var source = _map.Source;

                var nonRemapedDests = _map.DestinationNonReMapedProperties;
                for (int index = 0; index < nonRemapedDests.Length; index++)
                {
                    var o = nonRemapedDests[index];
                    destination[destinationObject, o] = source[sourceObject, o];
                }

                var additionalMaps = _map.AdditionalMaps;
                for (int index = 0; index < additionalMaps.Count; index++)
                {
                    var map = additionalMaps[index];

                    var sourceValue = map.Key.Invoker(sourceObject);
                    map.Value.Invoker(destinationObject, sourceValue);
                }

                if (_compiledMap == null) return destinationObject;

                foreach (var complexMap in _complexMaps)
                {
                    var val = source[sourceObject, complexMap.Key];
                    if(val == null) continue;

                    var valType = val.GetType();

                    var destList = destination.GetPropertyInfo(complexMap.Key).PropertyType.Create<IList>();

                    Resolver.Create(typeof(ArrayResolver<,>), valType, destList.GetType(), complexMap.Value)
                        .Resolve((Array)val, ref destList);
                
                    destination[destinationObject, complexMap.Key] = destList;
                }

                return destinationObject;
            }

            private object PerformAnonymousMapping(object sourceObject,
                object destinationObject = default(object))
            {
                if (destinationObject == null || destinationObject.Equals(default(TDestination)))
                    destinationObject = Activator.CreateInstance<TDestination>();

                var destination = _map.Destination;
                var source = _map.Source;

                var nonRemapedDests = _map.DestinationNonReMapedProperties;
                for (int index = 0; index < nonRemapedDests.Length; index++)
                {
                    var o = nonRemapedDests[index];
                    destination[destinationObject, o] = source[sourceObject, o];
                }

                var additionalMaps = _map.AdditionalMaps;
                for (int index = 0; index < additionalMaps.Count; index++)
                {
                    var map = additionalMaps[index];

                    var sourceValue = map.Key.Invoker((TSource)sourceObject);
                    map.Value.Invoker((TDestination)destinationObject, sourceValue);
                }

                return destinationObject;
            }

            private TDestination PerformMappingIgnoreDefaults(TSource sourceObject,
                TDestination destinationObject = default (TDestination))
            {
                if (destinationObject == null || destinationObject.Equals(default(TDestination)))
                    destinationObject = Activator.CreateInstance<TDestination>();

                var destination = _map.Destination;
                var source = _map.Source;

                var nonRemapedDests = _map.DestinationNonReMapedProperties;
                for (int index = 0; index < nonRemapedDests.Length; index++)
                {
                    var o = nonRemapedDests[index];

                    var value = source[sourceObject, o];
                    if (value != null && !value.Equals(default(TDestination)))
                    {
                        destination[destinationObject, o] = value;
                    }
                }

                var additionalMaps = _map.AdditionalMaps;
                for (int index = 0; index < additionalMaps.Count; index++)
                {
                    var map = additionalMaps[index];

                    var sourceValue = map.Key.Invoker(sourceObject);
                    if (sourceValue != null && !sourceValue.Equals(sourceValue.GetType().GetDefault()))
                    {
                        map.Value.Invoker(destinationObject, sourceValue);
                    }
                }

                return destinationObject;
            }

            private object PerformAnonymousMappingIgnoreDefaults(object sourceObject,
                object destinationObject = default (object))
            {
                if (destinationObject == null || destinationObject.Equals(default(TDestination)))
                    destinationObject = Activator.CreateInstance<TDestination>();

                var destination = _map.Destination;
                var source = _map.Source;

                var nonRemapedDests = _map.DestinationNonReMapedProperties;
                for (int index = 0; index < nonRemapedDests.Length; index++)
                {
                    var o = nonRemapedDests[index];

                    var value = source[sourceObject, o];
                    if (value != null && !value.Equals(default(TDestination)))
                    {
                        destination[destinationObject, o] = value;
                    }
                }

                var additionalMaps = _map.AdditionalMaps;
                for (int index = 0; index < additionalMaps.Count; index++)
                {
                    var map = additionalMaps[index];

                    var sourceValue = map.Key.Invoker((TSource)sourceObject);
                    if (sourceValue != null && !sourceValue.Equals(sourceValue.GetType().GetDefault()))
                    {
                        map.Value.Invoker((TDestination)destinationObject, sourceValue);
                    }
                }

                return destinationObject;
            }

            #region IPathProvider

            public string GetSourcePath<T, R>(Expression<Func<T, R>> destination)
            {
                var body = destination.Body as MemberExpression;
                if(body == null) throw new MissingMemberException();

                var path = Regex.Match(body.ToString(), @"^.+\.((.+\.)+(.+))").Groups[1].Value.Replace('.', _config.Separator);

                return GetSourcePath(path);
            }

            public string GetSourcePath(string destination)
            {
                try
                {
                    return _map.DestinationNonReMapedProperties.SingleOrDefault(o => o.Equals(destination)) ??
                           _map.AdditionalMaps.Single(o => o.Value.Path.Equals(destination)).Key.Path;
                }
                catch (InvalidOperationException)
                {
                    throw new AmbiguousMatchException("More then one result found.");
                }
            }

            public string GetDestinationPath<T, R>(Expression<Func<T, R>> source)
            {
                var body = source.Body as MemberExpression;
                if (body == null) throw new MissingMemberException();

                var path = Regex.Match(body.ToString(), @"^.+\.((.+\.)+(.+))").Groups[1].Value.Replace('.', _config.Separator);

                return GetDestinationPath(path);
            }

            public string GetDestinationPath(string source)
            {
                try
                {
                    return _map.SourceNonReMapedProperties.SingleOrDefault(o => o.Equals(source)) ??
                           _map.AdditionalMaps.Single(o => o.Key.Path.Equals(source)).Value.Path;
                }
                catch (InvalidOperationException)
                {
                    throw new AmbiguousMatchException("More then one result found.");
                }
            }

            #endregion


            #region IMap<,>

            /// <summary>        
            /// Generates mapping proxy object, that allows to perform real-time mapping using parent gettes and setters.
            /// </summary>
            /// <param name="obj"></param>
            /// <returns></returns>
            public MappingObject<TSource, TDestination> GenerateProxy(TSource obj)
            {
                var sourceType = obj.GetType();
                var getter = typeof (Func<,>);
                var setter = typeof (Action<,>);

                var metadatas = new List<FieldMetadata>();

                var additionalMaps = _map.AdditionalMaps;

                var nonRemapedDests = _map.DestinationNonReMapedProperties;
                for (int index = 0; index < nonRemapedDests.Length; index++)
                {
                    var o = nonRemapedDests[index];

                    var destType = DetermineResultType(obj, new[] {o});
                    var destProp = DetermineResultProperty(obj, new[] {o});
                    metadatas.Add(new FieldMetadata
                    {
                        FieldName = o,
                        FieldType = destType,
                        MappedPropertyGetter =
                            Delegate.CreateDelegate(getter.MakeGenericType(sourceType, destType), destProp[0]),
                        MappedPropertySetter =
                            Delegate.CreateDelegate(setter.MakeGenericType(sourceType, destType), destProp[1]),
                        DeclareType = sourceType,
                        Object = obj
                    });
                }

                for (int index = 0; index < additionalMaps.Count; index++)
                {
                    var map = additionalMaps[index];

                    var o = map.Key.LastInvokeTarget.DynamicInvoke(obj);
                    var destinationPath = map.Key.Path.Split(_config.Separator);
                    var destType = DetermineResultType(obj, destinationPath);
                    var destProp = DetermineResultProperty(obj, destinationPath);
                    var sType = destProp[0].DeclaringType;

                    metadatas.Add(new FieldMetadata
                    {
                        FieldName = destinationPath.Last(),
                        FieldType = destType,
                        MappedPropertyGetter =
                            Delegate.CreateDelegate(getter.MakeGenericType(sType, destType), destProp[0]),
                        MappedPropertySetter =
                            Delegate.CreateDelegate(setter.MakeGenericType(sType, destType), destProp[1]),
                        DeclareType = sType,
                        Object = o
                    });
                }

                return new MappingObject<TSource, TDestination>(metadatas) {UnderlyingObject = obj};
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
                return Do((TSource) obj);
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
                return GenerateProxy((TSource) obj);
            }

            public IMap Remap<TR>(string source, string destination)
            {
                return RegisterAdditionalMap<TR>(source, destination);
            }

            public IMap Remap(string source, string destination)
            {
                this.GetType().GetMethod("RegisterAdditionalMap", BindingFlags.NonPublic | BindingFlags.Instance)
                    .MakeGeneric(DetermineResultType(DestinationDefault, destination.Split(_config.Separator)))
                    .Invoke(this, new object[] {source, destination});

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

            private static Func<T, TR> GetSourceInvokeChain<T, TR>(T obj, string[] paths, out Delegate @out)
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
                        .MakeGeneric(typeof (T), propertyType)
                        .Invoke(null, new object[] {xc.Methods[firstPath].Delegate});
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
                    if (last == s) @out = @delegate as Delegate;
                    Type target;
                    var p = DataProxy.Create(propertyType);
                    if (p.ContainsProperty(s)) target = p.GetPropertyInfo(s).PropertyType;
                    else if (p.ContainsMethod(s)) target = p.Methods[s].Info.ReturnType;
                    else if(GlobalMethods.Value.ContainsKey(s)) target = GlobalMethods.Value[s].Info.ReturnType;
                    else throw new InvalidOperationException(string.Format("Cannot find entry '{0}' for current operation.", firstPath));

                    var pair =
                        (KeyValuePair<Type, Delegate>) GetterCreator.MakeGenericMethod(typeof (T), propertyType, target)
                            .Invoke(null,
                                new object[] {s, new KeyValuePair<Type, Delegate>(propertyType, @delegate as Delegate)});
                    @delegate = pair.Value;
                    propertyType = pair.Key;
                }

                return (Func<T, TR>) @delegate;
            }

            private Action<T, TR> GetDestinationInvokeChain<T, TR>(string path, T obj, out Delegate @out)
            {
                @out = null;
                var paths = path.Split(_config.Separator).Select(x => x.Trim()).ToList();
                var propertyName = paths.Last();                

                Type propertyType = null;
                var xc = DataProxy.Create<T>();

                var loop = Regex.IsMatch(propertyName, string.Format(@"{0}\((.+)\)", LoopMethodName));

                if(!loop) paths.Remove(propertyName);

                if (!paths.Any())
                {
                    var setter = DataProxy.Create(obj).GetSetter<TR>(propertyName);
                    @out = setter;
                    return setter;
                }

                int skip = 1;
                object f = null;

                var t = typeof (T);
                object fe = null;
                var firstPath = paths.First();
                if (loop)
                {
                    skip++;
                    f = LoopCreator.MakeGenericMethod(t, t)
                        .Invoke(null, new object[] { f, paths[0], _complexMaps, obj });
                }
                else                
                {
                    if (firstPath.Equals(InitMethodName))
                    {
                        skip++;
                        f = InitCreator.MakeGenericMethod(t, t)
                            .Invoke(null, new object[] {f, paths[1]});
                    }

                    firstPath = paths.First(y => !y.Equals(InitMethodName));

                    
                    if (xc.ContainsProperty(firstPath))
                    {
                        propertyType = xc.GetPropertyInfo(firstPath).PropertyType;
                        fe = xc.GetReflectedGetter(firstPath, propertyType);
                    }
                    else if (xc.ContainsMethod(firstPath))
                    {
                        propertyType = xc.Methods[firstPath].Info.ReturnType;
                        fe = FuncConverter
                            .MakeGeneric(typeof (T), propertyType)
                            .Invoke(null, new object[] {xc.Methods[firstPath].Delegate});
                    }
                    else if (GlobalMethods.Value.ContainsKey(firstPath))
                    {
                        var method = GlobalMethods.Value[firstPath];
                        fe = method.Delegate;
                        propertyType = method.Info.ReturnType;
                    }
                    else
                    {
                        throw new InvalidOperationException(
                            string.Format("Cannot find entry '{0}' for current operation.", firstPath));
                    }

                    if (f != null)
                    {
                        f = typeof(Mapper).GetMethod("__compose", BindingFlags.NonPublic | BindingFlags.Static)
                            .MakeGeneric(t, t, propertyType)
                            .Invoke(null, new[] { f, fe });
                    }
                    else
                    {
                        f = fe;
                    }
                }                

                int index = skip;
                KeyValuePair<Type, Delegate> ff = new KeyValuePair<Type, Delegate>(propertyType, (Delegate) f);
                foreach (var s in paths.Skip(skip))
                {
                    if (s.Equals(InitMethodName))
                    {
                        string prop = paths[index];

                        f = InitCreator.MakeGenericMethod(typeof (T), propertyType, propertyType)
                            .Invoke(null, new object[] {f, prop});

                        continue;
                    }

                    Type target;
                    var p = DataProxy.Create(propertyType);
                    if (p.ContainsProperty(s)) target = p.GetPropertyInfo(s).PropertyType;
                    else if (p.ContainsMethod(s)) target = p.Methods[s].Info.ReturnType;
                    else target = GlobalMethods.Value[s].Info.ReturnType;

                    ff = (KeyValuePair<Type, Delegate>) GetterCreator.MakeGenericMethod(t, propertyType, target)
                        .Invoke(null, new object[] {s, new KeyValuePair<Type, Delegate>(propertyType, (Delegate) f)});
                    f = ff.Value;
                    propertyType = ff.Key;

                    index++;
                }

                var type = typeof (DataProxy<>);
                Type[] typeArgs = {ff.Key};
                object o = Activator.CreateInstance(type.MakeGenericType(typeArgs));
                var m = (Delegate) SetterCreator.MakeGeneric(ff.Key, typeof (TR))
                    .Invoke(null, new object[] {o, propertyName});
                var minfo = m.Method;

                var a = ActionConverter
                    .MakeGeneric(propertyType, minfo.GetParameters()[0].ParameterType, TypeOfObject, typeof (TR))
                    .Invoke(null, new object[] {m});
                @out = (Delegate) a;
                return ((Func<T, object>) f).Compose((Action<object, TR>) a);
            }

            private IMap RegisterAdditionalMap<TR>(string source, string destination)
            {
                lock (this)
                {
                    if (!_map.AdditionalMaps.Any(o => o.Value.Path.Equals(destination)))
                    {
                        Delegate tempSource = null;
                        _map.AdditionalMaps.Add(new EditableKeyValuePair
                            <MapObject<Func<TSource, object>>, MapObject<Action<TDestination, object>>>
                            (new MapObject<Func<TSource, object>>
                            {
                                Path = source,
                                Invoker = source == null ? null :
                                    GetSourceInvokeChain<TSource, TR>(SourceDefault,
                                        source.Split(_config.Separator).Select(x => x.Trim()).ToArray(), out tempSource)
                                        .Convert<TSource, TR, TSource, object>(),
                                LastInvokeTarget = source == null ? null : tempSource
                            },
                                new MapObject<Action<TDestination, object>>
                                {
                                    Path = destination,
                                    Invoker =
                                        GetDestinationInvokeChain<TDestination, TR>(destination, DestinationDefault,
                                            out tempSource).Convert<TDestination, TR, TDestination, Object>(),
                                    LastInvokeTarget = tempSource
                                }
                            ));
                        _map.Calculate();
                    }
                    else _map.AdditionalMaps.Single(o => o.Value.Path.Equals(destination)).Key.Path = source;
                }
                return this;
            }

            private MethodInfo[] DetermineResultProperty<T>(T objType, string[] paths)
            {
                var proxy = DataProxy.Create((object) objType);
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
                    return new[] {info.GetGetMethod(), info.GetSetMethod()};
                }
                else if (proxy.ContainsMethod(lastPath))
                {
                    var info = proxy.Methods[lastPath].Info;
                    return new[] {info, info};
                }
                else if (GlobalMethods.Value.ContainsKey(lastPath))
                {
                    var info = GlobalMethods.Value[lastPath].Info;
                    return new[] { info, info };
                }
                else
                {
                    throw new InvalidOperationException(string.Format("Cannot find entry '{0}' for current operation.", lastPath));
                }
            }

            private Type DetermineResultType<T>(T objType, string[] paths)
            {
                Type target = null;
                var proxy = DataProxy.Create((object) objType);
                string lastPath = paths.Last();

                for (int i = 0; i < paths.Length; i++)
                {
                    var s = paths[i];

                    if(s.Equals(InitMethodName)) continue;
                    if (s.StartsWith(LoopMethodName))
                    {
                        s = Regex.Match(s, LoopMethodName + ".{1}(.+).{1}").Groups[1].Value;
                        lastPath = s;
                    }

                    if (proxy.ContainsProperty(s)) target = proxy.GetPropertyInfo(s).PropertyType;
                    else if (proxy.ContainsMethod(s)) target = proxy.Methods[s].Info.ReturnType;
                    else if (GlobalMethods.Value.ContainsKey(s)) target = GlobalMethods.Value[s].Info.ReturnType;                    
                    else throw new InvalidOperationException(string.Format("Cannot find entry '{0}' for current operation.", s));

                    if (s != lastPath)
                        proxy = DataProxy.Create(target);
                }                

                return target;
            }

            #endregion

            void IDisposable.Dispose()
            {                
                Mappers.Clear();
            }
        }

        #endregion

        #region Helpers

        private static KeyValuePair<Type, Delegate> /*Func<TF, TR>*/ GetterBuilder<TF, T, TR>(string firstPath,
            KeyValuePair<Type, Delegate> func /*Func<TF, T> func*/)
        {            
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
                tempValue = method.Delegate as Func<T, TR>;
            }
            else
            {
                throw new InvalidOperationException(string.Format("Cannot find entry '{0}' for current operation.", firstPath));
            }

            if (tempValue != null)
                return new KeyValuePair<Type, Delegate>(tempValue.Method.ReturnType,
                    (func.Value as Func<TF, T>).Compose(tempValue));

            throw new InvalidOperationException("Initialization failed");
        }

        private static Delegate InitBuilder<TF, T>(Delegate func, string prop)
        {
            var proxy = DataProxy.Create<T>();
            var pType = proxy.GetPropertyInfo(prop).PropertyType;
            var setter = (Action<T, object>)proxy.GetReflectedConvertedSetter(prop, pType);

            Func<T, T> initFunc = targetObj =>
            {
                setter(targetObj, Activator.CreateInstance(pType));

                return targetObj;                
            };

            if (func == null) return initFunc;

            return (func as Func<TF, T>).Compose(initFunc);
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
            return arg => (f as Func<T, TR>)(arg);
        }

        private static Func<T, TRNew> __compose<T, TR, TRNew>(Func<T, TR> f1, Func<TR, TRNew> f2)
        {
            return f1.Compose(f2);
        }

        private static Func<T, TR> _convertFunc<T, TR>(Func<T, object> f)
        {
            return arg => (TR) f(arg);
        }

        #endregion
    }
}