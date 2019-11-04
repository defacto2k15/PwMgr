using System.Collections.Generic;
using System.Linq;
using Assets.Ring2.BaseEntities;
using Assets.Ring2.IntensityProvider;
using Assets.Ring2.RegionsToPatchTemplate;

namespace Assets.Ring2
{
    public class Ring2PatchTemplateCombiner
    {
        public List<Ring2PatchTemplate> CombineTemplates(List<Ring2PatchTemplate> inTemplates)
        {
            var ring2PatchTemplates = inTemplates.Select(c => CombineSingleTemplate(c)).ToList();
            return ring2PatchTemplates;
        }

        private Ring2PatchTemplate CombineSingleTemplate(Ring2PatchTemplate template)
        {
            var fabricsList = new List<RegionPinnedRing2FabricWithMultiplier>();

            float sliceMultiplier = 1;
            foreach (var slice in template.SliceTemplates)
            {
                var substance = slice.Substance;
                foreach (var fabric in substance.LayerFabrics)
                {
                    fabricsList.Add(new RegionPinnedRing2FabricWithMultiplier()
                    {
                        Fabric = fabric,
                        Multiplier = sliceMultiplier
                    });
                }
                sliceMultiplier *= 0.5f;
            }

            // now, summing multipliers
            var summedFabricList = new List<RegionPinnedRing2FabricCollections>();
            foreach (var fabric in fabricsList)
            {
                bool sumDone = false;
                foreach (var summedFabric in summedFabricList)
                {
                    if (summedFabric.HasEqualCharacteristics(fabric))
                    {
                        summedFabric.Add(fabric);
                        sumDone = true;
                    }
                }
                if (!sumDone)
                {
                    summedFabricList.Add(new RegionPinnedRing2FabricCollections(fabric));
                }
            }

            //now, lets merge fabrics
            var mergedFabrics = new List<Ring2Fabric>();
            foreach (var summed in summedFabricList)
            {
                mergedFabrics.Add(
                    new Ring2Fabric(
                        summed.Fiber,
                        summed.FabricColors,
                        new MaxValueCollectionIntensityProvider(
                            summed.Fabrics.Select(c => c.Fabric.IntensityProvider).ToList()
                        ),
                        summed.Fabrics.Select(c => c.Multiplier * c.Fabric.LayerPriority).Sum()
                    ));
            }

            // lets create slices
            var slicesList = new List<Ring2SliceTemplate>();
            while (mergedFabrics.Count > 0)
            {
                List<Ring2Fabric> fabricToAddInThisSlice = new List<Ring2Fabric>();
                List<int> indexesToRemove = new List<int>();
                for (int i = 0; i < mergedFabrics.Count; i++)
                {
                    var fabric = mergedFabrics[i];
                    // when this fabric is new
                    if (!fabricToAddInThisSlice.Any(c => c.Fiber.Equals(fabric.Fiber)))
                    {
                        fabricToAddInThisSlice.Add(fabric);
                        indexesToRemove.Add(i);
                    }
                }
                foreach (var index in indexesToRemove.OrderByDescending(c => c))
                {
                    mergedFabrics.RemoveAt(index);
                }
                slicesList.Add(new Ring2SliceTemplate(new Ring2Substance(fabricToAddInThisSlice)));
            }
            return new Ring2PatchTemplate(slicesList, template.SliceArea);
        }

        private class RegionPinnedRing2FabricWithMultiplier
        {
            public float Multiplier;
            public Ring2Fabric Fabric;
        }

        private class RegionPinnedRing2FabricCollections
        {
            private Ring2Fiber _fiber;
            private Ring2FabricColors _fabricColors;

            private List<RegionPinnedRing2FabricWithMultiplier> _fabrics =
                new List<RegionPinnedRing2FabricWithMultiplier>();

            public RegionPinnedRing2FabricCollections(RegionPinnedRing2FabricWithMultiplier fabric)
            {
                _fiber = fabric.Fabric.Fiber;
                _fabricColors = fabric.Fabric.PaletteColors;
                _fabrics.Add(fabric);
            }

            public bool HasEqualCharacteristics(RegionPinnedRing2FabricWithMultiplier otherFabric)
            {
                return _fiber.Equals(otherFabric.Fabric.Fiber) &&
                       _fabricColors.Equals(otherFabric.Fabric.PaletteColors);
            }

            public void Add(RegionPinnedRing2FabricWithMultiplier fabric)
            {
                _fabrics.Add(fabric);
            }

            public Ring2Fiber Fiber
            {
                get { return _fiber; }
            }

            public Ring2FabricColors FabricColors
            {
                get { return _fabricColors; }
            }

            public List<RegionPinnedRing2FabricWithMultiplier> Fabrics
            {
                get { return _fabrics; }
            }
        }
    }
}