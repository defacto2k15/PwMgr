using System.Collections.Generic;
using System.Linq;
using GeoAPI.Geometries;
using NetTopologySuite.Index.Quadtree;

namespace Assets.Utils.Quadtree
{
    public class QuadtreeWithId<T_Id, T_QuadtreeEntity> where T_QuadtreeEntity : IHasEnvelope, IHasId<T_Id>
    {
        private Dictionary<T_Id, T_QuadtreeEntity> _registeredBodies = new Dictionary<T_Id, T_QuadtreeEntity>();
        private Quadtree<T_QuadtreeEntity> _spotsTree = new Quadtree<T_QuadtreeEntity>();

        public void Insert(T_QuadtreeEntity entity)
        {
            _registeredBodies[entity.Id] = entity;
            _spotsTree.Insert(entity.CalculateEnvelope(), entity);
        }

        public void Remove(T_Id id)
        {
            var entity = _registeredBodies[id];
            bool dictRemoval = _registeredBodies.Remove(id);
            Preconditions.Assert(dictRemoval, "There is no removed key in dictionary");
            bool treeRemoval = _spotsTree.Remove(entity.CalculateEnvelope(), entity);
            Preconditions.Assert(treeRemoval, "There is no removed entity in given envelope");
        }

        public T_QuadtreeEntity RetriveEntity(T_Id id)
        {
            return _registeredBodies[id];
        }

        public List<T_QuadtreeEntity> Query(Envelope queryEnvelope)
        {
            return _spotsTree.Query(queryEnvelope)
                .Where(c => c.CalculateEnvelope().Intersects(queryEnvelope)).ToList();
        }

        public IEnumerable<T_QuadtreeEntity> QueryAll()
        {
            return _registeredBodies.Values;
        }
    }
}