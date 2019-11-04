using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Roads.Pathfinding.AStar
{
    public class DummyAStarAlgorithmDebugObject : MonoBehaviour
    {
        public void Start()
        {
            var node0 = new DummyNode(new Vector2(0, 0), false);
            var node1 = new DummyNode(new Vector2(0, 5), false);
            var node2 = new DummyNode(new Vector2(2, 7), false);
            var node3 = new DummyNode(new Vector2(2, 5), false);
            var node4 = new DummyNode(new Vector2(2, 3), false);
            var node5 = new DummyNode(new Vector2(7, 5), false);
            var node6 = new DummyNode(new Vector2(7, 2), false);
            var node7 = new DummyNode(new Vector2(9, 5), false);
            var node8 = new DummyNode(new Vector2(6, 9), false);
            var node9 = new DummyNode(new Vector2(10, 10), true);

            var n0Neighbours = new List<DummyNode>() {node1, node4};
            var n1Neighbours = new List<DummyNode>() {node0, node2, node3, node4};
            var n2Neighbours = new List<DummyNode>() {node1, node8};
            var n3Neighbours = new List<DummyNode>() {node1, node5};
            var n4Neighbours = new List<DummyNode>() {node0, node1, node6};
            var n5Neighbours = new List<DummyNode>() {node3, node6, node7, node8};
            var n6Neighbours = new List<DummyNode>() {node4, node5, node7};
            var n7Neighbours = new List<DummyNode>() {node5, node6, node9};
            var n8Neighbours = new List<DummyNode>() {node2, node5, node9};
            var n9Neighbours = new List<DummyNode>() {node7, node8};

            node0.SetChildren(n0Neighbours);
            node1.SetChildren(n1Neighbours);
            node2.SetChildren(n2Neighbours);
            node3.SetChildren(n3Neighbours);
            node4.SetChildren(n4Neighbours);
            node5.SetChildren(n5Neighbours);
            node6.SetChildren(n6Neighbours);
            node7.SetChildren(n7Neighbours);
            node8.SetChildren(n8Neighbours);
            node9.SetChildren(n9Neighbours);

            AStarSolver starSolver = new AStarSolver(node0, node9);
            var state = starSolver.Run();
            Debug.Log($"t55 state {state}");

            var path = starSolver.GetPath();
            foreach (var node in path.Cast<DummyNode>())
            {
                Debug.Log($"{node.Position}|");
            }
        }
    }

    public class DummyNode : BasePathfindingNode
    {
        private Vector2 _position;
        private List<DummyNode> _children;
        private bool _isGoal;

        public DummyNode(Vector2 position, bool isGoal)
        {
            _position = position;
            _isGoal = isGoal;
        }

        public override void SetMovementCost(IRoadSearchNode parent)
        {
            var myParent = (DummyNode) parent;
            var distance = Vector2.Distance(_position, myParent._position);
            MovementCost = distance + parent.MovementCost;
        }

        public override void SetEstimatedCost(IRoadSearchNode goal)
        {
            EstimatedCost = Vector2.Distance(((DummyNode) goal)._position, _position);
        }

        public override IEnumerable<IRoadSearchNode> Children => _children.Cast<IRoadSearchNode>();

        public void SetChildren(List<DummyNode> children)
        {
            _children = children;
        }

        public override bool IsGoal(IRoadSearchNode goal)
        {
            return _isGoal;
        }

        public Vector2 Position
        {
            get { return _position; }
            set { _position = value; }
        }
    }
}