using System.Collections.Generic;
using OsmSharp.Collections.Tags;

namespace Assets.Habitat
{
    public class MyWorkRelation
    {
        public List<long> OuterWayIds;
        public List<long> InnerWayIds;
        public TagsCollectionBase Tags;
    }
}