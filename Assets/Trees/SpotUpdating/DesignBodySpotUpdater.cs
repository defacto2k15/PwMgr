using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.ComputeShaders;
using Assets.ComputeShaders.Templating;
using Assets.Heightmaps;
using Assets.Heightmaps.Ring1.TerrainDescription.FeatureGenerating;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.TerrainMat;
using Assets.Utils;
using Assets.Utils.Quadtree;
using Assets.Utils.Services;
using GeoAPI.Geometries;
using UnityEngine;
using UnityEngine.Profiling;
using Wintellect.PowerCollections;

namespace Assets.Trees.SpotUpdating
{
    public class DesignBodySpotUpdater : IDesignBodySpotUpdater
    {
        private QuadtreeWithId<SpotId, SpotInQuadtree> _spotsTree = new QuadtreeWithId<SpotId, SpotInQuadtree>();

        private QuadtreeWithId<SpotId, SpotGroupInQuadtree> _groupSpotsTree =
            new QuadtreeWithId<SpotId, SpotGroupInQuadtree>();

        private Set<SpotId> _emptySpots = new Set<SpotId>();

        private QuadtreeWithId<SpotUpdaterTerrainTextureId, UpdatedTerrainTextures> _terrainTree
            = new QuadtreeWithId<SpotUpdaterTerrainTextureId, UpdatedTerrainTextures>();

        private ISpotPositionChangesListener _changesListener;
        private DesignBodySpotChangeCalculator _spotChangeCalculator;

        public DesignBodySpotUpdater(
            DesignBodySpotChangeCalculator spotChangeCalculator,
            ISpotPositionChangesListener changesListener = null)
        {
            _changesListener = changesListener;
            _spotChangeCalculator = spotChangeCalculator;
        }

        public void SetChangesListener(ISpotPositionChangesListener listener)
        {
            _changesListener = listener;
        }

        public async Task RegisterDesignBodiesAsync(List<FlatPositionWithSpotId> positionsWithIds)
        {
            if (!positionsWithIds.Any())
            {
                return;
            }
            var newSpots = new List<SpotInQuadtree>();
            MyProfiler.BeginSample("DesignBodyUpdater: Adding spots to tree");
            foreach (var aPositionWithId in positionsWithIds)
            {
                var newSpotInTree = new SpotInQuadtree()
                {
                    FlatPosition = aPositionWithId.FlatPosition,
                    SpotId = aPositionWithId.SpotId
                };
                _spotsTree.Insert(newSpotInTree);
                newSpots.Add(newSpotInTree);
            }
            MyProfiler.EndSample();
            var unionEnvelope =
                MyNetTopologySuiteUtils.UnionEnvelope(newSpots.Select(c => c.CalculateEnvelope()).ToList());
            var touchedTerrainTextures = _terrainTree.Query(unionEnvelope);

            Dictionary<SpotUpdaterTerrainTextureId, List<SpotId>> terrainTextureToSpot
                = new Dictionary<SpotUpdaterTerrainTextureId, List<SpotId>>();
            foreach (var terrainId in touchedTerrainTextures.Select(c => c.Id))
            {
                terrainTextureToSpot[terrainId] = new List<SpotId>();
            }

            foreach (var aSpot in newSpots)
            {
                var spotEnvelope = aSpot.CalculateEnvelope();
                var usedTexture =
                    touchedTerrainTextures.FirstOrDefault(c => c.CalculateEnvelope().Intersects(spotEnvelope));
                if (usedTexture != null)
                {
                    terrainTextureToSpot[usedTexture.Id].Add(aSpot.SpotId);
                }
            }

            foreach (var pair in terrainTextureToSpot)
            {
                if (pair.Value.Any())
                {
                    var usedTexture = _terrainTree.RetriveEntity(pair.Key);
                    var spotsInChangedTexture = pair.Value.Select(c => _spotsTree.RetriveEntity(c)).ToList();

                    await CheckChangedSpots(usedTexture, spotsInChangedTexture);
                }
            }
        }

