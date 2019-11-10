﻿using System;
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
using UnityEngine;
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

        public EPropElevationManager(EPropElevationConfiguration configuration, UnityThreadComputeShaderExecutorObject shaderExecutorObject,
            EPropConstantPyramidParameters constantPyramidParameters)
        {
            _configuration = configuration;
            _localeBufferManager = new EPropLocaleBufferManager(shaderExecutorObject, _configuration, constantPyramidParameters);
            _pointersOccupancyContainer = new EPropElevationLocalePointersOccupancyContainer(configuration);
        }

        public ComputeBuffer EPropLocaleBuffer => _localeBufferManager.EPropLocaleBuffer;
        public ComputeBuffer EPropIdsBuffer => _localeBufferManager.EPropIdsBuffer;

        public void Initialize(ComputeBuffer ePyramidPerFrameParametersBuffer, ComputeBuffer ePyramidConfigurationBuffer,
            Dictionary<HeightPyramidLevel, Texture> heightmapTextures ) 
        {
            _localeBufferManager.Initialize(ePyramidPerFrameParametersBuffer, ePyramidConfigurationBuffer, heightmapTextures);
        }

        public EPropElevationPointer RegisterProp(Vector2 flatPosition)
        {
            var sectorRectangle = CalculateRootSectorAlignedRectangle(flatPosition);
            var sector = GetFreeSector(sectorRectangle);
            var localeId = sector.RegisterProp(flatPosition);
            return _pointersOccupancyContainer.ClaimFreePointer(localeId);
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

        public void Update(Vector2 travellerFlatPosition, Dictionary<HeightPyramidLevel, Vector2> levelCentersWorldSpace, EPropHotAreaSelectorWithParameters selectorWithParameters)
        {
            var changes = _quadTreeRoots.Values.SelectMany(c => c.RetriveAndClearUpdateOrders()).ToList();
            List<EPropSectorSoleUpdateOrder> updateOrders = changes.Select(c => new EPropSectorSoleUpdateOrder()
            {
                Change = c,
                Pointer = _pointersOccupancyContainer.RetrivePointer(new EPropElevationId() { InScopeIndex = c.ScopeUpdateOrder.Index, LocaleBufferScopeIndex = c.ScopeIndex})
            }).ToList();
            _localeBufferManager.UpdateBuffers(updateOrders, travellerFlatPosition);

            var sectorsWithState = _quadTreeRoots.Values.SelectMany(c => c.RetriveSectorsWithState(selectorWithParameters)).ToList();
            var hotScopes = sectorsWithState.Where(c => c.State == EPropSectorState.Hot).SelectMany(c => c.Sector.ScopeIds).ToList();
            _localeBufferManager.RecalculateLocales(travellerFlatPosition, hotScopes);
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

        public List<EPropIdChange> RecalculateSectorsDivision(Vector2 travellerPosition)
        {
            Debug.Log("Recalculating sectors division");
            Func<float, int> nodeDepthResolver = (distance =>
                Mathf.Max(0,
                    _configuration.QuadTreeMaxDepth-
                        Mathf.RoundToInt(Mathf.Sqrt(_configuration.QuadTreeDivisionMultiplier * distance / (_configuration.BaseSectorsWorldSpaceLength / 2)))));
            //Func<float, int> nodeDepthResolver = (distance => 3);
            var perTreeDivisionResults = _quadTreeRoots.Values
                .Select(c => c.ResolveDivision(new EPropQuadTreeDivisionDecider(nodeDepthResolver, travellerPosition, 0))).ToList();
            var divisionResult = new EPropQuadTreeDivisionResult()
            {
                ScopesToFree = perTreeDivisionResults.SelectMany(c => c.ScopesToFree).ToList(),
                IdChanges = perTreeDivisionResults.SelectMany(c => c.IdChanges).ToList()
            };

            _localeBufferManager.ProcessDivisionChanges(new EPropIdChangeOrder()
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
        private Queue<int> _freePointers; //todo use some better data structure
        private Queue<int> _takenPointers; //todo use some better data structure

        public EPropElevationLocalePointersOccupancyContainer(EPropElevationConfiguration configuration)
        {
            _freePointers = new Queue<int>(Enumerable.Range(0, configuration.MaxLocalePointersCount).ToList());
            _takenPointers = new Queue<int>();
            _pointerIdDict = new DoubleDictionary<EPropElevationId, EPropElevationPointer>();
        }

        public EPropElevationPointer ClaimFreePointer(EPropElevationId id)
        {
            uint freeIndex = (uint) _freePointers.Dequeue();
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
        private EPropLocaleBufferScopeRegistry[] _scopes;

        private ComputeBuffer _scopesUpdateOrdersBuffer;
        private ComputeBuffer _ePropLocaleBuffer;
        public ComputeBuffer _ePropIdsBuffer;
        private ComputeBuffer _scopesToRecalculateBuffer;
        private ComputeBuffer _localesCopyOrdersBuffer;

        private Func<int, Vector2, Task> _localeBufferUpdaterShaderOrderGenerator;
        private Func<int, Vector2, Task> _localeRecalculationShaderOrderGenerator;
        private Func<int, Task> _localesCopyShaderOrderGenerator;

        public EPropLocaleBufferManager(UnityThreadComputeShaderExecutorObject shaderExecutorObject, EPropElevationConfiguration configuration, EPropConstantPyramidParameters constantPyramidParameters)
        {
            _shaderExecutorObject = shaderExecutorObject;
            _configuration = configuration;
            _constantPyramidParameters = constantPyramidParameters;
            _scopes = new EPropLocaleBufferScopeRegistry[configuration.MaxScopesCount];
        }

        public ComputeBuffer EPropLocaleBuffer => _ePropLocaleBuffer;
        public ComputeBuffer EPropIdsBuffer => _ePropIdsBuffer;

        public void Initialize( ComputeBuffer ePyramidPerFrameParametersBuffer, ComputeBuffer ePyramidConfigurationBuffer, Dictionary<HeightPyramidLevel, Texture> heightmapTextures)
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

            GenerateShaderOrderTemplates(ePyramidPerFrameParametersBuffer, ePyramidConfigurationBuffer, heightmapTextures);
        }

        private void GenerateShaderOrderTemplates(ComputeBuffer ePyramidPerFrameParametersBuffer, ComputeBuffer ePyramidConfigurationBuffer,
            Dictionary<HeightPyramidLevel, Texture> heightmapTextures)
        {
            _localeBufferUpdaterShaderOrderGenerator = (ordersCount, travellerPositionWorldSpace) =>
            {
                var shader = ComputeShaderUtils.LoadComputeShader("eterrain_comp");
                MultistepComputeShader localeBufferUpdateShader =
                    new MultistepComputeShader(shader, new IntVector2(ordersCount, 1));

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

                var expectedLevels = new List<HeightPyramidLevel>() {HeightPyramidLevel.Bottom, HeightPyramidLevel.Mid, HeightPyramidLevel.Top};
                foreach (var level in expectedLevels) //TODO remove, this is temporary
                {
                    if (!heightmapTextures.ContainsKey(level))
                    {
                        heightmapTextures[level] = heightmapTextures.Values.Last();
                    }
                }
                foreach(var pair in heightmapTextures)
                {
                    var mapId = parametersContainer.AddExistingComputeShaderTexture(pair.Value);
                    var heightmapTextureName = $"_HeightMap{pair.Key.GetIndex()}";
                    localeBufferUpdateShader.SetTexture(heightmapTextureName, mapId, kernelHandles);
                }

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
                var shader = ComputeShaderUtils.LoadComputeShader("eterrain_comp");
                MultistepComputeShader localeBufferUpdateShader =
                    new MultistepComputeShader(shader, new IntVector2(ordersCount, 1));

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
                var shader = ComputeShaderUtils.LoadComputeShader("eterrain_comp");
                MultistepComputeShader localeBufferUpdateShader =
                    new MultistepComputeShader(shader, new IntVector2(ordersCount, 1));

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

                var expectedLevels = new List<HeightPyramidLevel>() {HeightPyramidLevel.Bottom, HeightPyramidLevel.Mid, HeightPyramidLevel.Top};
                foreach (var level in expectedLevels) //TODO remove, this is temporary
                {
                    if (!heightmapTextures.ContainsKey(level))
                    {
                        heightmapTextures[level] = heightmapTextures.Values.Last();
                    }
                }
                foreach(var pair in heightmapTextures)
                {
                    var mapId = parametersContainer.AddExistingComputeShaderTexture(pair.Value);
                    var heightmapTextureName = $"_HeightMap{pair.Key.GetIndex()}";
                    localeBufferUpdateShader.SetTexture(heightmapTextureName, mapId, kernelHandles);
                }

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

        public void UpdateBuffers(List<EPropSectorSoleUpdateOrder> updateOrders, Vector2 travellerPositionWorldSpace)
        {
            var passesCount = Mathf.CeilToInt(updateOrders.Count / ((float) _configuration.MaxScopeUpdateOrdersInBuffer));
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

                ExecuteUpdateOrder(scopeUpdateOrdersArray, thisPassOrdersCount, travellerPositionWorldSpace);
            }
        }


        private void ExecuteUpdateOrder(GpuSoleUpdateOrder[] scopeUpdateOrdersArray, int ordersCount, Vector2 travellerPositionWorldSpace)
        {
            _scopesUpdateOrdersBuffer.SetData(scopeUpdateOrdersArray);
            _localeBufferUpdaterShaderOrderGenerator(ordersCount,travellerPositionWorldSpace).Wait();
        }

        public void RecalculateLocales(Vector2 travellerPositionWorldSpace, List<LocaleBufferScopeIndexType> scopesToRecalculate)
        {
            var passesCount = Mathf.CeilToInt(scopesToRecalculate.Count / ((float) _configuration.MaxScopesToRecalculatePerPass));
            for (int i = 0; i < passesCount; i++)
            {
                var offset = i * _configuration.MaxScopesToRecalculatePerPass;

                var thisPassScopesCount = Mathf.Min(_configuration.MaxScopesToRecalculatePerPass,scopesToRecalculate.Count - offset);
                var scopesToRecalculateThisPass = scopesToRecalculate.Skip(offset).Take(thisPassScopesCount).ToList();
                ExecuteScopesRecalculation(scopesToRecalculateThisPass, thisPassScopesCount, travellerPositionWorldSpace);
            }
        }

        private void ExecuteScopesRecalculation(List<uint> scopesToRecalculateThisPass, int thisPassScopesCount, Vector2 travellerPositionWorldSpace)
        {
            _scopesToRecalculateBuffer.SetData(scopesToRecalculateThisPass);
            _localeRecalculationShaderOrderGenerator(thisPassScopesCount * _configuration.ScopeLength, travellerPositionWorldSpace).Wait();
        }

        public void ProcessDivisionChanges(EPropIdChangeOrder idChangeOrder)
        {
            CopyLocales(idChangeOrder.IdChanges);
            FreeScopes(idChangeOrder.ScopesToFree);
        }

        private void CopyLocales(List<EPropIdChangeWithPointer> idChanges)
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

                ExecuteLocalesCopyOrder(scopeUpdateOrdersArray, thisPassOrdersCount);
            }
        }

        private void ExecuteLocalesCopyOrder(GpuSoleLocaleCopyOrder[] localesCopyOrdersArray, int thisPassOrdersCount)
        {
            _localesCopyOrdersBuffer.SetData(localesCopyOrdersArray.OrderBy(c=> UnityEngine.Random.Range(0,1)).ToArray());
            _localesCopyShaderOrderGenerator(thisPassOrdersCount).Wait();
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
        public  int MaxScopeUpdateOrdersInBuffer = 1024;
        public int MaxScopesToRecalculatePerPass = 32;
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

        private EPropLocaleBufferScopeRegistryWithIndex GetFreeScope()
        {
            var firstFree = _scopes.Select(c => new {c.Key, c.Value}).FirstOrDefault(c => c.Value.HasFreeIndex());
            if (firstFree == null)
            {
                var newScopeWithIndex = _localeBufferManager.CreateNewScope();
                _scopes[newScopeWithIndex.ScopeIndex] = newScopeWithIndex.Registry;
                return newScopeWithIndex;
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
        private Vector2?[] _localeArray;
        private List<EPropLocaleBufferScopeUpdateOrder> _updateOrders;

        public EPropLocaleBufferScopeRegistry(EPropElevationConfiguration configuration)
        {
            _configuration = configuration;
            _localeArray = new Vector2?[_configuration.ScopeLength];
            _updateOrders = new List<EPropLocaleBufferScopeUpdateOrder>();
        }

        public bool HasFreeIndex()
        {
            return _localeArray.Any(c => !c.HasValue);
        }

        public uint ClaimFreeLocale(Vector2 flatPosition)
        {
            for (uint i = 0; i < _localeArray.Length; i++)
            {
                if (!_localeArray[i].HasValue)
                {
                    _localeArray[i] = flatPosition;
                    _updateOrders.Add(new EPropLocaleBufferScopeUpdateOrder()
                    {
                        FlatPosition = flatPosition,
                        Index = i
                    });
                    return i;
                }
            }

            Preconditions.Fail("There is no free locale is scope");
            return 0;
        }

        public List<EPropLocaleBufferScopeUpdateOrder> RetriveAndClearUpdateOrders()
        {
            var orders = _updateOrders;
            _updateOrders = new List<EPropLocaleBufferScopeUpdateOrder>();
            return orders;
        }

        public bool IsDirty => _updateOrders.Any();
        public bool IsEmpty => _localeArray.All(c => !c.HasValue);

        public List<InScopeIndexTypeWithFlatPosition> RetriveAllLocales()
        {
            return _localeArray.Where(c => c.HasValue).Select((c, i) => new InScopeIndexTypeWithFlatPosition()
                {
                    FlatPosition = c.Value,
                    InScopeIndex = (uint)i

                }
            ).ToList();
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
