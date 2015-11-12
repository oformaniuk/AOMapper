using System;
using System.ComponentModel;
using AOMapper.Helpers;
using AOMapper.Interfaces;

namespace AOMapper.Resolvers
{
    public class EnumResolver<TS, TD> : Resolver
    {             
        public EnumResolver(IMap map) : base(map)
        {
        }

        public EnumResolver(Type source, Type destination) : base(source, destination)
        {
        }

        public EnumResolver(IMap map, Type source, Type destination) : base(map, source, destination)
        {                        
        }

        public override void Resolve(object source, ref object destination)
        {
            destination = CastTo<TD>.From((TS)source);
        }
    }
}