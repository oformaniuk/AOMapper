using System;
using AOMapper.Data.Keys;
using AOMapper.Resolvers;

namespace AOMapper.Data
{
    public class CodeTreeNode : AbstractKey<string>
    {
        public CodeTreeNode(string value, Type type)
            : this(value)
        {
        }

        public CodeTreeNode(string value)
            : this(value, null, null)
        {
        }

        public CodeTreeNode(string value, Resolver resolver)
            : this(value, null, resolver)
        {
        }

        public CodeTreeNode(string value, Type type, Resolver resolver)
            : base(value)
        {
            Resolver = resolver;
            Type = type;
        }

        public TypeKey Type { get; private set; }

        public Resolver Resolver { get; internal set; }
    }
}