using System;
using Assets.Heightmaps.Ring1.treeNodeListener;

namespace Assets.Heightmaps.Ring1
{
    public class Ring1Root
    {
        private readonly Ring1Node _downLeftNode;
        private readonly Ring1Node _downRightNode;
        private readonly Ring1Node _topRightNode;
        private readonly Ring1Node _topLeftNode;
        private readonly IRing1NodeListener _nodeListener;

        public Ring1Root(Ring1Node downLeftNode, Ring1Node downRightNode, Ring1Node topRightNode, Ring1Node topLeftNode, IRing1NodeListener nodeListener)
        {
            _downLeftNode = downLeftNode;
            _downRightNode = downRightNode;
            _topRightNode = topRightNode;
            _topLeftNode = topLeftNode;
            _nodeListener = nodeListener;
        }

        private void doAllNodes(Action<Ring1Node> action)
        {
            action(_downLeftNode);
            action(_downRightNode);
            action(_topLeftNode);
            action(_topRightNode);
        }

        public void UpdateLod()
        {
            doAllNodes(c => c.UpdateLod());
            _nodeListener.EndBatch();
        }
    }
}