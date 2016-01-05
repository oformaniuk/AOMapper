using System;
using System.Linq.Expressions;
using AOMapper.Compiler.Helpers;
using AOMapper.Interfaces;
using AOMapper.Resolvers;

namespace AOMapper.Compiler.Resolvers
{
    public class ChangeTypeCompileTimeResolver<TS, TD> : CompileTimeResolver<TS, TD>
    {
        public ChangeTypeCompileTimeResolver(IMap map, Resolver parent) : base(map, parent)
        {
        }

        public override Expression Resolve(MemberExpression destinationExpression, Expression sourceExpression,
            Expression destinationParameterExpression, Expression sourceParameterExpression)
        {
            var conversionType = typeof(TD);
            if (typeof (TS) == conversionType)
                return sourceExpression;

            Expression<Func<TS, TD>> convert = s => (TD) Convert.ChangeType(s, conversionType);
            var resolveExpression = Expression.Assign(destinationExpression, new ExpressionRewriter().AutoInline(Expression.Invoke(convert, sourceExpression)));
            return resolveExpression;
        }
    }
}