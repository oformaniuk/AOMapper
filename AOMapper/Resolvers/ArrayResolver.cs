using System;
using System.Collections;
using AOMapper.Interfaces;

namespace AOMapper.Resolvers
{
    public class ArrayResolver<TS, TD> : Resolver where TD : new()
    {
        protected bool SameTypes { get; set; }        
        protected delegate void MapMathod(IList _destination, IList list);

        protected MapMathod _method;       

        public ArrayResolver(IMap map, Type source, Type destination) 
            : base(map, source, destination)
        {
            SameTypes = typeof (TS) == typeof (TD);
            if (SameTypes) _method = MapSameType;
            else _method = MapNotSameType;
        }

        private void MapNotSameType(IList _destination, IList list)
        {
            bool compileInners = false;
            _map.ConfigMap(config => compileInners = config.CompileInnerMaps);
            var map = Mapper.Create<TS, TD>();
            if (compileInners) map.Compile();

            if (_destination.IsFixedSize)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    _destination[i] = map.Do<TS, TD>((TS) list[i]);
                }
            }
            else
            {
                for (int i = 0; i < list.Count; i++)
                {
                    _destination.Add(map.Do<TS, TD>((TS) list[i]));
                }
            }
        }        

        private void MapSameType(IList _destination, IList list)
        {
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
        }

        public override void Resolve(object source, ref object destination)
        {            
            IList list = source as IList;
            if (destination == null)
            {
                destination = DestinationType.IsArray ? 
                    Activator.CreateInstance(DestinationType, list.Count) :
                    Activator.CreateInstance(DestinationType, list.Count * 2);
            }

            var _destination = destination as IList;

            _method(_destination, list);             

            destination = _destination;  
        }
    }    
}