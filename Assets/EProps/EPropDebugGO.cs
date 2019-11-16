using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.ComputeShaders;
using Assets.ETerrain.ETerrainIntegration;
using Assets.ETerrain.ETerrainIntegration.deos;
using Assets.ETerrain.GroundTexture;
using Assets.ETerrain.Pyramid;
using Assets.ETerrain.Pyramid.Map;
using Assets.ETerrain.SectorFilling;
using Assets.ETerrain.Tools.HeightPyramidExplorer;
using Assets.Heightmaps.Ring1.MeshGeneration;
using Assets.Heightmaps.Ring1.RenderingTex;
using Assets.Heightmaps.Ring1.TerrainDescription.FeatureGenerating;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.MeshGeneration;
using Assets.Utils;
using Assets.Utils.MT;
using Assets.Utils.ShaderBuffers;
using Assets.Utils.TextureRendering;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

namespace Assets.EProps
{
    public class EPropDebugGO : MonoBehaviour
    {
        public GameObject Traveller;

        public int ObjectsToCreateCount = 20;
        public int ObjectsToCreateOnOneLine = 20;
        public float MaxFlatPosition = 400;
        public Mesh DebugMesh;
        public Material DebugMeshMaterial;
        private EPropElevationManager _elevationManager;

        private ETerrainHeightPyramidFacade _eTerrainHeightPyramidFacade;

        public bool GenerateObjects;
        public bool DrawObjectsWithInstancing;
        public bool DisplayDummyObjects;
        private EPropDummyObjectsDisplayer _dummyObjectsDisplayer;
        private EPropDummyObjectsInstancingDisplayer _dummyObjectsInstancingDisplayer;
        public bool DisplaySelectorAreas;
        private EPropDebugSectorAreasDisplayer _sectorAreasDisplayer;
        public bool DisplayMergeRings;
        private EPropDebugMergeRingsDisplayer _mergeRingsDisplayer;

        public bool RecalculateSectorsDivision;

        private EPropHotAreaSelector _ePropHotAreaSelector;

