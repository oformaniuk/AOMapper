using System;
using System.Collections.Generic;
using AOMapper.Helpers;

namespace AOMapper
{
    internal class PropertyMap<TSource, TDestination>
    {
        public DataProxy<TSource> Source { get; set; }
        public DataProxy<TDestination> Destination { get; set; }
        public List<EditableKeyValuePair<MapObject<Func<TSource, object>>, MapObject<Action<TDestination, object>>>> AdditionalMaps { get; set; }
    }
}