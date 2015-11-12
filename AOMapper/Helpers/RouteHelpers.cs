using System;
using System.Linq;
using System.Text.RegularExpressions;
using AOMapper.Data.Keys;
using AOMapper.Interfaces;

namespace AOMapper.Helpers
{
    public static class RouteHelpers
    {
        public static string[] Parse(IMap map, string value)
        {
            Mapper.Config configuration = null;
            map.ConfigMap(config => configuration = config);

            return value.Split(configuration.Separator);
        }

        public static string GetParent(IMap map, string value)
        {
            Mapper.Config configuration = null;
            map.ConfigMap(config => configuration = config);

            var parts = Parse(map, value);
            return string.Join(configuration.Separator.ToString(), parts.Take(parts.Length - 2).ToArray());
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
    }
}