        public void Start()
        {
            UnityEngine.Random.InitState(412);
            TaskUtils.SetGlobalMultithreading(false);
            UnityThreadComputeShaderExecutorObject shaderExecutorObject = new UnityThreadComputeShaderExecutorObject();
            ComputeShaderContainerGameObject containerGameObject = GameObject.FindObjectOfType<ComputeShaderContainerGameObject>();
            UTTextureRendererProxy textureRendererProxy = new UTTextureRendererProxy(new TextureRendererService(
                new MultistepTextureRenderer(containerGameObject), new TextureRendererServiceConfiguration()
                {
                    StepSize = new Vector2(400, 400)
                }));
            var meshGeneratorUtProxy = new MeshGeneratorUTProxy(new MeshGeneratorService());

            var startConfiguration = ETerrainHeightPyramidFacadeStartConfiguration.DefaultConfiguration;

            ETerrainHeightBuffersManager buffersManager = new ETerrainHeightBuffersManager();
            _eTerrainHeightPyramidFacade = new ETerrainHeightPyramidFacade(buffersManager, meshGeneratorUtProxy, textureRendererProxy, startConfiguration);

            var perLevelTemplates = _eTerrainHeightPyramidFacade.GenerateLevelTemplates();

            var levels = startConfiguration.PerLevelConfigurations.Keys;
            var ePyramidShaderBuffersGeneratorPerRingInputs = levels.ToDictionary(c => c, c => new EPyramidShaderBuffersGeneratorPerRingInput()
            {
                CeilTextureResolution = startConfiguration.CommonConfiguration.CeilTextureSize.X,  //TODO i use only X, - works only for squares
                HeightMergeRanges = perLevelTemplates[c].LevelTemplate.PerRingTemplates.ToDictionary(k => k.Key, k => k.Value.HeightMergeRange),
                PyramidLevelWorldSize = startConfiguration.PerLevelConfigurations[c].PyramidLevelWorldSize.Width,  // TODO works only for square pyramids - i use width
                RingUvRanges = startConfiguration.CommonConfiguration.RingsUvRange
            });
            buffersManager.InitializeBuffers(ePyramidShaderBuffersGeneratorPerRingInputs, startConfiguration.CommonConfiguration.MaxLevelsCount, startConfiguration.CommonConfiguration.MaxRingsPerLevelCount);

            _eTerrainHeightPyramidFacade.Start(perLevelTemplates,
                new Dictionary<EGroundTextureType, OneGroundTypeLevelTextureEntitiesGenerator>()
                {
                    {
                        EGroundTextureType.HeightMap, new OneGroundTypeLevelTextureEntitiesGenerator()
                        {
                            GeneratorFunc = (level) =>
                            {
                                var ceilTexture =
                                    EGroundTextureGenerator.GenerateEmptyGroundTexture(startConfiguration.CommonConfiguration.CeilTextureSize,
                                        startConfiguration.CommonConfiguration.HeightTextureFormat);
                                var segmentsPlacer = new ESurfaceSegmentPlacer(textureRendererProxy, ceilTexture
                                    , startConfiguration.CommonConfiguration.SlotMapSize, startConfiguration.CommonConfiguration.CeilTextureSize);
                                var pyramidLevelManager = new GroundLevelTexturesManager(startConfiguration.CommonConfiguration.SlotMapSize);
                                var segmentModificationManager = new SoleLevelGroundTextureSegmentModificationsManager(segmentsPlacer, pyramidLevelManager);
                                return new SegmentFillingListenerWithCeilTexture()
                                {
                                    CeilTexture = ceilTexture,
                                    SegmentFillingListener = new LambdaSegmentFillingListener(
                                        (c) =>
                                        {
                                            var segmentTexture = ETerrainIntegrationMultipleSegmentsDEO.CreateDummySegmentTexture(c, level);
                                            segmentModificationManager.AddSegmentAsync(segmentTexture, c.SegmentAlignedPosition);
                                        },
                                        (c) => { },
                                        (c) => { }

                                    )
                                };

                            },
                        }
                    }
                }
            );
            //_eTerrainHeightPyramidFacade.DisableShapes();
            _eTerrainHeightPyramidFacade.DisableLevelShapes( HeightPyramidLevel.Bottom);

            Traveller.transform.position = new Vector3(startConfiguration.InitialTravellerPosition.x, 0, startConfiguration.InitialTravellerPosition.y);

            var ePropLocationConfiguration = new EPropElevationConfiguration();
            EPropConstantPyramidParameters ePropConstantPyramidParameters = new EPropConstantPyramidParameters()
            {
                LevelsCount = startConfiguration.HeightPyramidLevels.Count,
                RingsPerLevelCount = startConfiguration.CommonConfiguration.MaxRingsPerLevelCount, //TODO parametrize
                HeightScale =startConfiguration.CommonConfiguration.YScale
            };
            _elevationManager = new EPropElevationManager( ePropLocationConfiguration, shaderExecutorObject, ePropConstantPyramidParameters);
            _elevationManager.Initialize(buffersManager.PyramidPerFrameParametersBuffer, buffersManager.EPyramidConfigurationBuffer,
                _eTerrainHeightPyramidFacade.CeilTextures.ToDictionary(c => c.Key, c => c.Value.First(r => r.TextureType == EGroundTextureType.HeightMap).Texture as Texture) );
            var ePropLocaleBuffer = _elevationManager.EPropLocaleBuffer;
            var ePropIdsBuffer = _elevationManager.EPropIdsBuffer;

            _dummyObjectsDisplayer = new EPropDummyObjectsDisplayer(ePropLocationConfiguration.ScopeLength, ePropLocaleBuffer, ePropIdsBuffer, FindObjectOfType<BufferReloaderRootGO>());
            _dummyObjectsDisplayer.Start();

            _dummyObjectsInstancingDisplayer = new EPropDummyObjectsInstancingDisplayer(DebugMesh, DebugMeshMaterial);
            _dummyObjectsInstancingDisplayer.Start();
            _dummyObjectsInstancingDisplayer.SetLocaleBuffers(ePropLocaleBuffer, ePropIdsBuffer, ePropLocationConfiguration.ScopeLength);

            _sectorAreasDisplayer = new EPropDebugSectorAreasDisplayer();
            _sectorAreasDisplayer.Start();

            _mergeRingsDisplayer = new EPropDebugMergeRingsDisplayer();
            _mergeRingsDisplayer.Start();
            var levelWorldSizes = startConfiguration.PerLevelConfigurations.ToDictionary(c=>c.Key, c=>c.Value.PyramidLevelWorldSize.Size);
            var ringMergeRanges = perLevelTemplates.ToDictionary(c => c.Key,
                c => c.Value.LevelTemplate.PerRingTemplates.ToDictionary(k => k.Key, k => k.Value.HeightMergeRange));
            _ePropHotAreaSelector = new EPropHotAreaSelector(levelWorldSizes, ringMergeRanges);

            _elevationManager.DebugInitializeSectors(new MyRectangle(-200, -200, 400, 400));
        }

