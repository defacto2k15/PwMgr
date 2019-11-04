using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Heightmaps.GRing;
using Assets.Heightmaps.Ring1;
using Assets.Heightmaps.Ring1.TerrainDescription;
using Assets.Heightmaps.Ring1.valTypes;
using UnityEngine;

namespace Assets.FinalExecution
{
    [Serializable]
    public class FeGRingConfiguration
    {
        public FEConfiguration FeConfiguration { get; set; }

        public bool WeldingEnabled = false;
        public int Ring2IntensityPatternEnhancingSizeMultiplier => 12;
        public bool MakeTerrainVisible = true;

        public GRingGroundShapeProviderConfiguration GroundShapeProviderConfiguration =>
            new GRingGroundShapeProviderConfiguration()
            {
                SettingPerFlatLod = RootTerrainConfiguration.ToDictionary(c => c.Key, c => new GroundShapeLevelSetting()
                {
                    DetailResolution = c.Value.DetailResolution,
                    DirectNormalComputation = c.Value.DirectNormalComputation
                })
            };

        private float mainBaseQuadSideLength = 90;

        private float CalculatePrecisionDistancePow(float baseQuadSideLength, int pow)
        {
            int afterPow = (1 << pow);
            return CalculatePrecisionDistance(baseQuadSideLength, afterPow, afterPow);
        }

        private float CalculatePrecisionDistance(float baseQuadSideLength, int xMove, int yMove)
        {
            return (new Vector2(xMove - 0.5f, yMove - 0.5f) * baseQuadSideLength).magnitude * 1.02f;
        }

        public Dictionary<float, int> QuadLodPrecisionDistances => new Dictionary<float, int>
        {
            {CalculatePrecisionDistancePow(mainBaseQuadSideLength / 16, 1), 13},
            {CalculatePrecisionDistancePow(mainBaseQuadSideLength / 8, 1), 12},
            {CalculatePrecisionDistancePow(mainBaseQuadSideLength / 4, 1), 11},
            {CalculatePrecisionDistancePow(mainBaseQuadSideLength / 2, 1), 10},
            {CalculatePrecisionDistancePow(mainBaseQuadSideLength, 1), 9},
            {CalculatePrecisionDistancePow(mainBaseQuadSideLength, 2), 8},
            {CalculatePrecisionDistancePow(mainBaseQuadSideLength, 3), 7},
            {CalculatePrecisionDistancePow(mainBaseQuadSideLength, 4), 6},
            {CalculatePrecisionDistancePow(mainBaseQuadSideLength, 5), 5},
            {CalculatePrecisionDistancePow(mainBaseQuadSideLength, 6), 4},
            //{CalculatePrecisionDistancePow(baseQuadSideLength, 7), 3},
            //{CalculatePrecisionDistancePow(baseQuadSideLength, 8), 2},
            //{CalculatePrecisionDistancePow(baseQuadSideLength, 9), 1},
        };

        public FlatLodConfiguration FlatLodConfiguration => new FlatLodConfiguration()
        {
            PrecisionsSets = new List<FlatLotPrecisionsSet>()
            {
                new FlatLotPrecisionsSet()
                {
                    FlatLodPrecisionDistances = new Dictionary<float, int>()
                    {
                        {CalculatePrecisionDistancePow(mainBaseQuadSideLength / 32, 1), 14},
                        {CalculatePrecisionDistancePow(mainBaseQuadSideLength / 16, 1), 13},
                    },
                    MaxSupprotedQuadLod = 13,
                    MinSupprotedQuadLod = 13
                },
                new FlatLotPrecisionsSet()
                {
                    FlatLodPrecisionDistances = new Dictionary<float, int>()
                    {
                        {CalculatePrecisionDistancePow(mainBaseQuadSideLength, 6), 4},
                        {CalculatePrecisionDistancePow(mainBaseQuadSideLength, 7), 3},
                        {CalculatePrecisionDistancePow(mainBaseQuadSideLength, 8) * 0.7f, 2},
                        {CalculatePrecisionDistancePow(mainBaseQuadSideLength, 9) * 0.5f, 1},
                    },
                    MaxSupprotedQuadLod = 4,
                    MinSupprotedQuadLod = 4
                },
            },
        };

        public class FlatLodTerrainConfiguration
        {
            public int HeightmapLodOffset;
            public int ResolutionDivisionExp;
            public TerrainCardinalResolution DetailResolution;
            public bool DirectNormalComputation;
        }

