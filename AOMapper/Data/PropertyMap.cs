using System;
using System.Collections.Generic;
using System.Linq;

namespace AOMapper.Data
{
    internal class PropertyMap<TSource, TDestination>
    {
        public DataProxy<TSource> Source { get; set; }
        public DataProxy<TDestination> Destination { get; set; }
        public List<EditableKeyValuePair<MapObject<Func<TSource, object>>, MapObject<Action<TDestination, object>>>> AdditionalMaps { get; set; }

        public string[] DestinationNonReMapedProperties { get; private set; }
        public string[] DestinationReMapedProperties { get; private set; }

        public string[] DestinationLoopProperties { get; private set; }

        public string[] SourceNonReMapedProperties { get; private set; }
        public string[] SourceReMapedProperties { get; private set; }
        public string[] SourceLoopProperties { get; private set; }

        public void Calculate()
        {
            DestinationNonReMapedProperties = Destination.Where(o => !AdditionalMaps.Any(k => k.Value.Path.Contains(o)) && !Destination.IsEnumerable(o))
                .Where(o => Source.ContainsProperty(o))
                .ToArray();

            DestinationReMapedProperties = Destination.Where(o => AdditionalMaps.Any(k => k.Value.Path.Contains(o)) && Destination.IsEnumerable(o)).ToArray();

            DestinationLoopProperties = Destination.Where(o => !AdditionalMaps.Any(k => k.Value.Path.Contains(o)) && Destination.IsEnumerable(o))
                .Where(o => Source.ContainsProperty(o))
                .ToArray();

            SourceNonReMapedProperties = Source.Where(o => !AdditionalMaps.Any(k => k.Key.Path.Contains(o)) && !Source.IsEnumerable(o)).ToArray();
            SourceReMapedProperties = Source.Where(o => AdditionalMaps.Any(k => k.Key.Path.Contains(o)) && !Source.IsEnumerable(o)).ToArray();

            SourceLoopProperties = Source.Where(o => !AdditionalMaps.Any(k => k.Key.Path.Contains(o)) && Source.IsEnumerable(o)).ToArray();
        }
    }
}