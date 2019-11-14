using System.Collections.Generic;
using System.Linq;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.Ring2.BaseEntities;
using Assets.Ring2.IntensityProvider;
using Assets.Ring2.RegionsToPatchTemplate;
using Assets.Utils;

namespace Assets.Ring2.GRuntimeManagementOtherThread
{
    public class GRing2RegionsToPatchTemplateConventer
    {
        public List<Ring2PatchTemplate> Convert(List<Ring2Region> regions, MyRectangle queryArea)
        {
            var envelope = queryArea.ToEnvelope();
            var slices = regions
                .Where(c => !c.Space.IsEmpty)
                .Where(c => c.Space.Intersects(envelope))
                .Select(c =>
                    new Ring2SliceTemplate(
                        new Ring2Substance(c.Substance.LayerFabrics.Select(k => RegionPinnedRing2Fabric(k, c)).ToList())
                    ))
                .ToList();
            return new List<Ring2PatchTemplate>()
            {
                new Ring2PatchTemplate(slices, envelope)
            };
        }

        private Ring2Fabric RegionPinnedRing2Fabric(Ring2Fabric ring2Fabric, Ring2Region region)
        {
            if (ring2Fabric.IntensityProvider.RequiresRegionToCompute())
            {
                return new Ring2Fabric(ring2Fabric.Fiber, ring2Fabric.PaletteColors,
                    new RegionPinnedFabricRing2IntensityProvider(ring2Fabric.IntensityProvider, region.Space),
                    ring2Fabric.LayerPriority, ring2Fabric.PatternScale);
            }
            else
            {
                return ring2Fabric;
            }
        }
    }
}