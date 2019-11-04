using System.Collections.Generic;
using System.Threading.Tasks;
using Assets.Heightmaps.Ring1.MeshGeneration;
using Assets.MeshGeneration;
using Assets.ShaderUtils;
using Assets.Utils;
using UnityEngine;

namespace Assets.Heightmaps.GRing
{
    public class GRingTerrainMeshProvider
    {
        private readonly MeshGeneratorUTProxy _meshGenerator;
        private FlatLod _flatLod;
        private GRingTerrainMeshProviderConfiguration _configuration;

        private IntVector2 _gameObjectMeshResolution;
        private int _heightmapLodOffset;

        public GRingTerrainMeshProvider(MeshGeneratorUTProxy meshGenerator, FlatLod flatLod,
            GRingTerrainMeshProviderConfiguration configuration)
        {
            _meshGenerator = meshGenerator;
            _flatLod = flatLod;
            _configuration = configuration;
            CalculateMeshResolution();
        }

        private void CalculateMeshResolution()
        {
            var flatLodLevel = _flatLod.ScalarValue;

            if (!_configuration.FlatLodToMeshLodPower.ContainsKey(flatLodLevel))
            {
                Preconditions.Fail("Unsupported lod level: " + flatLodLevel);
            }
            var lodConfiguration = _configuration.FlatLodToMeshLodPower[flatLodLevel];
            int meshLodPower = lodConfiguration.ResolutionDivisionExp;

            int finalMeshResolution = Mathf.RoundToInt(_configuration.BaseMeshResolution / Mathf.Pow(2, meshLodPower));

            _gameObjectMeshResolution = new IntVector2(finalMeshResolution + 1, finalMeshResolution + 1);
            _heightmapLodOffset = lodConfiguration.HeightmapLodOffset;
        }

        public async Task<GRingMeshDetail> ProvideMeshDetailsAsync()
        {
            var mesh = await _meshGenerator.AddOrder(
                () => PlaneGenerator.CreateFlatPlaneMesh(_gameObjectMeshResolution.X, _gameObjectMeshResolution.Y));

            var pack = new UniformsPack();
            pack.SetUniform("_HeightmapLodOffset", _heightmapLodOffset);

            return new GRingMeshDetail()
            {
                Mesh = mesh,
                Uniforms = pack,
                HeightmapLod = _heightmapLodOffset
            };
        }
    }

    public class GRingTerrainMeshProviderConfiguration
    {
        public Dictionary<int, SingleLevelTerrainMeshGenerationConfiguration> FlatLodToMeshLodPower =
            new Dictionary<int, SingleLevelTerrainMeshGenerationConfiguration>()
            {
                {
                    1,
                    new SingleLevelTerrainMeshGenerationConfiguration()
                    {
                        HeightmapLodOffset = 3,
                        ResolutionDivisionExp = 3
                    }
                },
                {
                    2,
                    new SingleLevelTerrainMeshGenerationConfiguration()
                    {
                        HeightmapLodOffset = 3,
                        ResolutionDivisionExp = 3
                    }
                },
                {
                    3,
                    new SingleLevelTerrainMeshGenerationConfiguration()
                    {
                        HeightmapLodOffset = 2,
                        ResolutionDivisionExp = 2
                    }
                },
                {
                    4,
                    new SingleLevelTerrainMeshGenerationConfiguration()
                    {
                        HeightmapLodOffset = 2,
                        ResolutionDivisionExp = 2
                    }
                },
                {
                    5,
                    new SingleLevelTerrainMeshGenerationConfiguration()
                    {
                        HeightmapLodOffset = 1,
                        ResolutionDivisionExp = 1
                    }
                },
                {
                    6,
                    new SingleLevelTerrainMeshGenerationConfiguration()
                    {
                        HeightmapLodOffset = 0,
                        ResolutionDivisionExp = 0
                    }
                },
                {
                    7,
                    new SingleLevelTerrainMeshGenerationConfiguration()
                    {
                        HeightmapLodOffset = 0,
                        ResolutionDivisionExp = 0
                    }
                },
                {
                    8,
                    new SingleLevelTerrainMeshGenerationConfiguration()
                    {
                        HeightmapLodOffset = 1,
                        ResolutionDivisionExp = 1
                    }
                },
                {
                    9,
                    new SingleLevelTerrainMeshGenerationConfiguration()
                    {
                        HeightmapLodOffset = 0,
                        ResolutionDivisionExp = 0
                    }
                },
                {
                    10,
                    new SingleLevelTerrainMeshGenerationConfiguration()
                    {
                        HeightmapLodOffset = 0,
                        ResolutionDivisionExp = 0
                    }
                },
                {
                    11,
                    new SingleLevelTerrainMeshGenerationConfiguration()
                    {
                        HeightmapLodOffset = 0,
                        ResolutionDivisionExp = 1
                    }
                },
                {
                    12,
                    new SingleLevelTerrainMeshGenerationConfiguration()
                    {
                        HeightmapLodOffset = 0,
                        ResolutionDivisionExp = 2
                    }
                },
            };

        public int BaseMeshResolution = 240;
    }

    public class SingleLevelTerrainMeshGenerationConfiguration
    {
        public int ResolutionDivisionExp;
        public int HeightmapLodOffset;
    }
}