        public Dictionary<int, FlatLodTerrainConfiguration> RootTerrainConfiguration =
            new Dictionary<int, FlatLodTerrainConfiguration>()
            {
                {
                    1,
                    new FlatLodTerrainConfiguration()
                    {
                        DetailResolution = TerrainCardinalResolution.MIN_RESOLUTION,
                        DirectNormalComputation = false,
                        HeightmapLodOffset = 4,
                        ResolutionDivisionExp = 4
                    }
                },
                {
                    2,
                    new FlatLodTerrainConfiguration()
                    {
                        DetailResolution = TerrainCardinalResolution.MIN_RESOLUTION,
                        DirectNormalComputation = false,
                        HeightmapLodOffset = 4,
                        ResolutionDivisionExp = 4
                    }
                },
                {
                    3,
                    new FlatLodTerrainConfiguration()
                    {
                        DetailResolution = TerrainCardinalResolution.MIN_RESOLUTION,
                        DirectNormalComputation = false,
                        HeightmapLodOffset = 3,
                        ResolutionDivisionExp = 3
                    }
                },
                {
                    4,
                    new FlatLodTerrainConfiguration()
                    {
                        DetailResolution = TerrainCardinalResolution.MIN_RESOLUTION,
                        DirectNormalComputation = false,
                        HeightmapLodOffset = 2,
                        ResolutionDivisionExp = 2
                    }
                },
                {
                    5,
                    new FlatLodTerrainConfiguration()
                    {
                        DetailResolution = TerrainCardinalResolution.MIN_RESOLUTION,
                        DirectNormalComputation = false,
                        HeightmapLodOffset = 1,
                        ResolutionDivisionExp = 2
                    }
                },
                {
                    6,
                    new FlatLodTerrainConfiguration()
                    {
                        DetailResolution = TerrainCardinalResolution.MIN_RESOLUTION,
                        DirectNormalComputation = false,
                        HeightmapLodOffset = 0,
                        ResolutionDivisionExp = 2
                    }
                },
                {
                    7,
                    new FlatLodTerrainConfiguration()
                    {
                        DetailResolution = TerrainCardinalResolution.MID_RESOLUTION,
                        DirectNormalComputation = false,
                        HeightmapLodOffset = 2,
                        ResolutionDivisionExp = 2
                    }
                },
                {
                    8,
                    new FlatLodTerrainConfiguration()
                    {
                        DetailResolution = TerrainCardinalResolution.MID_RESOLUTION,
                        DirectNormalComputation = false,
                        HeightmapLodOffset = 1,
                        ResolutionDivisionExp = 2
                    }
                },
                {
                    9,
                    new FlatLodTerrainConfiguration()
                    {
                        DetailResolution = TerrainCardinalResolution.MID_RESOLUTION,
                        DirectNormalComputation = false,
                        HeightmapLodOffset = 0,
                        ResolutionDivisionExp = 2
                    }
                },
                {
                    10,
                    new FlatLodTerrainConfiguration()
                    {
                        DetailResolution = TerrainCardinalResolution.MAX_RESOLUTION,
                        DirectNormalComputation = false,
                        HeightmapLodOffset = 3,
                        ResolutionDivisionExp = 2
                    }
                },
                {
                    11,
                    new FlatLodTerrainConfiguration()
                    {
                        DetailResolution = TerrainCardinalResolution.MAX_RESOLUTION,
                        DirectNormalComputation = false,
                        HeightmapLodOffset = 2,
                        ResolutionDivisionExp = 2
                    }
                },
                {
                    12,
                    new FlatLodTerrainConfiguration()
                    {
                        DetailResolution = TerrainCardinalResolution.MAX_RESOLUTION,
                        DirectNormalComputation = false,
                        HeightmapLodOffset = 1,
                        ResolutionDivisionExp = 2
                    }
                },
                {
                    13,
                    new FlatLodTerrainConfiguration()
                    {
                        DetailResolution = TerrainCardinalResolution.MAX_RESOLUTION,
                        DirectNormalComputation = false,
                        HeightmapLodOffset = 0,
                        ResolutionDivisionExp = 3
                    }
                },
                {
                    14,
                    new FlatLodTerrainConfiguration()
                    {
                        DetailResolution = TerrainCardinalResolution.MAX_RESOLUTION,
                        DirectNormalComputation = false,
                        HeightmapLodOffset = 0,
                        ResolutionDivisionExp = 3
                    }
                },
            };


        public GRingTerrainMeshProviderConfiguration TerrainMeshProviderConfiguration =>
            new GRingTerrainMeshProviderConfiguration()
            {
                FlatLodToMeshLodPower = RootTerrainConfiguration.ToDictionary(c => c.Key,
                    c => new SingleLevelTerrainMeshGenerationConfiguration()
                    {
                        HeightmapLodOffset = c.Value.HeightmapLodOffset,
                        ResolutionDivisionExp = c.Value.ResolutionDivisionExp
                    }),
                BaseMeshResolution = 240
            };

        /////////////////////// RING1
        public MyRectangle Ring1GenerationArea =>
            FeConfiguration.Repositioner.InvMove(MyRectangle.FromVertex(new Vector2(-3600, -3240), new Vector2(3600, 2520)));

        public int Ring1GlobalSideLength = 92160;

        public Ring1TreeConfiguration Ring1TreeConfiguration = new Ring1TreeConfiguration()
        {
            MinimumTreeUpdateDelta = 5
        };


    }
}