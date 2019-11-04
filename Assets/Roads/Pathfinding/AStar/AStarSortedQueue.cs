using System.Collections.Generic;
using System.Linq;

namespace Assets.Roads.Pathfinding.AStar
{
    public class AStarSortedQueue
    {
        private readonly SortedList<float, IRoadSearchNode> _list;

        public AStarSortedQueue(DuplicateComparer comparer)
        {
            _list = new SortedList<float, IRoadSearchNode>(comparer);
        }

        public IEnumerable<IRoadSearchNode> Values => _list.Values;

        public bool Empty => !_list.Any();

        public void Clear()
        {
            _list.Clear();
        }

        public void Add(IRoadSearchNode node)
        {
            _list.Add(node.TotalCost, node);
        }

        public IRoadSearchNode Pop()
        {
            var toReturn = _list.First();
            _list.RemoveAt(0);
            return toReturn.Value;
        }
    }

    public class DuplicateComparer : IComparer<float>
    {
        public int Compare(float x, float y)
        {
            return (x <= y) ? -1 : 1;
        }
    }
}