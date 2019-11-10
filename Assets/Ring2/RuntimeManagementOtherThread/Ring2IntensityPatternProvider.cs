using System;
using System.Linq;
using System.Threading.Tasks;
using Assets.Ring2.PatchTemplateToPatch;
using Assets.Utils.Textures;
using GeoAPI.Geometries;
using UnityEngine;
using System.Collections.Generic;
using Assets.Ring2.RegionsToPatchTemplate;
using Assets.Utils;
using Assets.Utils.MT;

namespace Assets.Ring2.RuntimeManagementOtherThread
{
    public class Ring2IntensityPatternProvider
    {
        private TextureConcieverUTProxy _conciever;
        private Ring2IntensityPatternEnhancer _intensityPatternEnhancer;

        public Ring2IntensityPatternProvider(TextureConcieverUTProxy conciever,
            Ring2IntensityPatternEnhancer intensityPatternEnhancer = null)
        {
            _conciever = conciever;
            _intensityPatternEnhancer = intensityPatternEnhancer;
        }

        public async Task<Ring2Patch> ProvidePatchWithIntensityPattern(Ring2Patch patch,
            Ring2PatchTemplate patchTemplate, float patternPixelsPerUnit)
        {
            MyProfiler.BeginSample("Ring2IntensityPatternProvider : ProvidePatchWithIntensityPattern");
            var intensityPatterns = await TaskUtils.WhenAll(Enumerable.Range(0, patchTemplate.SliceTemplates.Count)
                .Select(
                    (i) =>
                    {
                        var sliceTemplate = patchTemplate.SliceTemplates[i];
                        var toReturn = CreateIntenstiyPatternAsync(patchTemplate.SliceArea, sliceTemplate,
                            patternPixelsPerUnit);
                        return toReturn;
                    }));

            for (int i = 0; i < patchTemplate.SliceTemplates.Count; i++)
            {
                patch.Slices[i].IntensityPattern = intensityPatterns[i];
            }
            MyProfiler.EndSample();

            return patch;
        }

        private async Task<Ring2PatchSliceIntensityPattern> CreateIntenstiyPatternAsync(Envelope sliceArea,
            Ring2SliceTemplate sliceTemplate, float patternPixelsPerUnit)
        {
            MyProfiler.BeginSample("Ring2IntensityPatternProvider :  CreateIntenstiyPatternAsync : Start");
            var layerFabrics = sliceTemplate.Substance.GetProperLayerFabrics.OrderBy(c => c.Fiber.Index).ToList();
            var sizeInPixels = new Vector2(Mathf.Ceil((float) (patternPixelsPerUnit * sliceArea.CalculatedWidth())),
                Mathf.Ceil((float) (patternPixelsPerUnit * sliceArea.CalculatedHeight())));
            var sizeOfOnePixel = new Vector2(1f / patternPixelsPerUnit, 1f / patternPixelsPerUnit);

            MyTextureTemplate texture = new MyTextureTemplate((int) sizeInPixels.x, (int) sizeInPixels.y,
                TextureFormat.RGBA32, false, FilterMode.Bilinear);
            texture.wrapMode = TextureWrapMode.Clamp;

            List<Vector2> queryPositions = new List<Vector2>();
            for (int x = 0; x < sizeInPixels.x; x++)
            {
                for (int y = 0; y < sizeInPixels.y; y++)
                {
                    var globalSamplePosition = new Vector2((x + 0.5f) * sizeOfOnePixel.x + (float) sliceArea.MinX,
                        (y + 0.5f) * sizeOfOnePixel.y + (float) sliceArea.MinY);
                    queryPositions.Add(globalSamplePosition);
                }
            }
            MyProfiler.EndSample();
            MyProfiler.BeginSample("Ring2IntensityPatternProvider :  CreateIntenstiyPatternAsync : Intensity creation");
            List<List<float>> intensities = await TaskUtils.WhenAll(layerFabrics.Select(async (layer) =>
            {
                var toReturn = await layer.IntensityProvider.RetriveIntensityAsync(queryPositions);
                return toReturn;
            })).ReportUnityExceptions();

            for (int x = 0; x < sizeInPixels.x; x++)
            {
                for (int y = 0; y < sizeInPixels.y; y++)
                {
                    Color color = new Color(0, 0, 0, 0);
                    for (int i = 0; i < layerFabrics.Count; i++)
                    {
                        color[i] = intensities[i][x * ((int) sizeInPixels.y) + y];
                    }
                    texture.SetPixel(x, y, color);
                }
            }
            MyProfiler.EndSample();
            MyProfiler.BeginSample("Ring2IntensityPatternProvider :  CreateIntenstiyPatternAsync : Concieve texture");
            Texture realTexture = await _conciever.ConcieveTextureAsync(texture);
            MyProfiler.EndSample();
            MyProfiler.BeginSample("Ring2IntensityPatternProvider :  CreateIntenstiyPatternAsync : Enhance intensity");
            if (_intensityPatternEnhancer != null)
            {
                realTexture = await _intensityPatternEnhancer.EnhanceIntensityPatternAsync(realTexture,
                    sizeInPixels.ToIntVector(), sliceArea.ToUnityCoordPositions2D());
            }
            MyProfiler.EndSample();

            return new Ring2PatchSliceIntensityPattern(realTexture);
        }
    }
}