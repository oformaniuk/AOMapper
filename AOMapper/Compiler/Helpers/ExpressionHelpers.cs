using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using AOMapper.Compiler.Resolvers;
using AOMapper.Data;
using AOMapper.Data.Keys;
using AOMapper.Helpers;
using AOMapper.Interfaces;
using AOMapper.Resolvers;

namespace AOMapper.Compiler.Helpers
{
    internal static class ExpressionHelpers
    {
        public static Expression GetVariable(this MappingRoute mappingRoute, ParameterExpression parameter, IMap map,
            Dictionary<StringKey, Expression> variablesExpressions, List<Expression> blockExpressions)
        {
            var parent = mappingRoute.Parent;
            if (parent != null)
            {
                if (parent.Parent != null) // is not root
                {
                    if (!variablesExpressions.ContainsKey(parent.Route))
                    {
                        var parameterExpression = Expression.Variable(parent.Type,
                            parent.Route.Replace(map.GetConfigurationParameter(o => o.Separator).ToString(),
                                string.Empty).ToLower() + "Source");
                        variablesExpressions.Add(parent.Route, parameterExpression);

                        blockExpressions.Add(Expression.Assign(parameterExpression, parent.ToExpression(parameter, null)));

                        return parameterExpression;
                    }

                    return variablesExpressions[parent.Route];
                }
            }

            return null;
        }

        public static Expression ToAssignExpression(this MappingRoute destinationMappingRoute,
            ParameterExpression sourcExpression, ParameterExpression destinationExpression,
            Expression destinationVariableExpression, Expression sourceVariableExpression)
        {
            var sourceMappingRoute = destinationMappingRoute.SourceRoute;

            var destinationGetterExpression = destinationMappingRoute.ToExpression(destinationExpression,
                destinationVariableExpression);
            var sourceGetterExpression = sourceMappingRoute.ToExpression(sourcExpression, sourceVariableExpression);

            var resolver = destinationMappingRoute.Resolver;
            if (resolver != null)
            {
                return ResolveExpression(destinationMappingRoute, sourceMappingRoute, resolver, sourceGetterExpression,
                    destinationGetterExpression);
            }

            if (sourceMappingRoute.Type != destinationMappingRoute.Type)
            {
                resolver = Resolver.Create(sourceMappingRoute.Type, destinationMappingRoute.Type,
                    destinationMappingRoute.Map);
                return ResolveExpression(destinationMappingRoute, sourceMappingRoute, resolver, sourceGetterExpression,
                    destinationGetterExpression);
            }

            return Expression.Assign(destinationGetterExpression, sourceGetterExpression);
        }

        public static Expression ToAssignExpression(this MappingRoute destinationMappingRoute,
            ParameterExpression sourcExpression, ParameterExpression destinationExpression, Resolver resolver,
            Expression destinationVariableExpression, Expression sourceVariableExpression)
        {
            var sourceMappingRoute = destinationMappingRoute.SourceRoute;

            var destinationGetterExpression = destinationMappingRoute.ToExpression(destinationExpression,
                destinationVariableExpression);
            var sourceGetterExpression = sourceMappingRoute.ToExpression(sourcExpression, sourceVariableExpression);

            return ResolveExpression(destinationMappingRoute, sourceMappingRoute, resolver, sourceGetterExpression,
                destinationGetterExpression);
        }

        public static Expression ResolveExpression(MappingRoute destinationMappingRoute, MappingRoute sourceMappingRoute,
            Resolver resolver, MemberExpression sourceGetterExpression, MemberExpression destinationGetterExpression)
        {
            var resolverType = resolver.GetType();
            var resolveCompileTime =
                resolverType.GetProperty("CompileTimeResolver", BindingFlags.Instance | BindingFlags.NonPublic)
                    .GetValue(resolver, null);
            if (resolveCompileTime != null)
            {
                var compileTimeResolver = (CompileTimeResolver) resolveCompileTime;
                return compileTimeResolver.Resolve(destinationGetterExpression, sourceGetterExpression,
                    destinationGetterExpression, sourceGetterExpression);
            }

            if (resolver.CanConvert)
            {
                resolverType = typeof (Resolver<,>).MakeGenericType(sourceMappingRoute.Type,
                    destinationMappingRoute.Type);

                var resolveMethod = resolverType.GetMethod("Resolve", new[] {sourceMappingRoute.Type});
                var resolverInstance = Expression.Convert(Expression.Constant(resolver), resolverType);
                var resolveExpression = Expression.Call(resolverInstance, resolveMethod, sourceGetterExpression);

                return Expression.Assign(destinationGetterExpression, resolveExpression);
            }
            else
            {
                resolverType = resolver.GetType();

                //var resolveCompileTime = resolverType.GetProperty("CompileTimeResolver", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(resolver, null);
                //if (resolveCompileTime != null)
                //{
                //    var compileTimeResolver = (CompileTimeResolver)resolveCompileTime;
                //    var resolved = compileTimeResolver.Resolve(destinationMappingRoute, destinationGetterExpression,
                //        sourceGetterExpression);
                //}

                var resolveMethod = resolverType.GetMethod("Resolve", new[] {typeof (object)});
                var resolverInstance = Expression.Constant(resolver);
                var resolveExpression = Expression.Call(resolverInstance, resolveMethod, sourceGetterExpression);

                return Expression.Assign(destinationGetterExpression,
                    Expression.Convert(resolveExpression, resolver.DestinationType));
            }
        }

