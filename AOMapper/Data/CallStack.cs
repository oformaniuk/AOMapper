using System;
using System.Collections.Generic;
using AOMapper.Data.Keys;
using AOMapper.Exceptions;

namespace AOMapper.Data
{
    public class CallStack<TSource, TDestination> //: Stack<CallStackNode>
    {
        //private readonly Dictionary<StringKey, CallStack<TSource, TDestination>> _callTree
        //    = new Dictionary<StringKey, CallStack<TSource, TDestination>>();

        private readonly List<CallStack<TSource, TDestination>> _callTree
            = new List<CallStack<TSource, TDestination>>();

        protected DataProxy _dataProxy;

        protected Type _thisType;

        internal CallStack(StringKey route, CallStack<TSource, TDestination> parent, object value,
            List<Map<MapObject<Func<TSource, object>>, MapObject<Action<TDestination, object>>>> maps,
            Map<MapObject<Func<TSource, object>>, MapObject<Action<TDestination, object>>> map = null)
        {
            //Action = (o, o1) => o;
            Route = route;
            Parent = parent;
            Value = value;
            AdditionalMaps = maps;
            Map = map;
            GlobalSource = Parent == null ? default(TSource) : Parent.GlobalSource;
            //if (string.IsNullOrEmpty(route.Value))
            //{
            //    if (Parent != null && Parent._callTree.Keys.Any())
            //        route = Parent._callTree.Keys.Last() + 1;
            //    else
            //        route = "1";
            //}

            if (Parent != null)
                Parent._callTree /*[route]*/.Add(this); // = this;
        }

        public StringKey Route { get; set; }
        public CallStack<TSource, TDestination> Parent { get; set; }
        public object Value { get; set; }
        public Func<object, TSource, object> Action { get; internal set; }

        internal List<Map<MapObject<Func<TSource, object>>, MapObject<Action<TDestination, object>>>> AdditionalMaps {
            get; set; }

        internal Map<MapObject<Func<TSource, object>>, MapObject<Action<TDestination, object>>> Map { get; set; }

        public TSource GlobalSource { get; set; }

        //public Func<object, object, object> Build(Func<object, object, object> compiledMap)
        //{
        //    object value = null;//destination ?? Value;
        //    if (Action != null)
        //    {
        //        try
        //        {
        //            var c = compiledMap;
        //            compiledMap = (source, destination) =>
        //            {
        //                var r = c(source, destination);
        //                value = Action(r, source);
        //                return value;
        //            };                    
        //        }
        //        catch (NullReferenceException e)
        //        {
        //            //throw new ValueIsNotInitializedException("", e, Parent.Route, source != null ? source.GetType() : null, destination != null ? destination.GetType() : null);
        //        }
        //    }

        //    foreach (var stack in _callTree)
        //    {
        //        compiledMap = stack.Value.Build(compiledMap);
        //    }

        //    return compiledMap;
        //}

        public void Call(TSource source, object destination)
        {
            object value; // = destination ?? Value;
            //if (Action != null)
            //{
            try
            {
                value = Action(destination, source);
            }
            catch (NullReferenceException e)
            {
                throw new ValueIsNotInitializedException("", e, Parent.Route,
                    source != null ? source.GetType() : null, destination != null ? destination.GetType() : null);
            }
            //}
            //else value = destination;

            for (var i = 0; i < _callTree.Count; i++)
            {
                _callTree[i].Call(source, value);
            }

            //foreach (var stack in _callTree)
            //{
            //    stack.Call(source, value);
            //}
        }

        public void AddAction(Func<object, TSource, object> action)
        {
            //var a = Action;
            //Action = (o, o1) => 
            //{
            //    var r = a(o, o1);
            //    return action(r, o1);
            //}; //+= action;
            Action += action;
        }
    }
}