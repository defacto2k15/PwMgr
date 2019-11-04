using System.Collections.Generic;

namespace Assets.Roads.Osm
{
    public class NodesSet
    {
        private Dictionary<long, MyWorkNode> _dict = new Dictionary<long, MyWorkNode>();

        public void Add(MyWorkNode workNode)
        {
            _dict[workNode.Id] = workNode;
        }

        public IEnumerable<MyWorkNode> Nodes => _dict.Values;

        public void Remove(MyWorkNode workNode)
        {
            _dict.Remove(workNode.Id);
        }

        public MyWorkNode OfIndex(long index)
        {
            return _dict[index];
        }
    }
}