using System.Linq.Expressions;
using AOMapper.Interfaces;
using AOMapper.Resolvers;

namespace AOMapper.Compiler.Resolvers
{
    public class SimpleCompileTimeResolver : CompileTimeResolver
    {
        public SimpleCompileTimeResolver(IMap map, Resolver parent, Expression resolver)
            : base(map, parent, resolver)
        {
        }

        public override Expression Resolve(MemberExpression destinationExpression, Expression sourceExpression,
            Expression destinationParameterExpression, Expression sourceParameterExpression)
        {
            var resolverBody = (LambdaExpression) _resolver;
            var parameterExpression = resolverBody.Parameters[0];
            var block = Expression.Block(new[] {parameterExpression},
                Expression.Assign(parameterExpression, sourceExpression),
                Expression.Assign(destinationExpression, resolverBody.Body));
            return block;
        }
    }
}