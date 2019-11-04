using Assets.Utils;

namespace Assets.Roads.Pathfinding.TerrainPath
{
    public class PathfindingSegment
    {
        public NodeWithHeight StartNode;
        public NodeWithHeight TargetNode;
        public MyLine IntraNodesLine;

        public static PathfindingSegment Calculate(IntVector2 startNode, IntVector2 endNode,
            GratePositionCalculator positionCalculator, TerrainSamplingSource samplingSource)
        {
            var p1 = positionCalculator.ToGlobalPosition(startNode);
            var p2 = positionCalculator.ToGlobalPosition(endNode);

            var height1 = samplingSource.SamplePosition(startNode);
            var height2 = samplingSource.SamplePosition(endNode);

            var line = MyLine.ComputeFromPoints(p1, p2);
            return new PathfindingSegment()
            {
                IntraNodesLine = line,
                StartNode = new NodeWithHeight()
                {
                    Position = startNode,
                    Height = height1
                },
                TargetNode = new NodeWithHeight()
                {
                    Position = endNode,
                    Height = height2
                }
            };
        }
    }
}