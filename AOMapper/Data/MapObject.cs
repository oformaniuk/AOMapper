using System;

namespace AOMapper.Data
{
    internal class MapObject<TF>
    {
        public string Path { get; set; }
        public TF Invoker { get; set; }
        public Delegate LastInvokeTarget { get; set; }
    }    
}