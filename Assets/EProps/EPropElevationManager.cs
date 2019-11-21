using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using Accord.Math;
using Assets.ComputeShaders;
using Assets.EProps;
using Assets.ETerrain.Pyramid;
using Assets.ETerrain.Pyramid.Map;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.Trees.RuntimeManagement.SubjectsInstancesContainer;
using Assets.Utils;
using Assets.Utils.Services;
using UnityEngine;
using UnityEngine.Experimental.PlayerLoop;
using Vector3 = UnityEngine.Vector3;

namespace Assets.EProps
{
    using LocaleBufferScopeIndexType = UInt32;
    using InScopeIndexType = UInt32;

    public struct EPropElevationId
    {
        public LocaleBufferScopeIndexType LocaleBufferScopeIndex;
        public InScopeIndexType InScopeIndex;

        public bool Equals(EPropElevationId other)
        {
            return LocaleBufferScopeIndex == other.LocaleBufferScopeIndex && InScopeIndex == other.InScopeIndex;
        }

        public override bool Equals(object obj)
        {
            return obj is EPropElevationId other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int) LocaleBufferScopeIndex * 397) ^ (int) InScopeIndex;
            }
        }
    }

    public class EPropElevationManager
    {
        private EPropElevationConfiguration _configuration;
        private Dictionary<MyQuantRectangle , EPropQuadTreeBaseNode> _quadTreeRoots = new Dictionary<MyQuantRectangle, EPropQuadTreeBaseNode>();

        private EPropLocaleBufferManager _localeBufferManager;
        private EPropElevationLocalePointersOccupancyContainer _pointersOccupancyContainer;

        public EPropElevationManager(CommonExecutorUTProxy commonExecutor, UnityThreadComputeShaderExecutorObject shaderExecutorObject,
            EPropElevationConfiguration configuration, EPropConstantPyramidParameters constantPyramidParameters)
        {
            _configuration = configuration;
            _localeBufferManager = new EPropLocaleBufferManager(commonExecutor, shaderExecutorObject, configuration, constantPyramidParameters);
            _pointersOccupancyContainer = new EPropElevationLocalePointersOccupancyContainer(configuration);
        }

        public EPropLocaleBufferManagerInitializedBuffers Initialize(ComputeBuffer ePyramidPerFrameParametersBuffer, ComputeBuffer ePyramidConfigurationBuffer,
            Texture heightmapArray) 
        {
            return _localeBufferManager.Initialize(ePyramidPerFrameParametersBuffer, ePyramidConfigurationBuffer, heightmapArray);
        }

        public EPropElevationPointer RegisterProp(Vector2 flatPosition)
        {
            var sectorRectangle = CalculateRootSectorAlignedRectangle(flatPosition);
            var sector = GetFreeSector(sectorRectangle);
            var localeId = sector.RegisterProp(flatPosition);
            return _pointersOccupancyContainer.ClaimFreePointer(localeId);
        }

        public List<EPropElevationPointer> RegisterPropsGroup(List<Vector2> positions)
        {
            var maxSubgroupLength = _configuration.ScopeLength;
            var subgroupCount = Mathf.CeilToInt(positions.Count / ((float) maxSubgroupLength));
            return Enumerable.Range(0, subgroupCount)
                .SelectMany(c => RegisterOnePropsGroup(positions.Skip(c * maxSubgroupLength).Take(maxSubgroupLength).ToList())).ToList();
        }

        private List<EPropElevationPointer> RegisterOnePropsGroup(List<Vector2> positions)
        {
            // todo checking if group-positions are aligned to smallest quad
            Preconditions.Assert(positions.Count <= _configuration.ScopeLength, "Subgroup size is too big, as it equals "+positions.Count);
            var center = positions.Aggregate((a, b) => a + b) / positions.Count;
            var sectorRectangle = CalculateRootSectorAlignedRectangle(center);
            var sector = GetFreeSector(sectorRectangle);
            var localeIds = sector.RegisterPropGroup(positions,center);
            return localeIds.Select(c => _pointersOccupancyContainer.ClaimFreePointer(c)).ToList();
        }

        public EPropPointerWithId DebugRegisterPropWithElevationId(Vector2 flatPosition)
        {
            var pointer = RegisterProp(flatPosition);
            var id = _pointersOccupancyContainer.RetriveId(pointer);
            return new EPropPointerWithId()
            {
                Id = id,
                Pointer = pointer
            };
        }

        private EPropQuadTreeBaseNode GetFreeSector(MyQuantRectangle sectorRectangle)
        {
            if (!_quadTreeRoots.ContainsKey(sectorRectangle))
            {
                _quadTreeRoots[sectorRectangle] = new EPropQuadTreeBaseNode(sectorRectangle, _localeBufferManager);
            }

            return _quadTreeRoots[sectorRectangle];
        }

        private MyQuantRectangle CalculateRootSectorAlignedRectangle(Vector2 flatPosition)
        {
            var newCoords = (flatPosition/_configuration.BaseSectorsWorldSpaceLength).FloorToInt();
            return new MyQuantRectangle(newCoords.X*_configuration.QuadTreeMaxDepth, newCoords.Y*_configuration.QuadTreeMaxDepth
                , _configuration.QuadTreeMaxDepth
                , _configuration.QuadTreeMaxDepth,
                _configuration.BaseSectorsWorldSpaceLength/_configuration.QuadTreeMaxDepth);
        }

         public Task UpdateAsync(EPropElevationManagerUpdateInputData inputData)
         {
             return UpdateAsync(inputData.TravellerFlatPosition, inputData.LevelCentersWorldSpace, inputData.SelectorWithParameters);
         }

        public async Task UpdateAsync(Vector2 travellerFlatPosition, Dictionary<HeightPyramidLevel, Vector2> levelCentersWorldSpace, EPropHotAreaSelectorWithParameters selectorWithParameters)
        {
            var changes = _quadTreeRoots.Values.SelectMany(c => c.RetriveAndClearUpdateOrders()).ToList();
            var updateOrders = changes.Select(c => new EPropSectorSoleUpdateOrder()
            {
                Change = c,
                Pointer = _pointersOccupancyContainer.RetrivePointer(new EPropElevationId() { InScopeIndex = c.ScopeUpdateOrder.Index, LocaleBufferScopeIndex = c.ScopeIndex})
            }).ToList();
            await _localeBufferManager.UpdateBuffersAsync(updateOrders, travellerFlatPosition);

            var sectorsWithState = _quadTreeRoots.Values.SelectMany(c => c.RetriveSectorsWithState(selectorWithParameters)).ToList();
            var hotScopes = sectorsWithState.Where(c => c.State == EPropSectorState.Hot).SelectMany(c => c.Sector.ScopeIds).ToList();
            await _localeBufferManager.RecalculateLocalesAsync(travellerFlatPosition, hotScopes);
        }

        public List<DebugSectorInformation> DebugQuerySectorStates(EPropHotAreaSelectorWithParameters selectorWithParameters)
        {
            return _quadTreeRoots.Select(c => new DebugSectorInformation()
            {
                Area = c.Key,
                Children = new List<DebugSectorInformation>() {c.Value.DebugQuerySectorStates(selectorWithParameters, 0)},
                Depth = 0,
                SectorState = EPropSectorState.Cold
            }).ToList();
        }

        public async Task<List<EPropIdChange>> RecalculateSectorsDivisionAsync(Vector2 travellerPosition)
        {
            Debug.Log("Recalculating sectors division");
            Func<float, int> nodeDepthResolver = (distance =>
                Mathf.Max(0,
                    _configuration.QuadTreeMaxDepth-
                        Mathf.RoundToInt(Mathf.Sqrt(_configuration.QuadTreeDivisionMultiplier * distance / (_configuration.BaseSectorsWorldSpaceLength / 2)))));
            var perTreeDivisionResults = _quadTreeRoots.Values
                .Select(c => c.ResolveDivision(new EPropQuadTreeDivisionDecider(nodeDepthResolver, travellerPosition, 0))).ToList();
            var divisionResult = new EPropQuadTreeDivisionResult()
            {
                ScopesToFree = perTreeDivisionResults.SelectMany(c => c.ScopesToFree).ToList(),
                IdChanges = perTreeDivisionResults.SelectMany(c => c.IdChanges).ToList()
            };

            await _localeBufferManager.ProcessDivisionChangesAsync(new EPropIdChangeOrder()
            {
                ScopesToFree = divisionResult.ScopesToFree,
                IdChanges = divisionResult.IdChanges.Select(c=> new EPropIdChangeWithPointer()
                {
                    Change = c,
                    Pointer = _pointersOccupancyContainer.RetrivePointer(c.OldId)
                }).ToList()
            });

            foreach (var change in divisionResult.IdChanges)
            {
                _pointersOccupancyContainer.ChangeIds(change);
            }

            return divisionResult.IdChanges;
        }

        public void DebugInitializeSectors(MyRectangle myRectangle)
        {
            for (int x = (int) myRectangle.X; x < myRectangle.MaxX; x++)
            {
                for (int y = (int) myRectangle.Y; y < myRectangle.MaxY; y++)
                {
                    var alignedRectangle = CalculateRootSectorAlignedRectangle(new Vector2(x, y));
                    GetFreeSector(alignedRectangle);
                }
            }
        }
    }

    public class EPropElevationManagerUpdateInputData
    {
        public Vector2 TravellerFlatPosition;
        public Dictionary<HeightPyramidLevel, Vector2> LevelCentersWorldSpace;
        public EPropHotAreaSelectorWithParameters SelectorWithParameters;
    }

    public class EPropPointerWithId
    {
        public EPropElevationPointer Pointer;
        public EPropElevationId Id;
    }

    public struct EPropElevationPointer
    {
        public uint Value;

        public bool Equals(EPropElevationPointer other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is EPropElevationPointer other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (int) Value;
        }
    }

    public class EPropElevationLocalePointersOccupancyContainer
    {
        private DoubleDictionary<EPropElevationId, EPropElevationPointer> _pointerIdDict;
        private ConstantSizeClaimableContainer<object> _pointersContainer; //todo possible much less space taking data structure

        public EPropElevationLocalePointersOccupancyContainer(EPropElevationConfiguration configuration)
        {
            _pointersContainer = ConstantSizeClaimableContainer<object>.CreateEmpty(configuration.MaxLocalePointersCount);
            _pointerIdDict = new DoubleDictionary<EPropElevationId, EPropElevationPointer>();
        }

        public EPropElevationPointer ClaimFreePointer(EPropElevationId id)
        {
            uint freeIndex = _pointersContainer.AddElement(null);
            var pointer = new EPropElevationPointer()
            {
                Value = freeIndex
            };
            _pointerIdDict.Add(id,pointer);
            return pointer;
        }

        public void ChangeIds(EPropIdChange change)
        {
            var pointer = _pointerIdDict.Get(change.OldId);
            _pointerIdDict.Remove(change.OldId);
            _pointerIdDict.Add( change.NewId, pointer);
        }

        public EPropElevationId RetriveId(EPropElevationPointer pointer)
        {
            return _pointerIdDict.Get(pointer);
        }

        public EPropElevationPointer RetrivePointer(EPropElevationId id)
        {
            return _pointerIdDict.Get(id);
        }
    }

    public class DebugSectorInformation
    {
        public int Depth;
        public MyQuantRectangle Area;
        public EPropSectorState SectorState;
        public List<DebugSectorInformation> Children;
    }

    public class EPropHotAreaSelector
    {
        private Dictionary<HeightPyramidLevel, Vector2> _levelWorldSizes;
        private Dictionary<HeightPyramidLevel, Dictionary<int, Vector2>> _ringMergeRanges;

        public EPropHotAreaSelector(Dictionary<HeightPyramidLevel, Vector2> levelWorldSizes, Dictionary<HeightPyramidLevel, Dictionary<int, Vector2>> ringMergeRanges)
        {
            _levelWorldSizes = levelWorldSizes;
            _ringMergeRanges = ringMergeRanges;
        }

        private Vector2 TravellerWorldPositionToConstantLevelUv(Vector2 travellerPositionWorldSpace, ELevelAndRingIndexes levelAndRingIndexes)
        {
            Vector2 levelWorldSize = _levelWorldSizes[levelAndRingIndexes.LevelIndex];
            return VectorUtils.MemberwiseDivide(travellerPositionWorldSpace, levelWorldSize).Add(0.5f);
        }

        private Vector2 ConstantLevelUvSpaceToWorldSpace(Vector2 levelUv, ELevelAndRingIndexes levelAndRingIndexes)
        {
            Vector2 levelWorldSize = _levelWorldSizes[levelAndRingIndexes.LevelIndex];
            return VectorUtils.MemberwiseMultiply(levelUv.Add(-0.5f), levelWorldSize);
        }

        private Vector2 LevelUvSpaceToWorldSpace(Vector2 levelUv, ELevelAndRingIndexes levelAndRingIndexes, Dictionary<HeightPyramidLevel, Vector2> levelCentersWorldSpace)
        {
            Vector2 levelWorldSize = _levelWorldSizes[levelAndRingIndexes.LevelIndex];
            Vector2 pyramidCenterWorldSize = levelCentersWorldSpace[levelAndRingIndexes.LevelIndex];
            Vector2 offset = (levelUv.Add(-0.5f)) * levelWorldSize;
            return (pyramidCenterWorldSize + offset);
        }

        private Vector2 WorldSpaceToLevelUvSpace(Vector2 worldSpace, HeightPyramidLevel levelIndex, Dictionary<HeightPyramidLevel, Vector2> levelCentersWorldSpace)
        {
            Vector2 levelWorldSize = _levelWorldSizes[levelIndex];
            Vector2 pyramidCenterWorldSize = levelCentersWorldSpace[levelIndex];
            return ((worldSpace - pyramidCenterWorldSize) / (levelWorldSize)).Add(0.5f);
        }

        private MyRectangle RecalculateMergeRectangles3(Vector2 worldSpaceTravellerPosition, Vector2 pyramidCenterUv, float transitionLimit,
            ELevelAndRingIndexes levelAndRingIndexes, Dictionary<HeightPyramidLevel, Vector2> levelCentersWorldSpace)
        {
            var travellerPositionInLevelUv = TravellerWorldPositionToConstantLevelUv(worldSpaceTravellerPosition, levelAndRingIndexes);

            var isuv_x1 = 0.5f * transitionLimit + travellerPositionInLevelUv.x + 0.5f - pyramidCenterUv.x;
            var isuv_x2 = -0.5f * transitionLimit + travellerPositionInLevelUv.x + 0.5f - pyramidCenterUv.x;
            var isuv_y1 = 0.5f * transitionLimit + travellerPositionInLevelUv.y + 0.5f - pyramidCenterUv.y;
            var isuv_y2 = -0.5f * transitionLimit + travellerPositionInLevelUv.y + 0.5f - pyramidCenterUv.y;

            var minValuesUvSpace = new Vector2(
                Mathf.Min(isuv_x1, isuv_x2),
                Mathf.Min(isuv_y1, isuv_y2)
            );

            var maxValuesUvSpace = new Vector2(
                Mathf.Max(isuv_x1, isuv_x2),
                Mathf.Max(isuv_y1, isuv_y2)
            );

            var minValuesWorldSpace = ConstantLevelUvSpaceToWorldSpace(minValuesUvSpace, levelAndRingIndexes);
            var maxValuesWorldSpace = ConstantLevelUvSpaceToWorldSpace(maxValuesUvSpace, levelAndRingIndexes);

            var worldSpaceRectangle = new MyRectangle(minValuesWorldSpace.x, minValuesWorldSpace.y, maxValuesWorldSpace.x - minValuesWorldSpace.x,
                maxValuesWorldSpace.y - minValuesWorldSpace.y);

            return worldSpaceRectangle;
        }

        public  Dictionary<HeightPyramidLevel, Dictionary<int, EPropMergeRing>> CalculateMergeRectangles(Vector2 worldSpaceTravellerPosition, Dictionary<HeightPyramidLevel, Vector2> levelCentersWorldSpace)
        {
            return levelCentersWorldSpace.ToDictionary(c => c.Key, pair =>
            {
                var levelIndex = pair.Key;
                Vector2 pyramidCenterUv = WorldSpaceToLevelUvSpace(levelCentersWorldSpace[levelIndex], levelIndex, levelCentersWorldSpace);

                return _ringMergeRanges[levelIndex].ToDictionary(k => k.Key, ringMergeRange =>
                {
                    var ringIndex = ringMergeRange.Key;
                    ELevelAndRingIndexes levelAndRingIndexes = new ELevelAndRingIndexes()
                    {
                        RingIndex = ringIndex,
                        LevelIndex = levelIndex
                    };
                    var innerRectangle = RecalculateMergeRectangles3(worldSpaceTravellerPosition, pyramidCenterUv, ringMergeRange.Value.x, levelAndRingIndexes,
                        levelCentersWorldSpace);
                    var outerRectangle = RecalculateMergeRectangles3(worldSpaceTravellerPosition, pyramidCenterUv, ringMergeRange.Value.y, levelAndRingIndexes,
                        levelCentersWorldSpace);
                    return new EPropMergeRing()
                    {
                        InnerRectangle = innerRectangle,
                        OuterRectangle = outerRectangle
                    };
                });
            });
        }
    }

    public class ELevelAndRingIndexes
    {
        public HeightPyramidLevel LevelIndex;
        public int RingIndex;
    }

    public class EPropHotAreaSelectorWithParameters
    {
        private EPropHotAreaSelector _hotAreaSelector;
        private Dictionary<HeightPyramidLevel, Dictionary<int, EPropMergeRing>> _mergeRings;

        private EPropHotAreaSelectorWithParameters(EPropHotAreaSelector hotAreaSelector)
        {
            _hotAreaSelector = hotAreaSelector;
        }

        private void RecalculateMergeRectangles(Vector2 worldSpaceTravellerPosition, Dictionary<HeightPyramidLevel, Vector2> levelCentersWorldSpace)
        {
            _mergeRings = _hotAreaSelector.CalculateMergeRectangles(worldSpaceTravellerPosition, levelCentersWorldSpace);
        }

        public bool IsRectangleInAnyMergeArea(MyRectangle queryRectangle)
        {
            foreach (var levelRingsPair in _mergeRings)
            {
                foreach (var ringsPair in levelRingsPair.Value)
                {
                    var mergeRing = ringsPair.Value;
                    if (MyRectangle.Intersects(mergeRing.OuterRectangle, queryRectangle) &&
                        !MyRectangle.IsCompletlyInside(mergeRing.InnerRectangle, queryRectangle))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public Dictionary<HeightPyramidLevel, Dictionary<int, EPropMergeRing>> MergeRings => _mergeRings;

        public static EPropHotAreaSelectorWithParameters Create(EPropHotAreaSelector hotAreaSelector, Dictionary<HeightPyramidLevel, Vector2> levelCentersWorldSpace, Vector2 worldSpaceTravellerPosition)
        {
            var newSelector = new EPropHotAreaSelectorWithParameters(hotAreaSelector);
            newSelector.RecalculateMergeRectangles(worldSpaceTravellerPosition, levelCentersWorldSpace);
            return newSelector;
        }

    }

    public class EPropMergeRing
    {
        public MyRectangle InnerRectangle;
        public MyRectangle OuterRectangle;
    }

    public enum EPropSectorState
    {
        Hot, Cold
    }

    public class SectorWithStateAndRectangle
    {
        public EPropSector Sector;
        public EPropSectorState State;
        public MyQuantRectangle Rectangle;
    }

    public class EPropConstantPyramidParameters
    {
        public int LevelsCount;
        public int RingsPerLevelCount;
        public float HeightScale;
    }

    public class EPropLocaleBufferManager
    {
        private EPropElevationConfiguration _configuration;
        private readonly EPropConstantPyramidParameters _constantPyramidParameters;
        private UnityThreadComputeShaderExecutorObject _shaderExecutorObject;
        private CommonExecutorUTProxy _commonExecutor;
        private EPropLocaleBufferScopeRegistry[] _scopes;

        private ComputeBuffer _scopesUpdateOrdersBuffer;
        private ComputeBuffer _ePropLocaleBuffer;
        private ComputeBuffer _ePropIdsBuffer;
        private ComputeBuffer _scopesToRecalculateBuffer;
        private ComputeBuffer _localesCopyOrdersBuffer;

        private Func<int, Vector2, Task> _localeBufferUpdaterShaderOrderGenerator;
        private Func<int, Vector2, Task> _localeRecalculationShaderOrderGenerator;
        private Func<int, Task> _localesCopyShaderOrderGenerator;

        private ComputeShader _eTerrainComputeShader;

        public EPropLocaleBufferManager(CommonExecutorUTProxy commonExecutor, UnityThreadComputeShaderExecutorObject shaderExecutorObject, EPropElevationConfiguration configuration
            , EPropConstantPyramidParameters constantPyramidParameters)
        {
            _shaderExecutorObject = shaderExecutorObject;
            _configuration = configuration;
            _commonExecutor = commonExecutor;
            _constantPyramidParameters = constantPyramidParameters;
            _scopes = new EPropLocaleBufferScopeRegistry[configuration.MaxScopesCount];
        }

        public EPropLocaleBufferManagerInitializedBuffers Initialize( ComputeBuffer ePyramidPerFrameParametersBuffer, ComputeBuffer ePyramidConfigurationBuffer
            , Texture heightmapArray)
        {
            _scopesUpdateOrdersBuffer = new ComputeBuffer(_configuration.MaxScopeUpdateOrdersInBuffer,
                System.Runtime.InteropServices.Marshal.SizeOf(typeof(GpuSoleUpdateOrder)), ComputeBufferType.Default);

            _ePropLocaleBuffer = new ComputeBuffer( _configuration.LocalesCount,
                System.Runtime.InteropServices.Marshal.SizeOf(typeof(GpuEPropLocale)), ComputeBufferType.Default);

            _localesCopyOrdersBuffer = new ComputeBuffer(_configuration.MaxLocalesToCopyInBuffer,
                System.Runtime.InteropServices.Marshal.SizeOf(typeof(GpuSoleLocaleCopyOrder)), ComputeBufferType.Default);

            _scopesToRecalculateBuffer = new ComputeBuffer(_configuration.MaxScopesToRecalculatePerPass, sizeof(int), ComputeBufferType.Default);

            _ePropIdsBuffer = new ComputeBuffer(_configuration.MaxLocalePointersCount, 
                System.Runtime.InteropServices.Marshal.SizeOf(typeof(GpuEPropElevationId)), ComputeBufferType.Default);

            GenerateShaderOrderTemplates(ePyramidPerFrameParametersBuffer, ePyramidConfigurationBuffer, heightmapArray);

            _eTerrainComputeShader = ComputeShaderUtils.LoadComputeShader("eterrain_comp");

            return new EPropLocaleBufferManagerInitializedBuffers()
            {
                EPropIdsBuffer = _ePropIdsBuffer,
                EPropLocaleBuffer = _ePropLocaleBuffer
            };
        }

        private void GenerateShaderOrderTemplates(ComputeBuffer ePyramidPerFrameParametersBuffer, ComputeBuffer ePyramidConfigurationBuffer,
            Texture heightmapArray)
        {
            _localeBufferUpdaterShaderOrderGenerator = (ordersCount, travellerPositionWorldSpace) =>
            {
                MultistepComputeShader localeBufferUpdateShader = new MultistepComputeShader(_eTerrainComputeShader, new IntVector2(ordersCount, 1));

                ComputeShaderParametersContainer parametersContainer = new ComputeShaderParametersContainer() { };
                var scopesUpdateOrdersBufferId = parametersContainer.AddExistingComputeBuffer(_scopesUpdateOrdersBuffer);
                var ePropLocalBufferId = parametersContainer.AddExistingComputeBuffer(_ePropLocaleBuffer);
                var ePropIdsBuffer = parametersContainer.AddExistingComputeBuffer(_ePropIdsBuffer);

                var kernel = localeBufferUpdateShader.AddKernel("CSETerrain_LocaleBufferUpdater");
                var kernelHandles = new List<MyKernelHandle>() {kernel};
                localeBufferUpdateShader.SetBuffer("_ScopesUpdateOrdersBuffer", scopesUpdateOrdersBufferId, kernelHandles);
                localeBufferUpdateShader.SetBuffer("_EPropLocaleBuffer", ePropLocalBufferId, kernelHandles);
                localeBufferUpdateShader.SetBuffer("_EPropIdsBuffer", ePropIdsBuffer, kernelHandles);

                var ePyramidPerFrameParametersBufferId = parametersContainer.AddExistingComputeBuffer(ePyramidPerFrameParametersBuffer);
                localeBufferUpdateShader.SetBuffer("_EPyramidPerFrameConfigurationBuffer", ePyramidPerFrameParametersBufferId, kernelHandles);

                var ePyramidConfigurationBufferId = parametersContainer.AddExistingComputeBuffer(ePyramidConfigurationBuffer);
                localeBufferUpdateShader.SetBuffer("_EPyramidConfigurationBuffer", ePyramidConfigurationBufferId, kernelHandles);

                localeBufferUpdateShader.SetGlobalUniform("g_ringsPerLevelCount", _constantPyramidParameters.RingsPerLevelCount);
                localeBufferUpdateShader.SetGlobalUniform("g_levelsCount", _constantPyramidParameters.LevelsCount);
                localeBufferUpdateShader.SetGlobalUniform("g_ScopeLength", _configuration.ScopeLength);
                localeBufferUpdateShader.SetGlobalUniform("g_heightScale", _constantPyramidParameters.HeightScale);

                var mapId = parametersContainer.AddExistingComputeShaderTexture(heightmapArray);
                localeBufferUpdateShader.SetTexture($"_HeightMap", mapId, kernelHandles); //todo parametrize HeightMap name

                localeBufferUpdateShader.SetGlobalUniform("g_travellerPositionWorldSpace", travellerPositionWorldSpace);

                return _shaderExecutorObject.DispatchComputeShader(new ComputeShaderOrder()
                {
                    OutParameters = new ComputeBufferRequestedOutParameters(),
                    ParametersContainer = parametersContainer,
                    WorkPacks = new List<ComputeShaderWorkPack>()
                    {
                        new ComputeShaderWorkPack()
                        {
                            DispatchLoops = new List<ComputeShaderDispatchLoop>()
                            {
                                new ComputeShaderDispatchLoop()
                                {
                                    DispatchCount = 1,
                                    KernelHandles = kernelHandles
                                }
                            },
                            Shader = localeBufferUpdateShader
                        }
                    }
                });
            };

            _localesCopyShaderOrderGenerator = (ordersCount) =>
            {
                MultistepComputeShader localeBufferUpdateShader =
                    new MultistepComputeShader(_eTerrainComputeShader, new IntVector2(ordersCount, 1));

                ComputeShaderParametersContainer parametersContainer = new ComputeShaderParametersContainer() { };
                var localesCopyOrdersBufferId = parametersContainer.AddExistingComputeBuffer(_localesCopyOrdersBuffer);
                var ePropLocalBufferId = parametersContainer.AddExistingComputeBuffer(_ePropLocaleBuffer);
                var ePropIdsBuffer = parametersContainer.AddExistingComputeBuffer(_ePropIdsBuffer);

                var kernel = localeBufferUpdateShader.AddKernel("CSETerrain_LocalesCopy");
                var kernelHandles = new List<MyKernelHandle>() {kernel};
                localeBufferUpdateShader.SetBuffer("_LocalesCopyOrdersBuffer", localesCopyOrdersBufferId, kernelHandles);
                localeBufferUpdateShader.SetBuffer("_EPropLocaleBuffer", ePropLocalBufferId, kernelHandles);
                localeBufferUpdateShader.SetBuffer("_EPropIdsBuffer", ePropIdsBuffer, kernelHandles);

                localeBufferUpdateShader.SetGlobalUniform("g_ringsPerLevelCount", _constantPyramidParameters.RingsPerLevelCount);
                localeBufferUpdateShader.SetGlobalUniform("g_levelsCount", _constantPyramidParameters.LevelsCount);
                localeBufferUpdateShader.SetGlobalUniform("g_ScopeLength", _configuration.ScopeLength);
                localeBufferUpdateShader.SetGlobalUniform("g_heightScale", _constantPyramidParameters.HeightScale);

                return _shaderExecutorObject.DispatchComputeShader(new ComputeShaderOrder()
                {
                    OutParameters = new ComputeBufferRequestedOutParameters(),
                    ParametersContainer = parametersContainer,
                    WorkPacks = new List<ComputeShaderWorkPack>()
                    {
                        new ComputeShaderWorkPack()
                        {
                            DispatchLoops = new List<ComputeShaderDispatchLoop>()
                            {
                                new ComputeShaderDispatchLoop()
                                {
                                    DispatchCount = 1,
                                    KernelHandles = kernelHandles
                                }
                            },
                            Shader = localeBufferUpdateShader
                        }
                    }
                });
            };

            _localeRecalculationShaderOrderGenerator = (ordersCount, travellerPositionWorldSpace) =>
            {
                MultistepComputeShader localeBufferUpdateShader =
                    new MultistepComputeShader(_eTerrainComputeShader, new IntVector2(ordersCount, 1));

                ComputeShaderParametersContainer parametersContainer = new ComputeShaderParametersContainer() { };

                var kernel = localeBufferUpdateShader.AddKernel("CSETerrain_LocaleRecalculate");

                var scopesUpdateOrdersBufferId = parametersContainer.AddExistingComputeBuffer(_scopesUpdateOrdersBuffer);
                var kernelHandles = new List<MyKernelHandle>(){kernel};
                localeBufferUpdateShader.SetBuffer("_ScopesUpdateOrdersBuffer", scopesUpdateOrdersBufferId, kernelHandles);

                var ePropLocalBufferId = parametersContainer.AddExistingComputeBuffer(_ePropLocaleBuffer);
                localeBufferUpdateShader.SetBuffer("_EPropLocaleBuffer", ePropLocalBufferId, kernelHandles);

                var ePyramidPerFrameParametersBufferId = parametersContainer.AddExistingComputeBuffer(ePyramidPerFrameParametersBuffer);
                localeBufferUpdateShader.SetBuffer("_EPyramidPerFrameConfigurationBuffer", ePyramidPerFrameParametersBufferId, kernelHandles);

                var ePyramidConfigurationBufferId = parametersContainer.AddExistingComputeBuffer(ePyramidConfigurationBuffer);
                localeBufferUpdateShader.SetBuffer("_EPyramidConfigurationBuffer", ePyramidConfigurationBufferId, kernelHandles);

                var scopesToRecalculateBufferId = parametersContainer.AddExistingComputeBuffer(_scopesToRecalculateBuffer);
                localeBufferUpdateShader.SetBuffer("_ScopesToRecalculateBuffer", scopesToRecalculateBufferId, kernelHandles);

                localeBufferUpdateShader.SetGlobalUniform("g_ringsPerLevelCount", _constantPyramidParameters.RingsPerLevelCount);
                localeBufferUpdateShader.SetGlobalUniform("g_levelsCount", _constantPyramidParameters.LevelsCount);
                localeBufferUpdateShader.SetGlobalUniform("g_ScopeLength", _configuration.ScopeLength);
                localeBufferUpdateShader.SetGlobalUniform("g_heightScale", _constantPyramidParameters.HeightScale);

                var mapId = parametersContainer.AddExistingComputeShaderTexture(heightmapArray);
                localeBufferUpdateShader.SetTexture($"_HeightMap", mapId, kernelHandles); //todo parametrize HeightMap name

                localeBufferUpdateShader.SetGlobalUniform("g_travellerPositionWorldSpace", travellerPositionWorldSpace);

                return _shaderExecutorObject.DispatchComputeShader(new ComputeShaderOrder()
                {
                    OutParameters = new ComputeBufferRequestedOutParameters(),
                    ParametersContainer = parametersContainer,
                    WorkPacks = new List<ComputeShaderWorkPack>()
                    {
                        new ComputeShaderWorkPack()
                        {
                            DispatchLoops = new List<ComputeShaderDispatchLoop>()
                            {
                                new ComputeShaderDispatchLoop()
                                {
                                    DispatchCount = 1,
                                    KernelHandles = kernelHandles
                                }
                            },
                            Shader = localeBufferUpdateShader
                        }
                    }
                });
            };
        }

        public EPropLocaleBufferScopeRegistryWithIndex CreateNewScope()
        {
            for (var i = 0u; i < _scopes.Length; i++)
            {
                if (_scopes[i] == null)
                {
                    _scopes[i] = new EPropLocaleBufferScopeRegistry(_configuration);
                    return new EPropLocaleBufferScopeRegistryWithIndex()
                    {
                        ScopeIndex = i,
                        Registry = _scopes[i]
                    };
                }
            }
            Preconditions.Fail("Max scope count exceded");
            return null;
        }

        public async Task UpdateBuffersAsync(List<EPropSectorSoleUpdateOrder> updateOrders, Vector2 travellerPositionWorldSpace)
        {
            var passesCount = Mathf.CeilToInt(updateOrders.Count / ((float) _configuration.MaxScopeUpdateOrdersInBuffer));
            Debug.Log("UpdateBufefrsCount: "+passesCount);
            for (int i = 0; i < passesCount; i++)
            {
                var scopeUpdateOrdersArray = new GpuSoleUpdateOrder[_configuration.MaxScopeUpdateOrdersInBuffer];
                var offset = i * _configuration.MaxScopeUpdateOrdersInBuffer;

                var thisPassOrdersCount = Mathf.Min(_configuration.MaxScopeUpdateOrdersInBuffer, updateOrders.Count - offset);
                for (var j = 0; j < thisPassOrdersCount; j++)
                {
                    var order = updateOrders[offset + j];
                    scopeUpdateOrdersArray[j] = new GpuSoleUpdateOrder()
                    {
                        Pointer = order.Pointer.Value,
                        FlatPosition = order.Change.ScopeUpdateOrder.FlatPosition,
                        ScopeIndex = order.Change.ScopeIndex,
                        IndexInScope = order.Change.ScopeUpdateOrder.Index
                    };
                }

                await ExecuteUpdateOrderAsync(scopeUpdateOrdersArray, thisPassOrdersCount, travellerPositionWorldSpace);
            }
        }


        private async Task ExecuteUpdateOrderAsync(GpuSoleUpdateOrder[] scopeUpdateOrdersArray, int ordersCount, Vector2 travellerPositionWorldSpace)
        {
            await _commonExecutor.AddAction(() => _scopesUpdateOrdersBuffer.SetData(scopeUpdateOrdersArray));
            await _localeBufferUpdaterShaderOrderGenerator(ordersCount,travellerPositionWorldSpace);
        }

        public async Task RecalculateLocalesAsync(Vector2 travellerPositionWorldSpace, List<LocaleBufferScopeIndexType> scopesToRecalculate)
        {
            var passesCount = Mathf.CeilToInt(scopesToRecalculate.Count / ((float) _configuration.MaxScopesToRecalculatePerPass));
            Debug.Log("Recalculating pass count: "+passesCount);
            for (int i = 0; i < passesCount; i++)
            {
                var offset = i * _configuration.MaxScopesToRecalculatePerPass;

                var thisPassScopesCount = Mathf.Min(_configuration.MaxScopesToRecalculatePerPass,scopesToRecalculate.Count - offset);
                var scopesToRecalculateThisPass = scopesToRecalculate.Skip(offset).Take(thisPassScopesCount).ToList();
                await ExecuteScopesRecalculationAsync(scopesToRecalculateThisPass, thisPassScopesCount, travellerPositionWorldSpace);
            }
        }

        private async Task ExecuteScopesRecalculationAsync(List<uint> scopesToRecalculateThisPass, int thisPassScopesCount, Vector2 travellerPositionWorldSpace)
        {
            await _commonExecutor.AddAction(() => _scopesToRecalculateBuffer.SetData(scopesToRecalculateThisPass));
            await _localeRecalculationShaderOrderGenerator(thisPassScopesCount * _configuration.ScopeLength, travellerPositionWorldSpace);
        }

        public async Task ProcessDivisionChangesAsync(EPropIdChangeOrder idChangeOrder)
        {
            if (idChangeOrder.IdChanges.Any())
            {
                await CopyLocalesAsync(idChangeOrder.IdChanges);
            }

            FreeScopes(idChangeOrder.ScopesToFree);
        }

        private async Task CopyLocalesAsync(List<EPropIdChangeWithPointer> idChanges)
        {
            var passesCount = Mathf.CeilToInt(idChanges.Count / ((float) _configuration.MaxLocalesToCopyInBuffer));

            for (int i = 0; i < passesCount; i++)
            {
                var scopeUpdateOrdersArray = new GpuSoleLocaleCopyOrder[_configuration.MaxLocalesToCopyInBuffer];
                var offset = i * _configuration.MaxLocalesToCopyInBuffer;

                var thisPassOrdersCount = Mathf.Min(_configuration.MaxLocalesToCopyInBuffer, idChanges.Count - offset);
                for (var j = 0; j < thisPassOrdersCount; j++)
                {
                    var changeWithPointer = idChanges[offset + j];
                    var change = changeWithPointer.Change;
                    scopeUpdateOrdersArray[j] = new   GpuSoleLocaleCopyOrder()
                    {
                        Pointer = changeWithPointer.Pointer.Value,
                        OldIndexInScope = change.OldId.InScopeIndex,
                        OldScopeIndex = change.OldId.LocaleBufferScopeIndex,
                        NewIndexInScope = change.NewId.InScopeIndex,
                        NewScopeIndex = change.NewId.LocaleBufferScopeIndex
                    };
                }

                await ExecuteLocalesCopyOrderAsync(scopeUpdateOrdersArray, thisPassOrdersCount);
            }
        }

        private async Task ExecuteLocalesCopyOrderAsync(GpuSoleLocaleCopyOrder[] localesCopyOrdersArray, int thisPassOrdersCount)
        {
            await _commonExecutor.AddAction(() => _localesCopyOrdersBuffer.SetData(localesCopyOrdersArray));
            await _localesCopyShaderOrderGenerator(thisPassOrdersCount);
        }

        private void FreeScopes(List<LocaleBufferScopeIndexType> scopesToFree)
        {
            foreach (var id in scopesToFree)
            {
                Preconditions.Assert(!_scopes[id].IsDirty, $"Scope of id {id} we are trying to free is still dirty");
                _scopes[id] = null;
            }
        }

        public struct GpuEPropElevationId
        {
            public uint LocaleBufferScopeIndex;
            public uint InScopeIndex;
        }

        public struct GpuSoleUpdateOrder
        {
            public uint Pointer;
            public uint ScopeIndex;
            public uint IndexInScope;
            public Vector2 FlatPosition;
        }

        public struct GpuEPropLocale
        {
            public Vector2 FlatPosition;
            public float Height;
            public Vector3 Normal;
        }

        public struct GpuSoleLocaleCopyOrder
        {
            public uint Pointer;
            public uint OldScopeIndex;
            public uint OldIndexInScope;
            public uint NewScopeIndex;
            public uint NewIndexInScope;
        }
    }

    public class EPropLocaleBufferManagerInitializedBuffers
    {
        public ComputeBuffer EPropLocaleBuffer;
        public ComputeBuffer EPropIdsBuffer;
    }

    public class EPropIdChangeOrder
    {
        public List<EPropIdChangeWithPointer> IdChanges;
        public List<LocaleBufferScopeIndexType> ScopesToFree;
    }

    public class EPropIdChangeWithPointer
    {
        public EPropIdChange Change;
        public EPropElevationPointer Pointer;
    }

    public class EPropElevationConfiguration
    {
        public  float BaseSectorsWorldSpaceLength = 128f;
        public  IntVector2 SectorsGridResolution = new IntVector2(256,256);
        public  int ScopeLength = 256;
        public  int MaxScopesCount = 1024*8;
        public int LocalesCount => ScopeLength * MaxScopesCount;
        public  int MaxScopeUpdateOrdersInBuffer = 1024*8;
        public int MaxScopesToRecalculatePerPass = 64;
        public int QuadTreeMaxDepth = 6;
        public float QuadTreeDivisionMultiplier = 4f*4;
        public int MaxLocalesToCopyInBuffer = 1024;
        public int MaxLocalePointersCount => MaxScopesCount * ScopeLength;
    }

    public class EPropSector
    {
        private readonly EPropLocaleBufferManager _localeBufferManager;
        private Dictionary<LocaleBufferScopeIndexType, EPropLocaleBufferScopeRegistry> _scopes;

        public EPropSector(EPropLocaleBufferManager localeBufferManager)
        {
            _localeBufferManager = localeBufferManager;
            _scopes = new Dictionary<LocaleBufferScopeIndexType, EPropLocaleBufferScopeRegistry>();
        }

        public EPropElevationId RegisterProp(Vector2 flatPosition)
        {
            var scope = GetFreeScope();
            var indexInScope = scope.Registry.ClaimFreeLocale(flatPosition);
            return new EPropElevationId()
            {
                InScopeIndex = indexInScope,
                LocaleBufferScopeIndex = scope.ScopeIndex
            };
        }

        public List<EPropElevationId> RegisterPropGroup(List<Vector2> positions)
        {
            var scope = AllocateNewScope();
            var indexes =  scope.Registry.ClaimForGroup(positions);
            return indexes.Select(c => new EPropElevationId()
            {
                InScopeIndex = c,
                LocaleBufferScopeIndex = scope.ScopeIndex
            }).ToList();
        }

        private EPropLocaleBufferScopeRegistryWithIndex AllocateNewScope()
        {
            var newScopeWithIndex = _localeBufferManager.CreateNewScope();
            _scopes[newScopeWithIndex.ScopeIndex] = newScopeWithIndex.Registry;
            return newScopeWithIndex;
        }

        private EPropLocaleBufferScopeRegistryWithIndex GetFreeScope()
        {
            var firstFree = _scopes.Select(c => new {c.Key, c.Value}).FirstOrDefault(c => c.Value.HasFreeIndex());
            if (firstFree == null)
            {
                return AllocateNewScope();
            }
            else
            {
                return  new EPropLocaleBufferScopeRegistryWithIndex()
                {
                    Registry = firstFree.Value,
                    ScopeIndex = firstFree.Key
                };
            }
        }

        public bool IsDirty => _scopes.Any(c => c.Value.IsDirty);
        public bool IsEmpty => _scopes.All(c => c.Value.IsEmpty);
        public List<LocaleBufferScopeIndexType> ScopeIds => _scopes.Keys.ToList();

        public List<EPropSectorSoleChange>  RetriveAndClearUpdateOrders()
        {
            return _scopes.Where(c => c.Value.IsDirty).SelectMany(c =>
                c.Value.RetriveAndClearUpdateOrders().Select(k => new EPropSectorSoleChange() {ScopeIndex = c.Key, ScopeUpdateOrder = k})).ToList();
        }

        public Dictionary<LocaleBufferScopeIndexType, EPropLocaleBufferScopeRegistry> TakeAwayScopes()
        {
            Preconditions.Assert(!IsDirty,"One of the scopes is still dirty");
            var toReturn = _scopes;
            _scopes = new Dictionary<LocaleBufferScopeIndexType, EPropLocaleBufferScopeRegistry>();
            return toReturn;
        }

        public void SetScopes(Dictionary<LocaleBufferScopeIndexType, EPropLocaleBufferScopeRegistry> scopes)
        {
            Preconditions.Assert(!_scopes.Any(), "There are arleady scopes in sector");
            _scopes = scopes;
        }

    }

    public class EPropSectorSoleUpdateOrder
    {
        public EPropSectorSoleChange Change;
        public EPropElevationPointer Pointer;
    }

    public class EPropSectorSoleChange
    {
        public LocaleBufferScopeIndexType ScopeIndex;
        public EPropLocaleBufferScopeUpdateOrder ScopeUpdateOrder;
    }

    public class EPropLocaleBufferScopeRegistry
    {
        private EPropElevationConfiguration _configuration;
        private ConstantSizeClaimableContainer<Vector2?> _localeArray;
        private List<EPropLocaleBufferScopeUpdateOrder> _updateOrders;

        public EPropLocaleBufferScopeRegistry(EPropElevationConfiguration configuration)
        {
            _configuration = configuration;
            _localeArray = ConstantSizeClaimableContainer<Vector2?>.CreateEmpty(configuration.ScopeLength);
            _updateOrders = new List<EPropLocaleBufferScopeUpdateOrder>();
        }

        public bool HasFreeIndex()
        {
            return _localeArray.HasFreeSpace();
        }

        public uint ClaimFreeLocale(Vector2 flatPosition)
        {
            uint idx = _localeArray.AddElement(flatPosition);
            _updateOrders.Add(new EPropLocaleBufferScopeUpdateOrder()
            {
                FlatPosition = flatPosition,
                Index = idx
            });
            return idx;
        }

        public List<uint> ClaimForGroup(List<Vector2> positions)
        {
            Preconditions.Assert(!IsDirty, "Cannot claim for group, registry is dirty");
            Preconditions.Assert(IsEmpty, "Cannot claim for group, registry is not empty");
            Preconditions.Assert(positions.Count <= _configuration.ScopeLength, "Group count is bigger than  scopeLength, count is " + positions.Count);

            _localeArray = ConstantSizeClaimableContainer<Vector2?>.CreateFull(_configuration.ScopeLength);
            for (int i = 0; i < positions.Count; i++)
            {
                _localeArray.SetElementWithoutClaimedSpaceChanges(positions[i],i);
            }

            var outList = new List<uint>();
            for (uint i = 0; i < positions.Count; i++)
            {
                _updateOrders.Add(new EPropLocaleBufferScopeUpdateOrder()
                {
                    Index = i,
                    FlatPosition = positions[(int) i]
                });
                outList.Add(i);
            }

            return outList;
        }

        public List<EPropLocaleBufferScopeUpdateOrder> RetriveAndClearUpdateOrders()
        {
            var orders = _updateOrders;
            _updateOrders = new List<EPropLocaleBufferScopeUpdateOrder>();
            return orders;
        }

        public bool IsDirty => _updateOrders.Any();
        public bool IsEmpty => _localeArray.IsEmpty();

        public List<InScopeIndexTypeWithFlatPosition> RetriveAllLocales()
        {
            return _localeArray.RetriveAllElements().Where(c=>c.Element.HasValue).Select(c => new InScopeIndexTypeWithFlatPosition()
            {
                FlatPosition = c.Element.Value,
                InScopeIndex = c.Index
            }).ToList();
        }

    }

    public class EPropLocaleBufferScopeUpdateOrder // todo add removal data
    {
        public Vector2 FlatPosition;
        public InScopeIndexType Index;
    }

    public class InScopeIndexTypeWithFlatPosition
    {
        public Vector2 FlatPosition;
        public InScopeIndexType InScopeIndex;
    }

    public class EPropLocaleBufferScopeRegistryWithIndex
    {
        public EPropLocaleBufferScopeRegistry Registry;
        public LocaleBufferScopeIndexType ScopeIndex;
    }

}
