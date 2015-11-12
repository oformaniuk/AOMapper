using System;
using AOMapper.Data;

namespace AOMapper.Helpers
{
    public class SimpleLazy<TSource>
    {
        private readonly Func<TSource, object, MappingRoute, object> _creator;
        private object _value;
        
        public bool Initialized { get; private set; }        

        public object Get(object globalSource, object source, MappingRoute route)
        {
            if (!Initialized)
            {
                _value = _creator((TSource) globalSource, source, route);
                Initialized = true;
            }

            return _value;
        }

        public SimpleLazy(Func<TSource, object, MappingRoute, object> creator)            
        {
            _creator = creator;
            Initialized = false;
        }
    }
}