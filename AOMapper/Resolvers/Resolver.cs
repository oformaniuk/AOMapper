using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using AOMapper.Compiler.Resolvers;
#if !NET35
using System.Diagnostics.Contracts;
#endif
using AOMapper.Data.Keys;
using AOMapper.Extensions;
using AOMapper.Interfaces;

namespace AOMapper.Resolvers
{
    public class SimpleResolver<TS, TD> : Resolver<TS, TD>
    {
        private readonly Func<TS, TD> _resolver;

        public SimpleResolver(Expression<Func<TS, TD>> resolver) 
            : base(typeof(TS), typeof(TD))
        {
            _resolver = resolver.Compile();
            CompileTimeResolver = new SimpleCompileTimeResolver(_map, this, resolver);
        }        

        public override TD Resolve(TS source)
        {
            return _resolver(source);
        }        

        public static implicit operator SimpleResolver<TS, TD>(Expression<Func<TS, TD>> resolver)
        {
            return new SimpleResolver<TS, TD>(resolver);
        }        
    }

    public abstract class Resolver<TS, TD> : Resolver
    {
        public abstract TD Resolve(TS source);        

        public override object Resolve(object source)
        {
            return Resolve((TS) source);
        }

        protected Resolver(IMap map) : base(map)
        {
        }

        protected Resolver(Type source, Type destination) : base(source, destination)
        {
        }

        protected Resolver(IMap map, Type source, Type destination) : base(map, source, destination)
        {
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
        public Type SouceType { get; protected set; }
        public Type DestinationType { get; protected set; }
        internal static readonly TypeKey TypeOfIList = typeof (IList);

        internal CompileTimeResolver CompileTimeResolver { get; set; }

        public virtual Resolver Create(Type resolver, Type source, Type destination, IMap map)
        {
            return this;
        }

        public static Resolver Create(Type source, Type destination, IMap map, Type resolver = null)
        {          
            if(!ResolverMapping.ContainsKey(map)) 
                ResolverMapping[map] = new Dictionary<KeyValuePair<TypeKey, TypeKey>, Resolver>();

            var valuePair = new KeyValuePair<TypeKey, TypeKey>(source, destination);
            if (ResolverMapping[map].ContainsKey(valuePair))
            {
                return ResolverMapping[map][valuePair];
            }

            var type = ResolverFactory.Get(source, destination, resolver);
            if(type != null)
            {
                if (type.IsGenericType) type = type.MakeGenericType(source, destination);

                var res = ((Resolver) type.Create(map, source, destination))
                    .Create(resolver, source, destination, map);

                ResolverMapping[map].Add(valuePair, res);
                return res;
            }

            return null;
        }

        public virtual bool CanConvert { get { return true; } }

        public abstract object Resolve(object source);

        protected Resolver(IMap map)
            :this(map, null, null)
        {            
        }

        protected Resolver(Type source, Type destination)
            :this(null, source, destination)
        {            
        }

        protected Resolver(IMap map, Type source, Type destination)
        {
            _map = map;
            SouceType = source;
            DestinationType = destination;            
        }
    }
}