using System.Collections.Generic;
using System.Linq;
using AOMapper.Data.Keys;
using AOMapper.Extensions;
using AOMapper.Helpers;
using AOMapper.Interfaces;
using AOMapper.Resolvers;

namespace AOMapper.Data
{    
    internal class MappingCodeTree
    {
        private readonly IMap _map;

        private Mapper.Config _config;

        public MappingCodeTree Parent { get; private set; }

        private readonly Dictionary<StringKey, MappingCodeTree> _codeTree
            = new Dictionary<StringKey, MappingCodeTree>();        

        public MappingCodeTree(IMap map, MappingCodeTree parent)
        {
            Parent = parent;
            _map = map;
            _map.ConfigMap(o => _config = o);
        }

        public MappingCodeTree GetRoot()
        {
            MappingCodeTree root = this;
            while (root.Parent != null)
            {
                root = root.Parent;
            }

            return root;
        }

        public MappingCodeTree GetTarget(MappingCodeTree root, IEnumerable<StringKey> routes)
        {
            var target = root;
            foreach (var route in routes)
            {
                if (target._codeTree.ContainsKey(route))
                {
                    target = target._codeTree[route];
                }
                else
                {
                    target = new MappingCodeTree(root._map, target);
                    target.Parent._codeTree.Add(route, target);
                }                
            }

            return target;
        }                

        public MappingCodeTree Add(StringKey route)
        {
            var root = GetRoot();
            var routes = RouteHelpers.Parse(_map, route).ToStringKeys();
            var target = GetTarget(root, routes);
            return target;
        }
    }
}