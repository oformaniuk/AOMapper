using System;
using AOMapper.Interfaces;

namespace AOMapper.Resolvers
{
    public class ActivationResolver<TSource, TDestination> : Resolver
    {        
        public override void Resolve(object source, ref object destination)
        {
            destination = Activator.CreateInstance<TDestination>();
        }

        public ActivationResolver(IMap map) 
            : base(map)
        {
            SouceType = typeof (TSource);
            DestinationType = typeof (TDestination);
        }

        public ActivationResolver(Type source, Type destination) 
            : base(source, destination)
        {
        }
    }
}