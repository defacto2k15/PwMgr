using System;
using System.Collections.Generic;
using System.Linq;
using Assets.ETerrain.GroundTexture;
using Assets.ETerrain.Pyramid;
using Assets.ETerrain.Pyramid.Map;
using Assets.ETerrain.SectorFilling;
using Assets.ETerrain.TestUtils;
using Assets.ETerrain.Tools.HeightPyramidExplorer;
using Assets.Heightmaps.Ring1.MeshGeneration;
using Assets.Heightmaps.Ring1.RenderingTex;
using Assets.Heightmaps.Ring1.TerrainDescription.FeatureGenerating;
using Assets.Repositioning;
using Assets.Utils.MT;
using Assets.Utils.TextureRendering;
using UnityEngine;

namespace Assets.ETerrain.ETerrainIntegration
{
    public class  ETerrainIntegrationMultipleSegmentsDEO: MonoBehaviour
    {
        public GameObject Traveller;
        private HeightPyramidExplorer2 _explorer;
        private ETerrainHeightPyramidFacade _eTerrainHeightPyramidFacade;

        public void Start()
        {
            TaskUtils.SetGlobalMultithreading(false);
            ComputeShaderContainerGameObject containerGameObject = GameObject.FindObjectOfType<ComputeShaderContainerGameObject>();
            UTTextureRendererProxy textureRendererProxy = new UTTextureRendererProxy(new TextureRendererService(
                new MultistepTextureRenderer(containerGameObject), new TextureRendererServiceConfiguration()
                {
                    StepSize = new Vector2(400, 400)
                }));
            var meshGeneratorUtProxy = new MeshGeneratorUTProxy(new MeshGeneratorService());

            var startConfiguration = ETerrainHeightPyramidFacadeStartConfiguration.DefaultConfiguration;
            startConfiguration.HeightPyramidLevels = new List<HeightPyramidLevel>(){HeightPyramidLevel.Top, HeightPyramidLevel.Mid, HeightPyramidLevel.Bottom};

            ETerrainHeightBuffersManager buffersManager = new ETerrainHeightBuffersManager();
            _eTerrainHeightPyramidFacade = new ETerrainHeightPyramidFacade(buffersManager,meshGeneratorUtProxy, textureRendererProxy, startConfiguration);

            var perLevelTemplates = _eTerrainHeightPyramidFacade.GenerateLevelTemplates();
            var levels = startConfiguration.PerLevelConfigurations.Keys.Where(c=> startConfiguration.HeightPyramidLevels.Contains(c));
            buffersManager.InitializeBuffers(levels.ToDictionary(c => c, c => new EPyramidShaderBuffersGeneratorPerRingInput()
            {
                CeilTextureResolution = startConfiguration.CommonConfiguration.CeilTextureSize.X,  //TODO i use only X, - works only for squares
                HeightMergeRanges = perLevelTemplates[c].LevelTemplate.PerRingTemplates.ToDictionary(k => k.Key, k => k.Value.HeightMergeRange),
                PyramidLevelWorldSize = startConfiguration.PerLevelConfigurations[c].PyramidLevelWorldSize.Width,  // TODO works only for square pyramids - i use width
                RingUvRanges = startConfiguration.CommonConfiguration.RingsUvRange
            }),startConfiguration.CommonConfiguration.MaxLevelsCount, startConfiguration.CommonConfiguration.MaxRingsPerLevelCount);

            _eTerrainHeightPyramidFacade.Start( perLevelTemplates,
                new Dictionary<EGroundTextureType, OneGroundTypeLevelTextureEntitiesGenerator>()
                {
                    {
                        EGroundTextureType.HeightMap, new OneGroundTypeLevelTextureEntitiesGenerator()
                        {
                            LambdaSegmentFillingListenerGenerator =
                                (level, segmentModificationManager) => new LambdaSegmentFillingListener(
                                    (c) =>
                                    {
                                        var segmentTexture = CreateDummySegmentTexture(c, level);
                                        segmentModificationManager.AddSegment(segmentTexture, c.SegmentAlignedPosition);
                                    },
                                    (c) => { },
                                    (c) => { }),
                            CeilTextureGenerator = () => EGroundTextureGenerator.GenerateEmptyGroundTexture(
                                startConfiguration.CommonConfiguration.CeilTextureSize, startConfiguration.CommonConfiguration.HeightTextureFormat),
                            SegmentPlacerGenerator = (ceilTexture) =>
                            {
                                var modifiedCornerBuffer =
                                    EGroundTextureGenerator.GenerateModifiedCornerBuffer(startConfiguration.CommonConfiguration.SegmentTextureResolution,
                                        startConfiguration.CommonConfiguration.HeightTextureFormat);

                                return new HeightSegmentPlacer(textureRendererProxy, ceilTexture, startConfiguration.CommonConfiguration.SlotMapSize,
                                    startConfiguration.CommonConfiguration.CeilTextureSize, startConfiguration.CommonConfiguration.InterSegmentMarginSize,
                                    modifiedCornerBuffer);
                            }
                        }
                    }
                }
            );

            Traveller.transform.position = new Vector3(startConfiguration.InitialTravellerPosition.x, 0, startConfiguration.InitialTravellerPosition.y);
            _explorer = new HeightPyramidExplorer2(_eTerrainHeightPyramidFacade.CeilTextures
                .ToDictionary(c => c.Key, c => c.Value.First(r => r.TextureType == EGroundTextureType.HeightMap).Texture as Texture));

            //_eTerrainHeightPyramidFacade.DisableLevelShapes(HeightPyramidLevel.Bottom);
        }

        public void OnGUI()
        {
            _explorer.OnGUI();
        }

        public void Update()
        {
            var position3D = Traveller.transform.position;
            var flatPosition = new Vector2(position3D.x, position3D.z);

            _eTerrainHeightPyramidFacade.Update(flatPosition);
        }

        public static Texture CreateDummySegmentTexture(SegmentInformation segmentInformation, HeightPyramidLevel level)
        {
            var tex = new Texture2D(240, 240, TextureFormat.RFloat, false);
            int heightLevels = 8;
            var height = ((segmentInformation.SegmentAlignedPosition.X + segmentInformation.SegmentAlignedPosition.Y + (heightLevels/2)) % heightLevels) / ((float)heightLevels);

            float multiplier = 1 / 3f;
            float offset= level.GetIndex() * multiplier;
            height = height * multiplier + offset;

            for (int x = 0; x < tex.width; x++)
            {
                for (int y = 0; y < tex.height; y++)
                {
                    if (level == HeightPyramidLevel.Mid )
                    {
                        height = y / ((float) tex.width / 2.0f);
                    }
                    else
                    {
                        height = x / ((float) tex.height);

                    }

                    tex.SetPixel(x,y, new Color(height,0,0));
                }
            }

            tex.Apply();
            return tex;
        }
    }
}
