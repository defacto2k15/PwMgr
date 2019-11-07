using System;
using System.Collections.Generic;
using System.Linq;
using Assets.ETerrain.GroundTexture;
using Assets.ETerrain.Pyramid;
using Assets.ETerrain.Pyramid.Map;
using Assets.ETerrain.Pyramid.Shape;
using Assets.ETerrain.SectorFilling;
using Assets.ETerrain.TestUtils;
using Assets.ETerrain.Tools.HeightPyramidExplorer;
using Assets.Heightmaps.Ring1.MeshGeneration;
using Assets.Heightmaps.Ring1.RenderingTex;
using Assets.Heightmaps.Ring1.TerrainDescription.FeatureGenerating;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.Repositioning;
using Assets.Utils;
using Assets.Utils.MT;
using Assets.Utils.ShaderBuffers;
using Assets.Utils.TextureRendering;
using UnityEngine;
using Object = UnityEngine.Object;

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

    public class ETerrainHeightBuffersManager
    {
        private EPyramidShaderBuffersGenerator _configurationBufferGenerator;

        private ComputeBuffer _ePyramidPerFrameParametersBuffer;
        private ComputeBuffer _ePyramidConfigurationBuffer;

        public ETerrainHeightBuffersManager()
        {
            _configurationBufferGenerator = new EPyramidShaderBuffersGenerator();
        }

        public void InitializeBuffers(
            Dictionary<HeightPyramidLevel, EPyramidShaderBuffersGeneratorPerRingInput> input, int maxLevelsCount, int maxRingsInLevelsCount)
        {
            _ePyramidConfigurationBuffer = _configurationBufferGenerator.GenerateConfigurationBuffer(input, maxLevelsCount,maxRingsInLevelsCount);
            _ePyramidPerFrameParametersBuffer = _configurationBufferGenerator.GenerateEPyramidPerFrameParametersBuffer( maxLevelsCount);
        }

        public ComputeBuffer EPyramidConfigurationBuffer => _ePyramidConfigurationBuffer;
        public ComputeBuffer PyramidPerFrameParametersBuffer => _ePyramidPerFrameParametersBuffer;

        public void UpdateEPyramidPerFrameParametersBuffer(List<HeightPyramidLevel> levels, int maxLevelsCount, Dictionary<HeightPyramidLevel, Vector2> pyramidCenterPerLevel)
        {
            _configurationBufferGenerator.UpdateEPyramidPerFrameParametersBuffer(_ePyramidPerFrameParametersBuffer, levels, maxLevelsCount, pyramidCenterPerLevel);
        }
    }


    public class ETerrainHeightPyramidFacade
    {
        private ETerrainHeightBuffersManager _buffersManager;
        private readonly MeshGeneratorUTProxy _meshGeneratorUtProxy;
        private readonly UTTextureRendererProxy _textureRendererProxy;
        private readonly ETerrainHeightPyramidFacadeStartConfiguration _startConfiguration;

        private Dictionary<HeightPyramidLevel, HeightPyramidLevelEntities> _perLevelEntites;
        private HeightPyramidCommonEntites _pyramidCommonEntites;
        private Dictionary<HeightPyramidLevel, Vector2> _pyramidCenterWorldSpacePerLevel;
        private GameObject _pyramidRootGo;

        public ETerrainHeightPyramidFacade(ETerrainHeightBuffersManager buffersManager,
            MeshGeneratorUTProxy meshGeneratorUtProxy, UTTextureRendererProxy textureRendererProxy,
            ETerrainHeightPyramidFacadeStartConfiguration startConfiguration )
        {
            _buffersManager = buffersManager;
            _meshGeneratorUtProxy = meshGeneratorUtProxy;
            _textureRendererProxy = textureRendererProxy;
            _startConfiguration = startConfiguration;
        }

        public Dictionary<HeightPyramidLevel, HeightPyramidLevelTemplateWithShapeConfiguration> GenerateLevelTemplates()
        {
            var heightPyramidMapConfiguration = _startConfiguration.CommonConfiguration;
            var perLevelConfigurations = _startConfiguration.PerLevelConfigurations;

            return _startConfiguration.HeightPyramidLevels.ToDictionary(c => c, c =>
            {
                var perLevelConfiguration = perLevelConfigurations[c];

                var heightPyramidLevelShapeGenerationConfiguration = new HeightPyramidLevelShapeGenerationConfiguration()
                {
                    YScale = heightPyramidMapConfiguration.YScale,
                    PyramidLevelWorldSize = perLevelConfiguration.PyramidLevelWorldSize,
                    CenterObjectMeshVertexLength = heightPyramidMapConfiguration.SegmentTextureResolution,
                    CenterObjectLength = perLevelConfiguration.BiggestShapeObjectInGroupLength,
                    TransitionSingleStepPercent = perLevelConfiguration.TransitionSingleStepPercent,
                    PerRingMergeWidths = perLevelConfiguration.PerRingMergeWidths
                };

                var templateGenerator = new HeightPyramidLevelTemplateGenerator(heightPyramidLevelShapeGenerationConfiguration);
                var perLevelTemplate =
                    templateGenerator.CreateGroup(Vector2.zero, perLevelConfiguration.CreateCenterObject, heightPyramidMapConfiguration.RingsUvRange);
                return new HeightPyramidLevelTemplateWithShapeConfiguration()
                {
                    LevelTemplate = perLevelTemplate,
                    ShapeConfiguration = heightPyramidLevelShapeGenerationConfiguration
                };
            });
        }

        public void Start(
            Dictionary<HeightPyramidLevel, HeightPyramidLevelTemplateWithShapeConfiguration> templates,
            Dictionary<EGroundTextureType, OneGroundTypeLevelTextureEntitiesGenerator> groundEntitiesGenerators
            )
        {
            var heightLevels = _startConfiguration.HeightPyramidLevels.OrderBy(c=> c.GetIndex()).ToList();
            var heightPyramidMapConfiguration = _startConfiguration.CommonConfiguration;
            var perLevelConfigurations = _startConfiguration.PerLevelConfigurations;

            _pyramidRootGo = new GameObject("HeightPyramidRoot");
            _perLevelEntites = new Dictionary<HeightPyramidLevel, HeightPyramidLevelEntities>();
            foreach (var level in heightLevels)
            {
                var perLevelConfiguration = perLevelConfigurations[level];

                var perGroundTypesEntities = groundEntitiesGenerators.Select(c =>
                {
                    var groundType = c.Key;
                    var generators = c.Value;
                    var ceilTexture = new EGroundTexture(
                        generators.CeilTextureGenerator(),
                        groundType
                    );

                    var segmentsPlacer = generators.SegmentPlacerGenerator( ceilTexture.Texture);

                    var pyramidLevelManager = new GroundLevelTexturesManager(heightPyramidMapConfiguration.SlotMapSize);

                    var segmentAddingManager = new SoleLevelGroundTextureSegmentModificationsManager(segmentsPlacer, pyramidLevelManager);
                    var segmentFiller = new SegmentFiller(heightPyramidMapConfiguration.SlotMapSize, perLevelConfiguration.SegmentFillerStandByMarginsSize,
                        perLevelConfiguration.BiggestShapeObjectInGroupLength,
                        generators.LambdaSegmentFillingListenerGenerator(level, segmentAddingManager));

                    return new PerGroundTypeEntities()
                    {
                        CeilTexture = ceilTexture,
                        Filler = segmentFiller
                    };
                }).ToList();

                var heightPyramidLevelShapeGenerationConfiguration = templates[level].ShapeConfiguration;
                var perLevelTemplate = templates[level].LevelTemplate;
                var instancer = new HeightPyramidShapeInstancer(_meshGeneratorUtProxy, heightPyramidLevelShapeGenerationConfiguration);
                var group = instancer.CreateGroup(perLevelTemplate,level, _pyramidRootGo);

                var heightPyramidLocationParametersUpdaterConfiguration = new HeightPyramidLocationParametersUpdaterConfiguration()
                {
                    PyramidLevelWorldSize = perLevelConfiguration.PyramidLevelWorldSize,
                    TransitionSingleStep = perLevelConfiguration.BiggestShapeObjectInGroupLength * perLevelConfiguration.TransitionSingleStepPercent
                };
                var updater = new HeightPyramidLocationUniformsGenerator(heightPyramidLocationParametersUpdaterConfiguration);
                var transitionResolver = new HeightPyramidGroupTransitionResolver(group, heightPyramidLocationParametersUpdaterConfiguration);

                _perLevelEntites[level] = new HeightPyramidLevelEntities()
                {
                    PerGroundEntities = perGroundTypesEntities,
                    ShapeGroup = group,
                    LocationUniformsGenerator = updater,
                    TransitionResolver = transitionResolver,
                    LevelTemplate = perLevelTemplate,
                    PerLevelConfiguration = perLevelConfiguration,
                };
            }

            var heightmapUniformsSetter = new HeightPyramidSegmentsUniformsSetter();
            var groupMover = new HeightPyramidGroupMover();

            var pyramidLevelsWorldSizes =
                perLevelConfigurations.ToDictionary(c => c.Key, c => c.Value.PyramidLevelWorldSize.Width); // TODO works only for square pyramids - i use width
            ComputeBuffer configurationBuffer = _buffersManager.EPyramidConfigurationBuffer;
            var levelTextures = _perLevelEntites.ToDictionary(c => c.Key, c =>c.Value.PerGroundEntities.Select(k => k.CeilTexture).ToList());
            var bufferReloaderRootGo = Object.FindObjectOfType<BufferReloaderRootGO>();

            var ePyramidPerFrameParametersBuffer = _buffersManager.PyramidPerFrameParametersBuffer;
            foreach (var pair in _perLevelEntites)
            {
                var shapeGroup = pair.Value.ShapeGroup;
                var heightPyramidLevel = pair.Key;
                var ringsPerLevelCount = _startConfiguration.CommonConfiguration.MaxRingsPerLevelCount; //TODO
                heightmapUniformsSetter.InitializePyramidUniforms( shapeGroup, heightPyramidLevel, pyramidLevelsWorldSizes, heightPyramidMapConfiguration, levelTextures, heightLevels.Count, ringsPerLevelCount);
                heightmapUniformsSetter.InitializePerRingUniforms(shapeGroup, heightPyramidLevel, levelTextures, pyramidLevelsWorldSizes);
                heightmapUniformsSetter.PassPyramidBuffers(shapeGroup, configurationBuffer, bufferReloaderRootGo, ePyramidPerFrameParametersBuffer);

                foreach (var perGroundEntities in pair.Value.PerGroundEntities)
                {
                    perGroundEntities.Filler.InitializeField(_startConfiguration.InitialTravellerPosition);
                }
            }

            _pyramidCommonEntites = new HeightPyramidCommonEntites()
            {
                HeightPyramidMapConfiguration = heightPyramidMapConfiguration,
                HeightmapUniformsSetter = heightmapUniformsSetter,
                GroupMover = groupMover,
            };
        }

        public void Update(Vector2 flatPosition)
        {
            var transitionsDict = _perLevelEntites.ToDictionary(c => c.Key, c => c.Value.TransitionResolver.ResolveTransition(flatPosition));
            var uniformsDict = _perLevelEntites.ToDictionary(c => c.Key, c => c.Value.LocationUniformsGenerator.GenerateUniforms(transitionsDict[c.Key]));
            foreach (var pair in _perLevelEntites)
            {
                var entities = pair.Value;
                var groundLevel = pair.Key;
                _pyramidCommonEntites.HeightmapUniformsSetter.UpdateUniforms(entities.ShapeGroup, groundLevel, flatPosition, uniformsDict);
                _pyramidCommonEntites.GroupMover.MoveGroup(pair.Value.ShapeGroup, transitionsDict[groundLevel]);

                foreach (var perGroundEntities in entities.PerGroundEntities)
                {
                    perGroundEntities.Filler.Update(flatPosition);
                }
            }

            var heightLevels = _startConfiguration.HeightPyramidLevels.OrderBy(c=> c.GetIndex()).ToList();
            _pyramidCenterWorldSpacePerLevel = uniformsDict.ToDictionary(c => c.Key, c => c.Value.PyramidCenterWorldSpace);
            _buffersManager.UpdateEPyramidPerFrameParametersBuffer( heightLevels, _startConfiguration.CommonConfiguration.MaxLevelsCount, _pyramidCenterWorldSpacePerLevel);
        }

        public Dictionary<HeightPyramidLevel, Vector2> PyramidCenterWorldSpacePerLevel => _pyramidCenterWorldSpacePerLevel;

        public Dictionary<HeightPyramidLevel, List<EGroundTexture>> CeilTextures =>
            _perLevelEntites.ToDictionary(c => c.Key, c => c.Value.PerGroundEntities.Select(k => k.CeilTexture).ToList());

        public void DisableShapes()
        {
            foreach (var heightPyramidSegmentShapeGroup in _perLevelEntites.Values.Select(c=>c.ShapeGroup))
            {
                heightPyramidSegmentShapeGroup.DisableShapes();
            }
        }

        public void DisableLevelShapes(HeightPyramidLevel level)
        {
            foreach (var heightPyramidSegmentShapeGroup in _perLevelEntites.Where(c=>c.Key==level).Select(c=>c.Value.ShapeGroup))
            {
                heightPyramidSegmentShapeGroup.DisableShapes();
            }
        }

        public void SetShapeRootTransform(MyTransformTriplet myTransformTriplet)
        {
            myTransformTriplet.SetTransformTo(_pyramidRootGo.transform);
        }
    }


    public class HeightPyramidLevelEntities
    {
        public List<PerGroundTypeEntities> PerGroundEntities;
        public HeightPyramidSegmentShapeGroup ShapeGroup;
        public HeightPyramidLocationUniformsGenerator LocationUniformsGenerator;
        public HeightPyramidGroupTransitionResolver TransitionResolver;
        public HeightPyramidLevelTemplate LevelTemplate;
        public HeightPyramidPerLevelConfiguration PerLevelConfiguration;
    }

    public class HeightPyramidCommonEntites
    {
        public HeightPyramidSegmentsUniformsSetter HeightmapUniformsSetter;
        public HeightPyramidGroupMover GroupMover;
        public HeightPyramidCommonConfiguration HeightPyramidMapConfiguration;
    }

    public class HeightPyramidCommonConfiguration
    {
        public IntVector2 SlotMapSize { get; set; }
        public IntVector2 SegmentTextureResolution { get; set; }
        public float InterSegmentMarginSize { get; set; }
        public RenderTextureFormat HeightTextureFormat { get; set; }
        public RenderTextureFormat SurfaceTextureFormat { get; set; }

        public IntVector2 CeilTextureSize => SlotMapSize * SegmentTextureResolution;
        public float YScale { get; set; }

        public Dictionary<int, Vector2> RingsUvRange;

        public int MaxLevelsCount;
        public int MaxRingsPerLevelCount;
    }

    public class HeightPyramidPerLevelConfiguration
    {
        public IntVector2 SegmentFillerStandByMarginsSize;
        public float BiggestShapeObjectInGroupLength;
        public int OneRingShapeObjectsCount =>  HeightPyramidLevelShapeGenerationConfiguration.OneRingShapeObjectsCount;
        public MyRectangle PyramidLevelWorldSize => (new MyRectangle(0, 0, BiggestShapeObjectInGroupLength * (OneRingShapeObjectsCount + 2), BiggestShapeObjectInGroupLength * (OneRingShapeObjectsCount + 2)))
            .SubRectangle(new MyRectangle(-0.5f, -0.5f, 1, 1));

        public float TransitionSingleStepPercent;
        public bool CreateCenterObject;

        public Dictionary<int, float> PerRingMergeWidths;
    }

    public class HeightPyramidSegmentsUniformsSetter
    {
        public void InitializePerRingUniforms( HeightPyramidSegmentShapeGroup @group, HeightPyramidLevel groupLevel,
            Dictionary<HeightPyramidLevel, List<EGroundTexture>> levelsTexture, Dictionary<HeightPyramidLevel, float> pyramidLevelsWorldSizes )
        {
            if (group.CentralShape != null)
            {
                levelsTexture[groupLevel].ForEach(t => SetHeightmapUniformsToShape(group.CentralShape, t, null));
            }

            var maxRingIndex = group.ShapesPerRing.Keys.Max();
            group.ShapesPerRing[maxRingIndex]
                .ForEach(c => levelsTexture[groupLevel].ForEach(t => SetHeightmapUniformsToShape(c, t,
                    GetLowerLevelTexture(groupLevel, levelsTexture.ToDictionary(r => r.Key, r => r.Value.First(k => k.TextureType == t.TextureType))))));

            group.ShapesPerRing.Where(c => c.Key != maxRingIndex).SelectMany(c => c.Value).ToList()
                .ForEach(c => levelsTexture[groupLevel].ForEach(t =>  SetHeightmapUniformsToShape(c, t, null)));


            var higherLevel = groupLevel.GetHigherLevel();
            if (higherLevel.HasValue && levelsTexture.ContainsKey(higherLevel.Value))
            {
                group.CentralShapeMaterial.SetInt("_HigherLevelAreaCutting", 1);
                group.CentralShapeMaterial.SetFloat("_AuxPyramidLevelWorldSize", pyramidLevelsWorldSizes[higherLevel.Value]);
            }
            
            var lowerLevel = groupLevel.GetLowerLevel();
            if (lowerLevel.HasValue && levelsTexture.ContainsKey(lowerLevel.Value))
            {
                group.ShapesPerRing.SelectMany(c => c.Value).ToList().ForEach(
                    c => c.GetComponent<MeshRenderer>().material.SetFloat("_AuxPyramidLevelWorldSize", pyramidLevelsWorldSizes[lowerLevel.Value]));
            }
        }

       public void InitializePyramidUniforms( HeightPyramidSegmentShapeGroup @group, HeightPyramidLevel groupLevel,
           Dictionary<HeightPyramidLevel, float> pyramidLevelsWorldSizes, HeightPyramidCommonConfiguration heightPyramidMapConfiguration,
           Dictionary<HeightPyramidLevel, List<EGroundTexture>> levelTextures, int levelsCount, int ringsPerLevelCount)
        {
            group.ETerrainMaterials.ForEach(c =>
            {
                c.SetInt("_LevelsCount", levelsCount);
                c.SetInt("_RingsPerLevelCount", ringsPerLevelCount);
                c.SetFloat("_MainPyramidLevelWorldSize", pyramidLevelsWorldSizes[groupLevel]);
                c.SetVector("_LastRingSegmentUvRange", heightPyramidMapConfiguration.RingsUvRange[2/* TODO*/]);

                foreach (var pair in levelTextures)
                {
                    foreach (var groundTexture in pair.Value)
                    {
                        c.SetTexture("_" + groundTexture.Name + pair.Key.GetIndex(), groundTexture.Texture);
                    }
                }
            });
        }

       public void PassPyramidBuffers( HeightPyramidSegmentShapeGroup @group, ComputeBuffer configurationBuffer, BufferReloaderRootGO bufferReloaderRootGo, ComputeBuffer ePyramidPerFrameConfigurationBuffer)
        {
            group.ETerrainMaterials.ForEach(c =>
            {
                c.SetBuffer("_EPyramidConfigurationBuffer", configurationBuffer);
                bufferReloaderRootGo.RegisterBufferToReload(c, "_EPyramidConfigurationBuffer", configurationBuffer);

                c.SetBuffer("_EPyramidPerFrameConfigurationBuffer", ePyramidPerFrameConfigurationBuffer);
                bufferReloaderRootGo.RegisterBufferToReload(c, "_EPyramidPerFrameConfigurationBuffer", ePyramidPerFrameConfigurationBuffer);
            });
        }


        public void UpdateUniforms(HeightPyramidSegmentShapeGroup group, HeightPyramidLevel groupLevel, Vector2 travelerPosition, Dictionary<HeightPyramidLevel, LocationParametersUniforms> uniformsForAllLevels)
        {
            var thisLevelUniforms = uniformsForAllLevels[groupLevel];
            group.ETerrainMaterials.ForEach(c =>
            {
                c.SetVector("_TravellerPositionWorldSpace", travelerPosition); 
                c.SetVector("_MainPyramidCenterWorldSpace", thisLevelUniforms.PyramidCenterWorldSpace);
            });

            if (groupLevel == HeightPyramidLevel.Top) // TODO
            { 
                //Shader.SetGlobalVector("_GlobalTravellerPosition", thisLevelUniforms.TravellerPosition);
            }

            var higherLevel = groupLevel.GetHigherLevel();
            if (higherLevel.HasValue && uniformsForAllLevels.ContainsKey(higherLevel.Value))
            {
                var higherLevelUniforms = uniformsForAllLevels[higherLevel.Value];
                var mat = group.CentralShapeMaterial;
                mat.SetVector("_AuxPyramidCenterWorldSpace", higherLevelUniforms.PyramidCenterWorldSpace);
            }

            var lowerLevel = groupLevel.GetLowerLevel();
            if (lowerLevel.HasValue && uniformsForAllLevels.ContainsKey(lowerLevel.Value))
            {
                var lowerLevelUniforms = uniformsForAllLevels[lowerLevel.Value];
                group.ShapesPerRing.SelectMany(c => c.Value).ToList().ForEach(
                    c => c.GetComponent<MeshRenderer>().material.SetVector("_AuxPyramidCenterWorldSpace", lowerLevelUniforms.PyramidCenterWorldSpace));
            }
        }

        private void SetHeightmapUniformsToShape(GameObject groupCentralShape, EGroundTexture mainTexture, EGroundTexture auxTexture)
        {
            var material = groupCentralShape.GetComponent<MeshRenderer>().material;
            material.SetTexture("_Main" + mainTexture.Name, mainTexture.Texture);
            var auxTexturePresent = 0;
            if (auxTexture != null)
            {
                auxTexturePresent = 1;
                material.SetTexture("_Aux"+auxTexture.Name, auxTexture.Texture);
            }
            material.SetInt("_Aux"+mainTexture.Name+"Mode", auxTexturePresent);
        }

        private Texture GetHigherLevelTexture(HeightPyramidLevel level, Dictionary<HeightPyramidLevel, Texture> levelsTexture)
        {
            var higher = level.GetHigherLevel();
            if (higher.HasValue)
            {
                if (levelsTexture.ContainsKey(higher.Value))
                {
                    return levelsTexture[higher.Value];
                }
            }

            return null;
        }

        private EGroundTexture GetLowerLevelTexture(HeightPyramidLevel level, Dictionary<HeightPyramidLevel, EGroundTexture> levelsTexture)
        {
            var lower = level.GetLowerLevel();
            if (lower.HasValue)
            {
                if (levelsTexture.ContainsKey(lower.Value))
                {
                    return levelsTexture[lower.Value];
                }
            }

            return null;
        }
    }

    public class EPyramidShaderBuffersGeneratorPerRingInput
    {
        public Dictionary<int, Vector2> RingUvRanges;
        public float PyramidLevelWorldSize;
        public Dictionary<int, Vector2> HeightMergeRanges;
        public int CeilTextureResolution;
    }

    public class EPyramidShaderBuffersGenerator
    {
        struct ERingConfiguration
        {
            public Vector2 UvRange;
            public Vector2 MergeRange;
        };

        struct ELevelConfiguration
        {
            public ERingConfiguration[] RingsConfiguration;
            public float LevelWorldSize;
            public int CeilTextureResolution;
        };

        struct EPyramidConfiguration
        {
            public ELevelConfiguration[] LevelsConfiguration;
        };

        public ComputeBuffer GenerateConfigurationBuffer(Dictionary<HeightPyramidLevel,  EPyramidShaderBuffersGeneratorPerRingInput> input, int maxLevelsCount, int maxRingsInLevelsCount)
        {
            var ePyramidConfiguration = GenerateConfiguration(input);

            var floatsInBufferCount = maxLevelsCount * (maxRingsInLevelsCount * 4 + 2);

            var floatsArray = Enumerable.Range(0, floatsInBufferCount).Select(c => 0f).ToArray();

            var index = 0;
            foreach(var levelConfiguration in ePyramidConfiguration.LevelsConfiguration)
            {
                foreach (var ringConfiguration in levelConfiguration.RingsConfiguration)
                {
                    floatsArray[index + 0] = ringConfiguration.UvRange.x;
                    floatsArray[index + 1] = ringConfiguration.UvRange.y;
                    floatsArray[index + 2] = ringConfiguration.MergeRange.x;
                    floatsArray[index + 3] = ringConfiguration.MergeRange.y;
                    index += 4;
                }

                floatsArray[index] = levelConfiguration.LevelWorldSize;
                floatsArray[index + 1] = levelConfiguration.CeilTextureResolution;
                index += 2;
            }


            var buffer = new ComputeBuffer(1,floatsArray.Length* sizeof(float), ComputeBufferType.Default);
            buffer.SetData(floatsArray);
            return buffer;
        }

        public ComputeBuffer GenerateEPyramidPerFrameParametersBuffer( int maxLevelsCount)
        {
            var floatsArray = new float[maxLevelsCount*2];
            var buffer = new ComputeBuffer(1,floatsArray.Length* sizeof(float), ComputeBufferType.Default);
            return buffer;
        }

        public void UpdateEPyramidPerFrameParametersBuffer(ComputeBuffer buffer, List<HeightPyramidLevel> levels, int maxLevelsCount, Dictionary<HeightPyramidLevel, Vector2> pyramidCenterPerLevel)
        {
            var floatsArray = Enumerable.Range(0, maxLevelsCount * 2).Select(c => 0f).ToArray();
            for (var i = 0; i < levels.Count; i++)
            {
                var heightPyramidLevel = levels[i];
                floatsArray[i*2+0] = pyramidCenterPerLevel[heightPyramidLevel].x;
                floatsArray[i*2+1] = pyramidCenterPerLevel[heightPyramidLevel].y;
            }
            buffer.SetData(floatsArray);
        }

        private EPyramidConfiguration GenerateConfiguration(Dictionary<HeightPyramidLevel,  EPyramidShaderBuffersGeneratorPerRingInput> input)
        {
            var levels = input.Keys.OrderBy(c => c.GetIndex());
            return new EPyramidConfiguration()
            {
                LevelsConfiguration = levels.Select(i =>
                {
                    return new ELevelConfiguration
                    {
                        RingsConfiguration = Enumerable.Range(0, input[i].RingUvRanges.Count)
                            .Select(c => new ERingConfiguration()
                            {
                                UvRange = input[i].RingUvRanges[c],
                                MergeRange = input[i].HeightMergeRanges[c]
                            }).ToArray(),
                        LevelWorldSize = input[i].PyramidLevelWorldSize,
                        CeilTextureResolution = input[i].CeilTextureResolution
                    };
                }).ToArray()
            };
        }
    }
}
