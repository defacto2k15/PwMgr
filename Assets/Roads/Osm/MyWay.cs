using System.Collections.Generic;

namespace Assets.Roads.Osm
{
    public class MyWay
    {
        public List<MyWorkNode> Nodes;
        public List<MyWay> StartWays;
        public List<MyWay> EndWays;
    }
}