        public static Expression ResolveExpression(this MappingRoute destinationMappingRoute,
            ParameterExpression destinationParameterExpression,
            ParameterExpression sourceParameterExpression, Resolver resolver, Expression destinationVariableExpression)
        {
            var destinationGetterExpression = destinationMappingRoute.ToExpression(destinationParameterExpression,
                destinationVariableExpression);
            var sourceGetterExpression = destinationMappingRoute.SourceRoute != null
                ? destinationMappingRoute.SourceRoute.ToExpression(sourceParameterExpression, null)
                : null;

            var resolverType = resolver.GetType();

            var resolveCompileTime =
                resolverType.GetProperty("CompileTimeResolver", BindingFlags.Instance | BindingFlags.NonPublic)
                    .GetValue(resolver, null);
            if (resolveCompileTime != null)
            {
                var compileTimeResolver = (CompileTimeResolver) resolveCompileTime;
                return compileTimeResolver.Resolve(destinationGetterExpression,
                    sourceGetterExpression ?? (Expression) sourceParameterExpression, destinationGetterExpression,
                    sourceGetterExpression);
            }

            resolverType = typeof (Resolver<,>).MakeGenericType(resolver.SouceType,
                resolver.DestinationType);

            var resolveMethod = resolverType.GetMethod("Resolve", new[] {resolver.SouceType});
            var resolverInstance = Expression.Convert(Expression.Constant(resolver), resolverType);
            var resolveExpression = Expression.Call(resolverInstance, resolveMethod, sourceParameterExpression);

            return Expression.Assign(destinationGetterExpression, resolveExpression);
        }

        public static Expression ResolveExpression(Type destinationType, Type sourceType,
            Resolver resolver, MemberExpression sourceGetterExpression, MemberExpression destinationGetterExpression)
        {
            var resolverType = resolver.GetType();

            var resolveCompileTime =
                resolverType.GetProperty("CompileTimeResolver", BindingFlags.Instance | BindingFlags.NonPublic)
                    .GetValue(resolver, null);
            if (resolveCompileTime != null)
            {
                var compileTimeResolver = (CompileTimeResolver) resolveCompileTime;
                return compileTimeResolver.Resolve(destinationGetterExpression, sourceGetterExpression,
                    destinationGetterExpression, sourceGetterExpression);
            }

            if (resolver.CanConvert)
            {
                resolverType = typeof (Resolver<,>).MakeGenericType(resolver.SouceType,
                    resolver.DestinationType);

                var resolveMethod = resolverType.GetMethod("Resolve", new[] {sourceType});
                var resolverInstance = Expression.Convert(Expression.Constant(resolver), resolverType);
                var resolveExpression = Expression.Call(resolverInstance, resolveMethod, sourceGetterExpression);

                return Expression.Assign(destinationGetterExpression, resolveExpression);
            }
            else
            {
                resolverType = resolver.GetType();

                //var resolveCompileTime = resolverType.GetProperty("CompileTimeResolver", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(resolver, null);
                //if (resolveCompileTime != null)
                //{
                //    var compileTimeResolver = (CompileTimeResolver)resolveCompileTime;
                //    return compileTimeResolver.Resolve(destinationGetterExpression, sourceGetterExpression, destinationGetterExpression, sourceGetterExpression);
                //}

                var resolveMethod = resolverType.GetMethod("Resolve", new[] {typeof (object)});
                var resolverInstance = Expression.Constant(resolver);
                var resolveExpression = Expression.Call(resolverInstance, resolveMethod, sourceGetterExpression);

                return Expression.Assign(destinationGetterExpression,
                    Expression.Convert(resolveExpression, destinationType));
            }
        }

        public static MemberExpression ToExpression(this MappingRoute mappingRoute,
            ParameterExpression parameterExpression, Expression variable)
        {
            if (variable != null)
            {
                var map = mappingRoute.Map;
                var routeParts = RouteHelpers.Parse(map, mappingRoute.Route);

                var getterExpression = Expression.Property(variable, routeParts.Last());

                return getterExpression;
            }
            else
            {
                var map = mappingRoute.Map;
                var routeParts = RouteHelpers.Parse(map, mappingRoute.Route);

                var getterExpression = Expression.Property(parameterExpression, routeParts[0]);
                for (var i = 1; i < routeParts.Length; i++)
                {
                    getterExpression = Expression.Property(getterExpression, routeParts[i]);
                }

                return getterExpression;
            }
        }
    }
}