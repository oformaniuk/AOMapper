using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using AOMapper.Compiler;
using AOMapper.Compiler.Resolvers;
using AOMapper.Data;
using AOMapper.Data.Keys;
using AOMapper.Extensions;
using AOMapper.Helpers;
using AOMapper.Interfaces;
using AOMapper.Resolvers;

#if !NET35

#endif
#if NET35
using AOMapper.Helpers;
#endif

namespace AOMapper
{
    /// <summary>
    /// </summary>
    [SuppressMessage("ReSharper", "MethodOverloadWithOptionalParameter")]
    public class Mapper
    {
        static Mapper()
        {            
            Maps = new Dictionary<ArgArray, object>();
            CreatedMappers = new List<object>();
        }

        /// <summary>
        ///     Creates new or get cached objects map
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">Type should have parameterless constructor in order to be used with this method overload.</exception>
        public static IMap<TS, TR> Create<TS, TR>()
            //where TR : new()
        {
            var type = typeof(TR);
            if (type.GetConstructor(new Type[0]) == null && type.IsClass)
                throw new InvalidOperationException(
                    string.Format(
                        "Type {0} should have parameterless constructor in order to be used with this method overload",
                        type.Name));

            return MapperInnerClass<TS, TR>.Map(null as Expression<Func<TS, TR>>);
        }

        /// <summary>
        ///     Creates new or get cached objects map
        /// </summary>
        /// <returns></returns>        
        internal static IMap<TS, TR> Create<TS, TR>(CompileTimeResolver resolver)        
        {            
            return MapperInnerClass<TS, TR>.Map(resolver);
        }

        /// <summary>
        ///     Creates new or get cached objects map
        /// </summary>
        /// <returns></returns>
        public static IMap<TS, TR> Create<TS, TR>(Expression<Func<TS, TR>> resolver)
        {
            return MapperInnerClass<TS, TR>.Map(resolver);
        }

        /// <summary>
        ///     Clears information about all created maps
        /// </summary>
        public static void Clear()
        {
            foreach (var map in CreatedMappers)
                map.As<IDisposable>().Dispose();
            CreatedMappers.Clear();
            Maps.Clear();
            Resolver.Clear();
        }

        #region ConfigClass

        /// <summary>
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
            ///     <para>Gets or sets a value indicating whether default values would be ignored during mapping.</para>
            ///     <para>Default: False</para>
            /// </summary>
            /// <value>
            ///     <c>true</c> if default values should be ignored during mapping; otherwise, <c>false</c>.
            /// </value>
            public bool IgnoreDefaultValues { get; set; }

            /// <summary>
            ///     Gets or sets the path separator.
            /// </summary>
            public char Separator { get; set; }

            /// <summary>
            ///     <para>Gets or sets a value indicating whether properties with null values should be initialized during mapping.</para>
            ///     <para>Default: <c>True</c></para>
            /// </summary>
            /// <value>
            ///     <c>true</c> if null values should be initialized during mapping; otherwise, <c>false</c>.
            /// </value>
            public bool InitialyzeNullValues { get; set; }

            /// <summary>
            ///     <para>
            ///         Gets or sets a value indicating whether inner maps that are generated automatically during automatic mapping
            ///         should be compiled.
            ///     </para>
            ///     <para>Default: <c>True</c></para>
            /// </summary>
            /// <value>
            ///     <c>true</c> if inner maps should be compiled; otherwise, <c>false</c>.
            /// </value>
            public bool CompileInnerMaps { get; set; }

            /// <summary>
            ///     Registers the resolver.
            /// </summary>
            /// <typeparam name="TS">The type of the s.</typeparam>
            /// <typeparam name="TD">The type of the d.</typeparam>
            /// <param name="resolver">The resolver.</param>
            public void RegisterResolver<TS, TD>(Expression<Func<TS, TD>> resolver)
            {
                Resolver.RegisterResolver(_map, (SimpleResolver<TS, TD>) resolver);
            }

            /// <summary>
            ///     Registers the resolver.
            /// </summary>
            /// <param name="resolver">The resolver.</param>
            public void RegisterResolver(Resolver resolver)
            {
                Resolver.RegisterResolver(_map, resolver);
            }
        }

        #endregion

        #region Mapper

