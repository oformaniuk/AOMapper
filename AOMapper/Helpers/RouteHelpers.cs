using System;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using AOMapper.Data.Keys;
using AOMapper.Interfaces;

namespace AOMapper.Helpers
{
    internal static class RouteHelpers
    {
        public static string[] Parse(IMap map, string value)
        {
            var separator = map.GetConfigurationParameter(config => config.Separator);

            return value.Split(separator);
        }        

        public static string GetParent(IMap map, string value)
        {
            string separator = map.GetConfigurationParameter(config => config.Separator).ToString();

            var parts = Parse(map, value);
            return string.Join(separator, parts.Take(parts.Length - 2).ToArray());
        }

        public static Expression<Func<T, TR>> ConvertRouteToFuncExpression<T, TR>(IMap map, string route, Type convertTo = null)
        {
            var routeParts = Parse(map, route);

            ParameterExpression paramExpression = Expression.Parameter(typeof(T), "o");

            Expression expression = Expression.Property(paramExpression, routeParts[0]);
            for (int i = 1; i < routeParts.Length; i++)
            {
                expression = Expression.Property(expression, routeParts[i]);
            }

            if (convertTo != null) expression = Expression.Convert(expression, convertTo);

            var readyExpression = Expression.Lambda<Func<T, TR>>(expression, paramExpression);

            if (readyExpression.CanReduce)
                readyExpression = (Expression<Func<T, TR>>)readyExpression.Reduce();

            return readyExpression;
        }

        public static Func<T, TR> ConvertRouteToFunc<T, TR>(IMap map, string route, Type convertTo = null)
        {            
            return ConvertRouteToFuncExpression<T, TR>(map, route, convertTo).Compile();
        }

        public static Expression<Action<T, TR>> ConvertRouteToActionExpression<T, TR>(IMap map, string route)
        {
            var routeParts = Parse(map, route);

            ParameterExpression paramExpression = Expression.Parameter(typeof(T), "o");

            ParameterExpression paramExpression2 = Expression.Parameter(typeof(TR), "e");
            
            Expression expression = Expression.Property(paramExpression, routeParts[0]);
            for (int i = 1; i < routeParts.Length; i++)
            {
                expression = Expression.Property(expression, routeParts[i]);
            }

            var assign = Expression.Assign(expression, paramExpression2);
            return Expression.Lambda<Action<T, TR>>(assign, paramExpression, paramExpression2);
        }

        public static Action<T, TR> ConvertRouteToAction<T, TR>(IMap map, string route)
        {
            return ConvertRouteToActionExpression<T, TR>(map, route).Compile();            
        }

        public static Type DetermineResultType<T>(T objType, string[] paths)
        {
            Type target = null;
            var proxy = DataProxy.Create((object)objType);
            var lastPath = paths.Last();

            for (var i = 0; i < paths.Length; i++)
            {
                var s = paths[i];                

                if (proxy.ContainsProperty(s)) target = proxy.GetPropertyInfo(s).PropertyType;
                else if (proxy.ContainsMethod(s)) target = proxy.Methods[s].Info.ReturnType;                
                else
                    throw new InvalidOperationException(
                        string.Format("Cannot find entry '{0}' for current operation.", s));

                if (s != lastPath)
                    proxy = DataProxy.Create(target);
            }

            return target;
        }

        public static Type DetermineResultType(Type objType, string[] paths)
        {
            Type target = null;
            var proxy = DataProxy.Create(objType);
            var lastPath = paths.Last();

            for (var i = 0; i < paths.Length; i++)
            {
                var s = paths[i];

                if (proxy.ContainsProperty(s)) target = proxy.GetPropertyInfo(s).PropertyType;
                else if (proxy.ContainsMethod(s)) target = proxy.Methods[s].Info.ReturnType;
                else
                    throw new InvalidOperationException(
                        string.Format("Cannot find entry '{0}' for current operation.", s));

                if (s != lastPath)
                    proxy = DataProxy.Create(target);
            }

            return target;
        }
    }
}