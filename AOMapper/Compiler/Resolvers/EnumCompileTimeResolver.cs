using System;
using System.Linq.Expressions;
using System.Reflection;
using AOMapper.Compiler.Helpers;
using AOMapper.Helpers;
using AOMapper.Interfaces;
using AOMapper.Resolvers;

namespace AOMapper.Compiler.Resolvers
{
    public class EnumCompileTimeResolver<TS, TD> : CompileTimeResolver
    {
        public EnumCompileTimeResolver(IMap map, Resolver parent) : base(map, parent)
        {
        }

        public EnumCompileTimeResolver(IMap map, Resolver parent, Expression resolver) : base(map, parent, resolver)
        {
        }

        public override Expression Resolve(MemberExpression destinationExpression, Expression sourceExpression, Expression destinationParameterExpression, Expression sourceParameterExpression)
        {            
            Expression<Func<TS, TD>> castMethodExpression = s => CastTo<TD>.From(s);
            
            var resolveExpression = Expression.Assign(destinationExpression, new ExpressionRewriter().AutoInline(Expression.Invoke(castMethodExpression, sourceExpression)));           
            return resolveExpression;
        }
    }
}