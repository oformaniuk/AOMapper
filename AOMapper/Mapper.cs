using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using AOMapper.Data;
using AOMapper.Data.Keys;
using AOMapper.Exceptions;
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
    public class Mapper
    {
        static Mapper()
        {
            var mapperType = typeof (Mapper);
            Maps = new Dictionary<ArgArray, object>();
            CreatedMappers = new List<object>();
        }

        /// <summary>
        ///     Creates new or get cached objects map
        /// </summary>
        /// <returns></returns>
        public static IMap<TS, TR> Create<TS, TR>() where TR : new()
        {
            return MapperInnerClass<TS, TR>.Map();
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
            public void RegisterResolver<TS, TD>(Func<TS, TD> resolver)
            {
                Resolver.RegisterResolver(_map, (Resolver<TS, TD>) resolver);
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
            Mapper, IMap<TSource, TDestination>, IPathProvider, IDisposable
            where TDestination : new()
        {
            #region ctor's

            internal MapperInnerClass()
            {
                _config = new Config(this);

                var @delegate = (Func<TSource, TSource>) (o => o);
                _sourceMappingRoute = new MappingRoute(this, typeof (TSource))
                {
                    GetDelegate = @delegate,
                    GetConverteDelegate = @delegate.Convert<TSource, TSource, TSource, object>()
                };
                _destinationMappingRoute = new MappingRoute(this, typeof (TDestination))
                {
                    GetDelegate = (Func<TDestination, TDestination>) (o => o)
                };
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

            //private void MapRemaperPropertiesNonCompiled(TSource source, object obj, MappingRoute mappingRoute)
            //{
            //    var proxy = DataProxy.Create(obj);
            //    var map = _map.AdditionalMaps
            //        .FirstOrDefault(o => o.Value.Path.Equals(mappingRoute.Route));

            //    object value = null;
            //    var destinationPropertyType = proxy.GetPropertyInfo(mappingRoute.Key).PropertyType;

            //    if (map != null)
            //    {
            //        if (map.Key.Path == null)
            //        {
            //            object v;
            //            if (!IgnoreValue(() => map.Key.Invoker(source), out v))
            //                value = proxy[mappingRoute.Key] = v;
            //        }
            //        else
            //        {
            //            if (map.Key.Resolver != null)
            //            {
            //                object v;
            //                if (!IgnoreValue(() => map.Key.Invoker(source), out v))
            //                {
            //                    map.Key.Resolver.Resolve(v, ref value);
            //                    proxy[mappingRoute.Key] = value;
            //                }
            //            }
            //            else
            //            {
            //                var sourcePropertyType = map.Key.Type;
            //                var canMap = sourcePropertyType == destinationPropertyType;
            //                if (canMap)
            //                {
            //                    object v;
            //                    if (!IgnoreValue(() => map.Key.Invoker(source), out v))
            //                        value = proxy[mappingRoute.Key] = v;
            //                }
            //                else
            //                {
            //                    var resolver = Resolver.Create(sourcePropertyType, destinationPropertyType, this);
            //                    if (resolver != null)
            //                    {
            //                        object v;
            //                        if (!IgnoreValue(() => map.Key.Invoker(source), out v))
            //                        {
            //                            resolver.Resolve(v, ref value);
            //                            proxy[mappingRoute.Key] = value;
            //                        }
            //                    }
            //                    else
            //                    {
            //                        throw new InvalidTypeBindingException(map.Value.Path, sourcePropertyType,
            //                            destinationPropertyType);
            //                    }
            //                }
            //            }
            //        }
            //    }
            //    else
            //    {
            //        if (proxy[mappingRoute.Key] == null && _config.InitialyzeNullValues)
            //        {
            //            var prop = destinationPropertyType;
            //            value = proxy[mappingRoute.Key] = Activator.CreateInstance(prop);
            //        }
            //    }

            //    foreach (var route in mappingRoute)
            //    {
            //        MapRemaperPropertiesNonCompiled(source, value, route);
            //    }
            //}            

            private void MapRemaperPropertiesNonCompiled(object source, object obj, MappingRoute mappingRoute, 
                SimpleLazy<TSource> provider, TSource globalSource, Map<MapObject<Func<TSource, object>>, MapObject<Action<TDestination, object>>> map)
            {
                var proxy = DataProxy.Create(obj);
                //var map = _map.AdditionalMaps
                //    .FirstOrDefault(o => o.Value.Path.Equals(mappingRoute.Route));

                object value = null;
                var destinationPropertyType = proxy.GetPropertyInfo(mappingRoute.Key).PropertyType;

                if (map != null)
                {
                    if (map.Key.Path == null)
                    {
                        object v;
                        if (!IgnoreValue(() => provider.Get(globalSource, source, mappingRoute), out v))
                            value = proxy[mappingRoute.Key] = v;
                    }
                    else
                    {
                        if (map.Key.Resolver != null)
                        {
                            object v;
                            if (!IgnoreValue(() => provider.Get(globalSource, source, mappingRoute), out v))
                            {
                                map.Key.Resolver.Resolve(v, ref value);
                                proxy[mappingRoute.Key] = value;
                            }
                        }
                        else
                        {
                            var sourcePropertyType = map.Key.Type;
                            var canMap = sourcePropertyType == destinationPropertyType;
                            if (canMap)
                            {
                                object v;
                                if (!IgnoreValue(() => provider.Get(globalSource, source, mappingRoute), out v))
                                    value = proxy[mappingRoute.Key] = v;
                            }
                            else
                            {
                                var resolver = Resolver.Create(sourcePropertyType, destinationPropertyType, this);
                                if (resolver != null)
                                {
                                    object v;
                                    if (!IgnoreValue(() => provider.Get(globalSource, source, mappingRoute), out v))
                                    {
                                        resolver.Resolve(v, ref value);
                                        proxy[mappingRoute.Key] = value;
                                    }
                                }
                                else
                                {
                                    throw new InvalidTypeBindingException(map.Value.Path, sourcePropertyType,
                                        destinationPropertyType);
                                }
                            }
                        }
                    }
                }
                else
                {
                    if (proxy[mappingRoute.Key] == null && _config.InitialyzeNullValues)
                    {
                        var prop = destinationPropertyType;
                        value = proxy[mappingRoute.Key] = Activator.CreateInstance(prop);
                    }
                }

                GroupMappingRoutes(source, mappingRoute, globalSource, value);

                //foreach (var route in mappingRoute)
                //{
                //    MapRemaperPropertiesNonCompiled(source, value, route);
                //}
            }

            private void GroupMappingRoutes(object source, MappingRoute mappingRoute, TSource globalSource, object value)
            {
                foreach (
                    var route in
                        mappingRoute.Where(o => o.SourceRoute == null /*|| string.IsNullOrEmpty(o.SourceRoute.Parent.Key)*/))
                {
                    var mmap = _map.AdditionalMaps
                        .FirstOrDefault(o => o.Value.Path.Equals(route.Route));

                    var p = new SimpleLazy<TSource>((global, s, r) => mmap.Key.Invoker(global));

                    MapRemaperPropertiesNonCompiled(source, value, route, p, globalSource, mmap);
                }

                var groups = mappingRoute
                    .Where(o => o.SourceRoute != null)
                    //.Where(o => !string.IsNullOrEmpty(o.SourceRoute.Parent.Key))
                    .GroupBy(o => o.SourceRoute.Parent);

                foreach (var group in groups)
                {                    
                    var getter = @group.Key.GetConverteDelegate.As<Func<TSource, object>>();
                    var p = new SimpleLazy<TSource>((global, s, r) => getter(global));

                    foreach (var grp in @group.GroupBy(o => o.SourceRoute))
                    {                        
                        //var rr = route;
                        var pp = new SimpleLazy<TSource>((g, s, r) =>
                        {
                            var v = p.Get(g, s, r);
                            return r.SourceRoute._dataProxy[v, r.SourceRoute.Key];
                        });

                        foreach (var route in grp)
                        {
                            var mmap = _map.AdditionalMaps
                                .FirstOrDefault(o => o.Value.Path.Equals(route.Route));

                            MapRemaperPropertiesNonCompiled(source, value, route, pp, globalSource, mmap);
                        }
                    }                    
                }
            }

            private void GroupMappingRoutesCompiled(object source, MappingRoute mappingRoute, 
                TSource globalSource, object value, CallStack<TSource, TDestination> parentStack)
            {
                foreach (
                    var route in
                        mappingRoute.Where(o => o.SourceRoute == null /*|| string.IsNullOrEmpty(o.SourceRoute.Parent.Key)*/))
                {
                    var mmap = _map.AdditionalMaps
                        .FirstOrDefault(o => o.Value.Path.Equals(route.Route));

                    var p = new SimpleLazy<TSource>((global, s, r) => mmap.Key.Invoker(global));

                    MapRemaperProperties(source, value, route, p, globalSource, mmap, parentStack);
                }

                var groups = mappingRoute
                    .Where(o => o.SourceRoute != null)
                    //.Where(o => !string.IsNullOrEmpty(o.SourceRoute.Parent.Key))
                    .GroupBy(o => o.SourceRoute.Parent);

                foreach (var group in groups)
                {
                    var getter = @group.Key.GetConverteDelegate.As<Func<TSource, object>>();
                    var p = new SimpleLazy<TSource>((global, s, r) => getter(global));

                    foreach (var grp in @group.GroupBy(o => o.SourceRoute))
                    {
                        SimpleLazy<TSource> pp;
                        //var get = grp.Key.SourceRoute._dataProxy.GetGetter(grp.Key.SourceRoute.Key);
                        if (grp.Key.Resolver != null)
                        {
                            var resolver = grp.Key.Resolver;                            
                            pp = new SimpleLazy<TSource>((g, s, r) =>
                            {
                                var v = p.Get(g, s, r);
                                var ss = r.SourceRoute._dataProxy[v, r.SourceRoute.Key];
                                object result = null;
                                resolver.Resolve(ss, ref result);

                                return result;
                            });
                        }
                        else
                        {
                            pp = new SimpleLazy<TSource>((g, s, r) =>
                            {
                                var v = p.Get(g, s, r);
                                return r.SourceRoute._dataProxy[v, r.SourceRoute.Key];
                            }); 
                        }                       

                        foreach (var route in grp)
                        {
                            var mmap = _map.AdditionalMaps
                                .FirstOrDefault(o => o.Value.Path.Equals(route.Route));

                            MapRemaperProperties(source, value, route, pp, globalSource, mmap, parentStack);
                        }
                    }
                }
            }

            private void MapRemaperProperties(object source, object obj, MappingRoute mappingRoute,
                SimpleLazy<TSource> provider, TSource globalSource, Map<MapObject<Func<TSource, object>>, 
                MapObject<Action<TDestination, object>>> map,
                CallStack<TSource, TDestination> parentStack)
            {
                var stack = new CallStack<TSource, TDestination>("", parentStack, obj, null);

                var proxy = mappingRoute._dataProxy;//DataProxy.Create(obj);
                //var map = _map.AdditionalMaps
                //    .FirstOrDefault(o => o.Value.Path.Equals(mappingRoute.Route));

                object value = null;
                var destinationPropertyType = proxy.GetPropertyInfo(mappingRoute.Key).PropertyType;

                //var getter = proxy.GetGetter(mappingRoute.Key);
                //var setter = proxy.GetSetter(mappingRoute.Key);

                var getter = proxy.GetPlainGetter(mappingRoute.Key);
                var setter = proxy.GetPlainSetter(mappingRoute.Key);

                if (map != null)
                {
                    if (map.Key.Path == null)
                    {                        
                        if(_config.IgnoreDefaultValues)
                        {                            
                            stack.AddAction((d, s) =>
                            {
                                object v;
                                if (!IgnoreValue(() => provider.Get(s, s, mappingRoute), out v))
                                    value = proxy[d, mappingRoute.Key] = v;

                                return value;
                            });
                        }
                        else
                        {
                            stack.AddAction(
                                (d, s) => /*proxy[d, mappingRoute.Key] = provider.Get(s, s, mappingRoute)*/
                                {
                                    var v = provider.Get(s, s, mappingRoute);
                                    setter(d, v);
                                    return v;
                                });
                        }                     
                    }
                    else
                    {
                        if (map.Key.Resolver != null)
                        {
                            if(_config.IgnoreDefaultValues)
                            {
                                stack.AddAction((d, s) =>
                                {
                                    object v;
                                    if (!IgnoreValue(() => provider.Get(s, s, mappingRoute), out v))
                                    {
                                        //map.Key.Resolver.Resolve(v, ref value);
                                        proxy[d, mappingRoute.Key] = value;
                                    }

                                    return value;
                                }); 
                            }
                            else
                            {
                                stack.AddAction((d, s) =>
                                {
                                    //proxy[d, mappingRoute.Key] = provider.Get(s, s, mappingRoute)
                                    var v = provider.Get(s, s, mappingRoute);
                                    setter(d, v);
                                    return v;
                                });
                            }                           
                        }
                        else
                        {
                            var sourcePropertyType = map.Key.Type;
                            var canMap = sourcePropertyType == destinationPropertyType;
                            if (canMap)
                            {
                                if(_config.IgnoreDefaultValues)
                                {
                                    stack.AddAction((d, s) =>
                                    {
                                        object v;
                                        if (!IgnoreValue(() => provider.Get(s, s, mappingRoute), out v))
                                            value = proxy[d, mappingRoute.Key] = v;

                                        return value;
                                    }); 
                                }
                                else
                                {
                                    stack.AddAction((d, s) =>
                                    {
                                        //proxy[d, mappingRoute.Key] = provider.Get(s, s, mappingRoute)
                                        var v = provider.Get(s, s, mappingRoute);
                                        setter(d, v);
                                        return v;
                                    });
                                }                               
                            }
                            else
                            {
                                var resolver = Resolver.Create(sourcePropertyType, destinationPropertyType, this);
                                if (resolver != null)
                                {
                                    if(_config.IgnoreDefaultValues)
                                    {
                                        stack.AddAction((d, s) =>
                                        {
                                            object v;
                                            if (!IgnoreValue(() => provider.Get(s, source, mappingRoute), out v))
                                            {
                                                resolver.Resolve(v, ref value);
                                                proxy[d, mappingRoute.Key] = value;
                                            }

                                            return value;
                                        });  
                                    }
                                    else
                                    {
                                        stack.AddAction((d, s) =>
                                        {
                                            resolver.Resolve(provider.Get(s, source, mappingRoute), ref value);
                                            //proxy[d, mappingRoute.Key] = value;
                                            setter(d, value);

                                            return value;
                                        });
                                    }                                
                                }
                                else
                                {
                                    throw new InvalidTypeBindingException(map.Value.Path, sourcePropertyType,
                                        destinationPropertyType);
                                }
                            }
                        }
                    }
                }
                else
                {
                    if(_config.InitialyzeNullValues)
                    {
                        stack.AddAction((d, s) =>
                        {
                            if (/*proxy[d, mappingRoute.Key]*/ getter(d) == null/* && _config.InitialyzeNullValues*/)
                            {
                                var prop = destinationPropertyType;
                                //value = proxy[d, mappingRoute.Key] = Activator.CreateInstance(prop);
                                value = Activator.CreateInstance(prop);
                                setter(d, value);
                            }

                            return value;
                        });  
                    }
                    else
                    {
                        stack.AddAction((d, s) => getter(d));
                    }                  
                }

                GroupMappingRoutesCompiled(source, mappingRoute, globalSource, value, stack);

                //foreach (var route in mappingRoute)
                //{
                //    MapRemaperPropertiesNonCompiled(source, value, route);
                //}
            }

            private TDestination PerformMapping(TSource sourceObject,
                TDestination destinationObject = default(TDestination))
            {
                if (destinationObject == null || destinationObject.Equals(default(TDestination)))
                    destinationObject = new TDestination();

                var destination = _map.Destination;
                var source = _map.Source;

                MapNonRemaperPropertiesNonCompiled(sourceObject, destinationObject, destination, source);


                GroupMappingRoutes(null, _destinationMappingRoute, sourceObject, destinationObject);
                //foreach (var route in _destinationMappingRoute)
                //{
                //    MapRemaperPropertiesNonCompiled(sourceObject, destinationObject, route);
                //}

                return destinationObject;
            }

            private void MapNonRemaperPropertiesNonCompiled(TSource sourceObject, TDestination destinationObject,
                DataProxy<TDestination> destination, DataProxy<TSource> source)
            {
                var nonRemapedDests = _map.DestinationNonReMapedProperties;
                for (var index = 0; index < nonRemapedDests.Length; index++)
                {
                    var o = nonRemapedDests[index];
                    var v = source[sourceObject, o];
                    if (_config.IgnoreDefaultValues)
                    {
                        if (v != null)
                        {
                            var @default = v.GetDefault();
                            if (ReferenceEquals(v, @default)) continue;
                        }
                        else continue;
                    }
                    destination[destinationObject, o] = v;
                }

                nonRemapedDests = _map.NonResolvedProperties;
                for (var index = 0; index < nonRemapedDests.Length; index++)
                {
                    var o = nonRemapedDests[index];

                    var sourceType = source.GetPropertyInfo(o).PropertyType;
                    var destinationType = destination.GetPropertyInfo(o).PropertyType;

                    var resolver = Resolver.Create(sourceType, destinationType, this);
                    if (resolver != null)
                    {
                        object value = null;

                        var v = source[sourceObject, o];
                        if (_config.IgnoreDefaultValues)
                        {
                            if (v != null)
                            {
                                var @default = v.GetDefault();
                                if (ReferenceEquals(v, @default)) continue;
                            }
                            else continue;
                        }

                        resolver.Resolve(v, ref value);
                        destination[destinationObject, o] = value;
                    }
                    else
                    {
                        throw new InvalidTypeBindingException(o, sourceType, destinationType);
                    }
                }

                nonRemapedDests = _map.DestinationLoopProperties;
                for (var index = 0; index < nonRemapedDests.Length; index++)
                {
                    var o = nonRemapedDests[index];

                    var sourceType = source.GetPropertyInfo(o).PropertyType;
                    var destinationType = destination.GetPropertyInfo(o).PropertyType;

                    var resolver = Resolver.Create(sourceType, destinationType, this);
                    if (resolver != null)
                    {
                        object value = null;

                        var v = source[sourceObject, o];
                        if (_config.IgnoreDefaultValues)
                        {
                            if (v != null)
                            {
                                var @default = v.GetDefault();
                                if (ReferenceEquals(v, @default)) continue;
                            }
                            else continue;
                        }

                        resolver.Resolve(v, ref value);
                        destination[destinationObject, o] = value;
                    }
                    else
                    {
                        throw new InvalidTypeBindingException(o, sourceType, destinationType);
                    }
                }
            }

            private void MapNonRemaperProperties(TSource sourceObject, TDestination destinationObject,
                DataProxy<TDestination> destination, DataProxy<TSource> source)
            {
                var stack = new CallStack<TSource, TDestination>("", _callStack, destinationObject, null);

                var nonRemapedDests = _map.DestinationNonReMapedProperties;
                for (var index = 0; index < nonRemapedDests.Length; index++)
                {
                    var o = nonRemapedDests[index];
                    var getter = source.GetPlainGetter(o);
                    var setter = destination.GetPlainSetter(o);

                    if(_config.IgnoreDefaultValues)
                    {
                        stack.AddAction((x, s) =>
                        {
                            object v;
                            if (!IgnoreValue(() => source[s, o], out v))
                                destination[x, o] = v;

                            return x;
                        });
                    }
                    else
                    {
                        stack.AddAction((d, s) =>
                        {
                            setter((TDestination) d, getter((TSource) s));
                            return d;
                        });
                    }
                }

                nonRemapedDests = _map.NonResolvedProperties;
                for (var index = 0; index < nonRemapedDests.Length; index++)
                {
                    var o = nonRemapedDests[index];

                    var getter = source.GetPlainGetter(o);
                    var setter = destination.GetPlainSetter(o);

                    var sourceType = source.GetPropertyInfo(o).PropertyType;
                    var destinationType = destination.GetPropertyInfo(o).PropertyType;

                    var resolver = Resolver.Create(sourceType, destinationType, this);
                    if (resolver != null)
                    {
                        if(_config.IgnoreDefaultValues)
                        {
                            stack.AddAction((x, s) =>
                            {
                                object value = null;

                                object v;
                                if (!IgnoreValue(() => source[s, o], out v))
                                {
                                    resolver.Resolve(v, ref value);
                                    destination[x, o] = value;
                                }

                                return x;
                            });
                        }
                        else
                        {                            
                            stack.AddAction((d, s) =>
                            {
                                object value = null;
                                resolver.Resolve(getter((TSource) s), ref value);
                                setter((TDestination) d, value);

                                return d;
                            });
                        }
                    }
                    else
                    {
                        throw new InvalidTypeBindingException(o, sourceType, destinationType);
                    }
                }

                nonRemapedDests = _map.DestinationLoopProperties;
                for (var index = 0; index < nonRemapedDests.Length; index++)
                {
                    var o = nonRemapedDests[index];

                    var getter = source.GetPlainGetter(o);
                    var setter = destination.GetPlainSetter(o);

                    var sourceType = source.GetPropertyInfo(o).PropertyType;
                    var destinationType = destination.GetPropertyInfo(o).PropertyType;

                    var resolver = Resolver.Create(sourceType, destinationType, this);
                    if (resolver != null)
                    {
                        if(_config.IgnoreDefaultValues)
                        {
                            stack.AddAction((x, s) =>
                            {
                                object value = null;

                                object v;
                                if (!IgnoreValue(() => source[s, o], out v))
                                {
                                    resolver.Resolve(v, ref value);
                                    destination[x, o] = value;
                                }

                                return x;
                            });
                        }
                        else
                        {
                            stack.AddAction((d, s) =>
                            {
                                object value = null;
                                resolver.Resolve(getter((TSource) s), ref value);
                                setter((TDestination) d, value);

                                return d;
                            });
                        }
                    }
                    else
                    {
                        throw new InvalidTypeBindingException(o, sourceType, destinationType);
                    }
                }
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

            private static readonly TDestination DestinationDefault = Activator.CreateInstance<TDestination>();

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

            private readonly MappingRoute _sourceMappingRoute;
            private readonly MappingRoute _destinationMappingRoute;

            private readonly CallStack<TSource, TDestination> _callStack = new CallStack<TSource, TDestination>("", null, null, null);

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

            private IMap _compile()
            {
                if (_compiledMap != null) return this;

                MapNonRemaperProperties(default(TSource), default(TDestination), _map.Destination, _map.Source);

                GroupMappingRoutesCompiled(default(TSource), _destinationMappingRoute, default(TSource), default(TDestination), _callStack);

                Func<object, object, object> func = (s, d) =>
                {
                    if (ReferenceEquals(d, default(TDestination)))
                        d = new TDestination();

                    _callStack.GlobalSource = (TSource) s;

                    return d;
                };
                    
                //func = _callStack.Build(func);

                //_compiledMap = (source, destination) => (TDestination)func(source, destination);

                _compiledMap = (source, destination) =>
                {
                    //return (TDestination)func(source, destination);
                    if (ReferenceEquals(destination, default(TDestination)))
                        destination = new TDestination();

                    _callStack.GlobalSource = source;

                    _callStack.Call(source, destination);

                    return destination;
                };

                return this;
            }

            #endregion

            #region Do            

            /// <summary>
            ///     Executes mapping from target to destination object
            /// </summary>
            /// <param name="sourceObject"></param>
            /// <returns></returns>
            public TDestination Do(TSource sourceObject)
            {
                if (_compiledMap == null)
                    return PerformMapping(sourceObject, new TDestination());
                return _compiledMap(sourceObject, default(TDestination));
            }

            public TDestination Do(TSource sourceObject, TDestination dest)
            {
                if (_compiledMap == null)
                    return PerformMapping(sourceObject, dest);
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
                return PerformMapping((TSource) source, (TDestination) destination);
            }

            public object Do(object source)
            {
                return PerformMapping((TSource) source, new TDestination());
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

            public IMap<TSource, TDestination> Remap(Expression<Func<TSource, object>> source,
                Expression<Func<TDestination, object>> destination)
            {
                var sourceBody = source.Body as MemberExpression;
                var destinationBody = destination.Body as MemberExpression;

                if (sourceBody == null) throw new MissingMemberException();
                if (destinationBody == null) throw new MissingMemberException();

                var sourcePath =
                    sourceBody.ToString().Replace('.', _config.Separator).Split(new[] {_config.Separator}, 2)[1];

                var destinationPath =
                    destinationBody.ToString().Replace('.', _config.Separator).Split(new[] {_config.Separator}, 2)[1];

                Remap(sourcePath, destinationPath, null);

                return this;
            }

            public IMap<TSource, TDestination> Remap<TS, TR>(Expression<Func<TSource, TS>> source,
                Expression<Func<TDestination, TR>> destination, Resolver<TS, TR> resolver)
            {
                var sourceBody = source.Body as MemberExpression;
                var destinationBody = destination.Body as MemberExpression;

                if (sourceBody == null) throw new MissingMemberException();
                if (destinationBody == null) throw new MissingMemberException();

                var sourcePath =
                    sourceBody.ToString().Replace('.', _config.Separator).Split(new[] {_config.Separator}, 2)[1];

                var destinationPath =
                    destinationBody.ToString().Replace('.', _config.Separator).Split(new[] {_config.Separator}, 2)[1];

                RegisterAdditionalMap<TS, TR>(sourcePath, destinationPath, resolver);

                return this;
            }

            public IMap<TSource, TDestination> RemapFrom<TR>(Expression<Func<TDestination, TR>> destination,
                Func<TSource, TR> selector)
            {
                var destinationBody = destination.Body as MemberExpression;

                if (destinationBody == null) throw new MissingMemberException();

                var destinationPath =
                    destinationBody.ToString().Replace('.', _config.Separator).Split(new[] {_config.Separator}, 2)[1];

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
                return RegisterAdditionalMap<TR, TR>(source, destination, resolver);
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
                        .GetMethod("RegisterAdditionalMap", BindingFlags.NonPublic | BindingFlags.Instance);

                    if (resolver == null)
                    {
                        var sourceType = RouteHelpers.DetermineResultType(SourceDefault, source.Split(new []{_config.Separator.ToString()}, StringSplitOptions.RemoveEmptyEntries));
                        var destinationType = RouteHelpers.DetermineResultType(DestinationDefault,
                            destination.Split(_config.Separator));
                        registerAdditionalMap
                            .MakeGeneric(sourceType, destinationType)
                            .Invoke(this, new object[] {source, destination, resolver});
                    }
                    else
                    {
                        registerAdditionalMap
                            .MakeGeneric(resolver.SouceType, resolver.DestinationType)
                            .Invoke(this, new object[] {source, destination, resolver});
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

            private IMap RegisterAdditionalMap<TS, TR>(string source, string destination, Resolver resolver = null)
            {
                lock (this)
                {
                    if (!_map.AdditionalMaps.Any(o => o.Value.Path.Equals(destination)))
                    {
                        var destinationMapObject = CreateDestinationMapObject<TR, TR>(destination, resolver);
                        var sourceMapObject = CreateSourceMapObject<TS, TS>(source, resolver,
                            destinationMapObject.MappingRoute);

                        destinationMapObject.MappingRoute.SourceRoute = sourceMapObject.MappingRoute;

                        destinationMapObject.MappingRoute.AutoGenerated = false;
                        sourceMapObject.MappingRoute.AutoGenerated = false;

                        var mapObject =
                            new Map<MapObject<Func<TSource, object>>, MapObject<Action<TDestination, object>>>(
                                sourceMapObject, destinationMapObject);
                        _map.AdditionalMaps.Add(mapObject);
                        _map.Calculate(this);
                    }
                    else _map.AdditionalMaps.Single(o => o.Value.Path.Equals(destination)).Key.Path = source;
                }
                return this;
            }

            private IMap RegisterAdditionalFromMap<TS, TR>(Func<TSource, TR> source, string destination,
                Resolver resolver = null)
            {
                lock (this)
                {
                    if (!_map.AdditionalMaps.Any(o => o.Value.Path.Equals(destination)))
                    {
                        IEnumerable<KeyValuePair<string, string>> registredPaths = null;

                        var destinationMapObject = CreateDestinationMapObject<TR, TR>(destination, resolver);
                        var sourceMapObject = CreateSourceMapObject<TS, TR>(null, resolver,
                            destinationMapObject.MappingRoute, source);
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
                var mappingRoute = MappingRoute.Parse(destination, router, resolver: resolver, initObjects: false);
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
                        selector == null ? mappingRoute.GetConverteDelegate.As<Func<TSource, object>>() :
                            selector.Convert<TSource, TRR, TSource, object>(),
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