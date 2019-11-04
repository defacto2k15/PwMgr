using Assets.Heightmaps.Ring1;
using Assets.Heightmaps.Ring1.treeNodeListener;
using UnityEngine;

namespace Assets.Heightmaps.GRing
{
    public class NewListenersCreatorWithLimitation
    {
        public FlatLod MaximumLod;
        public INewGRingListenersCreator Creator;
        public NewListenersCreatorPositionLimiter PositionLimiter;
        public bool IsFallthroughCreator = false;
    }

    public class NewListenersCreatorPositionLimiter
    {
        private Vector2 _center;
        private float _maxDistance;

        public NewListenersCreatorPositionLimiter(Vector2 center, float maxDistance)
        {
            _center = center;
            _maxDistance = maxDistance;
        }

        public bool IsAccepted(Ring1Node node)
        {
            var dst = Vector2.Distance(node.Ring1Position.Center, _center);
            //Debug.Log("Position: "+node.Ring1Position.Center+" distance "+dst+" size "+node.Ring1Position.Width);
            return Vector2.Distance(node.Ring1Position.Center, _center) < _maxDistance;
        }
    }
}