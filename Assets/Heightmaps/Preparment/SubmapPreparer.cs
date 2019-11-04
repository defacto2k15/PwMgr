using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.Heightmaps.Preparment.MarginMerging;
using Assets.Heightmaps.Preparment.Simplyfying;
using Assets.Heightmaps.Preparment.Slicing;
using Assets.Heightmaps.submaps;

namespace Assets.Heightmaps.Preparment
{
    class SubmapPreparer
    {
        private readonly TerrainSlicer _slicer = new TerrainSlicer();
        private readonly MarginMerger _merger = new MarginMerger();
        private readonly HeightmapSimplyfyer _simplyfyer = new HeightmapSimplyfyer();

        public SubmapPreparingOutput PrepareSubmaps(HeightmapArray globalHeightArray,
            List<SubmapPreparmentOrder> preparmentOrders)
        {
            var slicedSubmaps = _slicer.Slice(globalHeightArray, preparmentOrders.Select(c => c.Position).ToList(), 0);

            var submaps =
                slicedSubmaps.Select(
                    (submap, index) => _simplyfyer.SimplyfySubmap(submap, preparmentOrders[index].LodFactor)).ToList();

            _merger.MergeMargins(submaps);

            var output = new SubmapPreparingOutput(
                submaps.Select((submap, index) => new {submap, index})
                    .Where(e => preparmentOrders[e.index].RingNumber == 0)
                    .Select(e => e.submap)
                    .ToList(),
                submaps.Select((submap, index) => new {submap, index})
                    .Where(e => preparmentOrders[e.index].RingNumber == 1)
                    .Select(e => e.submap)
                    .ToList()
            );
            return output;
        }
    }

    class SubmapPreparingOutput
    {
        private readonly List<Submap> ring0Submaps;
        private readonly List<Submap> ring1Submaps;

        public SubmapPreparingOutput(List<Submap> ring0Submaps, List<Submap> ring1Submaps)
        {
            this.ring0Submaps = ring0Submaps;
            this.ring1Submaps = ring1Submaps;
        }

        public List<Submap> Ring0Submaps
        {
            get { return ring0Submaps; }
        }

        public List<Submap> Ring1Submaps
        {
            get { return ring1Submaps; }
        }
    }
}