        public async Task RegisterDesignBodiesGroupAsync(SpotId spotId, List<Vector2> flatPositions)
        {
            if (!flatPositions.Any())
            {
                _emptySpots.Add(spotId);
                return;
            }
            var newGroup = new SpotGroupInQuadtree(spotId, flatPositions);
            _groupSpotsTree.Insert(newGroup);

            var envelope = newGroup.CalculateEnvelope();
            var touchedTerrainTextures = _terrainTree.Query(envelope);

            var usedTexture = touchedTerrainTextures.FirstOrDefault(c => c.CalculateEnvelope().Intersects(envelope));
            if (usedTexture != null)
            {
                await CheckChangedSpots(usedTexture, new List<SpotGroupInQuadtree>()
                {
                    newGroup
                });
            }
        }

        public void ForgetDesignBodies(List<SpotId> bodiesToRemove)
        {
            foreach (var spotId in bodiesToRemove)
            {
                if (_emptySpots.Contains(spotId))
                {
                    _emptySpots.Remove(spotId);
                }
                else if (spotId.IsGroup)
                {
                    _groupSpotsTree.Remove(spotId);
                }
                else
                {
                    _spotsTree.Remove(spotId);
                }
            }
        }

        public async Task UpdateBodiesSpotsAsync(UpdatedTerrainTextures newHeightTexture)
        {
            _terrainTree.Insert(newHeightTexture);

            var queryRectangle = newHeightTexture.UsedGlobalArea();
            var queryEnvelope = MyNetTopologySuiteUtils.ToEnvelope(queryRectangle);

            var changedSpots = _spotsTree.Query(queryEnvelope);
            if (changedSpots.Any())
            {
                await CheckChangedSpots(newHeightTexture, changedSpots);
            }

            var changedGroups = _groupSpotsTree.Query(queryEnvelope);
            if (changedGroups.Any())
            {
                await CheckChangedSpots(newHeightTexture, changedGroups);
            }
        }

        private async Task CheckChangedSpots(UpdatedTerrainTextures heightTexture, List<SpotInQuadtree> spots)
        {
            var newSpotDatas =
                await _spotChangeCalculator.CalculateChangeAsync(heightTexture,
                    spots.Select(c => c.FlatPosition).ToList());


            _changesListener.SpotsWereChanged(
                Enumerable.Range(0, spots.Count).Select(i => new
                {
                    Id = spots[i].Id,
                    Data = newSpotDatas[i]
                }).ToDictionary(c => c.Id, c => c.Data)
            );
        }

        private async Task CheckChangedSpots(UpdatedTerrainTextures heightTexture, List<SpotGroupInQuadtree> spots)
        {
            var groupsLengths = spots.Select(c => c.FlatPositions.Count).ToList();

            var newSpotDatas =
                await _spotChangeCalculator.CalculateChangeAsync(heightTexture,
                    spots.SelectMany(c => c.FlatPositions).ToList());

            var outDict = new Dictionary<SpotId, List<SpotData>>();
            var spotsSum = 0;
            for (int i = 0; i < spots.Count; i++)
            {
                outDict[spots[i].Id] = newSpotDatas.GetRange(spotsSum, groupsLengths[i]);
                spotsSum += groupsLengths[i];
            }

            _changesListener.SpotGroupsWereChanged(outDict);
        }

        public void RemoveTerrainTextures(SpotUpdaterTerrainTextureId id)
        {
            _terrainTree.Remove(id);
        }


        private class SpotInQuadtree : IHasEnvelope, IHasId<SpotId>
        {
            public SpotId SpotId;
            public Vector2 FlatPosition;

            public Envelope CalculateEnvelope()
            {
                return MyNetTopologySuiteUtils.ToPointEnvelope(FlatPosition);
            }

            public SpotId Id
            {
                get { return SpotId; }
            }
        }

