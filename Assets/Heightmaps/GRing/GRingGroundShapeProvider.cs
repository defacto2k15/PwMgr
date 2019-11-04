using System.Collections.Generic;
using System.Threading.Tasks;
using Assets.Heightmaps.Ring1.Painter;
using Assets.Heightmaps.Ring1.TerrainDescription;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.Heightmaps.Welding;
using Assets.Ring2.BaseEntities;
using Assets.ShaderUtils;
using Assets.Trees.SpotUpdating;
using Assets.Utils;
using UnityEngine;

namespace Assets.Heightmaps.GRing
{
    public class GRingGroundShapeProvider
    {
        private readonly ITerrainShapeDb _terrainShapeDb;
        private FlatLod _flatLod;
        private readonly MyRectangle _terrainDetailAreaPosition;
        private readonly GRingSpotUpdater _spotUpdater;
        private readonly GRingGroundShapeProviderConfiguration _configuration;


        public GRingGroundShapeProvider(
            ITerrainShapeDb terrainShapeDb,
            FlatLod flatLod,
            MyRectangle terrainDetailAreaPosition,
            GRingSpotUpdater spotUpdater,
            GRingGroundShapeProviderConfiguration configuration
        )
        {
            _terrainShapeDb = terrainShapeDb;
            _flatLod = flatLod;
            _terrainDetailAreaPosition = terrainDetailAreaPosition;
            _spotUpdater = spotUpdater;
            _configuration = configuration;
        }


        public async Task<GRingGroundShapeDetail> ProvideGroundTextureDetail()
        {
            int flatLodLevel = _flatLod.ScalarValue;

            if (!_configuration.SettingPerFlatLod.ContainsKey(flatLodLevel))
            {
                Preconditions.Fail("Unsupported lod level: " + flatLodLevel);
            }
            var setting = _configuration.SettingPerFlatLod[flatLodLevel];

            TerrainCardinalResolution detailResolution = setting.DetailResolution;
            bool directNormalCalculation = setting.DirectNormalComputation;

            var queryElementDetails = new List<TerrainDescriptionQueryElementDetail>()
            {
                new TerrainDescriptionQueryElementDetail()
                {
                    Resolution = detailResolution,
                    Type = TerrainDescriptionElementTypeEnum.HEIGHT_ARRAY
                },
            };
            //if (!directNormalCalculation) //we have to do it every time , as we need normals for spot updating!
            {
                queryElementDetails.Add(
                    new TerrainDescriptionQueryElementDetail()
                    {
                        Resolution = detailResolution,
                        Type = TerrainDescriptionElementTypeEnum.NORMAL_ARRAY
                    }
                );
            }

            var uniformsPack = new UniformsPack();
            uniformsPack.SetUniform("_LodLevel", flatLodLevel);

            var terrainOutput = await _terrainShapeDb.Query(new TerrainDescriptionQuery()
            {
                QueryArea = _terrainDetailAreaPosition,
                RequestedElementDetails = queryElementDetails
            });

            var heightmapTexture = terrainOutput.GetElementOfType(TerrainDescriptionElementTypeEnum.HEIGHT_ARRAY);
            uniformsPack.SetTexture("_HeightmapTex",
                heightmapTexture.TokenizedElement.DetailElement.Texture.Texture);
            uniformsPack.SetUniform("_HeightmapUv", heightmapTexture.UvBase.ToVector4());

            var normalAsTexture = terrainOutput.GetElementOfType(TerrainDescriptionElementTypeEnum.NORMAL_ARRAY);
            if (!directNormalCalculation)
            {
                uniformsPack.SetTexture("_NormalmapTex",
                    normalAsTexture.TokenizedElement.DetailElement.Texture.Texture);
                uniformsPack.SetUniform("_NormalmapUv", normalAsTexture.UvBase.ToVector4());
            }

            List<string> keywords = new List<string>();
            if (directNormalCalculation)
            {
                keywords.Add("DYNAMIC_NORMAL_GENERATION");
            }

            IGroundShapeToken token = null;
            if (_spotUpdater != null) //todo remove when tests over!
            {
                token = _spotUpdater.AddArea(new GroundShapeInfo()
                    {
                        TextureGlobalPosition = _terrainDetailAreaPosition,
                        TextureCoords = new MyRectangle(heightmapTexture.UvBase),
                        HeightmapResolution = detailResolution,
                    },
                    new UpdatedTerrainTextures()
                    {
                        HeightTexture = heightmapTexture.TokenizedElement.DetailElement.Texture.Texture,
                        NormalTexture = normalAsTexture.TokenizedElement.DetailElement.Texture.Texture,
                        TextureGlobalPosition = heightmapTexture.TokenizedElement.DetailElement.DetailArea,
                        TextureCoords = new MyRectangle(heightmapTexture.UvBase),
                    });
            }
            else
            {
                Debug.Log("T333 WARNING. SPOT UPDATER NOT SET. SHOUDL BE USED ONLY IN TESTING");
                token = new DummyGroundShapeToken();
            }

            return new GRingGroundShapeDetail()
            {
                ShaderKeywordSet = new ShaderKeywordSet(keywords),
                Uniforms = uniformsPack,
                GroundShapeToken = token,
                HeightDetailOutput = heightmapTexture
            };
        }
    }

