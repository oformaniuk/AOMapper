using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace AOMapper.Resolvers
{
    public class ResolverFactory
    {
        static readonly List<KeyValuePair<Func<Type, Type, Type, bool>, Type>> Resolvers;            

        static ResolverFactory()
        {
            Resolvers = new List<KeyValuePair<Func<Type, Type, Type, bool>, Type>>
            {                
                {
                    new KeyValuePair<Func<Type, Type, Type, bool>, Type>(
                        (source, destination, arg3) =>
                            Resolver.TypeOfIList.Value.IsAssignableFrom(source) &&
                            Resolver.TypeOfIList.Value.IsAssignableFrom(destination),
                        typeof (IEnumerableResolver))
                },                

                {
                    new KeyValuePair<Func<Type, Type, Type, bool>, Type>(
                        (source, destination, resolver) => resolver == null && source.IsEnum || destination.IsEnum,
                        typeof (EnumResolver<,>))
                },                

                { // should be the last one
                    new KeyValuePair<Func<Type, Type, Type, bool>, Type>(
                        (source, destination, arg3) =>
                        {
                            bool isNull = arg3 == null;
                            var isSourceConvertible = source.GetInterfaces().Contains(typeof (IConvertible));
                            var isDestinationConvertible = destination.GetInterfaces().Contains(typeof(IConvertible));

                            return isNull && isSourceConvertible && isDestinationConvertible;
                        },
                        typeof (ChangeTypeResolver<,>))
                },
            };
        }        

        public static Type Get(Type source, Type destination, Type resolver)
        {
            var @default = Resolvers.FirstOrDefault(o => o.Key(source, destination, resolver)).Value;
            if (@default == null) return resolver;
            return @default;
        }
    }
}