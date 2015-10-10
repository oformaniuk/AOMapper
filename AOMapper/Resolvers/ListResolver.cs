using System;
using System.Collections;
using System.Collections.Generic;
using AOMapper.Interfaces;

namespace AOMapper.Resolvers
{
    public class ListResolver<TS, TD> : Resolver
    {        
        public ListResolver(IMap map)
            :base(map)
        {            
        }


        public override void Resolve(object source, ref object destination)
        {
            var _source = source as Array;
            var _destination = (TD[])destination;
            Array.Resize<TD>(ref _destination, _source.Length);
            for (int index = 0; index < _source.Length; index++)
            {                
                var s = (TS)_source.GetValue(index);
                _destination.SetValue(_map.Do<TS, TD>(s), index);
            }
        }
    }
}