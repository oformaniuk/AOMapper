using System;
using AOMapper.Compiler.Resolvers;
using AOMapper.Exceptions;
using AOMapper.Helpers;
using AOMapper.Interfaces;

namespace AOMapper.Resolvers
{
    public class ChangeTypeResolver<TS, TD> : Resolver<TS, TD> 
        where TS : IConvertible
        where TD : IConvertible
    {        
        public ChangeTypeResolver(IMap map) : base(map)
        {
            CompileTimeResolver = new ChangeTypeCompileTimeResolver<TS, TD>(_map, this);
        }

        public ChangeTypeResolver(Type source, Type destination) : base(source, destination)
        {
            CompileTimeResolver = new ChangeTypeCompileTimeResolver<TS, TD>(_map, this);
        }

        public ChangeTypeResolver(IMap map, Type source, Type destination) : base(map, source, destination)
        {
            CompileTimeResolver = new ChangeTypeCompileTimeResolver<TS, TD>(_map, this);
        }

        public override TD Resolve(TS source)
        {
#if !PORTABLE
            return (TD) Convert.ChangeType(source, DestinationType);
#else
            return (TD)Convert.ChangeType(source, DestinationType, null);
#endif            
        }        
    }
}