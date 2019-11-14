using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Assets.Ring2.BaseEntities;
using Assets.Ring2.RegionsToPatchTemplate;
using Assets.Utils;
using Assets.Utils.Textures;
using GeoAPI.Geometries;
using UnityEngine;

namespace Assets.Ring2.PatchTemplateToPatch
{
    public class Ring2PatchCreator
    {
        public Ring2Patch CreatePatch(Ring2PatchTemplate patchTemplate)
        {
            MyProfiler.BeginSample("Ring2PatchCreator : CreatePatch");
            var slices = patchTemplate.SliceTemplates
                .Select(template => CreateRing2Slice(template)).ToList();
            MyProfiler.EndSample();
            return new Ring2Patch(patchTemplate.SliceArea, slices);
        }

        private Ring2Slice CreateRing2Slice(Ring2SliceTemplate sliceTemplate)
        {
            var keywords = sliceTemplate.Substance.RetriveShaderKeywordSet();
            var slicePalette = CreateSlicePalette(sliceTemplate.Substance);
            var layerPriorities = sliceTemplate.Substance.GetProperLayerFabricsPriorities;
            var layerPatternScales = sliceTemplate.Substance.GetLayerFabricsPatternScales;
            var randomSeeds = CreateRandomSeeds(sliceTemplate.Substance);

            var ring2Slice = new Ring2Slice(keywords, slicePalette, layerPriorities, layerPatternScales, randomSeeds);
            return ring2Slice;
        }

        private Vector4 CreateRandomSeeds(Ring2Substance substance)
        {
            var outSeeds = Vector4.zero;

            var fabrics = substance.GetProperLayerFabrics.OrderBy(c => c.Fiber.Index).ToList();
            for (int i = 0; i < fabrics.Count; i++)
            {
                var ring2Fabric = fabrics[i];
                var fabricColors = ring2Fabric.PaletteColors.Colors;

                unchecked
                {
                    var hashCode = ring2Fabric.Fiber.FiberKeyword.GetHashCode();
                    for (int k = 0; k < fabricColors.Count; k++)
                    {
                        hashCode = (hashCode * 397) ^ fabricColors[k].GetHashCode();
                    }

                    outSeeds[i] = hashCode;
                }
            }

            return outSeeds;
        }

        private Ring2SlicePalette CreateSlicePalette(Ring2Substance substance)
        {
            Color[] colors = new Color[4 * 4];

            var fabrics = substance.GetProperLayerFabrics.OrderBy(c => c.Fiber.Index).ToList();
            for (int i = 0; i < fabrics.Count; i++)
            {
                var fabricColors = fabrics[i].PaletteColors.Colors;
                for (int k = 0; k < fabricColors.Count; k++)
                {
                    colors[i * 4 + k] = fabricColors[k];
                }
            }
            return new Ring2SlicePalette(colors);
        }
    }
}