        public void Update()
        {
            var position3D = Traveller.transform.position;
            var travellerFlatPosition = new Vector2(position3D.x, position3D.z);
            _eTerrainHeightPyramidFacade.Update(travellerFlatPosition);
            DrawObjects();

            var selectorWithParameters = EPropHotAreaSelectorWithParameters.Create(_ePropHotAreaSelector, _eTerrainHeightPyramidFacade.PyramidCenterWorldSpacePerLevel, travellerFlatPosition);
             _elevationManager.Update(travellerFlatPosition, _eTerrainHeightPyramidFacade.PyramidCenterWorldSpacePerLevel, selectorWithParameters);

            if (Time.frameCount % 100 == 0)
            {
                if (RecalculateSectorsDivision)
                {
                    var propLocaleChanges = _elevationManager.RecalculateSectorsDivision(travellerFlatPosition);
                    _dummyObjectsDisplayer.ProcessLocaleChanges(propLocaleChanges);
                    _dummyObjectsInstancingDisplayer.ProcessLocaleChanges(propLocaleChanges);
                }
                _sectorAreasDisplayer.Update(_elevationManager.DebugQuerySectorStates(selectorWithParameters));
            }

            var rt4 = selectorWithParameters.MergeRings;
            _mergeRingsDisplayer.Update(rt4);

            _mergeRingsDisplayer.SetActive(DisplayMergeRings);
            _sectorAreasDisplayer.SetActive(DisplaySelectorAreas);
            _dummyObjectsDisplayer.SetActive(DisplayDummyObjects);


            if (Time.frameCount > 10000 || _dummyObjectsInstancingDisplayer.CurrentObjectsCount > ObjectsToCreateCount)
            {
                return;
            }

            if (GenerateObjects)
            {
                var sideLength = 400f;
                var axisCount = ObjectsToCreateOnOneLine;
                var newPropIds = Enumerable.Range(0, axisCount).SelectMany(x =>

                Enumerable.Range(0, axisCount).Select(y =>
                    {
                        var flatPosition = new Vector2(((float) x / axisCount - 0.5f) * sideLength, ((float) y / axisCount - 0.5f) * sideLength);
                        return new DebugFlatPositionWithEPropPointerAndId()
                        {
                            PointerWithId = _elevationManager.DebugRegisterPropWithElevationId(flatPosition),
                            FlatPosition = flatPosition
                        };
                    }
                )).ToArray();
                AddProps(newPropIds);
            }
        }

        private void AddProps(DebugFlatPositionWithEPropPointerAndId[] newPropIds)
        {
            _dummyObjectsInstancingDisplayer.AddProps(newPropIds);
            _dummyObjectsDisplayer.AddObjects(newPropIds.ToList());
        }

