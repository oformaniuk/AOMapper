using System;
using AOMapper.Data.Keys;
using AOMapper.Resolvers;

namespace AOMapper.Data
{
    public class CodeTreeNode : AbstractKey<string>
    {
        public TypeKey Type { get; private set; }

        public Resolver Resolver { get; internal set; }

        public CodeTreeNode(string value, Type type)
            : this(value)
        {
            Type = type;
        }

        public CodeTreeNode(string value) : base(value)
        {            
        }
    }
}