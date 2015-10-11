using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
#if !NET35
using System.Diagnostics.Contracts;
#endif
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using AOMapper.Data;
using AOMapper.Data.Keys;
using AOMapper.Extensions;
using AOMapper.Exceptions;
#if NET35
using AOMapper.Helpers;
#endif
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
            GlobalMethods = new Lazy<Dictionary<StringKey, MethodProperty>>();
            Maps = new Dictionary<ArgArray, object>();
            ActionConverter = mapperType.GetMethod("_convertAction", BindingFlags.NonPublic | BindingFlags.Static);
            FuncConverter = mapperType.GetMethod("_convertFunc", BindingFlags.NonPublic | BindingFlags.Static);           
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

        /// <summary>
        /// Clears information about all created maps
        /// </summary>
        public static void Clear()
        {
            foreach (var map in CreatedMappers) 
                map.As<IDisposable>().Dispose(); 
            CreatedMappers.Clear();
            GlobalMethods.Value.Clear();            
            Maps.Clear();
            Resolver.Clear();
        }

        #region Fields

        private static readonly MethodInfo ActionConverter;

        private static readonly MethodInfo FuncConverter;        

        private static readonly MethodInfo GetterCreator;

        private static readonly MethodInfo InitCreator;

        private static readonly MethodInfo LoopCreator;

        private static readonly Lazy<Dictionary<StringKey, MethodProperty>> GlobalMethods;

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
            private readonly IMap _map;

            internal Config(IMap map)
            {
                _map = map;
                IgnoreDefaultValues = false;
                Separator = '/';
                InitialyzeNullValues = true;
                CompileInnerMaps = true;
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
            /// <para>Default: <c>True</c></para>
            /// </summary>
            /// <value>
            /// <c>true</c> if null values should be initialized during mapping; otherwise, <c>false</c>.
            /// </value>
            public bool InitialyzeNullValues { get; set; }

            /// <summary>
            /// <para>Gets or sets a value indicating whether inner maps that are generated automatically during automatic mapping should be compiled.</para>
            /// <para>Default: <c>True</c></para>
            /// </summary>
            /// <value>
            ///   <c>true</c> if inner maps should be compiled; otherwise, <c>false</c>.
            /// </value>
            public bool CompileInnerMaps { get; set; }

            /// <summary>
            /// Registers the resolver.
            /// </summary>
            /// <typeparam name="TS">The type of the s.</typeparam>
            /// <typeparam name="TD">The type of the d.</typeparam>
            /// <param name="resolver">The resolver.</param>
            public void RegisterResolver<TS, TD>(Func<TS, TD> resolver)
            {
                Resolver.RegisterResolver(_map, (Resolver<TS, TD>)resolver);
            }

            /// <summary>
            /// Registers the resolver.
            /// </summary>
            /// <param name="resolver">The resolver.</param>
            public void RegisterResolver(Resolver resolver)
            {
                Resolver.RegisterResolver(_map, resolver);
            }
        }

        #endregion

        #region Mapper

        protected class MapperInnerClass<TSource, TDestination> : 
            Mapper, IMap<TSource, TDestination>, IPathProvider, IDisposable 
            where TDestination : new()
        {
            #region ctor's

            internal MapperInnerClass()
            {
                _config = new Config(this);
            }

            #endregion

            #region General overloads

            protected bool Equals(MapperInnerClass<TSource, TDestination> other)
            {
                return Equals(_map, other._map);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((MapperInnerClass<TSource, TDestination>) obj);
            }

            public override int GetHashCode()
            {
                return (_map != null ? _map.GetHashCode() : 0);
            }

            ~MapperInnerClass()
            {
                Resolver.Clear(this);
            }

            void IDisposable.Dispose()
            {
                Mappers.Remove(new ArgArray(_map.Source, _map.Destination));
                Resolver.Clear(this);
            }

            #endregion

            #region Fields

            private static readonly TDestination DestinationDefault = Activator.CreateInstance<TDestination>();

            private static readonly Dictionary<ArgArray, MapperInnerClass<TSource, TDestination>> Mappers =
                new Dictionary<ArgArray, MapperInnerClass<TSource, TDestination>>();

            private static readonly TSource SourceDefault = Activator.CreateInstance<TSource>();
            private readonly Config _config;
            private PropertyMap<TSource, TDestination> _map;

            private Dictionary<StringKey, CodeTreeNode> _destinationCodeTree;
            private Dictionary<StringKey, CodeTreeNode> _sourceCodeTree;

            private Dictionary<StringKey, IMap> _complexMaps;

            private Func<TSource, TDestination, TDestination> _compiledMap; 

            #endregion

            #region auto-mapping            

            private MapperInnerClass<TSource, TDestination> Auto()
            {
                
                _destinationCodeTree = new Dictionary<StringKey, CodeTreeNode>();
                GenerateCodeTree<TDestination>(_destinationCodeTree, _config.InitialyzeNullValues);
                _sourceCodeTree = new Dictionary<StringKey, CodeTreeNode>();
                GenerateCodeTree<TSource>(_sourceCodeTree, false);                

                if (_destinationCodeTree.Values.Any(o => o.Value.StartsWith(LoopMethodName)) &&
                    _sourceCodeTree.Values.Any(o => o.Value.StartsWith(LoopMethodName)))
                {
                    _complexMaps = new Dictionary<StringKey, IMap>();
                    CreateComplexMapping<TDestination, TSource>();
                }

                CreateMapping(_destinationCodeTree, _sourceCodeTree);

                return this;
            }

            /// <summary>
            /// Tries to automatically configure the map using name comparation
            /// </summary>
            /// <returns></returns>
            IMap<TSource, TDestination> IMap<TSource, TDestination>.Auto()
            {
                return Auto();
            }

            /// <summary>
            /// Tries to automatically configure the map using name comparation
            /// </summary>
            /// <returns></returns>
            IMap<TDestination> IMap<TDestination>.Auto()
            {
                return Auto();
            }

            /// <summary>
            /// Tries to automatically configure the map using name comparation
            /// </summary>
            /// <returns></returns>
            IMap IMap.Auto()
            {
                return Auto();
            }

            private void CreateMapping(Dictionary<StringKey, CodeTreeNode> destinationMap, Dictionary<StringKey, CodeTreeNode> sourceMap)
            {
                foreach (var dest in destinationMap)
                {
                    foreach (var source in sourceMap)
                    {
                        try
                        {
                            if (dest.Key.Equals(source.Key) && !dest.Value.Equals(source.Value) ||
                                (source.Value.Value.Contains(_config.Separator.ToString()) &&
                                 dest.Key.Equals(source.Value.Value.Replace(_config.Separator.ToString(), string.Empty))))
                            {
                                Remap(GetPropertyNameFromPath(source.Value.Value),
                                    GetPropertyNameFromPath(dest.Value.Value));
                            }
                            else if (dest.Key.Equals(source.Key) && dest.Value.Type != null &&
                                     !dest.Value.Type.Equals(source.Value.Type))
                            {
                                if (
                                    !(typeof (IList).IsAssignableFrom(dest.Value.Type.Value) &&
                                      typeof (IList).IsAssignableFrom(source.Value.Type.Value)))
                                    Remap(GetPropertyNameFromPath(source.Value.Value),
                                        GetPropertyNameFromPath(dest.Value.Value),
                                        Resolver.Create(source.Value.Type, dest.Value.Type, this));
                            }
                        }
                        catch (InvalidTypeBindingException e)
                        {
                            throw;
                        }
                        catch (Exception e)
                        {
                            // ignoring - auto-mapping failed
                        }
                    }
                }                
            }

            private static string GetPropertyNameFromPath(string value)
            {
                Match match = null;
                return (match = Regex.Match(value, @".*\((.+)\)")).Success ? match.Groups[1].Value : value;
            }

            private void CreateComplexMapping<TFirst, TSecond>()
            {                
                var destType = DataProxy.Create<TFirst>();                

                var sType = DataProxy.Create<TSecond>();                

                List<KeyValuePair<string, KeyValuePair<Type, Type>>> pairss = new List<KeyValuePair<string, KeyValuePair<Type, Type>>>();
                foreach (var pair in _destinationCodeTree.Where(o => o.Value.Value.StartsWith(LoopMethodName)))
                {
                    foreach (var valuePair in _sourceCodeTree.Where(o => o.Value.Value.StartsWith(LoopMethodName)))
                    {
                        var key = new KeyValuePair<TypeKey, TypeKey>(valuePair.Value.Type, pair.Value.Type);
                        var resolvers = Resolver.GetResolvers(this);
                        if (resolvers.ContainsKey(key))
                        {
                            pair.Value.Resolver = valuePair.Value.Resolver = resolvers[key];
                            continue;
                        }

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
                    map = map.Auto();//.Compile();
                    if (_config.CompileInnerMaps) map = map.Compile();
                    _complexMaps.Add(pairss[i].Key, map);                    
                }                                                 
            }

            private void GenerateCodeTree<T>(Dictionary<StringKey, CodeTreeNode> dictionary, bool init)
            {
                var main = DataProxy.Create<T>();
                var builder = new StringBuilder();

                var generalRule = main.Where(o => main.CanGet(o) && main.CanSet(o)).ToArray();
                foreach (var prop in generalRule.Where(o => !main.IsEnumerable(o)))
                {
                    dictionary.Add(prop, new CodeTreeNode(prop, main.GetPropertyInfo(prop).PropertyType));

                    builder.Append(prop);
                    var propProxy = DataProxy.Create(main.GetPropertyInfo(prop).PropertyType);
                    if (propProxy.Any())
                    {
                        if (init)
                            builder.Replace(prop, string.Format("{0}{1}{2}", InitMethodName, _config.Separator, prop));
                        BuildTree(propProxy, builder, dictionary, init);
                    }
#if NET35
                    builder = new StringBuilder();
#else
                    builder.Clear();
#endif
                }

                foreach (var prop in generalRule.Where(main.IsEnumerable))
                {                    
                    if (!dictionary.ContainsKey(prop))
                        dictionary.Add(prop, new CodeTreeNode(string.Format("{0}({1})", LoopMethodName, prop), main.GetPropertyInfo(prop).PropertyType));
                    else
                        dictionary[prop] = new CodeTreeNode(string.Format("{0}({1})", LoopMethodName, prop), main.GetPropertyInfo(prop).PropertyType);

#if NET35
                    builder = new StringBuilder();
#else
                    builder.Clear();
#endif
                }
            }

            private void BuildTree<T>(DataProxy<T> main, StringBuilder builder, Dictionary<StringKey, CodeTreeNode> dictionary,
                bool init)
            {
                var generalRule = main.Where(o => main.CanGet(o) && main.CanSet(o)).ToArray();
                foreach (var prop in generalRule.Where(o => !main.IsEnumerable(o)))
                {
                    builder.Append(_config.Separator);
                    builder.Append(prop);
                    dictionary.Add(prop, new CodeTreeNode(builder.ToString(), main.GetPropertyInfo(prop).PropertyType));
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
                        dictionary.Add(builder.ToString(), new CodeTreeNode(string.Format("{0}({1})", LoopMethodName, builder.ToString()), main.GetPropertyInfo(prop).PropertyType));
                    else
                        dictionary[builder.ToString()] = new CodeTreeNode(string.Format("{0}({1})", LoopMethodName, builder.ToString()), main.GetPropertyInfo(prop).PropertyType);                    
                }
            }

            #endregion

            #region Compile

            /// <summary>
            /// Compiles the map
            /// </summary>
            /// <returns></returns>
            IMap IMap.Compile()
            {
                return _compile();
            }

            /// <summary>
            /// Compiles the map
            /// </summary>
            /// <returns></returns>
            IMap<TDestination> IMap<TDestination>.Compile()
            {
                return _compile() as IMap<TDestination>;
            }

            /// <summary>
            /// Compiles the map
            /// </summary>
            /// <returns></returns>
            IMap<TSource, TDestination> IMap<TSource, TDestination>.Compile()
            {
                return _compile() as IMap<TSource, TDestination>;
            }

            private IMap _compile()
            {
                _config.CompileInnerMaps = true;
                return _config.IgnoreDefaultValues ? CompileNoIgnore() : CompileWithIgnore();
            }

            private IMap CompileWithIgnore()
            {
#if !NET35
                Contract.Ensures(Contract.Result<IMap>() != null);
#endif
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

                var mm = _compiledMap;
                var nonRemapedDests = _map.DestinationNonReMapedProperties;
                _compiledMap = (sourceObject, destinationObject) =>
                {
                    Debug.Assert(mm != null, "m != null");
                    destinationObject = mm(sourceObject, destinationObject);

                    for (int index = 0; index < nonRemapedDests.Length; index++)
                    {
                        var o = nonRemapedDests[index];
                        var value = source[sourceObject, o];
                        if (value != null && value != value.GetDefault()) destination[destinationObject, o] = value;
                    }

                    return destinationObject;
                };

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
                        if (sourceValue != null && sourceValue != sourceValue.GetDefault()) valueInvoker(dObject, sourceValue);
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

                    var resolver = Resolver.Create(valType, destType, map.Value, typeof(ArrayResolver<,>));

                    var o = map.Key;
                    var setter =
                            destination.GetReflectedConvertedSetter(o, destination.GetPropertyInfo(o).PropertyType) as Action<TDestination, object>;
                    var getter =
                            source.GetReflectedConvertedGetter(o, source.GetPropertyInfo(o).PropertyType) as Func<TSource, object>;

                    _compiledMap = (sourceObject, destinationObject) =>
                    {
                        destinationObject = m(sourceObject, destinationObject);

                        var val = getter(sObject);
                        if (val == null) return destinationObject;

                        var valList = val as IList;
                        IList destList = null;
                        destList = destType.IsArray
                            ? Array.CreateInstance(destType.GetElementType(), valList.Count)
                            : destType.Create<IList>();

                        var destination1 = destList as object;
                        resolver.Resolve(valList, ref destination1);
#if !PORTABLE
                        setter(destinationObject, destList);
#else
                        setter(destinationObject, destList);
#endif
                        return destinationObject;
                    };
                }

                return this;
            }

            private IMap CompileNoIgnore()
            {
#if !NET35
                Contract.Ensures(Contract.Result<IMap>() != null);
#endif
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

                var mm = _compiledMap;
                var nonRemapedDests = _map.DestinationNonReMapedProperties;
                _compiledMap = (sourceObject, destinationObject) =>
                {
                    destinationObject = mm(sourceObject, destinationObject);

                    for (int index = 0; index < nonRemapedDests.Length; index++)
                    {
                        var o = nonRemapedDests[index];
                        destination[destinationObject, o] = source[sourceObject, o];
                    }

                    return destinationObject;
                };

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

                var type = typeof(ArrayResolver<,>);
                foreach (var complexMap in _complexMaps)
                {
                    var map = complexMap;
                    var m = _compiledMap;

                    var valType = source.GetPropertyInfo(complexMap.Key).PropertyType;
                    var destType = destination.GetPropertyInfo(map.Key).PropertyType;

                    var resolver = Resolver.Create(valType, destType, map.Value, type);

                    var o = map.Key;
                    var setter =
                            destination.GetReflectedConvertedSetter(o, destination.GetPropertyInfo(o).PropertyType) as Action<TDestination, object>;
                    var getter =
                            source.GetReflectedConvertedGetter(o, source.GetPropertyInfo(o).PropertyType) as Func<TSource, object>;

                    _compiledMap = (sourceObject, destinationObject) =>
                    {
                        destinationObject = m(sourceObject, destinationObject);

                        var val = getter(sObject);
                        if (val == null) return destinationObject;

                        var valList = val as IList;
                        IList destList = null;
                        destList = destType.IsArray
                            ? Array.CreateInstance(destType.GetElementType(), valList.Count)
                            : destType.Create<IList>();

                        var destination1 = destList as object;
                        resolver.Resolve(valList, ref destination1);
#if !PORTABLE
                        setter(destinationObject, destList);
#else
                        setter(destinationObject, destList);
#endif
                        return destinationObject;
                    };
                }

                return this;
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
                if (_compiledMap == null)
                    return _config.IgnoreDefaultValues
                        ? PerformMappingIgnoreDefaults(sourceObject, dest)
                        : PerformMapping(sourceObject, dest);
                return _compiledMap(sourceObject, dest);
            }

            public TD Do<TS, TD>(TS obj, TD dest)
            {
                return (TD)(object)Do((TSource)(object)obj, (TDestination)(object)dest);
            }

            public TDestination Do(object obj, TDestination dest)
            {
                return Do((TSource)(object)obj);
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
                            new List<Map<MapObject<Func<TSource, object>>, MapObject<Action<TDestination, object>>>>()
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

                if (_complexMaps == null) return destinationObject;

                var resolver = typeof(ArrayResolver<,>);
                foreach (var complexMap in _complexMaps)
                {
                    var val = source[sourceObject, complexMap.Key] as IList;
                    if(val == null) continue;

                    var valType = val.GetType();

                    var destList = destination.GetPropertyInfo(complexMap.Key).PropertyType.Create<IList>(val.Count);

                    var o = destList as object;
                    Resolver.Create(valType, destList.GetType(), complexMap.Value, resolver)
                        .Resolve(val, ref o);
                
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

                if (_complexMaps == null) return destinationObject;

                var resolver = typeof(ArrayResolver<,>);
                foreach (var complexMap in _complexMaps)
                {
                    var val = source[sourceObject, complexMap.Key];
                    if (val == null) continue;

                    var valType = val.GetType();

                    var destList = destination.GetPropertyInfo(complexMap.Key).PropertyType.Create<IList>();

                    var o = destList as object;
                    Resolver.Create(valType, destList.GetType(), complexMap.Value, resolver)
                        .Resolve(val, ref o);

                    destination[destinationObject, complexMap.Key] = destList;
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


            /// <summary>
            /// Gets the source path.
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <typeparam name="R"></typeparam>
            /// <param name="destination">The destination path.</param>
            /// <returns></returns>
            /// <exception cref="System.MissingMemberException"></exception>
            [Obsolete("This method is not currently well implemented")]
            public string GetSourcePath<T, R>(Expression<Func<T, R>> destination)
            {
                var body = destination.Body as MemberExpression;
                if(body == null) throw new MissingMemberException();
                //^(.+\(.+\))\.((.+\.)+(.+))|^.+\.((.+\.)+(.+)) // TODO: add method detection support
                var path = Regex.Match(body.ToString(), @"^.+\.((.+\.)+(.+))").Groups[1].Value.Replace('.', _config.Separator);

                return GetSourcePath(path);
            }


            /// <summary>
            /// Gets the source path that is mapped to destination.
            /// </summary>
            /// <param name="destination">The destination path.</param>
            /// <returns></returns>
            /// <exception cref="System.Reflection.AmbiguousMatchException">More then one result found.</exception>
            public string GetSourcePath(string destination)
            {
                try
                {
                    return _map.DestinationNonReMapedProperties.SingleOrDefault(o => o.Equals(destination)) ??
                           _map.AdditionalMaps.Single(o => o.Value.Path.Equals(destination)).Key.Path;
                }
                catch (InvalidOperationException e)
                {
                    throw new AmbiguousMatchException("More then one result found.", e);
                }
            }

            /// <summary>
            /// Gets the source path that is mapped to destination.
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <typeparam name="R"></typeparam>
            /// <param name="source">The source path.</param>
            /// <returns></returns>
            /// <exception cref="System.MissingMemberException"></exception>
            [Obsolete("This method is not currently well implemented")]
            public string GetDestinationPath<T, R>(Expression<Func<T, R>> source)
            {
                var body = source.Body as MemberExpression;
                if (body == null) throw new MissingMemberException();

                var path = Regex.Match(body.ToString(), @"^.+\.((.+\.)+(.+))").Groups[1].Value.Replace('.', _config.Separator);

                return GetDestinationPath(path);
            }

            /// <summary>
            /// Gets the destination path that is mapped to destination.
            /// </summary>
            /// <param name="source">The source path.</param>
            /// <returns></returns>
            /// <exception cref="System.Reflection.AmbiguousMatchException">More then one result found.</exception>
            public string GetDestinationPath(string source)
            {
                try
                {
                    return _map.SourceNonReMapedProperties.SingleOrDefault(o => o.Equals(source)) ??
                           _map.AdditionalMaps.Single(o => o.Key.Path.Equals(source)).Value.Path;
                }
                catch (InvalidOperationException e)
                {
                    throw new AmbiguousMatchException("More then one result found.", e);
                }
            }

            #endregion

            #region IMap<,>

            /// <summary>        
            /// Generates mapping proxy object, that allows to perform real-time mapping using parent getters and setters.
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
            
            /// <exception cref="System.MissingMemberException">
            /// </exception>
            /// <exception cref="System.ArgumentException">
            /// Source path cannot be recognized
            /// or
            /// Destination path cannot be recognized
            /// </exception>
            [Obsolete("This method is not currently well implemented")]            
            public IMap Remap<TS, TR>(Expression<Func<TSource, TS>> source, Expression<Func<TDestination, TR>> destination)                
            {
                var sourceBody = source.Body as MemberExpression;
                var destinationBody = destination.Body as MemberExpression;

                if (sourceBody == null) throw new MissingMemberException();
                if (destinationBody == null) throw new MissingMemberException();

                var sourceMatch = Regex.Match(sourceBody.ToString(), @"^.+\.((.+\.)+(.+))");
                if(!sourceMatch.Success) throw new ArgumentException("Source path cannot be recognized");
                var sourcePath = sourceMatch.Groups[1].Value.Replace('.', _config.Separator);

                var destinationMatch = Regex.Match(destinationBody.ToString(), @"^.+\.((.+\.)+(.+))");
                if (!destinationMatch.Success) throw new ArgumentException("Destination path cannot be recognized");
                var destinationPath = destinationMatch.Groups[1].Value.Replace('.', _config.Separator);

                RegisterAdditionalMap<TR, TR>(sourcePath, destinationPath);

                return this;
            }


            /// <summary>
            /// Registers a new method for the destination type
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="name"></param>
            /// <param name="method"></param>
            /// <returns></returns>
            IMap<TSource, TDestination> IMap<TSource, TDestination>.RegisterDestinationMethod<T>(string name, T method)
            {
                RegisterDestinationMethod(name, method);
                return this;
            }


            /// <summary>
            /// Registers a new global method
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="name"></param>
            /// <param name="method"></param>
            /// <returns></returns>
            IMap<TSource, TDestination> IMap<TSource, TDestination>.RegisterGlobalMethod<T>(string name, T method)
            {
                RegisterGlobalMethod(name, method);
                return this;
            }


            /// <summary>
            /// Registers a new method for all available targets
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="name"></param>
            /// <param name="method"></param>
            /// <returns></returns>
            IMap<TSource, TDestination> IMap<TSource, TDestination>.RegisterMethod<T>(string name, T method)
            {
                RegisterMethod(name, method);
                return this;
            }

            /// <summary>
            /// Registers a new method for the source type
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="name"></param>
            /// <param name="method"></param>
            /// <returns></returns>
            IMap<TSource, TDestination> IMap<TSource, TDestination>.RegisterSourceMethod<T>(string name, T method)
            {
                RegisterSourceMethod(name, method);
                return this;
            }

            /// <summary>
            /// Remaps the specified property according to specified path
            /// </summary>
            /// <param name="source"></param>
            /// <param name="destination"></param>
            /// <returns></returns>
            IMap<TSource, TDestination> IMap<TSource, TDestination>.Remap(string source, string destination, Resolver resolver = null)
            {
                Remap(source, destination, resolver);
                return this;
            }

            /// <summary>
            /// Remaps the specified property according to specified path
            /// </summary>
            /// <typeparam name="TR"></typeparam>
            /// <param name="source"></param>
            /// <param name="destination"></param>
            /// <returns></returns>
            IMap<TSource, TDestination> IMap<TSource, TDestination>.Remap<TR>(string source, string destination)
            {
                Remap<TR>(source, destination);
                return this;
            }

            #endregion

            #region IMap<>            

            #region Method registration

            /// <summary>
            /// Registers a new method for the destination type
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="name"></param>
            /// <param name="method"></param>
            /// <returns></returns>
            IMap<TDestination> IMap<TDestination>.RegisterDestinationMethod<T>(string name, T method)
            {
                RegisterDestinationMethod(name, method);
                return this;
            }

            /// <summary>
            /// Registers a new global method
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="name"></param>
            /// <param name="method"></param>
            /// <returns></returns>
            IMap<TDestination> IMap<TDestination>.RegisterGlobalMethod<T>(string name, T method)
            {
                RegisterGlobalMethod(name, method);
                return this;
            }

            /// <summary>
            /// Registers a new method for all available targets
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="name"></param>
            /// <param name="method"></param>
            /// <returns></returns>
            IMap<TDestination> IMap<TDestination>.RegisterMethod<T>(string name, T method)
            {
                RegisterMethod(name, method);
                return this;
            }

            /// <summary>
            /// Registers a new method for the source type
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="name"></param>
            /// <param name="method"></param>
            /// <returns></returns>
            IMap<TDestination> IMap<TDestination>.RegisterSourceMethod<T>(string name, T method)
            {
                RegisterSourceMethod(name, method);
                return this;
            }

            #endregion

            /// <summary>
            /// Executes mapping from target to destination object
            /// </summary>
            /// <param name="obj"></param>
            /// <returns></returns>
            TDestination IMap<TDestination>.Do(object obj)
            {
                return Do((TSource) obj);
            }

            /// <summary>
            /// Remaps the specified property according to specified path
            /// </summary>
            /// <typeparam name="TR"></typeparam>
            /// <param name="source"></param>
            /// <param name="destination"></param>
            /// <returns></returns>
            IMap<TDestination> IMap<TDestination>.Remap<TR>(string source, string destination, Resolver resolver = null)
            {
                Remap<TR>(source, destination);
                return this;
            }

            /// <summary>
            /// Remaps the specified property according to specified path
            /// </summary>
            /// <param name="source"></param>
            /// <param name="destination"></param>
            /// <returns></returns>
            IMap<TDestination> IMap<TDestination>.Remap(string source, string destination, Resolver resolver = null)
            {
                Remap(source, destination);
                return this;
            }

            #endregion

            #region IMap
            
            TR IMap.Do<T, TR>(T obj)
            {
                return (TR)(object)Do((TSource)(object)obj);
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

            /// <summary>
            /// Remaps the specified property according to specified path
            /// </summary>
            /// <typeparam name="TR">Last value type</typeparam>
            /// <param name="source"></param>
            /// <param name="destination"></param>
            /// <returns></returns>
            public IMap Remap<TR>(string source, string destination, Resolver resolver = null)
            {
                return RegisterAdditionalMap<TR, TR>(source, destination, resolver);
            }

            /// <summary>
            /// Remaps the specified property according to specified path
            /// </summary>
            /// <param name="source"></param>
            /// <param name="destination"></param>
            /// <param name="resolver"></param>
            /// <returns></returns>
            /// <exception cref="Exception"></exception>
            public IMap Remap(string source, string destination, Resolver resolver = null)
            {
                try
                {
                    if(resolver == null)
                    {
                        var t = DetermineResultType(DestinationDefault, destination.Split(_config.Separator));
                        this.GetType().GetMethod("RegisterAdditionalMap", BindingFlags.NonPublic | BindingFlags.Instance)
                            .MakeGeneric(t, t)
                            .Invoke(this, new object[] {source, destination, resolver});
                    }
                    else
                    {
                        this.GetType().GetMethod("RegisterAdditionalMap", BindingFlags.NonPublic | BindingFlags.Instance)
                            .MakeGeneric(resolver.SouceType, resolver.DestinationType)
                            .Invoke(this, new object[] { source, destination, resolver });
                    }
                }
                catch (TargetInvocationException e)
                {
                    throw e.InnerException;
                }

                return this;
            }

            #endregion

            #region Method registration

            /// <summary>
            /// Registers a new method for the destination type
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="name"></param>
            /// <param name="method"></param>
            /// <returns></returns>
            public IMap RegisterDestinationMethod<T>(string name, T method)
            {
                _map.Destination.RegisterMethod(name, method);
                return this;
            }

            /// <summary>
            /// Registers a new global method
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="name"></param>
            /// <param name="method"></param>
            /// <returns></returns>
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

            /// <summary>
            /// Registers a new method for all available targets
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="name"></param>
            /// <param name="method"></param>
            /// <returns></returns>
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

            /// <summary>
            /// Registers a new method for the source type
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="name"></param>
            /// <param name="method"></param>
            /// <returns></returns>
            public IMap RegisterSourceMethod<T>(string name, T method)
            {
                _map.Source.RegisterMethod(name, method);
                return this;
            }

            #endregion

            #region Helpers

            private void ValidateSection<T>(DataProxy<T> xc, string firstPath,
                Action<Type, object> onResult) 
                //ref Type propertyType, ref object @delegate)
            {
                Type propertyType; object @delegate;
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
            }
            
            /// <exception cref="System.InvalidOperationException"></exception>
            /// <exception cref="AOMapper.Exceptions.InvalidTypeBindingException">
            ///     <para>It is not possible to resolve type binding.</para>
            ///     <para>Please register <see cref="Resolver"/> to handle this conversion.</para>
            /// </exception>
            private Func<T, TRR> GetSourceInvokeChain<T, TR, TRR>(string path, T obj, out Delegate @out, Resolver resolver)
            {
                @out = null;
                var paths = path.Split(_config.Separator).Select(x => x.Trim()).ToArray();                
                var firstPath = paths.First();
                var xc = DataProxy.Create<T>();

                Type propertyType = null;
                object @delegate = null;

                var typeArguments = typeof (T);
                if (xc.ContainsProperty(firstPath))
                {
                    propertyType = xc.GetPropertyInfo(firstPath).PropertyType;
                    @delegate = xc.GetReflectedGetter(firstPath, propertyType);
                }
                else if (xc.ContainsMethod(firstPath))
                {
                    propertyType = xc.Methods[firstPath].Info.ReturnType;
                    @delegate = FuncConverter
                        .MakeGeneric(typeArguments, propertyType)
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
                        (KeyValuePair<Type, Delegate>) GetterCreator.MakeGenericMethod(typeArguments, propertyType, target)
                            .Invoke(null,
                                new object[] {s, new KeyValuePair<Type, Delegate>(propertyType, @delegate as Delegate)});
                    @delegate = pair.Value;
                    propertyType = pair.Key;
                }
                if (resolver != null)
                {
                    @delegate = (@delegate as Func<T, TR>).Compose(o1 =>
                    {
                        object a = o1;
                        resolver.Resolve(o1, ref a);
                        return (TRR)a;
                    });
                }

                try
                {
                    return (Func<T, TRR>) @delegate;
                }
                catch (InvalidCastException e)
                {
                    var sourceType = @delegate.GetType().GetGenericArguments().Last();
                    var targetType = typeof(TRR);

                    throw new InvalidTypeBindingException("It is not possible to resolve type binding from " + sourceType.Name + " to " + targetType.Name +".\n" +
                                                          "Please register Resolver to handle this conversion.", e, path, sourceType, targetType);
                }
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

                var typeArguments = typeof (T);
                var t = typeArguments;
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
                            .MakeGeneric(typeArguments, propertyType)
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

                        f = InitCreator.MakeGenericMethod(typeArguments, propertyType, propertyType)
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
                var m = SetterCreator.MakeGeneric(ff.Key, typeof (TR))
                    .Invoke(null, new object[] {o, propertyName}) as Delegate;
                var minfo = m.Method;

                var a = ActionConverter
                    .MakeGeneric(propertyType, minfo.GetParameters()[0].ParameterType, TypeOfObject, typeof (TR))
                    .Invoke(null, new object[] {m});
                @out = a as Delegate;
                return (f as Func<T, object>).Compose(a as Action<object, TR>);
            }

            private IMap RegisterAdditionalMap<TR, TRR>(string source, string destination, Resolver resolver = null)
            {
                lock (this)
                {
                    if (!_map.AdditionalMaps.Any(o => o.Value.Path.Equals(destination)))
                    {
                        Delegate tempSource = null;
                        _map.AdditionalMaps.Add(new Map
                            <MapObject<Func<TSource, object>>, MapObject<Action<TDestination, object>>>
                                (CreateSourceMapObject<TR, TRR>(source, resolver), CreateDestinationMapObject<TR, TRR>(destination)));
                        _map.Calculate();
                    }
                    else _map.AdditionalMaps.Single(o => o.Value.Path.Equals(destination)).Key.Path = source;
                }
                return this;
            }

            private MapObject<Action<TDestination, object>> CreateDestinationMapObject<TR, TRR>(string destination)
            {
                Delegate tempSource;
                return new MapObject<Action<TDestination, object>>
                {
                    Path = destination,
                    Invoker =
                        GetDestinationInvokeChain<TDestination, TRR>(destination, DestinationDefault,
                            out tempSource).Convert<TDestination, TRR, TDestination, Object>(),
                    LastInvokeTarget = tempSource
                };
            }

            private MapObject<Func<TSource, object>> CreateSourceMapObject<TR, TRR>(string source, Resolver resolver)
            {
                Delegate tempSource = null;
                return new MapObject<Func<TSource, object>>
                {
                    Path = source,
                    Invoker = source == null ? null :
                        GetSourceInvokeChain<TSource, TR, TRR>(source, SourceDefault, out tempSource, resolver)
                            .Convert<TSource, TRR, TSource, object>(),
                    LastInvokeTarget = source == null ? null : tempSource
                };
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
            return (o, o1) => f((T)(object)o, (TR)(object)o1);
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