        private void DrawObjects()
        {
            if (DrawObjectsWithInstancing)
            {
                _dummyObjectsInstancingDisplayer.DrawObjects();
            }
        }
    }
    public class DebugFlatPositionWithEPropPointerAndId
    {
        public Vector2 FlatPosition;
        public EPropPointerWithId PointerWithId;
    }

    public class EPropDummyObjectsInstancingDisplayer
    {
        private static int MaxObjectsCount = 1000000;
        private int _currentObjectsCount = 0;
        private Matrix4x4[] _maticesArray = new Matrix4x4[MaxObjectsCount];
        private float[] _localeBufferScopeIndexArray = new float[MaxObjectsCount];
        private float[] _inScopeIndexArray = new float[MaxObjectsCount];
        private float[] _pointersArray = new float[MaxObjectsCount];
        private Dictionary<EPropElevationId, int> _elevationToIndexDict;

        private MaterialPropertyBlock _propertyBlock;
        private Mesh _debugMesh;
        private Material _debugMeshMaterial;

        public EPropDummyObjectsInstancingDisplayer(Mesh debugMesh, Material debugMeshMaterial)
        {
            _debugMesh = debugMesh;
            _debugMeshMaterial = debugMeshMaterial;
            _elevationToIndexDict = new Dictionary<EPropElevationId, int>();
        }

        public int CurrentObjectsCount => _currentObjectsCount;

        public void Start()
        {
            _propertyBlock = new MaterialPropertyBlock();
        }

        public void AddProps(DebugFlatPositionWithEPropPointerAndId[] newPropIds)
        {
            for (var i = 0; i < newPropIds.Length; i++)
            {
                var eProp = newPropIds[i];
                var newIndex = _currentObjectsCount + i;
                _localeBufferScopeIndexArray[newIndex] = CastUtils.BitwiseCastUIntToFloat(eProp.PointerWithId.Id.LocaleBufferScopeIndex);
                _inScopeIndexArray[newIndex] = CastUtils.BitwiseCastUIntToFloat(eProp.PointerWithId.Id.InScopeIndex);
                _maticesArray[newIndex] = Matrix4x4.TRS(new Vector3(eProp.FlatPosition.x, 0, eProp.FlatPosition.y),
                    Quaternion.identity, new Vector3(1, 1, 1));
                _pointersArray[newIndex] = CastUtils.BitwiseCastUIntToFloat(newPropIds[i].PointerWithId.Pointer.Value);
                _elevationToIndexDict[eProp.PointerWithId.Id] = newIndex;
            }

            _propertyBlock.SetFloatArray("_LocaleBufferScopeIndexArray", _localeBufferScopeIndexArray);
            _propertyBlock.SetFloatArray("_InScopeIndexArray", _inScopeIndexArray);
            _propertyBlock.SetFloatArray("_Pointer", _pointersArray);
            _currentObjectsCount += newPropIds.Length;
        }

        public void SetLocaleBuffers(ComputeBuffer ePropLocaleBuffer, ComputeBuffer ePropIdsBuffer, float scopeLength)
        {
            _propertyBlock.SetBuffer("_EPropLocaleBuffer", ePropLocaleBuffer);
            _propertyBlock.SetBuffer("_EPropIdsBuffer", ePropIdsBuffer);
            _propertyBlock.SetFloat("_ScopeLength", scopeLength);
        }

        private static int MaxObjectsPerInstancingPass = 1023;
        private Matrix4x4[] _tempMaticesArray = new Matrix4x4[MaxObjectsPerInstancingPass];
        private float[] _tempLocaleBufferScopeIndexArray = new float[MaxObjectsPerInstancingPass];
        private float[] _tempInScopeIndexArray = new float[MaxObjectsPerInstancingPass];

