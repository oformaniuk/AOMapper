using System;
using System.Reflection;

namespace AOMapper.Data
{
    public struct MethodProperty
    {
        public MethodInfo Info { get; set; }
        public Delegate Delegate { get; set; }
    }
}