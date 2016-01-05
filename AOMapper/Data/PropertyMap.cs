using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using AOMapper.Extensions;
using AOMapper.Helpers;
using AOMapper.Interfaces;
using AOMapper.Resolvers;

namespace AOMapper.Data
{    
    internal class PropertyMap<TSource, TDestination>
    {
        public DataProxy<TSource> Source { get; set; }
        public DataProxy<TDestination> Destination { get; set; }
        public List<Map<MapObject<Func<TSource, object>>, MapObject<Action<TDestination, object>>>> AdditionalMaps { get; set; }

        public string[] DestinationNonReMapedProperties { get; private set; }
        public string[] DestinationReMapedProperties { get; private set; }

        public string[] DestinationLoopProperties { get; private set; }

        public string[] SourceNonReMapedProperties { get; private set; }
        public string[] SourceReMapedProperties { get; private set; }
        public string[] SourceLoopProperties { get; private set; }
        public string[] NonResolvedProperties { get; set; }

        public void Calculate(IMap map)
        {
            NonResolvedProperties = Destination
                .Where(o => !AdditionalMaps.Any(k => k.Value.Path.Contains(o)) && !Destination.IsEnumerable(o))
                .Where(o => Source.ContainsProperty(o))
                .Where(o => Destination.GetPropertyInfo(o).PropertyType != Source.GetPropertyInfo(o).PropertyType)
                .Select(o =>
                {
                    Resolver.Create(Source.GetPropertyInfo(o).PropertyType, 
                        Destination.GetPropertyInfo(o).PropertyType,
                        map);

                    return o;
                })
                .ToArray();

            DestinationNonReMapedProperties = Destination
                .Except(NonResolvedProperties)
                .Where(o => !AdditionalMaps.Any(k => k.Value.Path.Contains(o)) && !Destination.IsEnumerable(o))
                .Where(o => Source.ContainsProperty(o))
                .ToArray();

            DestinationReMapedProperties =
                Destination.Where(o => AdditionalMaps.Any(k => k.Value.Path.Contains(o)))                
                .ToArray();

            DestinationLoopProperties =
                Destination.Where(o => !AdditionalMaps.Any(k => k.Value.Path.Contains(o)) && Destination.IsEnumerable(o))
                    .Where(o => Source.ContainsProperty(o))
                    .ToArray();

            foreach (var o in AdditionalMaps)
            {
                Resolver.Create(o.Key.Type, o.Value.Type, map);
            }            

            SourceNonReMapedProperties = Source
                .Except(NonResolvedProperties)
                .Where(
                    o =>
                        !AdditionalMaps.Where(k => k.Key.Path != null).Any(k => k.Key.Path.Contains(o)) &&
                        !Source.IsEnumerable(o))                
                .ToArray();

            SourceReMapedProperties =
                Source.Where(
                    o =>
                        AdditionalMaps.Where(k => k.Key.Path != null).Any(k => k.Key.Path.Contains(o)) &&
                        !Source.IsEnumerable(o)).ToArray();

            SourceLoopProperties =
                Source.Where(
                    o =>
                        !AdditionalMaps.Where(k => k.Key.Path != null).Any(k => k.Key.Path.Contains(o)) &&
                        Source.IsEnumerable(o)).ToArray();
        }        

        protected bool Equals(PropertyMap<TSource, TDestination> other)
        {
            return Equals(Source, other.Source) && Equals(Destination, other.Destination);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((PropertyMap<TSource, TDestination>) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Source != null ? Source.GetHashCode() : 0)*397) ^ (Destination != null ? Destination.GetHashCode() : 0);
            }
        }
    }
}