        public void DrawObjects()
        {
            var passCount = Mathf.CeilToInt(_currentObjectsCount / ((float) MaxObjectsPerInstancingPass));
            for (var passIndex = 0; passIndex < passCount; passIndex++)
            {
                var offset = passIndex * MaxObjectsPerInstancingPass;
                var inThisPassCount = Mathf.Min(MaxObjectsPerInstancingPass, _currentObjectsCount - offset);

                Array.Copy(_maticesArray, offset, _tempMaticesArray, 0, inThisPassCount);
                Array.Copy(_localeBufferScopeIndexArray, offset, _tempLocaleBufferScopeIndexArray, 0, inThisPassCount);
                Array.Copy(_inScopeIndexArray, offset, _tempInScopeIndexArray, 0, inThisPassCount);
                _propertyBlock.SetFloatArray("_LocaleBufferScopeIndexArray", _tempLocaleBufferScopeIndexArray);
                _propertyBlock.SetFloatArray("_InScopeIndexArray", _tempInScopeIndexArray);

                Graphics.DrawMeshInstanced(_debugMesh, 0, _debugMeshMaterial, _tempMaticesArray, inThisPassCount, _propertyBlock, ShadowCastingMode.Off);
            }
        }

        public void ProcessLocaleChanges(List<EPropIdChange> propLocaleChanges)
        {
            foreach (var change in propLocaleChanges)
            {
                Debug.Log("Instancing PROCESSING CHANGE");
                var idx = _elevationToIndexDict[change.OldId];
                _elevationToIndexDict.Remove(change.OldId);
                _elevationToIndexDict[change.NewId] = idx;

                _localeBufferScopeIndexArray[idx] = CastUtils.BitwiseCastUIntToFloat(change.NewId.LocaleBufferScopeIndex);
                _inScopeIndexArray[idx] = CastUtils.BitwiseCastUIntToFloat(change.NewId.InScopeIndex);
            }
        }
    }

    public class EPropDummyObjectsDisplayer
    {
        private int _scopeLength;
        private ComputeBuffer _ePropLocaleBuffer;
        private ComputeBuffer _ePropIdsBuffer;
        private BufferReloaderRootGO _bufferReloaderRootGo;
        private Mesh _dummyMesh;
        
        private GameObject _rootParent;
        private Dictionary<uint, GameObject> _perScopeParents;
        private Dictionary<EPropElevationId, GameObject> _dummyObjects;

        public EPropDummyObjectsDisplayer(int scopeLength, ComputeBuffer ePropLocaleBuffer, ComputeBuffer ePropIdsBuffer, BufferReloaderRootGO bufferReloaderRootGo)
        {
            _scopeLength = scopeLength;
            _ePropLocaleBuffer = ePropLocaleBuffer;
            _ePropIdsBuffer = ePropIdsBuffer;
            _bufferReloaderRootGo = bufferReloaderRootGo;
        }

        public void Start()
        {
            _rootParent = new GameObject("EPropDummyObjectDisplayer");
            _perScopeParents = new Dictionary<uint, GameObject>();
            _dummyObjects = new Dictionary<EPropElevationId, GameObject>();

            var newGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _dummyMesh = MeshGenerationUtils.CloneMesh(newGo.GetComponent<MeshFilter>().mesh);
            var bounds = _dummyMesh.bounds;
            bounds.Expand(1000);
            _dummyMesh.bounds = bounds;
        }

        public void AddObjects(List<DebugFlatPositionWithEPropPointerAndId> positionsWithIds)
        {
            foreach (var aPositionsWithId in positionsWithIds)
            {
                var scopeIndex = aPositionsWithId.PointerWithId.Id.LocaleBufferScopeIndex;
                if (!_perScopeParents.ContainsKey(scopeIndex))
                {
                    var newGo = new GameObject(scopeIndex.ToString());
                    newGo.transform.SetParent(_rootParent.transform);
                    _perScopeParents[scopeIndex] = newGo;
                }

                var newDummy = CreateDummy(aPositionsWithId);
                newDummy.transform.SetParent(_perScopeParents[scopeIndex].transform);
                _dummyObjects[aPositionsWithId.PointerWithId.Id] = newDummy;
            }
        }

