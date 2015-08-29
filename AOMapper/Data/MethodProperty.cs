using System;

namespace AOMapper.Data
{
    public struct MethodProperty
    {        
        public System.Reflection.MethodInfo Info { get; set; }
        public Delegate Delegate { get; set; }
    }
}