    public class GroundShapeLevelSetting
    {
        public TerrainCardinalResolution DetailResolution;
        public bool DirectNormalComputation;
    }

    public class GRingGroundShapeProviderConfiguration
    {
        public Dictionary<int, GroundShapeLevelSetting> SettingPerFlatLod =
            new Dictionary<int, GroundShapeLevelSetting>()
            {
                {
                    1, new GroundShapeLevelSetting()
                    {
                        DetailResolution = TerrainCardinalResolution.MIN_RESOLUTION,
                        DirectNormalComputation = false
                    }
                },
                {
                    2, new GroundShapeLevelSetting()
                    {
                        DetailResolution = TerrainCardinalResolution.MIN_RESOLUTION,
                        DirectNormalComputation = false
                    }
                },
                {
                    3, new GroundShapeLevelSetting()
                    {
                        DetailResolution = TerrainCardinalResolution.MIN_RESOLUTION,
                        DirectNormalComputation = false
                    }
                },
                {
                    4, new GroundShapeLevelSetting()
                    {
                        DetailResolution = TerrainCardinalResolution.MID_RESOLUTION,
                        DirectNormalComputation = false
                    }
                },
                {
                    5, new GroundShapeLevelSetting()
                    {
                        DetailResolution = TerrainCardinalResolution.MID_RESOLUTION,
                        DirectNormalComputation = false
                    }
                },
                {
                    6, new GroundShapeLevelSetting()
                    {
                        DetailResolution = TerrainCardinalResolution.MID_RESOLUTION,
                        DirectNormalComputation = true
                    }
                },
                {
                    7, new GroundShapeLevelSetting()
                    {
                        DetailResolution = TerrainCardinalResolution.MAX_RESOLUTION,
                        DirectNormalComputation = true
                    }
                },
                {
                    8, new GroundShapeLevelSetting()
                    {
                        DetailResolution = TerrainCardinalResolution.MAX_RESOLUTION,
                        DirectNormalComputation = true
                    }
                },
                {
                    9, new GroundShapeLevelSetting()
                    {
                        DetailResolution = TerrainCardinalResolution.MAX_RESOLUTION,
                        DirectNormalComputation = true
                    }
                },
                {
                    10, new GroundShapeLevelSetting()
                    {
                        DetailResolution = TerrainCardinalResolution.MAX_RESOLUTION,
                        DirectNormalComputation = true
                    }
                },
                {
                    11, new GroundShapeLevelSetting()
                    {
                        DetailResolution = TerrainCardinalResolution.MAX_RESOLUTION,
                        DirectNormalComputation = true
                    }
                },
            };
    }
}