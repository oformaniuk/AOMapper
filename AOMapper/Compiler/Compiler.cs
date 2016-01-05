using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using AOMapper.Compiler.Helpers;
using AOMapper.Data;
using AOMapper.Data.Keys;
using AOMapper.Exceptions;
using AOMapper.Resolvers;

namespace AOMapper.Compiler
{
    internal class Compiler<TSource, TDestination>
        //where TDestination : new()
    {
        #region ctor's

        public Compiler(Mapper.MapperInnerClass<TSource, TDestination> map)
        {
            _map = map;
            _sourceParameter = Expression.Parameter(typeof (TSource), "source");
            _destinationParameter = Expression.Parameter(typeof (TDestination), "destination");
            _blockExpressions = new List<Expression>();
            _variablesExpressions = new Dictionary<StringKey, Expression>();
            _sourceVariablesExpressions = new Dictionary<StringKey, Expression>();
        }

        #endregion

        #region Members

        private readonly Mapper.MapperInnerClass<TSource, TDestination> _map;
        private readonly ParameterExpression _sourceParameter;
        private readonly ParameterExpression _destinationParameter;
        private BlockExpression _bodyExpression;

        private readonly List<Expression> _blockExpressions;
        private readonly Dictionary<StringKey, Expression> _variablesExpressions;
        private readonly Dictionary<StringKey, Expression> _sourceVariablesExpressions;

        #endregion

        #region Expression tree methods

        private void Prepare()
        {
            // TODO: rewrite to initialize only in case of default value
            if (_map.Constructor == null)
                _blockExpressions.Add(Expression.Assign(_destinationParameter, Expression.New(typeof (TDestination))));
            else
            {
                var constructor = _map.Constructor as LambdaExpression;
                if (constructor != null)
                {                    
                    var parameterExpression = constructor.Parameters[0];
                    _blockExpressions.Add(
                        Expression.Block(
                            new[] {parameterExpression},
                            Expression.Assign(parameterExpression, _sourceParameter),
                            Expression.Assign(_destinationParameter,
                                new ExpressionRewriter().AutoInline(Expression.Invoke(constructor, _sourceParameter)))
                            )
                        );
                }
                else
                {
                    _blockExpressions.Add(Expression.Assign(_destinationParameter, _map.Constructor));
                }
            }

            PrepareNoRemapedProperties();
            PrepareNoResolverProperties();
            PrepareLoopProperties();

            PrepareRemapedProperties();

            PrepareReturnStatement();

            BuildBody();
        }

        public Func<TSource, TDestination, TDestination> Compile()
        {
            ParameterExpression sourceParameterExpression, destinationParameterExpression;
            var body = GetCompileReadyExpression(out sourceParameterExpression, out destinationParameterExpression);
            return
                Expression.Lambda<Func<TSource, TDestination, TDestination>>(body, _sourceParameter,
                    _destinationParameter)
                    .Compile();
        }

        public Expression GetCompileReadyExpression(out ParameterExpression sourceParameterExpression,
            out ParameterExpression destinationParameterExpression)
        {
            sourceParameterExpression = _sourceParameter;
            destinationParameterExpression = _destinationParameter;

            if (_bodyExpression != null) return _bodyExpression;

            Prepare();

            //var visitor = new ExpressionVisitor();

            //_bodyExpression = (BlockExpression) visitor.Visit(_bodyExpression);

            return _bodyExpression;
        }

        #endregion

        #region Prepare no remaped properties

        /*
         * TODO
         * Need to improve code to add back support for default values ignoring
         */

        private void PrepareNoRemapedProperties()
        {
            var nonRemapedDests = _map._map.DestinationNonReMapedProperties;
            for (var index = 0; index < nonRemapedDests.Length; index++)
            {
                var o = nonRemapedDests[index];
                var sourcePropertyExpression = Expression.Property(_sourceParameter, o);
                var destinationPropertyExpression = Expression.Property(_destinationParameter, o);
                var assign = Expression.Assign(destinationPropertyExpression, sourcePropertyExpression);
                _blockExpressions.Add(assign);
            }
        }

        private void PrepareNoResolverProperties()
        {
            var nonRemapedDests = _map._map.NonResolvedProperties;
            PreparePropertiesWithResolver(nonRemapedDests);
        }

        private void PrepareLoopProperties()
        {
            var nonRemapedDests = _map._map.DestinationLoopProperties;
            PreparePropertiesWithResolver(nonRemapedDests);
        }

        private void PreparePropertiesWithResolver(string[] nonRemapedDests)
        {
            for (var index = 0; index < nonRemapedDests.Length; index++)
            {
                var o = nonRemapedDests[index];

                var sourceType = _map._map.Source.GetPropertyInfo(o).PropertyType;
                var destinationType = _map._map.Destination.GetPropertyInfo(o).PropertyType;

                var resolver = Resolver.Create(sourceType, destinationType, _map);
                if (resolver != null)
                {
                    var sourcePropertyExpression = Expression.Property(_sourceParameter, o);
                    var destinationPropertyExpression = Expression.Property(_destinationParameter, o);

                    var resolve = ExpressionHelpers.ResolveExpression(destinationType, sourceType, resolver,
                        sourcePropertyExpression,
                        destinationPropertyExpression);
                    _blockExpressions.Add(resolve);
                }
                else
                {
                    throw new InvalidTypeBindingException(o, sourceType, destinationType);
                }
            }
        }

        #endregion

        #region Prepare remaped properties

        private void PrepareRemapedProperties()
        {
            foreach (var route in _map._destinationMappingRoute)
            {
                var mmap = _map._map.AdditionalMaps
                    .FirstOrDefault(o => o.Value.Path.Equals(route.Route));

                MapRemaperProperties(route, mmap);
            }
        }

        private void MapRemaperProperties(MappingRoute mappingRoute,
            Map<MapObject<Func<TSource, object>>, MapObject<Action<TDestination, object>>> map,
            Expression variableExpression = null)
        {
            var proxy = mappingRoute._dataProxy;

            var destinationPropertyType = proxy.GetPropertyInfo(mappingRoute.Key).PropertyType;

            if (map != null)
            {
                if (map.Key.Path == null)
                {
                    var expr = mappingRoute.ResolveExpression(_destinationParameter, _sourceParameter, map.Key.Resolver,
                        variableExpression);
                    _blockExpressions.Add(expr);
                }
                else
                {
                    if (map.Key.Resolver != null)
                    {
                        var expr = mappingRoute.ToAssignExpression(_sourceParameter, _destinationParameter,
                            map.Key.Resolver, variableExpression,
                            mappingRoute.SourceRoute.GetVariable(_sourceParameter, _map, _sourceVariablesExpressions,
                                _blockExpressions));
                        _blockExpressions.Add(expr);
                    }
                    else
                    {
                        var sourcePropertyType = map.Key.Type;
                        var canMap = sourcePropertyType == destinationPropertyType;
                        if (canMap)
                        {
                            var expr = mappingRoute.ToAssignExpression(_sourceParameter, _destinationParameter,
                                variableExpression,
                                mappingRoute.SourceRoute.GetVariable(_sourceParameter, _map, _sourceVariablesExpressions,
                                    _blockExpressions));
                            _blockExpressions.Add(expr);
                        }
                        else
                        {
                            var resolver = Resolver.Create(sourcePropertyType, destinationPropertyType, _map);
                            if (resolver != null)
                            {
                                var expr = mappingRoute.ToAssignExpression(_sourceParameter, _destinationParameter,
                                    resolver, variableExpression,
                                    mappingRoute.SourceRoute.GetVariable(_sourceParameter, _map,
                                        _sourceVariablesExpressions, _blockExpressions));
                                _blockExpressions.Add(expr);
                            }
                            else
                            {
                                throw new InvalidTypeBindingException(map.Value.Path, sourcePropertyType,
                                    destinationPropertyType);
                            }
                        }
                    }
                }
            }
            else
            {
                // initializing null property
                var propertyExpression = mappingRoute.ToExpression(_destinationParameter, variableExpression);
                var ifNullExpression = Expression.IfThen(
                    Expression.Equal(propertyExpression, Expression.Constant(null)),
                    Expression.Assign(propertyExpression, Expression.New(destinationPropertyType)));

                _blockExpressions.Add(ifNullExpression);
            }

            if (!mappingRoute.Any()) return;

            if (!_variablesExpressions.ContainsKey(mappingRoute.Route))
            {
                var parameterExpression = Expression.Variable(mappingRoute.Type,
                    mappingRoute.Route.Replace(_map.GetConfigurationParameter(o => o.Separator).ToString(), string.Empty)
                        .ToLower() + "Destination");
                _variablesExpressions.Add(mappingRoute.Route, parameterExpression);

                _blockExpressions.Add(Expression.Assign(parameterExpression,
                    mappingRoute.ToExpression(_destinationParameter, null)));
            }

            var variable = _variablesExpressions[mappingRoute.Route];

            foreach (var route in mappingRoute)
            {
                var mmap = _map._map.AdditionalMaps
                    .FirstOrDefault(o => o.Value.Path.Equals(route.Route));

                MapRemaperProperties(route, mmap, variable);
            }
        }

        private void PrepareReturnStatement()
        {
            var returnTarget = Expression.Label(typeof (TDestination));

            var returnExpression = Expression.Return(returnTarget,
                _destinationParameter, typeof (TDestination));

            var returnLabel = Expression.Label(returnTarget, Expression.Default(typeof (TDestination)));

            _blockExpressions.Add(returnExpression);
            _blockExpressions.Add(returnLabel);
        }

        private void BuildBody()
        {
            var variables =
                new List<ParameterExpression>(_variablesExpressions.Select(o => o.Value).Cast<ParameterExpression>());
            variables.AddRange(_sourceVariablesExpressions.Select(o => o.Value).Cast<ParameterExpression>());

            _bodyExpression = Expression.Block(variables, _blockExpressions);
        }

        #endregion
    }
}