        internal class MapperInnerClass<TSource, TDestination> :
            Mapper, IMap<TSource, TDestination>,
            IPathProvider, IDisposable            
        {
            #region ctor's

            internal MapperInnerClass(Expression<Func<TSource, TDestination>> constructor)
            {
                Constructor = constructor;
                _config = new Config(this);

                var @delegate = (Func<TSource, TSource>) (o => o);
                var @delegateConverted = (Func<TSource, object>) (o => o);
                _sourceMappingRoute = new MappingRoute(this, typeof (TSource))
                {
                    GetDelegate = @delegate,
                    GetConverteDelegate = @delegateConverted
                };
                _destinationMappingRoute = new MappingRoute(this, typeof (TDestination))
                {
                    GetDelegate = (Func<TDestination, TDestination>) (o => o)
                };
            }

            internal MapperInnerClass(CompileTimeResolver resolver)
            {
                Constructor = resolver.GetExpression();
                _config = new Config(this);

                var @delegate = (Func<TSource, TSource>)(o => o);
                var @delegateConverted = (Func<TSource, object>)(o => o);
                _sourceMappingRoute = new MappingRoute(this, typeof(TSource))
                {
                    GetDelegate = @delegate,
                    GetConverteDelegate = @delegateConverted
                };
                _destinationMappingRoute = new MappingRoute(this, typeof(TDestination))
                {
                    GetDelegate = (Func<TDestination, TDestination>)(o => o)
                };
            }

            #endregion

            public T GetConfigurationParameter<T>(Func<Config, T> selector)
            {
                return selector(_config);
            }

            internal static MapperInnerClass<TSource, TDestination> Map(Expression<Func<TSource, TDestination>> resolver)
            {
                var s = typeof (TSource);
                var t = typeof (TDestination);
                var args = new ArgArray(s, t);
                if (Mappers.ContainsKey(args))
                {
                    return Mappers[args];                    
                }

                if (typeof (TSource) == typeof (TDestination) && resolver == null)
                {
                    resolver = source => (TDestination)(object)source;
                }

                var mapper = new MapperInnerClass<TSource, TDestination>(resolver);

                if (!Maps.ContainsKey(args))
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

                    Maps.Add(args, mapper._map.Apply(o => o.Calculate(mapper)));
                }
                else mapper._map = (PropertyMap<TSource, TDestination>) Maps[args];

                Mappers.Add(args, mapper);
                CreatedMappers.Add(mapper);

                return mapper;
            }

            internal static MapperInnerClass<TSource, TDestination> Map(CompileTimeResolver resolver)
            {
                var s = typeof(TSource);
                var t = typeof(TDestination);
                var args = new ArgArray(s, t);
                if (Mappers.ContainsKey(args))
                {
                    return Mappers[args];
                }                

                var mapper = new MapperInnerClass<TSource, TDestination>(resolver);

                if (!Maps.ContainsKey(args))
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

                    Maps.Add(args, mapper._map.Apply(o => o.Calculate(mapper)));
                }
                else mapper._map = (PropertyMap<TSource, TDestination>)Maps[args];

                Mappers.Add(args, mapper);
                CreatedMappers.Add(mapper);

