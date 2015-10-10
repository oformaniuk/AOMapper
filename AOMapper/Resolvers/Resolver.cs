using System;
using System.Collections;
using System.Collections.Generic;
#if !NET35
using System.Diagnostics.Contracts;
#endif
using AOMapper.Data.Keys;
using AOMapper.Extensions;
using AOMapper.Interfaces;

namespace AOMapper.Resolvers
{
    public class Resolver<TS, TD> : Resolver
    {
        private Func<TS, TD> _resolver;  

        public Resolver(Func<TS, TD> resolver) 
            : base(typeof(TS), typeof(TD))
        {
            _resolver = resolver;            
        }

        public override void Resolve(object source, ref object destination)
        {
            destination = _resolver((TS)source);
        }

        public static implicit operator Resolver<TS, TD>(Func<TS, TD> resolver)
        {
            return new Resolver<TS, TD>(resolver);
        }
    }

    public abstract class Resolver
    {
        protected static Dictionary<IMap, Dictionary<KeyValuePair<TypeKey, TypeKey>, Resolver>> ResolverMapping
            = new Dictionary<IMap, Dictionary<KeyValuePair<TypeKey, TypeKey>, Resolver>>();

#if !NET45 
        internal static Dictionary<KeyValuePair<TypeKey, TypeKey>, Resolver> GetResolvers(IMap map)
#else
        internal static IReadOnlyDictionary<KeyValuePair<TypeKey, TypeKey>, Resolver> GetResolvers(IMap map)
#endif
        {
            if (!ResolverMapping.ContainsKey(map)) 
                ResolverMapping[map] = new Dictionary<KeyValuePair<TypeKey, TypeKey>, Resolver>();
            return ResolverMapping[map];
        }

        internal static void Clear()
        {
            ResolverMapping.Clear();
        }

        internal static void Clear(IMap map)
        {
            if(ResolverMapping.ContainsKey(map))
                ResolverMapping[map].Clear();
        }

        public static void RegisterResolver(IMap map, Resolver resolver)
        {
            if (!ResolverMapping.ContainsKey(map)) 
                ResolverMapping[map] = new Dictionary<KeyValuePair<TypeKey, TypeKey>, Resolver>();
            ResolverMapping[map][new KeyValuePair<TypeKey, TypeKey>(resolver.SouceType, resolver.DestinationType)] = resolver;
        }

        protected readonly IMap _map;
        public readonly Type SouceType;
        public readonly Type DestinationType;
        private static readonly TypeKey TypeOfIList = typeof (IList);

        public static Resolver Create(Type source, Type destination, IMap map, Type resolver = null)
        {          
            if(!ResolverMapping.ContainsKey(map)) 
                ResolverMapping[map] = new Dictionary<KeyValuePair<TypeKey, TypeKey>, Resolver>();

            var valuePair = new KeyValuePair<TypeKey, TypeKey>(source, destination);
            if (ResolverMapping[map].ContainsKey(valuePair))
            {
                return ResolverMapping[map][valuePair];
            }
            
            Resolver res;
            if (TypeOfIList.Value.IsAssignableFrom(source) && TypeOfIList.Value.IsAssignableFrom(destination))
            {                
                res = IEnumerableResolver.CreateIEnumerable(resolver, source, destination, map);
            }            
            else if(resolver != null)
            {
                res = (Resolver)resolver.MakeGenericType(source, destination)
                    .Create(map);
            }
            else
            {
                return null;
            }

            ResolverMapping[map].Add(valuePair, res);
            return res;
        }

        public abstract void Resolve(object source, ref object destination);

        protected Resolver(IMap map = null)
        {
            _map = map;
        }

        protected Resolver(Type source, Type destination)
        {
            SouceType = source;
            DestinationType = destination;
        }
    }
}