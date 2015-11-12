using System;
using System.Collections.Generic;
using System.Linq;
using AOMapper.Data.Keys;
using AOMapper.Exceptions;
using AOMapper.Extensions;
using AOMapper.Helpers;

namespace AOMapper.Data
{
    public class CallStack<TSource, TDestination> //: Stack<CallStackNode>
    {
        private readonly Dictionary<StringKey, CallStack<TSource, TDestination>> _callTree
            = new Dictionary<StringKey, CallStack<TSource, TDestination>>();

        protected Type _thisType;        

        protected DataProxy _dataProxy;

        public StringKey Route { get; set; }
        public CallStack<TSource, TDestination> Parent { get; set; }
        public object Value { get; set; }
        public Func<object, object, object> Action { get; set; }
        internal List<Map<MapObject<Func<TSource, object>>, MapObject<Action<TDestination, object>>>> AdditionalMaps { get; set; }
        internal Map<MapObject<Func<TSource, object>>, MapObject<Action<TDestination, object>>> Map { get; set; }

        public TSource GlobalSource { get; set; }

        public void Call(object source, object destination)
        {
            object value = destination ?? Value;
            if (Action != null)
            {
                try
                {
                    value = Action(destination, source);
                }
                catch (NullReferenceException e)
                {
                    throw new ValueIsNotInitializedException("", e, Parent.Route, source != null ? source.GetType() : null, destination != null ? destination.GetType() : null);
                }
            }

            var nullMaps = _callTree
                .Where(o => o.Value.Map == null || o.Value.Map.Value.MappingRoute.SourceRoute == null)
                .ToArray();

            var groups = _callTree
                .Where(o => o.Value.Map != null)
                .Where(o => o.Value.Map.Value.MappingRoute.SourceRoute != null)
                .Select(o => new
                {
                    Stack = o, 
                    SourceParent = o.Value.Map.Value.MappingRoute.SourceRoute.Parent,
                    Source = o.Value.Map.Value.MappingRoute.SourceRoute,
                    M = o.Value.Map
                    //Parent = RouteHelpers.GetParent(o.Value.Map.Key.MappingRoute.Map, o.Value.Map.Key.Path),
                }).GroupBy(o => o.SourceParent);

            foreach (var map in nullMaps)
            {
                map.Value.GlobalSource = GlobalSource;
                map.Value.Call(GlobalSource, value);
            }

            // _callTree.Where(o => groups.All(x => x.Stack.Key != o.Key)).Select(o => o.Value)
            foreach (var group in groups)
            {
                var sourceObject = (TSource) group.Key.GetConverteDelegate.As<Func<TSource, object>>()(GlobalSource);

                //if (group.Key.Resolver != null)
                    //group.Key.Resolver.Resolve(s, ref s);

                foreach (var stack in group)
                {
                    var s = stack.Source._dataProxy[sourceObject, stack.Source.Key];//_GetConverteDelegate.As<Func<TSource, object>>()(sourceObject);

                    stack.Stack.Value.GlobalSource = GlobalSource;
                    stack.Stack.Value.Call(s, value);   
                }                
            }


        }

        public void AddAction(Func<object, object, object> action)
        {
            var a = Action;
            Action = (o, o1) => 
            {
                var r = a(o, o1);
                return action(r, o1);
            }; //+= action;
        }

        internal CallStack(StringKey route, CallStack<TSource, TDestination> parent, object value,
            List<Map<MapObject<Func<TSource, object>>, MapObject<Action<TDestination, object>>>> maps, 
            Map<MapObject<Func<TSource, object>>, MapObject<Action<TDestination, object>>> map = null)
        {
            Action = (o, o1) => o;
            Route = route;
            Parent = parent;
            Value = value;
            AdditionalMaps = maps;
            Map = map;
            GlobalSource = Parent == null ? default(TSource) : Parent.GlobalSource;
            if (string.IsNullOrEmpty(route.Value))
            {
                if (Parent != null && Parent._callTree.Keys.Any())
                    route = Parent._callTree.Keys.Last() + 1;
                else
                    route = "1";
            }

            if(Parent != null)
                Parent._callTree[route] = this;
        }        
    }    
}