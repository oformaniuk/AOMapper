using System.Linq.Expressions;
using AOMapper.Interfaces;
using AOMapper.Resolvers;

namespace AOMapper.Compiler.Resolvers
{
    public abstract class CompileTimeResolver
    {
        protected readonly IMap _map;
        protected readonly Resolver _parent;
        protected readonly Expression _resolver;

        protected CompileTimeResolver(IMap map, Resolver parent)
        {
            _map = map;
            _parent = parent;
        }

        protected CompileTimeResolver(IMap map, Resolver parent, Expression resolver)
            : this(map, parent)
        {
            _resolver = resolver;
        }

        public Expression GetExpression()
        {
            return _resolver;
        }

        public abstract Expression Resolve(MemberExpression destinationExpression, Expression sourceExpression,
            Expression destinationParameterExpression, Expression sourceParameterExpression);
    }

    public abstract class CompileTimeResolver<TS, TD> : CompileTimeResolver
    {
        protected CompileTimeResolver(IMap map, Resolver parent) : base(map, parent)
        {
        }
    }
}