        private GameObject CreateDummy(DebugFlatPositionWithEPropPointerAndId dummy)
        {
            var material = new Material(Shader.Find("Custom/EProp/Dummy"));
            material.SetInt("_ScopeLength", _scopeLength);
            material.SetBuffer("_EPropLocaleBuffer", _ePropLocaleBuffer);
            material.SetBuffer("_EPropIdsBuffer", _ePropIdsBuffer);
            _bufferReloaderRootGo.RegisterBufferToReload(material, "_EPropLocaleBuffer", _ePropLocaleBuffer);

            var newGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            GameObject.Destroy(newGo.GetComponent<SphereCollider>());
            newGo.AddComponent<BoxCollider>().size = new Vector3(1,100,1);
            newGo.GetComponent<MeshFilter>().mesh = _dummyMesh;
            newGo.GetComponent<MeshRenderer>().allowOcclusionWhenDynamic = false;
            newGo.GetComponent<MeshRenderer>().material = material;
            newGo.transform.position = new Vector3(dummy.FlatPosition.x, 0, dummy.FlatPosition.y);
            newGo.transform.localScale = new Vector3(2,2, 2);

            SetIdAndLocaleIndexes(newGo, dummy.PointerWithId);
            return newGo;
        }

        private void SetLocaleIndexes(GameObject go, EPropElevationId id)
        {
            var material = go.GetComponent<MeshRenderer>().material;
            material.SetInt("_LocaleBufferScopeIndex", (int)id.LocaleBufferScopeIndex);
            material.SetInt("_InScopeIndex", (int)id.InScopeIndex);
        }

        private void SetIdAndLocaleIndexes(GameObject go, EPropPointerWithId pointerAndId)
        {
            SetLocaleIndexes(go,pointerAndId.Id);
            var material = go.GetComponent<MeshRenderer>().material;
            material.SetInt("_Pointer", (int) pointerAndId.Pointer.Value);
        }

        public void SetActive(bool active)
        {
            _rootParent.SetActive(active);
        }

        public void ProcessLocaleChanges(List<EPropIdChange> propLocaleChanges)
        {
            foreach (var change in propLocaleChanges)
            {
                Debug.Log("PROCESSING CHANGE");
                var dummyObject = _dummyObjects[change.OldId];

                var newScopeIndex = change.NewId.LocaleBufferScopeIndex;
                if (!_perScopeParents.ContainsKey(newScopeIndex))
                {
                    _perScopeParents[newScopeIndex] = new GameObject(newScopeIndex.ToString());
                    _perScopeParents[newScopeIndex].transform.SetParent(_rootParent.transform);
                }

                var newScopeGo = _perScopeParents[newScopeIndex];
                dummyObject.transform.SetParent(newScopeGo.transform);

                SetLocaleIndexes(dummyObject, change.NewId);
                _dummyObjects.Remove(change.OldId);
                _dummyObjects[change.NewId] = dummyObject;

                dummyObject.name = "CL " + dummyObject.name;
            }
        }
    }

    public class EPropDebugSectorAreasDisplayer
    {
        private GameObject _rootParent;
        private Dictionary<MyQuantRectangle, GameObject> _sectorObjects = new Dictionary<MyQuantRectangle, GameObject>();

        public void Start()
        {
            _rootParent = new GameObject("EPropSectorsAreasRoot");
            _rootParent.transform.position = new Vector3(0,10,0);
        }

        public void SetActive(bool active)
        {
            _rootParent.SetActive(active);
        }

        public void Update(List<DebugSectorInformation> debugSectorsInformation)
        {
            var usedRectangles = debugSectorsInformation.SelectMany(c => GenerateSectorDebugObject(_rootParent.transform, c)).ToList();
            var rectanglesThatDisappearedGo = _sectorObjects.Where(c => !usedRectangles.Contains(c.Key)).ToList();
            foreach (var pair in rectanglesThatDisappearedGo)
            {
                GameObject.Destroy(pair.Value);
                _sectorObjects.Remove(pair.Key);
            }
        }

