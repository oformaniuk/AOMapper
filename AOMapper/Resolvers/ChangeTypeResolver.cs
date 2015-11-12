using System;
using AOMapper.Exceptions;
using AOMapper.Helpers;
using AOMapper.Interfaces;

namespace AOMapper.Resolvers
{
    public class ChangeTypeResolver<TS, TD> : Resolver 
    {        
        public ChangeTypeResolver(IMap map) : base(map)
        {
        }

        public ChangeTypeResolver(Type source, Type destination) : base(source, destination)
        {
        }

        public ChangeTypeResolver(IMap map, Type source, Type destination) : base(map, source, destination)
        {            
        }

        public override void Resolve(object source, ref object destination)
        {
            try
            {
                destination = Convert.ChangeType(source, DestinationType);
            }
            catch (Exception e)
            {
                throw new InvalidTypeBindingException("", e, "", SouceType, DestinationType);
            }
        }
    }
}