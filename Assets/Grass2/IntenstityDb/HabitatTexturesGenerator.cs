using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Assets.Habitat;
using Assets.Heightmaps.Ring1.RenderingTex;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.TerrainMat;
using Assets.Utils;
using Assets.Utils.MT;
using Assets.Utils.Quadtree;
using Assets.Utils.Textures;
using GeoAPI.Geometries;
using UnityEngine;

namespace Assets.Grass2.IntenstityDb
{
    public class HabitatTexturesGenerator
    {
        private HabitatMapDbProxy _habitatDbProxy;
        private HabitatTexturesGeneratorConfiguration _configuration;
        private TextureConcieverUTProxy _textureConciever;

        public HabitatTexturesGenerator(HabitatMapDbProxy habitatDbProxy,
            HabitatTexturesGeneratorConfiguration configuration,
            TextureConcieverUTProxy textureConciever)
        {
            _habitatDbProxy = habitatDbProxy;
            _configuration = configuration;
            _textureConciever = textureConciever;
        }

        public async Task<Dictionary<HabitatType, Texture2D>> GenerateHabitatTextures(
            MyRectangle queryArea,
            IntVector2 habitatTextureSize)
        {
            var marginedQueryArea = queryArea.EnlagreByMargins(_configuration.HabitatMargin);
            var habitats = (await _habitatDbProxy.Query(marginedQueryArea)).QueryAll();

            var habitatTexturesDict =
                await CreateHabitatTexturesDict(queryArea.DownLeftPoint, habitatTextureSize, habitats);

            return habitatTexturesDict;
        }

        private async Task<Dictionary<HabitatType, Texture2D>> CreateHabitatTexturesDict(
            Vector2 startOffset,
            IntVector2 habitatTextureSize,
            List<HabitatFieldInTree> habitats)
        {
            var habitatTextureTemplatesDict = new Dictionary<HabitatType, MyTextureTemplate>();
            var allTypes = habitats.Select(c => c.Field.Type).Distinct();
            foreach (var type in allTypes)
            {
                //var newTex = new Texture2D(habitatTextureSize.X, habitatTextureSize.Y, TextureFormat.RGB24, false, true);
                var newTexTemplate = new MyTextureTemplate(habitatTextureSize.X, habitatTextureSize.Y,
                    TextureFormat.ARGB32, false, FilterMode.Bilinear) {wrapMode = TextureWrapMode.Clamp};
                habitatTextureTemplatesDict[type] = newTexTemplate;
            }

            var enlargedTree = GenerateEnlargedTree(habitats.Select(c => c.Field).ToList());
            for (int x = 0; x < habitatTextureSize.X; x++)
            {
                for (int y = 0; y < habitatTextureSize.Y; y++)
                {
                    var centerPoint = new Vector2(
                                          (x + 0.5f) * (_configuration.HabitatSamplingUnit),
                                          (y + 0.5f) * (_configuration.HabitatSamplingUnit)
                                      ) + startOffset;
                    var pointGeometry =
                        MyNetTopologySuiteUtils.ToGeometryEnvelope(MyNetTopologySuiteUtils
                            .ToPointEnvelope(centerPoint));
                    var marginTouchingHabitats = enlargedTree.QueryWithIntersection( pointGeometry );

                    foreach (var texture in habitatTextureTemplatesDict.Values)
                    {
                        texture.SetPixel(x, y, new Color(0, 0, 0));
                    }

                    foreach (var habitatType in marginTouchingHabitats.Select(c => c.Type).Distinct())
                    {
                        var maxIntensityValue = 0f;
                        foreach (var habitat in marginTouchingHabitats.Where(c => c.Type == habitatType))
                        {
                            if (habitat.StandardHabitatField.Contains(pointGeometry))
                            {
                                maxIntensityValue = 1;
                            }
                            else
                            {
                                var distance = MyNetTopologySuiteUtils.Distance(pointGeometry,
                                    habitat.StandardHabitatField,
                                    _configuration.HabitatMargin);
                                maxIntensityValue =
                                    Mathf.Max(maxIntensityValue,
                                        Mathf.Clamp01(1 - (distance / _configuration.HabitatMargin)));
                            }
                        }

                        habitatTextureTemplatesDict[habitatType].SetPixel(x, y, new Color(maxIntensityValue, 0, 0));
                    }
                }
            }

            var renderedTextures =
                await TaskUtils.WhenAll(
                    habitatTextureTemplatesDict.Values
                        .Select(async (c) => await _textureConciever.ConcieveTextureAsync(c)));

            var outDict = Enumerable.Range(0, renderedTextures.Count)
                .ToDictionary(i => habitatTextureTemplatesDict.Keys.ToList()[i], i => renderedTextures[i]);

            return outDict;
        }


        public class HabitatTexturesGeneratorConfiguration
        {
            public float HabitatMargin;
            public float HabitatSamplingUnit;
        }

        private MyQuadtree<HabitatEnlargedByMarginInTree> GenerateEnlargedTree(List<HabitatField> habitatFields)
        {
            var outTree = new MyQuadtree<HabitatEnlargedByMarginInTree>();
            foreach (var field in habitatFields)
            {
                var geo = field.Geometry;
                var enlargedGeo = MyNetTopologySuiteUtils.EnlargeByMargin(geo, _configuration.HabitatMargin);
                outTree.Add(new HabitatEnlargedByMarginInTree()
                {
                    Type = field.Type,
                    MarginedHabitatField = enlargedGeo,
                    StandardHabitatField = geo
                });
            }
            return outTree;
        }

        private class HabitatEnlargedByMarginInTree : IHasEnvelope, ICanTestIntersect
        {
            public HabitatType Type;
            public IGeometry StandardHabitatField;
            public IGeometry MarginedHabitatField;

            public Envelope CalculateEnvelope()
            {
                return MarginedHabitatField.EnvelopeInternal;
            }

            public bool Intersects(IGeometry geometry)
            {
                return MarginedHabitatField.Envelope.Intersects(geometry);
            }
        }
    }
}