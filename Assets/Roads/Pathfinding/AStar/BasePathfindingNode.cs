using System.Collections.Generic;

namespace Assets.Roads.Pathfinding.AStar
{
    public abstract class BasePathfindingNode : IRoadSearchNode
    {
        private bool _closedList;
        private bool _openList;

        public bool IsOpenList(IEnumerable<IRoadSearchNode> openList)
        {
            return _openList;
        }

        public void SetOpenList(bool value)
        {
            _openList = value;
        }

        public bool IsClosedList(IEnumerable<IRoadSearchNode> closedList)
        {
            return _closedList;
        }

        public void SetClosedList(bool value)
        {
            _closedList = true;
        }

        public float TotalCost => MovementCost + EstimatedCost;
        public float MovementCost { set; get; }
        public float EstimatedCost { set; get; }

        public abstract void SetMovementCost(IRoadSearchNode parent);
        public abstract void SetEstimatedCost(IRoadSearchNode goal);

        public IRoadSearchNode Parent { get; set; }
        public abstract IEnumerable<IRoadSearchNode> Children { get; }
        public abstract bool IsGoal(IRoadSearchNode goal);
    }
}