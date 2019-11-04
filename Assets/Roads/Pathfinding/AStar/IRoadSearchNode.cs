using System.Collections.Generic;

namespace Assets.Roads.Pathfinding.AStar
{
    public interface IRoadSearchNode
    {
        bool IsOpenList(IEnumerable<IRoadSearchNode> openList);

        void SetOpenList(bool value);

        bool IsClosedList(IEnumerable<IRoadSearchNode> closedList);

        void SetClosedList(bool value);

        float TotalCost { get; }

        float MovementCost { get; }

        float EstimatedCost { get; }

        void SetMovementCost(IRoadSearchNode parent);

        void SetEstimatedCost(IRoadSearchNode goal);

        IRoadSearchNode Parent { get; set; }

        IEnumerable<IRoadSearchNode> Children { get; }

        bool IsGoal(IRoadSearchNode goal);
    }
}