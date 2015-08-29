using System;
using System.Collections;
using System.Linq;
using AOMapper.Extensions;
using AOMapper.Interfaces;

namespace AOMapper.Resolvers
{
    public abstract class Resolver
    {
        protected readonly IMap _map;
        protected readonly Type SouceType;
        protected readonly Type DestinationType;

        public static Resolver Create(Type resolver, Type source, Type destination, IMap map)
        {
            var destArg = destination.IsArray ? destination.GetElementType() : destination.GetGenericArguments().Single();
            var sPropArg = source.IsArray ? source.GetElementType() : source.GetGenericArguments().Single();

            return (Resolver)resolver.MakeGenericType(sPropArg, destArg)
                .Create(map);
        }

        public abstract void Resolve(IList source, ref IList destination);

        protected Resolver(IMap map)
        {
            _map = map;
        }
    }
}