        public class SpotGroupInQuadtree : IHasEnvelope, IHasId<SpotId>
        {
            private SpotId _spotId;
            private List<Vector2> _flatPositions;
            private Envelope _positionsEnvelope;

            public SpotGroupInQuadtree(SpotId spotId, List<Vector2> flatPositions)
            {
                _spotId = spotId;
                _flatPositions = flatPositions;

                _positionsEnvelope = MyNetTopologySuiteUtils.ToEnvelope(flatPositions);
            }

            public Envelope CalculateEnvelope()
            {
                return _positionsEnvelope;
            }

            public SpotId Id => _spotId;

            public List<Vector2> FlatPositions => _flatPositions;
        }
    }

    public class UpdatedTerrainTextures : IHasEnvelope, IHasId<SpotUpdaterTerrainTextureId>
    {
        public Texture HeightTexture;
        public Texture NormalTexture;
        public MyRectangle TextureCoords;
        public MyRectangle TextureGlobalPosition;
        public SpotUpdaterTerrainTextureId TerrainTextureId;

        public MyRectangle UsedGlobalArea()
        {
            return RectangleUtils.CalculateSubPosition(TextureGlobalPosition, TextureCoords);
        }

        public Envelope CalculateEnvelope()
        {
            return MyNetTopologySuiteUtils.ToEnvelope(UsedGlobalArea());
        }

        public SpotUpdaterTerrainTextureId Id
        {
            get { return TerrainTextureId; }
        }
    }

    public class DesignBodySpotRegistrationInfo
    {
        public Vector2 FlatPosition;
    }

   public class SpotData
    {
        public float Height;
        public Vector3 Normal;
    }

    public struct SpotUpdaterTerrainTextureId
    {
        public SpotUpdaterTerrainTextureId(int value)
        {
            Value = value;
        }

        public int Value;

        public bool Equals(SpotUpdaterTerrainTextureId other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is SpotUpdaterTerrainTextureId && Equals((SpotUpdaterTerrainTextureId) obj);
        }

        public override int GetHashCode()
        {
            return Value;
        }
    }


    public struct SpotId
    {
        public SpotId(int value, bool isGroup = false)
        {
            Value = value;
            IsGroup = isGroup;
        }

        public int Value;
        public bool IsGroup;

