using System;

namespace AOMapper
{
    internal class MapObject<TF>
    {
        public string Path { get; set; }
        public TF Invoker { get; set; }
        public Delegate LastInvokeTarget { get; set; }
    }    
}