                return mapper;
            }

            private bool IgnoreValue(Func<object> selector, out object v)
            {
                v = selector();
                if (_config.IgnoreDefaultValues)
                {
                    if (v != null)
                    {
                        var @default = v.GetDefault();
                        if (!ReferenceEquals(v, @default)) return false;
                        return true;
                    }

                    return true;
                }

                return false;
            }

            #region General overloads

            protected bool Equals(MapperInnerClass<TSource, TDestination> other)
            {
                return Equals(_map, other._map);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != GetType()) return false;
                return Equals((MapperInnerClass<TSource, TDestination>) obj);
            }

            public override int GetHashCode()
            {
                return _map != null ? _map.GetHashCode() : 0;
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

            private static readonly Dictionary<ArgArray, MapperInnerClass<TSource, TDestination>> Mappers =
                new Dictionary<ArgArray, MapperInnerClass<TSource, TDestination>>();

            private static readonly TSource SourceDefault = Activator.CreateInstance<TSource>();
            private readonly Config _config;
            internal PropertyMap<TSource, TDestination> _map;

            private Dictionary<StringKey, CodeTreeNode> _destinationCodeTree;
            private Dictionary<StringKey, CodeTreeNode> _sourceCodeTree;

            private Dictionary<StringKey, IMap> _complexMaps;

            private Func<TSource, TDestination, TDestination> _compiledMap;

            private readonly List<string> _sourceIgnoreList = new List<string>();
            private readonly List<string> _destinationIgnoreList = new List<string>();

            internal readonly MappingRoute _sourceMappingRoute;
            internal readonly MappingRoute _destinationMappingRoute;

            private readonly CallStack<TSource, TDestination> _callStack = new CallStack<TSource, TDestination>("", null,
                null, null);

            internal readonly Expression Constructor;

            #endregion

            #region auto-mapping            

            private MapperInnerClass<TSource, TDestination> Auto()
            {
                _destinationCodeTree = new Dictionary<StringKey, CodeTreeNode>();
                GenerateCodeTree<TDestination>(_destinationCodeTree, false, _destinationIgnoreList);

                _sourceCodeTree = new Dictionary<StringKey, CodeTreeNode>();
                GenerateCodeTree<TSource>(_sourceCodeTree, false, _sourceIgnoreList);

                CreateMapping(_destinationCodeTree, _sourceCodeTree);

                return this;
            }

            #region Ignore

            private MapperInnerClass<TSource, TDestination> InnerIgnoreSource(string source)
            {
                _sourceIgnoreList.Add(source);
                return this;
            }

            private MapperInnerClass<TSource, TDestination> InnerIgnoreDestination(string destination)
            {
                _destinationIgnoreList.Add(destination);
                return this;
            }

            IMap<TSource, TDestination> IMap<TSource, TDestination>.IgnoreSource(string source)
            {
                return InnerIgnoreSource(source);
            }

            IMap<TSource, TDestination> IMap<TSource, TDestination>.IgnoreDestination(string destination)
            {
                return InnerIgnoreDestination(destination);
            }

            IMap<TDestination> IMap<TDestination>.IgnoreSource(string source)
            {
                return InnerIgnoreSource(source);
            }

            IMap<TDestination> IMap<TDestination>.IgnoreDestination(string destination)
            {
                return InnerIgnoreDestination(destination);
            }

            IMap IMap.IgnoreDestination(string destination)
            {
                return InnerIgnoreDestination(destination);
            }

            IMap IMap.IgnoreSource(string source)
            {
                return InnerIgnoreSource(source);
            }

            #endregion

            /// <summary>
            ///     Tries to automatically configure the map using name comparation
            /// </summary>
            /// <returns></returns>
            IMap<TSource, TDestination> IMap<TSource, TDestination>.Auto()
            {
                return Auto();
            }

            /// <summary>
            ///     Tries to automatically configure the map using name comparation
            /// </summary>
            /// <returns></returns>
            IMap<TDestination> IMap<TDestination>.Auto()
            {
                return Auto();
            }

            /// <summary>
            ///     Tries to automatically configure the map using name comparation
            /// </summary>
            /// <returns></returns>
            IMap IMap.Auto()
            {
                return Auto();
            }

            private void CreateMapping(Dictionary<StringKey, CodeTreeNode> destinationMap,
                Dictionary<StringKey, CodeTreeNode> sourceMap)
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
                            else if (dest.Key.Equals(source.Key) && /*dest.Value.Type != null &&*/
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
                        catch
                        {
                            // ignoring - auto-mapping failed
                        }
                    }
                }
            }

            private static string GetPropertyNameFromPath(string value)
            {
                Match match;
                return (match = Regex.Match(value, @".*\((.+)\)")).Success ? match.Groups[1].Value : value;
            }

            private void GenerateCodeTree<T>(Dictionary<StringKey, CodeTreeNode> dictionary,
                bool init, List<string> ignoreList)
            {
                var main = DataProxy.Create<T>();
                var builder = new StringBuilder();

                var generalRule = main.Where(o => main.CanGet(o) && main.CanSet(o))
                    .Where(o => !ignoreList.Any(x => x.EndsWith(o)))
                    .ToArray();

                foreach (var prop in generalRule /*.Where(o => !main.IsEnumerable(o))*/)
                {
                    dictionary[prop] = new CodeTreeNode(prop, main.GetPropertyInfo(prop).PropertyType);

                    builder.Append(prop);
                    var propProxy = DataProxy.Create(main.GetPropertyInfo(prop).PropertyType);
                    if (propProxy.Any())
                    {
                        BuildTree(propProxy, builder, dictionary, init, ignoreList);
                    }
#if NET35
                    builder = new StringBuilder();
#else
                    builder.Clear();
#endif
                }
            }

            private void BuildTree<T>(DataProxy<T> main, StringBuilder builder,
                Dictionary<StringKey, CodeTreeNode> dictionary, bool init, List<string> ignoreList)
            {
                var generalRule = main.Where(o => main.CanGet(o) && main.CanSet(o))
                    .Where(o => !ignoreList.Any(x => x.EndsWith(o)))
                    .ToArray();

                foreach (var prop in generalRule /*.Where(o => !main.IsEnumerable(o))*/)
                {
                    builder.Append(_config.Separator);
                    builder.Append(prop);
                    dictionary[prop] = new CodeTreeNode(builder.ToString(), main.GetPropertyInfo(prop).PropertyType);
                    var propProxy = DataProxy.Create(main.GetPropertyInfo(prop).PropertyType);

                    if (propProxy.Any())
                    {
                        BuildTree(propProxy, builder, dictionary, init, ignoreList);
                    }
                }

                builder.Clear();
            }

            #endregion

            #region Compile

            /// <summary>
            ///     Compiles the map
            /// </summary>
            /// <returns></returns>
            IMap IMap.Compile()
            {
                return _compile();
            }

            /// <summary>
            ///     Compiles the map
            /// </summary>
            /// <returns></returns>
            IMap<TDestination> IMap<TDestination>.Compile()
            {
                return _compile() as IMap<TDestination>;
            }

            /// <summary>
            ///     Compiles the map
            /// </summary>
            /// <returns></returns>
            IMap<TSource, TDestination> IMap<TSource, TDestination>.Compile()
            {
                return _compile() as IMap<TSource, TDestination>;
            }

            Expression IMap.CompileToExpression(out ParameterExpression sourceParameterExpression,
                out ParameterExpression destinationParameterExpression)
            {
                return new Compiler<TSource, TDestination>(this)
                    .GetCompileReadyExpression(out sourceParameterExpression, out destinationParameterExpression);
            }

            private IMap _compile()
            {
                if (_compiledMap != null) return this;

                var compiler = new Compiler<TSource, TDestination>(this);
                _compiledMap = compiler.Compile();

                return this;                
            }

            #endregion

            #region Do            

            /// <summary>
            ///     Executes mapping from target to destination object
            /// </summary>
            /// <param name="sourceObject"></param>
            /// <returns></returns>
            /// <exception cref="InvalidOperationException">Cannot perform mapping while map is not compiled</exception>
            public TDestination Do(TSource sourceObject)
            {
                if (_compiledMap == null)
                    throw new InvalidOperationException("Cannot perform mapping while map is not compiled");                
                return _compiledMap(sourceObject, default(TDestination));
            }

            /// <exception cref="InvalidOperationException">Cannot perform mapping while map is not compiled</exception>
            public TDestination Do(TSource sourceObject, TDestination dest)
            {
                if (_compiledMap == null)
                    //return PerformMapping(sourceObject, dest);
                    throw new InvalidOperationException("Cannot perform mapping while map is not compiled");
                return _compiledMap(sourceObject, dest);
            }

            public TD Do<TS, TD>(TS obj, TD dest)
            {
                return (TD) (object) Do((TSource) (object) obj, (TDestination) (object) dest);
            }

            public TDestination Do(object obj, TDestination dest)
            {
                return Do((TSource) obj);
            }

            public object Do(object source, object destination)
            {
                return Do((TSource) source, (TDestination) destination);
            }

            public object Do(object source)
            {
                return Do((TSource) source);
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

            #region IPathProvider

            /// <summary>
            ///     Gets the source path.
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
                if (body == null) throw new MissingMemberException();
                //^(.+\(.+\))\.((.+\.)+(.+))|^.+\.((.+\.)+(.+)) // TODO: add method detection support
                var path = Regex.Match(body.ToString(), @"^.+\.((.+\.)+(.+))").Groups[1].Value.Replace('.',
                    _config.Separator);

                return GetSourcePath(path);
            }


            /// <summary>
            ///     Gets the source path that is mapped to destination.
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
            ///     Gets the source path that is mapped to destination.
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

                var path = Regex.Match(body.ToString(), @"^.+\.((.+\.)+(.+))").Groups[1].Value.Replace('.',
                    _config.Separator);

                return GetDestinationPath(path);
            }

            /// <summary>
            ///     Gets the destination path that is mapped to destination.
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

            /// <exception cref="System.MissingMemberException">
            /// </exception>
            /// <exception cref="System.ArgumentException">
            ///     Source path cannot be recognized
            ///     or
            ///     Destination path cannot be recognized
            /// </exception>
            [Obsolete("This method is not currently well implemented")]
            public IMap<TSource, TDestination> Remap<TS>(Expression<Func<TSource, TS>> source,
                Expression<Func<TDestination, TS>> destination)
            {
                return Remap(source, destination, null);
            }

            /// <exception cref="MissingMemberException"></exception>
            public IMap<TSource, TDestination> Remap(Expression<Func<TSource, object>> source,
                Expression<Func<TDestination, object>> destination)
            {
                var sourceBody = source.Body as MemberExpression;
                if (sourceBody == null)
                {
                    var e = source.Body as UnaryExpression;
                    if (e != null) sourceBody = e.Operand as MemberExpression;
                }
                var destinationBody = destination.Body as MemberExpression;
                if (destinationBody == null)
                {
                    var e = destination.Body as UnaryExpression;
                    if (e != null) destinationBody = e.Operand as MemberExpression;
                }

                if (sourceBody == null) throw new MissingMemberException();
                if (destinationBody == null) throw new MissingMemberException();

#if !PORTABLE
                var sourcePath =
                    sourceBody.ToString().Replace('.', _config.Separator).Split(new[] {_config.Separator}, 2)[1];

                var destinationPath =
                    destinationBody.ToString().Replace('.', _config.Separator).Split(new[] {_config.Separator}, 2)[1];
#else
                var sourcePath =
                    sourceBody.ToString().Replace('.', _config.Separator).Split(_config.Separator)[1];

                var destinationPath =
                    destinationBody.ToString().Replace('.', _config.Separator).Split(_config.Separator)[1];
#endif

                Remap(sourcePath, destinationPath, null);

                return this;
            }

            public IMap<TSource, TDestination> Remap<TS, TR>(Expression<Func<TSource, TS>> source,
                Expression<Func<TDestination, TR>> destination, SimpleResolver<TS, TR> resolver)
            {
                var sourceBody = source.Body as MemberExpression;
                var destinationBody = destination.Body as MemberExpression;

                if (sourceBody == null) throw new MissingMemberException();
                if (destinationBody == null) throw new MissingMemberException();

#if !PORTABLE
                var sourcePath =
                    sourceBody.ToString().Replace('.', _config.Separator).Split(new[] {_config.Separator}, 2)[1];

                var destinationPath =
                    destinationBody.ToString().Replace('.', _config.Separator).Split(new[] {_config.Separator}, 2)[1];
#else
                var sourcePath =
                    sourceBody.ToString().Replace('.', _config.Separator).Split(_config.Separator)[1];

                var destinationPath =
                    destinationBody.ToString().Replace('.', _config.Separator).Split(_config.Separator)[1];
#endif

                RegisterAdditionalMap<TS, TR>(sourcePath, destinationPath, source, destination, resolver
                    /*, sourceSelector*/);

                return this;
            }

            public IMap<TSource, TDestination> Remap<TS, TR>(Expression<Func<TSource, TS>> source,
                Expression<Func<TDestination, TR>> destination, Expression<Func<TS, TR>> resolver)
            {
                var sourceBody = source.Body as MemberExpression;
                var destinationBody = destination.Body as MemberExpression;

                if (sourceBody == null) throw new MissingMemberException();
                if (destinationBody == null) throw new MissingMemberException();

#if !PORTABLE
                var sourcePath =
                    sourceBody.ToString().Replace('.', _config.Separator).Split(new[] {_config.Separator}, 2)[1];

                var destinationPath =
                    destinationBody.ToString().Replace('.', _config.Separator).Split(new[] {_config.Separator}, 2)[1];
#else
                var sourcePath =
                    sourceBody.ToString().Replace('.', _config.Separator).Split(_config.Separator)[1];

                var destinationPath =
                    destinationBody.ToString().Replace('.', _config.Separator).Split(_config.Separator)[1];
#endif

                RegisterAdditionalMap<TS, TR>(sourcePath, destinationPath, source, destination, resolver != null ? new SimpleResolver<TS, TR>(resolver) : null
                    /*, sourceSelector*/);

                return this;
            }

            public IMap<TSource, TDestination> RemapFrom<TR>(Expression<Func<TDestination, TR>> destination,
                Expression<Func<TSource, TR>> selector)
            {
                var destinationBody = destination.Body as MemberExpression;

                if (destinationBody == null) throw new MissingMemberException();

#if !PORTABLE
                var destinationPath =
                    destinationBody.ToString().Replace('.', _config.Separator).Split(new[] {_config.Separator}, 2)[1];
#else
                var destinationPath =
                    destinationBody.ToString().Replace('.', _config.Separator).Split(_config.Separator)[1];
#endif

                RegisterAdditionalFromMap<TSource, TR>(selector, destinationPath);

                return this;
            }

            /// <summary>
            ///     Remaps the specified property according to specified path
            /// </summary>
            /// <param name="source"></param>
            /// <param name="destination"></param>
            /// <returns></returns>
            IMap<TSource, TDestination> IMap<TSource, TDestination>.Remap(string source, string destination,
                Resolver resolver = null)
            {
                Remap(source, destination, resolver);
                return this;
            }

            /// <summary>
            ///     Remaps the specified property according to specified path
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

            /// <summary>
            ///     Executes mapping from target to destination object
            /// </summary>
            /// <param name="obj"></param>
            /// <returns></returns>
            TDestination IMap<TDestination>.Do(object obj)
            {
                return Do((TSource) obj);
            }

            /// <summary>
            ///     Remaps the specified property according to specified path
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
            ///     Remaps the specified property according to specified path
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
                return (TR) (object) Do((TSource) (object) obj);
            }

            /// <summary>
            ///     Remaps the specified property according to specified path
            /// </summary>
            /// <typeparam name="TR">Last value type</typeparam>
            /// <param name="source"></param>
            /// <param name="destination"></param>
            /// <returns></returns>
            public IMap Remap<TR>(string source, string destination, Resolver resolver = null)
            {
                return RegisterAdditionalMapFromString<TR, TR>(source, destination, resolver);
            }

            /// <summary>
            ///     Remaps the specified property according to specified path
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
                    var registerAdditionalMap = GetType()
                        .GetMethod("RegisterAdditionalMapFromString", BindingFlags.NonPublic | BindingFlags.Instance);

                    if (resolver == null)
                    {
                        var sourceType = RouteHelpers.DetermineResultType(SourceDefault,
                            source.Split(new[] {_config.Separator.ToString()}, StringSplitOptions.RemoveEmptyEntries));
                        var destinationType = RouteHelpers.DetermineResultType(typeof(TDestination),
                            destination.Split(_config.Separator));
                        registerAdditionalMap
                            .MakeGeneric(sourceType, destinationType)
                            .Invoke(this, new object[] {source, destination, resolver, null});
                    }
                    else
                    {
                        registerAdditionalMap
                            .MakeGeneric(resolver.SouceType, resolver.DestinationType)
                            .Invoke(this, new object[] {source, destination, resolver, null});
                    }
                }
                catch (TargetInvocationException e)
                {
                    throw e.InnerException;
                }

                return this;
            }

            #endregion

            #region Helpers                        

            private IMap RegisterAdditionalMapFromString<TS, TR>(string source, string destination,
                Resolver resolver = null,
                Func<TSource, TS> selector = null)
            {
                return RegisterAdditionalMap<TS, TR>(source, destination,
                    RouteHelpers.ConvertRouteToFuncExpression<TSource, TS>(this, source).Body,
                    RouteHelpers.ConvertRouteToActionExpression<TDestination, TR>(this, destination),
                    resolver, selector);
            }

            private IMap RegisterAdditionalMap<TS, TR>(string source, string destination, Expression sourceExpression,
                Expression destinationExpression,
                Resolver resolver = null, Func<TSource, TS> selector = null)
            {
                lock (this)
                {
                    if (!_map.AdditionalMaps.Any(o => o.Value.Path.Equals(destination)))
                    {
                        var destinationMapObject = CreateDestinationMapObject<TR, TR>(destination, resolver);
                        destinationMapObject.MappingRoute.Expression = destinationExpression;

                        var sourceMapObject = CreateSourceMapObject<TS, TS>(source, resolver,
                            destinationMapObject.MappingRoute, selector);

                        sourceMapObject.MappingRoute.Expression = sourceExpression;
                        destinationMapObject.MappingRoute.SourceRoute = sourceMapObject.MappingRoute;

                        destinationMapObject.MappingRoute.AutoGenerated = false;
                        sourceMapObject.MappingRoute.AutoGenerated = false;

                        var mapObject =
                            new Map<MapObject<Func<TSource, object>>, MapObject<Action<TDestination, object>>>(
                                sourceMapObject, destinationMapObject);
                        _map.AdditionalMaps.Add(mapObject);
                        _map.Calculate(this);
                    }
                    else
                    {
                        var map = _map.AdditionalMaps.Single(o => o.Value.Path.Equals(destination));
                        map.Key.Path = source;                        
                    }
                }
                return this;
            }

            private IMap RegisterAdditionalFromMap<TS, TR>(Expression<Func<TSource, TR>> selector, string destination,
                Resolver resolver = null)
            {
                lock (this)
                {
                    var source = selector.Compile();
                    if (!_map.AdditionalMaps.Any(o => o.Value.Path.Equals(destination)))
                    {
                        IEnumerable<KeyValuePair<string, string>> registredPaths = null;

                        var destinationMapObject = CreateDestinationMapObject<TR, TR>(destination, resolver);
                        var sourceMapObject = CreateSourceMapObject<TS, TR>(null, resolver,
                            destinationMapObject.MappingRoute, source);

                        sourceMapObject.Resolver = new SimpleResolver<TSource, TR>(selector);

                        var mapObject =
                            new Map<MapObject<Func<TSource, object>>, MapObject<Action<TDestination, object>>>(
                                sourceMapObject, destinationMapObject);
                        _map.AdditionalMaps.Add(mapObject);
                        _map.Calculate(this);
                    }
                    else
                        _map.AdditionalMaps.Single(o => o.Value.Path.Equals(destination)).Key.Invoker =
                            source.Convert<TSource, TR, TSource, object>();
                }
                return this;
            }

            private MapObject<Action<TDestination, object>> CreateDestinationMapObject<TR, TRR>(string destination,
                Resolver resolver)
            {
                Delegate tempSource;
                var router = _destinationMappingRoute;
                var mappingRoute = MappingRoute.Parse(destination, router, resolver, initObjects: false);
                return new MapObject<Action<TDestination, object>>
                {
                    Path = destination,
                    Invoker = null,
                    LastInvokeTarget = null,
                    MappingRoute = mappingRoute,
                    Resolver = resolver,
                    Type = typeof (TR)
                };
            }

            private MapObject<Func<TSource, object>> CreateSourceMapObject<TR, TRR>(string source, Resolver resolver,
                MappingRoute destination, Func<TSource, TRR> selector = null)
            {
                var router = _sourceMappingRoute;
                var mappingRoute = selector == null ? MappingRoute.Parse(source, router, resolver, destination) : null;

                if (resolver != null && mappingRoute != null)
                {
                    mappingRoute.Resolver = resolver;
                }

                var mapObject = new MapObject<Func<TSource, object>>
                {
                    Path = source,
                    Invoker =
                        selector == null
                            ? mappingRoute.GetConverteDelegate.As<Func<TSource, object>>()
                            : selector.Convert<TSource, TRR, TSource, object>(),
                    LastInvokeTarget = source == null ? null : router.GetRoute(source).Parent.GetDelegate,
                    Resolver = resolver,
                    Type = typeof (TR),
                    MappingRoute = mappingRoute
                };

                return mapObject;
            }

            #endregion
        }

        #endregion

        #region Fields           

        private static readonly Dictionary<ArgArray, object> Maps;

        protected static readonly List<object> CreatedMappers;

        #endregion
    }
}