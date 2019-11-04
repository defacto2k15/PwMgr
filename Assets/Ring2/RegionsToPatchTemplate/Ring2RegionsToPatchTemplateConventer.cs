using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.Ring2.BaseEntities;
using Assets.Ring2.IntensityProvider;
using GeoAPI.Geometries;
using UnityEngine;

namespace Assets.Ring2.RegionsToPatchTemplate
{
    public class Ring2RegionsToPatchTemplateConventer
    {
        public List<Ring2PatchTemplate> Convert(List<Ring2Region> regions, MyRectangle queryArea, Vector2 patchSize)
        {
            List<Ring2PatchTemplate> patches = new List<Ring2PatchTemplate>();
            for (float x = queryArea.X; x < queryArea.X + queryArea.Width; x += patchSize.x)
            {
                for (float y = queryArea.Y; y < queryArea.Y + queryArea.Height; y += patchSize.y)
                {
                    float endX = Math.Min(x + patchSize.x, queryArea.X + queryArea.Width);
                    float endY = Math.Min(y + patchSize.y, queryArea.Y + queryArea.Height);

                    var envelope = new Envelope(x, endX, y, endY);

                    var slices = regions
                        .Where(c => !c.Space.IsEmpty)
                        .Where(c => c.Space.Intersects(envelope))
                        .Select(c =>
                            new Ring2SliceTemplate(
                                new Ring2Substance(c.Substance.LayerFabrics.Select(k => RegionPinnedRing2Fabric(k, c))
                                    .ToList())
                            ))
                        .ToList();
                    patches.Add(new Ring2PatchTemplate(slices, envelope));
                }
            }
            return patches;
        }

        private Ring2Fabric RegionPinnedRing2Fabric(Ring2Fabric ring2Fabric, Ring2Region region)
        {
            if (ring2Fabric.IntensityProvider.RequiresRegionToCompute())
            {
                return new Ring2Fabric(ring2Fabric.Fiber, ring2Fabric.PaletteColors,
                    new RegionPinnedFabricRing2IntensityProvider(ring2Fabric.IntensityProvider, region.Space),
                    ring2Fabric.LayerPriority);
            }
            else
            {
                return ring2Fabric;
            }
        }
    }
}