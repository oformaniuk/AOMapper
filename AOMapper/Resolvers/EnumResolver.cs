using System;
using System.ComponentModel;
using System.Reflection;
using AOMapper.Compiler.Resolvers;
using AOMapper.Helpers;
using AOMapper.Interfaces;

namespace AOMapper.Resolvers
{
    public class EnumResolver<TS, TD> : Resolver<TS, TD>
    {             
        public EnumResolver(IMap map) 
            : this(map, null, null)
        {
        }

        public EnumResolver(Type source, Type destination) 
            : this(null, source, destination)
        {
        }

        public EnumResolver(IMap map, Type source, Type destination) 
            : base(map, source, destination)
        {     
            CompileTimeResolver = new EnumCompileTimeResolver<TS, TD>(_map, this);       
        }

        public override TD Resolve(TS source)
        {            
            return CastTo<TD>.From(source);
        }        
    }
}