using System;

namespace AOMapper.Helpers
{
    public class SimpleLazy
    {
        private readonly Func<object> _creator;
        private object _value;
        
        public bool Initialized { get; private set; }

        public object Value
        {
            get
            {
                if (Initialized) return _value;

                _value = _creator();
                Initialized = true;

                return _value;
            }
        }

        public SimpleLazy(Func<object> creator)            
        {
            _creator = creator;
            Initialized = false;
        }
    }
}