using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AOMapper.Compiler.Resolvers;
using AOMapper.Helpers;
using AOMapper.Interfaces;

namespace AOMapper.Resolvers
{
    public class ArrayResolver<TS, TD> : Resolver<TS, TD>                 
    {
        protected bool SameTypes { get; set; } 
       
        protected delegate void MapMathod(IList<TD> destination, IList<TS> list);

        protected readonly MapMathod _method;       

        public ArrayResolver(IMap map, Type source, Type destination) 
            : base(map, source, destination)
        {
            SameTypes = typeof (TS) == typeof (TD);
            if (SameTypes) _method = MapSameType;
            else _method = MapNotSameType;

            CompileTimeResolver = new ArrayCompileTimeResolver<TS, TD>(map, this);
        }

        private void MapNotSameType(IList<TD> destination, IList<TS> list)
        {
            bool compileInners = false;
            _map.ConfigMap(config => compileInners = config.CompileInnerMaps);
            var map = Mapper.Create<TS, TD>();
            if (compileInners) map.Compile();

            if (destination.IsReadOnly)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    destination[i] = map.Do<TS, TD>(list[i]);
                }
            }
            else
            {
                for (int i = 0; i < list.Count; i++)
                {
                    destination.Add(map.Do<TS, TD>(list[i]));
                }
            }
        }

        private void MapSameType(IList<TD> destination, IList<TS> list)            
        {
            if (destination.IsReadOnly)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    destination[i] = CastTo<TD>.From(list[i]);
                }
            }
            else
            {
                for (int i = 0; i < list.Count; i++)
                {
                    destination.Add(CastTo<TD>.From(list[i]));
                }
            }
        }

        public override bool CanConvert
        {
            get { return false; }
        }

        public override TD Resolve(TS source)
        {
            throw new NotSupportedException();
        }

        public override object Resolve(object source)
        {
            var list = source as IList<TS>;// ?? (source as IList).OfType<TS>().ToList();
            var destination = (DestinationType.IsArray
                ? Activator.CreateInstance(DestinationType, list.Count)
                : Activator.CreateInstance(DestinationType, list.Count*2)) as IList<TD>;

            _method(destination, list);

            return destination;
        }
    }    
}