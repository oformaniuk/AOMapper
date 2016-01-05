using System;
using System.Linq;
using AOMapper.Extensions;
using AOMapper.Interfaces;

namespace AOMapper.Resolvers
{
    internal class IEnumerableResolver : Resolver
    {
        public IEnumerableResolver(Type source, Type destination) : base(source, destination)
        {
        }

        public IEnumerableResolver(IMap map, Type source, Type destination) : base(map, source, destination)
        {
        }

        public IEnumerableResolver(IMap map) : base(map)
        {
        }

        public static Resolver CreateIEnumerable(Type resolver, Type source, Type destination, IMap map)
        {
            var destArg = destination.IsArray
                ? destination.GetElementType()
                : destination.GetGenericArguments().Single();
            var sPropArg = source.IsArray ? source.GetElementType() : source.GetGenericArguments().Single();

            return (Resolver) (resolver ?? typeof (ArrayResolver<,>))
                .MakeGenericType(sPropArg, destArg)
                .Create(map, source, destination);
        }

        public override Resolver Create(Type resolver, Type source, Type destination, IMap map)
        {
            return CreateIEnumerable(resolver, source, destination, map);
        }

        public override object Resolve(object source)
        {
            throw new NotSupportedException("Use CreateIEnumerable to create target resolver");
        }
    }
}