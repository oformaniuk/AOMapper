using System;
using AOMapper.Compiler.Resolvers;
using AOMapper.Interfaces;

namespace AOMapper.Resolvers
{
    public class ActivationResolver<TSource, TDestination> : 
        Resolver<TSource, TDestination>
        //where TDestination : new ()
    {
        public override TDestination Resolve(TSource source)
        {
            return Activator.CreateInstance<TDestination>();
        }        

        public ActivationResolver(IMap map) : base(map)
        {
            
        }

        public ActivationResolver(Type source, Type destination) : base(source, destination)
        {
        }

        public ActivationResolver(IMap map, Type source, Type destination) : base(map, source, destination)
        {
        }
    }
}