        public bool Equals(SpotId other)
        {
            return Value == other.Value && IsGroup == other.IsGroup;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is SpotId && Equals((SpotId) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Value * 397) ^ IsGroup.GetHashCode();
            }
        }
    }

    public class FlatPositionWithSpotId
    {
        public Vector2 FlatPosition;
        public SpotId SpotId;
    }

    public class DesignBodySpotChangeCalculator
    {
        private ComputeShaderContainerGameObject _computeShaderContainer;
        private UnityThreadComputeShaderExecutorObject _shaderExecutorObject;
        private CommonExecutorUTProxy _commonExecutor;
        private readonly HeightDenormalizer _heightDenormalizer;

        public DesignBodySpotChangeCalculator(
            ComputeShaderContainerGameObject computeShaderContainer,
            UnityThreadComputeShaderExecutorObject shaderExecutorObject,
            CommonExecutorUTProxy commonExecutor,
            HeightDenormalizer heightDenormalizer)
        {
            _computeShaderContainer = computeShaderContainer;
            _shaderExecutorObject = shaderExecutorObject;
            _commonExecutor = commonExecutor;
            _heightDenormalizer = heightDenormalizer;
        }

//todo write computation to find out normals without normal texture (direct normal computation?
        public async Task<List<SpotData>> CalculateChangeAsync(
            UpdatedTerrainTextures terrainTextures,
            List<Vector2> positions)
        {
            var elementsCount = positions.Count;
            ComputeShaderParametersContainer parametersContainer = new ComputeShaderParametersContainer();

            var terrainTexture = parametersContainer.AddExistingComputeShaderTexture(terrainTextures.HeightTexture);
            var normalTexture = parametersContainer.AddExistingComputeShaderTexture(terrainTextures.NormalTexture);

            InputPositionsComputeShaderStruct[] inputPositions = new InputPositionsComputeShaderStruct[elementsCount];
            foreach (var i in Enumerable.Range(0, elementsCount))
            {
                inputPositions[i] = new InputPositionsComputeShaderStruct()
                {
                    X = positions[i].x,
                    Y = positions[i].y
                };
            }

            var inputPositionsBuffer = parametersContainer.AddComputeBufferTemplate(new MyComputeBufferTemplate()
            {
                Count = elementsCount,
                Stride = 2 * sizeof(float),
                Type = ComputeBufferType.Default,
                BufferData = inputPositions
            });


            var outputDataBuffer = parametersContainer.AddComputeBufferTemplate(new MyComputeBufferTemplate()
            {
                Count = elementsCount,
                Stride = 4 * sizeof(float),
                Type = ComputeBufferType.Append
            });

            MultistepComputeShader spotResolvingComputeShader =
                new MultistepComputeShader(_computeShaderContainer.SpotResolvingComputeShader,
                    new IntVector2(elementsCount, 1));

            var resolveSpotKernel = spotResolvingComputeShader.AddKernel("CS_ResolveSpots");

            spotResolvingComputeShader.SetGlobalUniform("g_GlobalPosition",
                terrainTextures.TextureGlobalPosition.ToVector4());
            spotResolvingComputeShader.SetBuffer("InputPositionsBuffer", inputPositionsBuffer,
                new List<MyKernelHandle>()
                {
                    resolveSpotKernel
                });

            spotResolvingComputeShader.SetBuffer("OutputDataBuffer", outputDataBuffer,
                new List<MyKernelHandle>()
                {
                    resolveSpotKernel
                });

            spotResolvingComputeShader.SetTexture("TerrainTexture", terrainTexture, new List<MyKernelHandle>()
            {
                resolveSpotKernel
            });

            spotResolvingComputeShader.SetTexture("NormalsTexture", normalTexture, new List<MyKernelHandle>()
            {
                resolveSpotKernel
            });


            ComputeBufferRequestedOutParameters outParameters = new ComputeBufferRequestedOutParameters(
                requestedBufferIds: new List<MyComputeBufferId>()
                {
                    outputDataBuffer
                });

            await _shaderExecutorObject.DispatchComputeShader(new ComputeShaderOrder()
            {
                OutParameters = outParameters,
                ParametersContainer = parametersContainer,
                WorkPacks = new List<ComputeShaderWorkPack>()
                {
                    new ComputeShaderWorkPack()
                    {
                        Shader = spotResolvingComputeShader,
                        DispatchLoops = new List<ComputeShaderDispatchLoop>()
                        {
                            new ComputeShaderDispatchLoop()
                            {
                                DispatchCount = 1,
                                KernelHandles = new List<MyKernelHandle>() {resolveSpotKernel}
                            }
                        }
                    },
                }
            });

            var outDataBuffer = outParameters.RetriveBuffer(outputDataBuffer);

            var outSpotData = await _commonExecutor.AddAction(() =>
            {
                OutputSpotDataComputeShaderStruct[] iOutSpotData = new OutputSpotDataComputeShaderStruct[elementsCount];
                outDataBuffer.GetData(iOutSpotData);
                return iOutSpotData;
            });

            return Enumerable.Range(0, elementsCount).Select(i => new SpotData()
            {
                Height = _heightDenormalizer.Denormalize(outSpotData[i].Height),
                Normal = new Vector3(
                    outSpotData[i].NormalX,
                    outSpotData[i].NormalY,
                    outSpotData[i].NormalZ
                )
            }).ToList();
        }

        private struct InputPositionsComputeShaderStruct
        {
            public float X;
            public float Y;
        }

        private struct OutputSpotDataComputeShaderStruct
        {
            public float NormalX;
            public float NormalY;
            public float NormalZ;
            public float Height;
        }
    }
}