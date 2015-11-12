using System;
using System.Diagnostics;
using AOMapper.Resolvers;

namespace AOMapper.Data
{
    [DebuggerDisplay("{Path}")]
    internal class MapObject
    {
        public MappingRoute MappingRoute { get; set; }
        public string Path { get; set; }        
        public Delegate LastInvokeTarget { get; set; }
        public Resolver Resolver { get; set; }
        public Type Type { get; set; }

        protected bool Equals(MapObject other)
        {
            return string.Equals(Path, other.Path);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((MapObject) obj);
        }

        public override int GetHashCode()
        {
            return (Path != null ? Path.GetHashCode() : 0);
        }
    }   

    [DebuggerDisplay("{Path}")]
    internal class MapObject<TF> : MapObject
    {        
        public TF Invoker { get; set; }        
    }    
}