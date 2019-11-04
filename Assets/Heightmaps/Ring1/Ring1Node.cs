using System.Collections.Generic;
using Assets.Heightmaps.Ring1.treeNodeListener;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.ShaderUtils;
using UnityEngine;

namespace Assets.Heightmaps.Ring1
{
    public class Ring1Node
    {
        private readonly NodeSplitController _nodeSplitController;
        private readonly IRing1NodeListener _nodeListener;
        private readonly Ring1VisibilityResolver _visibilityResolver;

        private readonly MyRectangle _ring1Position;
        private readonly int _quadLodLevel;
        private Ring1NodePositionEnum _nodePositionEnum;

        private readonly Dictionary<Ring1NodePositionEnum, Ring1Node> _childNodes =
            new Dictionary<Ring1NodePositionEnum, Ring1Node>();

        public Ring1Node(NodeSplitController nodeSplitController, IRing1NodeListener nodeListener,
            Ring1VisibilityResolver visibilityResolver, MyRectangle ring1Position, int quadLodLevel,
            Ring1NodePositionEnum nodePositionEnum)
        {
            _nodeSplitController = nodeSplitController;
            _nodeListener = nodeListener;
            _visibilityResolver = visibilityResolver;
            _ring1Position = ring1Position;
            _quadLodLevel = quadLodLevel;
            _nodePositionEnum = nodePositionEnum;
            _nodeListener.CreatedNewNode(this);
        }

        public void UpdateLod()
        {
            if (!_visibilityResolver.IsVisible(_ring1Position))
            {
                _nodeListener.DoNotDisplay(this);
                foreach (var childNode in _childNodes.Values)
                {
                    childNode.DoNotDisplay();
                }
                return;
            }

            if (_nodeSplitController.IsTerminalNode(this))
            {
                var cameraPositon = _visibilityResolver.CameraPosition;
                _nodeListener.Update(this, cameraPositon);
                foreach (var childNode in _childNodes.Values)
                {
                    childNode.DoNotDisplay();
                }
            }
            else
            {
                _nodeListener.DoNotDisplay(this);
                if (_childNodes.Count == 0)
                {
                    CreateChildNodes();
                }
                foreach (var childNode in _childNodes.Values)
                {
                    childNode.UpdateLod();
                }
            }
        }

        public void DoNotDisplay()
        {
            _nodeListener.DoNotDisplay(this);
            foreach (var childNode in _childNodes.Values)
            {
                childNode.DoNotDisplay();
            }
        }

        private void CreateChildNodes()
        {
            _childNodes[Ring1NodePositionEnum.DOWN_LEFT] = new Ring1Node(
                _nodeSplitController,
                _nodeListener,
                _visibilityResolver,
                _ring1Position.DownLeftSubElement(),
                _quadLodLevel + 1,
                Ring1NodePositionEnum.DOWN_LEFT);


            _childNodes[Ring1NodePositionEnum.DOWN_RIGHT] = new Ring1Node(
                _nodeSplitController,
                _nodeListener,
                _visibilityResolver,
                _ring1Position.DownRightSubElement(),
                _quadLodLevel + 1,
                Ring1NodePositionEnum.DOWN_RIGHT);

            _childNodes[Ring1NodePositionEnum.TOP_LEFT] = new Ring1Node(
                _nodeSplitController,
                _nodeListener,
                _visibilityResolver,
                _ring1Position.TopLeftSubElement(),
                _quadLodLevel + 1,
                Ring1NodePositionEnum.TOP_LEFT);

            _childNodes[Ring1NodePositionEnum.TOP_RIGHT] = new Ring1Node(
                _nodeSplitController,
                _nodeListener,
                _visibilityResolver,
                _ring1Position.TopRightSubElement(),
                _quadLodLevel + 1,
                Ring1NodePositionEnum.TOP_RIGHT);
        }

        public MyRectangle Ring1Position
        {
            get { return _ring1Position; }
        }

        public int QuadLodLevel
        {
            get { return _quadLodLevel; }
        }
    }
}