        private List<MyQuantRectangle> GenerateSectorDebugObject(Transform parentTransform, DebugSectorInformation debugSectorInformation)
        {
            var rectangle = debugSectorInformation.Area;

            if (!_sectorObjects.ContainsKey(rectangle))
            {
                var newGo = GameObject.CreatePrimitive(PrimitiveType.Quad);
                newGo.name = $"D:{debugSectorInformation.Depth} {rectangle}";
                newGo.transform.SetParent(_rootParent.transform);
                newGo.transform.localScale =
                    new Vector3(rectangle.RealSpaceRectangle.Width, rectangle.RealSpaceRectangle.Height, 1);
                newGo.transform.localRotation = Quaternion.Euler(90, 0, 0);

                var transformLocalPosition = new Vector3(
                    rectangle.RealSpaceRectangle.X + rectangle.RealSpaceRectangle.Width / 2
                    , 0.1f + debugSectorInformation.Depth/10f
                    , rectangle.RealSpaceRectangle.Y + rectangle.RealSpaceRectangle.Height / 2);
                newGo.transform.localPosition = transformLocalPosition;
                    _sectorObjects[rectangle] = newGo;
            }

            var sectorObject = _sectorObjects[rectangle];
            sectorObject.transform.SetParent(parentTransform);
            var material = sectorObject.GetComponent<MeshRenderer>().material;
            var state = debugSectorInformation.SectorState;
            if (state == EPropSectorState.Cold)
            {
                material.color = Color.red;
            }
            else
            {
                material.color = Color.green;
            }

            var toReturn = new List<MyQuantRectangle> {rectangle};
            toReturn.AddRange(debugSectorInformation.Children.SelectMany(c => GenerateSectorDebugObject(sectorObject.transform, c)));
            return toReturn;
        }
    }


    public class EPropDebugMergeRingsDisplayer
    {
        private GameObject _rootParent;
        private List<GameObject> _mergeRingObjects = new List<GameObject>();

        public void Start()
        {
            _rootParent = new GameObject("MergeRingsRoot");
        }

        public void Update(Dictionary<HeightPyramidLevel, Dictionary<int, EPropMergeRing>> ringsDict)
        {
            var rings = ringsDict.SelectMany(c => c.Value.Select(k => new {ckey = c.Key, kkey = k.Key, kvalue = k.Value})).ToList();

            if (rings.Count*2 != _mergeRingObjects.Count)
            {
                RecreateDummyRingObjects(rings.Count*2);
            }

            var i = 0;
            foreach (var ringInfo in rings) 
            {
                foreach (var rectangle in new List<MyRectangle> {ringInfo.kvalue.InnerRectangle, ringInfo.kvalue.OuterRectangle})
                {
                    var go = _mergeRingObjects[i];

                    var center = rectangle.Center;
                    var size = rectangle.Size;
                    go.transform.localScale = new Vector3(size.x, size.y, 1);
                    go.transform.position = new Vector3(center.x, go.transform.position.y, center.y);
                    go.name = $"{rectangle} level:{ringInfo.ckey} ring:{ringInfo.kkey}";
                    i++;
                }
            }
        }

        private void RecreateDummyRingObjects(int count )
        {
            foreach (var go in _mergeRingObjects)
            {
                GameObject.Destroy(go);
            }

            _mergeRingObjects = Enumerable.Range(0, count).Select(i =>
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
                go.transform.localRotation = Quaternion.Euler(90, 0, 0);
                go.transform.localPosition = new Vector3(0,20 - i/10f, 0);
                go.transform.SetParent(_rootParent.transform);
                go.GetComponent<MeshRenderer>().material.color = UnityEngine.Random.ColorHSV();
                return go;
            }).ToList();
        }

        public void SetActive(bool active)
        {
            _rootParent.SetActive(active);
        }
    }

}
