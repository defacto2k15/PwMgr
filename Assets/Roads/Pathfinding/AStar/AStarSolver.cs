using System.Collections.Generic;

namespace Assets.Roads.Pathfinding.AStar
{
    public class AStarSolver
    {
        private readonly AStarSortedQueue _openList;

        private readonly AStarSortedQueue _closedList;

        private IRoadSearchNode _current;

        private IRoadSearchNode _goal;

        public int Steps { get; private set; }

        public IEnumerable<IRoadSearchNode> OpenList => _openList.Values;

        public IEnumerable<IRoadSearchNode> ClosedList => _closedList.Values;

        public IRoadSearchNode CurrentNode => _current;

        public AStarSolver(IRoadSearchNode start, IRoadSearchNode goal)
        {
            var duplicateComparer = new DuplicateComparer();
            _openList = new AStarSortedQueue(duplicateComparer);
            _closedList = new AStarSortedQueue(duplicateComparer);
            Reset(start, goal);
        }

        public void Reset(IRoadSearchNode start, IRoadSearchNode goal)
        {
            _openList.Clear();
            _closedList.Clear();
            _current = start;
            this._goal = goal;
            _openList.Add(_current);
            _current.SetOpenList(true);
        }

        public AStarSolverState Run()
        {
            // Continue searching until either failure or the goal node has been found.
            while (true)
            {
                AStarSolverState s = Step();
                if (s != AStarSolverState.Searching)
                    return s;
            }
        }

        public AStarSolverState Step()
        {
            Steps++;
            while (true)
            {
                // There are no more nodes to search, return failure.
                if (_openList.Empty)
                {
                    return AStarSolverState.Failed;
                }

                // Check the next best node in the graph by TotalCost.
                _current = _openList.Pop();

                // This node has already been searched, check the next one.
                if (_current.IsClosedList(ClosedList))
                {
                    continue;
                }

                // An unsearched node has been found, search it.
                break;
            }

            // Remove from the open list and place on the closed list 
            // since this node is now being searched.
            _current.SetOpenList(false);
            _closedList.Add(_current);
            _current.SetClosedList(true);

            // Found the goal, stop searching.
            if (_current.IsGoal(_goal))
            {
                return AStarSolverState.GoalFound;
            }

            // Node was not the goal so add all children nodes to the open list.
            // Each child needs to have its movement cost set and estimated cost.
            foreach (var child in _current.Children)
            {
                // If the child has already been searched (closed list) or is on
                // the open list to be searched then do not modify its movement cost
                // or estimated cost since they have already been set previously.
                if (child.IsOpenList(OpenList) || child.IsClosedList(ClosedList))
                {
                    continue;
                }

                child.Parent = _current;
                child.SetMovementCost(_current);
                child.SetEstimatedCost(_goal);
                _openList.Add(child);
                child.SetOpenList(true);
            }

            // This step did not find the goal so return status of still searching.
            return AStarSolverState.Searching;
        }

        public IEnumerable<IRoadSearchNode> GetPath()
        {
            if (_current != null)
            {
                var next = _current;
                var path = new List<IRoadSearchNode>();
                while (next != null)
                {
                    path.Add(next);
                    next = next.Parent;
                }
                path.Reverse();
                return path.ToArray();
            }
            return null;
        }
    }

    public enum AStarSolverState
    {
        Searching,
        GoalFound,
        Failed
    }
}