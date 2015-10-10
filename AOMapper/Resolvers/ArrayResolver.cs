using System;
using System.Collections;
using System.Linq;
using AOMapper.Extensions;
using AOMapper.Interfaces;

namespace AOMapper.Resolvers
{
    internal abstract class IEnumerableResolver : Resolver
    {
        public IEnumerableResolver(IMap map) : base(map)
        {
        }

        public static Resolver CreateIEnumerable(Type resolver, Type source, Type destination, IMap map)
        {
            var destArg = destination.IsArray ? destination.GetElementType() : destination.GetGenericArguments().Single();
            var sPropArg = source.IsArray ? source.GetElementType() : source.GetGenericArguments().Single();

            return (Resolver)resolver.MakeGenericType(sPropArg, destArg)
                .Create(map);
        }
    }

    public class ArrayResolver<TS, TD> : Resolver
    {
        protected bool SameTypes { get; set; }        

        public ArrayResolver(IMap map) : base(map)
        {
            SameTypes = typeof (TS) == typeof (TD);            
        }

        private void ResolveDifferent(object source, ref object destination)
        {
            var _destination = (IList)destination;
            IList list = source as IList;
            if (_destination.IsFixedSize)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    _destination[i] = _map.Do<TS, TD>((TS)list[i]);
                }
            }
            else
            {
                for (int i = 0; i < list.Count; i++)
                {
                    _destination.Add(_map.Do<TS, TD>((TS)list[i]));
                }
            }
            destination = _destination;  
        }

        private void ResolveSame(object source, ref object destination)
        {
            var _destination = (IList)destination;
            IList list = source as IList;
            if (_destination.IsFixedSize)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    _destination[i] = list[i];
                }
            }
            else
            {
                for (int i = 0; i < list.Count; i++)
                {
                    _destination.Add(list[i]);
                }
            }
            destination = _destination;  
        }

        public override void Resolve(object source, ref object destination)
        {
            if(SameTypes) ResolveSame(source, ref destination);
            else ResolveDifferent(source, ref destination);
        }
    }    
}