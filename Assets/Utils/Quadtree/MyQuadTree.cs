using System.Collections.Generic;
using System.Linq;
using GeoAPI.Geometries;
using NetTopologySuite.Index.Quadtree;

namespace Assets.Utils.Quadtree
{
    public class MyQuadtree<T> where T : IHasEnvelope, ICanTestIntersect
    {
        private readonly Quadtree<T> _tree = new Quadtree<T>();

        public void Add(T elem)
        {
            _tree.Insert(elem.CalculateEnvelope(), elem);
        }

        public List<T> QueryWithIntersection(IGeometry queryGeo)
        {
            return Query(queryGeo).Where(c => c.Intersects(queryGeo)).ToList();
        }

        public List<T> Query(IGeometry queryGeo)
        {
            return _tree.Query(queryGeo.EnvelopeInternal).ToList();
        }

        public List<T> QueryAll()
        {
            return _tree.QueryAll().ToList();
        }

        public static MyQuadtree<T> CreateWithElements(IEnumerable<T> elements)
        {
            var newTree = new MyQuadtree<T>();
            foreach (var element in elements)
            {
                newTree.Add(element);
            }
            return newTree;
        }
    }
}