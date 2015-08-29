using System;
using System.Collections;
using AOMapper.Interfaces;

namespace AOMapper.Resolvers
{
    public class ArrayResolver<TS, TD> : Resolver
    {        
        public ArrayResolver(IMap map) : base(map)
        {            
        }

        public override void Resolve(IList source, ref IList destination)
        {
            var _destination = destination;
            if (_destination.IsFixedSize)
            {
                for (int i = 0; i < source.Count; i++)
                {
                    _destination[i] = _map.Do<TS, TD>((TS)source[i]);
                }
            }
            else
            {
                for (int i = 0; i < source.Count; i++)
                {                                    
                    _destination.Add(_map.Do<TS, TD>((TS)source[i]));
                }            
            }
            destination = _destination;                                             
        }
    }
}