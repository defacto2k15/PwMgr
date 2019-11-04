using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.Utils;
using Assets.Utils.ArrayUtils;
using UnityEngine;

namespace Assets.Heightmaps.Ring1.Erosion
{
    public class MeiHydraulicEroder
    {
        public void Erode(SimpleHeightArray heightMap, MeiHydraulicEroderConfiguration configuration)
        {
            //int stepCount = configuration.StepCount;
            //float deltaT = configuration.DeltaT;
            //float constantWaterAdding = configuration.ConstantWaterAdding;
            //float A_pipeCrossSection = configuration.A_PipeCrossSection;
            //float l_pipeLength = configuration.L_PipeLength;
            //float g_GravityAcceleration = configuration.GravityAcceleration;
            //float ks_DissolvingConstant = configuration.DissolvingConstant;
            //float kd_DepositionConstant = configuration.DepositionConstant;
            //float ke_EvaporationConstant = configuration.EvaporationConstant;
            //float kc_SedimentCapacityConstant = configuration.SedimentCapacityConstant;
            //Vector2 gridSize = configuration.GridSize;

            int stepCount = configuration.StepCount;
            float deltaT = configuration.DeltaT;
            float constantWaterAdding = configuration.ConstantWaterAdding;
            float A_pipeCrossSection = configuration.A_PipeCrossSection;
            float l_pipeLength = configuration.L_PipeLength;
            float g_GravityAcceleration = configuration.GravityAcceleration;
            float ks_DissolvingConstant = configuration.DissolvingConstant;
            float kd_DepositionConstant = configuration.DepositionConstant;
            float ke_EvaporationConstant = configuration.EvaporationConstant;
            float kc_SedimentCapacityConstant = configuration.SedimentCapacityConstant;
            Vector2 gridSize = configuration.GridSize;

            float lX = gridSize.x;
            float lY = gridSize.y;

            var waterMap = new SimpleHeightArray(heightMap.Width, heightMap.Height);
            var waterMap_1 = new SimpleHeightArray(heightMap.Width, heightMap.Height);
            var waterMap_2 = new SimpleHeightArray(heightMap.Width, heightMap.Height);

            var fluxMap = new MySimpleArray<Vector4>(heightMap.Width, heightMap.Height);
            var velocityMap = new MySimpleArray<Vector2>(heightMap.Width, heightMap.Height);
            var sedimentMap = new SimpleHeightArray(heightMap.Width, heightMap.Height);
            var sedimentMap_1 = new SimpleHeightArray(heightMap.Width, heightMap.Height);

            for (int i = 0; i < stepCount; i++)
            {
                // water increment
                for (int y = 0; y < heightMap.Height; y++)
                {
                    for (int x = 0; x < heightMap.Width; x++)
                    {
                        var aPoint = new IntVector2(x, y);
                        waterMap_1.SetValue(aPoint, waterMap.GetValue(aPoint) + deltaT * constantWaterAdding);
                    }
                }

                // flow simulation
                for (int y = 0; y < heightMap.Height; y++)
                {
                    for (int x = 0; x < heightMap.Width; x++)
                    {
                        var aPoint = new IntVector2(x, y);
                        if (x == 10 && y == 10)
                        {
                            int yyy = 2;
                        }

                        Vector4 newFlux = Vector4.zero;
                        var neighbours = GetNeighbours(heightMap, aPoint);
                        foreach (var neighbour in neighbours)
                        {
                            var d_difference = (heightMap.GetValue(aPoint) + waterMap_1.GetValue(aPoint)) -
                                               (heightMap.GetValue(neighbour.Position) +
                                                waterMap_1.GetValue(neighbour.Position));

                            var d_flux = fluxMap.GetValue(aPoint)[(int) neighbour.DirectionIndex];
                            var d_new_flux = Mathf.Max(0,
                                d_flux + deltaT * A_pipeCrossSection * (g_GravityAcceleration * d_difference) /
                                l_pipeLength);
                            newFlux[(int) neighbour.DirectionIndex] = d_new_flux;
                        }

                        var aD1 = waterMap_1.GetValue(aPoint);
                        var K_factor = Mathf.Min(1,
                            aD1 * (lX * lY) / ((newFlux[0] + newFlux[1] + newFlux[2] + newFlux[3]) * deltaT));
                        fluxMap.SetValue(aPoint, K_factor * newFlux);
                    }
                }

                // velocity calculation
                for (int y = 0; y < heightMap.Height; y++)
                {
                    for (int x = 0; x < heightMap.Width; x++)
                    {
                        var aPoint = new IntVector2(x, y);
                        if (x == 10 && y == 10)
                        {
                            int yyy = 2;
                        }

                        var neighbours = GetNeighbours(heightMap, aPoint);

                        var outFlow = VectorUtils.SumMembers(fluxMap.GetValue(aPoint));
                        var inFlow =
                            neighbours.Select(c => fluxMap.GetValue(c.Position)[(int) c.DirectionIndex.GetOpposite()])
                                .Sum();

                        var aChangeOfWater = deltaT * (inFlow - outFlow);
                        waterMap_2.SetValue(aPoint,
                            Mathf.Max(0, waterMap_1.GetValue(aPoint) + aChangeOfWater / (lX * lY)));

                        var horizontalFlux =
                            GetFlux(fluxMap, neighbours, MaiNeighbourDirection.LEFT, MaiNeighbourDirection.RIGHT) -
                            fluxMap.GetValue(aPoint)[(int) MaiNeighbourDirection.LEFT] +
                            fluxMap.GetValue(aPoint)[(int) MaiNeighbourDirection.RIGHT] -
                            GetFlux(fluxMap, neighbours, MaiNeighbourDirection.RIGHT, MaiNeighbourDirection.LEFT);

                        var deltaWx = horizontalFlux / 2;

                        var verticalFlux =
                            GetFlux(fluxMap, neighbours, MaiNeighbourDirection.DOWN, MaiNeighbourDirection.UP) -
                            fluxMap.GetValue(aPoint)[(int) MaiNeighbourDirection.DOWN] +
                            fluxMap.GetValue(aPoint)[(int) MaiNeighbourDirection.UP] -
                            GetFlux(fluxMap, neighbours, MaiNeighbourDirection.UP, MaiNeighbourDirection.DOWN);

                        var deltaWy = verticalFlux / 2;

                        var avgHeight = (waterMap_1.GetValue(aPoint) + waterMap_2.GetValue(aPoint)) / 2;

                        var newVelocity = new Vector2(
                            (deltaWx / (lX * avgHeight)),
                            (deltaWy / (lY * avgHeight))
                        );
                        velocityMap.SetValue(aPoint, newVelocity); //todo ograniczenie CFL
                    }
                }

                // sediment calc
                for (int y = 0; y < heightMap.Height; y++)
                {
                    for (int x = 0; x < heightMap.Width; x++)
                    {
                        var aPoint = new IntVector2(x, y);
                        if (x == 10 && y == 10)
                        {
                            int yyy = 2;
                        }
                        var aHeight = heightMap.GetValue(aPoint);

                        Vector3 upDir = new Vector3(0, 0, lY);
                        if (y != heightMap.Height - 1)
                        {
                            var upHeight = heightMap.GetValue(aPoint + new IntVector2(0, 1));
                            upDir[1] = upHeight - aHeight;
                        }

                        Vector3 rightDir = new Vector3(lX, 0, 0);
                        if (x != heightMap.Width - 1)
                        {
                            var rightHeight = heightMap.GetValue(aPoint + new IntVector2(1, 0));
                            rightDir[1] = rightHeight - aHeight;
                        }

                        var aNormal = Vector3.Cross(upDir, rightDir);

                        var baseNormal = new Vector3(0, 1, 0);

                        var cosAlpha = Vector3.Dot(baseNormal, aNormal) / (baseNormal.magnitude * aNormal.magnitude);
                        var sinAlpha = Math.Sqrt(1 - cosAlpha * cosAlpha);

                        //////// OTHER CALCULATING

                        var _o_notmal =
                            new Vector3(
                                heightMap.GetValueWithZeroInMissing(y, x + 1) -
                                heightMap.GetValueWithZeroInMissing(y, x - 1),
                                heightMap.GetValueWithZeroInMissing(y + 1, x) -
                                heightMap.GetValueWithZeroInMissing(y - 1, x), 2);
                        var o_normal2 = _o_notmal.normalized;

                        Vector3 up = new Vector3(0, 1, 0);
                        float cosa = Vector3.Dot(o_normal2, up);
                        float o_sinAlpha = Mathf.Sin(Mathf.Acos(cosa));


                        // todo kontrolne zwiększenie wody
                        var capacity = kc_SedimentCapacityConstant * sinAlpha *
                                       velocityMap.GetValue(aPoint).magnitude; //todo set minimum alpha

                        var suspendedSediment = sedimentMap.GetValue(aPoint);

                        if (capacity > suspendedSediment)
                        {
                            var sedimentChangeAmount = (float) (ks_DissolvingConstant * (capacity - suspendedSediment));
                            heightMap.AddValue(aPoint, -sedimentChangeAmount);
                            sedimentMap_1.AddValue(aPoint, sedimentChangeAmount);
                        }
                        else
                        {
                            var sedimentChangeAmount = (float) (kd_DepositionConstant * (suspendedSediment - capacity));
                            heightMap.AddValue(aPoint,
                                sedimentChangeAmount); // sedimentChangeAmount jest ujemna, więc dodajemy teren
                            sedimentMap_1.AddValue(aPoint, -sedimentChangeAmount);
                        }
                    }
                }

                // sediment transportation
                for (int y = 0; y < heightMap.Height; y++)
                {
                    for (int x = 0; x < heightMap.Width; x++)
                    {
                        var aPoint = new IntVector2(x, y);
                        if (x == 10 && y == 10)
                        {
                            int yyy = 2;
                        }
                        var velocity = velocityMap.GetValue(aPoint);
                        sedimentMap.SetValue(aPoint,
                            sedimentMap_1.GetValueWithIndexClamped(aPoint.ToFloatVec() - velocity * deltaT));
                    }
                }

                //evaporation
                for (int y = 0; y < heightMap.Height; y++)
                {
                    for (int x = 0; x < heightMap.Width; x++)
                    {
                        var aPoint = new IntVector2(x, y);
                        if (x == 10 && y == 10)
                        {
                            int yyy = 2;
                        }
                        waterMap.SetValue(aPoint, waterMap_2.GetValue(aPoint) * (1 - ke_EvaporationConstant * deltaT));
                    }
                }
            }

            //for (int y = 0; y < heightMap.Height; y++)
            //{
            //    for (int x = 0; x < heightMap.Width; x++)
            //    {
            //        var aPoint = new IntVector2(x,y);
            //        heightMap.AddValue(aPoint, sedimentMap.GetValue(aPoint));
            //    }
            //}
        }

        public TerrainErosionDebugOutput ErodeWithDebug(SimpleHeightArray heightMap,
            MeiHydraulicEroderConfiguration configuration)
        {
            var debugOutput = new TerrainErosionDebugOutput();
            var sb = new StringBuilder();

            int stepCount = configuration.StepCount;
            float deltaT = configuration.DeltaT;
            float constantWaterAdding = configuration.ConstantWaterAdding;
            float A_pipeCrossSection = configuration.A_PipeCrossSection;
            float l_pipeLength = configuration.L_PipeLength;
            float g_GravityAcceleration = configuration.GravityAcceleration;
            float ks_DissolvingConstant = configuration.DissolvingConstant;
            float kd_DepositionConstant = configuration.DepositionConstant;
            float ke_EvaporationConstant = configuration.EvaporationConstant;
            float kc_SedimentCapacityConstant = configuration.SedimentCapacityConstant;
            Vector2 gridSize = configuration.GridSize;

            float lX = gridSize.x;
            float lY = gridSize.y;

            var waterMap = new SimpleHeightArray(heightMap.Width, heightMap.Height);
            var waterMap_1 = new SimpleHeightArray(heightMap.Width, heightMap.Height);
            var waterMap_2 = new SimpleHeightArray(heightMap.Width, heightMap.Height);

            var fluxMap = new Vector4ArrayTODO(heightMap.Width, heightMap.Height);
            var velocityMap = new VectorArrayTODO(heightMap.Width, heightMap.Height);
            var sedimentMap = new SimpleHeightArray(heightMap.Width, heightMap.Height);
            var sedimentMap_1 = new SimpleHeightArray(heightMap.Width, heightMap.Height);

            for (int i = 0; i < stepCount; i++)
            {
                if (i >= 0)
                {
                    // water increment
                    for (int y = 0; y < heightMap.Height; y++)
                    {
                        for (int x = 0; x < heightMap.Width; x++)
                        {
                            var aPoint = new IntVector2(x, y);
                            var oldValue = waterMap.GetValue(aPoint);
                            var newAmount = oldValue + deltaT * constantWaterAdding;
                            waterMap_1.SetValue(aPoint, newAmount);
                            if (x == 10 && y == 10)
                            {
                                var settings = DebugGetMapValues(x, y, waterMap, waterMap_1, waterMap_2, fluxMap,
                                    velocityMap, sedimentMap, sedimentMap_1);
                                int yyy = 2;
                            }
                        }
                    }
                }
                else
                {
                    MyArrayUtils.Copy(waterMap.Array, waterMap_1.Array);
                }

                debugOutput.AddArray("waterMap", SimpleHeightArray.ToHeightmap(waterMap_1), i);
                debugOutput.AddArray("heightMap", SimpleHeightArray.ToHeightmap(heightMap), i);
                debugOutput.AddArray("sedimentMap", SimpleHeightArray.ToHeightmap(sedimentMap), i);

                var speedMap = new float[sedimentMap.Width, sedimentMap.Height];
                for (int x = 0; x < sedimentMap.Width; x++)
                {
                    for (int y = 0; y < sedimentMap.Height; y++)
                    {
                        speedMap[x, y] = velocityMap.GetValue(x, y).magnitude;
                    }
                }
                debugOutput.AddArray("speedMap", new HeightmapArray(speedMap), i);

                // flow simulation
                int onesCount = 0;
                for (int y = 0; y < heightMap.Height; y++)
                {
                    for (int x = 0; x < heightMap.Width; x++)
                    {
                        var aPoint = new IntVector2(x, y);
                        if (x == 10 && y == 10)
                        {
                            var settings = DebugGetMapValues(x, y, waterMap, waterMap_1, waterMap_2, fluxMap,
                                velocityMap, sedimentMap, sedimentMap_1);
                            Debug.Log("T44. W0: " + waterMap.GetValue(aPoint) + " w1: " + waterMap_1.GetValue(aPoint));
                            int yyy = 2;
                        }

                        Vector4 newFlux = Vector4.zero;
                        var neighbours = GetNeighbours(heightMap, aPoint);
                        foreach (var neighbour in neighbours)
                        {
                            var d_difference = (heightMap.GetValue(aPoint) + waterMap_1.GetValue(aPoint)) -
                                               (heightMap.GetValue(neighbour.Position) +
                                                waterMap_1.GetValue(neighbour.Position));

                            var d_flux = fluxMap.GetValue(aPoint)[(int) neighbour.DirectionIndex];
                            var d_new_flux = Mathf.Max(0,
                                d_flux + deltaT * A_pipeCrossSection * (g_GravityAcceleration * d_difference) /
                                l_pipeLength);
                            Preconditions.Assert(!float.IsNaN(d_new_flux), "");
                            newFlux[(int) neighbour.DirectionIndex] = d_new_flux;
                        }

                        var aD1 = waterMap_1.GetValue(aPoint);
                        var fluxSum = VectorUtils.SumMembers(newFlux);
                        float K_factor = 0;
                        if (fluxSum != 0)
                        {
                            K_factor = Mathf.Min(1,
                                aD1 * (lX * lY) / (fluxSum * deltaT));
                        }
                        if (K_factor > 0.99999)
                        {
                            onesCount++;
                        }
                        fluxMap.SetValue(aPoint, K_factor * newFlux);
                    }
                }
                Debug.Log("t66: onesCount is " + onesCount);

                // velocity calculation
                for (int y = 0; y < heightMap.Height; y++)
                {
                    for (int x = 0; x < heightMap.Width; x++)
                    {
                        var aPoint = new IntVector2(x, y);
                        if (x == 10 && y == 10)
                        {
                            var settings = DebugGetMapValues(x, y, waterMap, waterMap_1, waterMap_2, fluxMap,
                                velocityMap, sedimentMap, sedimentMap_1);
                            int yyy = 2;
                        }

                        var neighbours = GetNeighbours(heightMap, aPoint);

                        var outFlow = VectorUtils.SumMembers(fluxMap.GetValue(aPoint));
                        var inFlow =
                            neighbours.Select(c => fluxMap.GetValue(c.Position)[(int) c.DirectionIndex.GetOpposite()])
                                .Sum();

                        var aChangeOfWater = deltaT * (inFlow - outFlow);
                        waterMap_2.SetValue(aPoint,
                            Mathf.Max(0, waterMap_1.GetValue(aPoint) + aChangeOfWater / (lX * lY)));

                        var horizontalFlux =
                            GetFlux(fluxMap, neighbours, MaiNeighbourDirection.LEFT, MaiNeighbourDirection.RIGHT) -
                            fluxMap.GetValue(aPoint)[(int) MaiNeighbourDirection.LEFT] +
                            fluxMap.GetValue(aPoint)[(int) MaiNeighbourDirection.RIGHT] -
                            GetFlux(fluxMap, neighbours, MaiNeighbourDirection.RIGHT, MaiNeighbourDirection.LEFT);

                        var deltaWx = horizontalFlux / 2;

                        var verticalFlux =
                            GetFlux(fluxMap, neighbours, MaiNeighbourDirection.DOWN, MaiNeighbourDirection.UP) -
                            fluxMap.GetValue(aPoint)[(int) MaiNeighbourDirection.DOWN] +
                            fluxMap.GetValue(aPoint)[(int) MaiNeighbourDirection.UP] -
                            GetFlux(fluxMap, neighbours, MaiNeighbourDirection.UP, MaiNeighbourDirection.DOWN);

                        var deltaWy = verticalFlux / 2;

                        var avgHeight = (waterMap_1.GetValue(aPoint) + waterMap_2.GetValue(aPoint)) / 2;

                        var newVelocity = new Vector2(
                            (deltaWx / (lX * avgHeight)),
                            (deltaWy / (lY * avgHeight))
                        );
                        if (float.IsNaN(newVelocity.magnitude))
                        {
                            newVelocity = Vector2.zero;
                        }
                        velocityMap.SetValue(aPoint, newVelocity); //todo ograniczenie CFL
                    }
                }

                var erodedAmountMap = new float[sedimentMap.Width, sedimentMap.Height];
                var depositionAmountMap = new float[sedimentMap.Width, sedimentMap.Height];

                // sediment calc
                for (int y = 0; y < heightMap.Height; y++)
                {
                    for (int x = 0; x < heightMap.Width; x++)
                    {
                        var aPoint = new IntVector2(x, y);
                        if (x == 10 && y == 10)
                        {
                            var settings = DebugGetMapValues(x, y, waterMap, waterMap_1, waterMap_2, fluxMap,
                                velocityMap, sedimentMap, sedimentMap_1);
                            int yyy = 2;
                        }
                        var aHeight = heightMap.GetValue(aPoint);

                        Vector3 upDir = new Vector3(0, 0, lY);
                        if (y != heightMap.Height - 1)
                        {
                            var upHeight = heightMap.GetValue(aPoint + new IntVector2(0, 1));
                            upDir[1] = upHeight - aHeight;
                        }

                        Vector3 rightDir = new Vector3(lX, 0, 0);
                        if (x != heightMap.Width - 1)
                        {
                            var rightHeight = heightMap.GetValue(aPoint + new IntVector2(1, 0));
                            rightDir[1] = rightHeight - aHeight;
                        }

                        var aNormal = Vector3.Cross(upDir, rightDir);

                        var baseNormal = new Vector3(0, 1, 0);

                        var cosAlpha = Vector3.Dot(baseNormal, aNormal) / (baseNormal.magnitude * aNormal.magnitude);
                        var sinAlpha = Math.Sqrt(1 - cosAlpha * cosAlpha);

                        //////// OTHER CALCULATING

                        var _o_notmal =
                            new Vector3(
                                heightMap.GetValueWithZeroInMissing(y, x + 1) -
                                heightMap.GetValueWithZeroInMissing(y, x - 1),
                                heightMap.GetValueWithZeroInMissing(y + 1, x) -
                                heightMap.GetValueWithZeroInMissing(y - 1, x), 2);
                        var o_normal2 = _o_notmal.normalized;

                        Vector3 up = new Vector3(0, 1, 0);
                        float cosa = Vector3.Dot(o_normal2, up);
                        float o_sinAlpha = Mathf.Sin(Mathf.Acos(cosa));


                        // todo kontrolne zwiększenie wody
                        var capacity = kc_SedimentCapacityConstant * Mathf.Max(o_sinAlpha, 0.1f) *
                                       velocityMap.GetValue(aPoint).magnitude; //todo set minimum alpha

                        var suspendedSediment = sedimentMap.GetValue(aPoint);

                        erodedAmountMap[x, y] = 0;
                        depositionAmountMap[x, y] = 0;
                        if (capacity > suspendedSediment)
                        {
                            var sedimentChangeAmount = (float) (ks_DissolvingConstant * (capacity - suspendedSediment));
                            erodedAmountMap[x, y] = sedimentChangeAmount;
                            heightMap.AddValue(aPoint, -sedimentChangeAmount);
                            sedimentMap_1.AddValue(aPoint, sedimentChangeAmount);
                        }
                        else
                        {
                            var sedimentChangeAmount = (float) (kd_DepositionConstant * (suspendedSediment - capacity));
                            depositionAmountMap[x, y] = sedimentChangeAmount;
                            heightMap.AddValue(aPoint,
                                sedimentChangeAmount); // sedimentChangeAmount jest ujemna, więc dodajemy teren
                            sedimentMap_1.AddValue(aPoint, -sedimentChangeAmount);
                        }
                    }
                }

                debugOutput.AddArray("erodedAmount", new HeightmapArray(erodedAmountMap), i);
                debugOutput.AddArray("depositedAmount", new HeightmapArray(depositionAmountMap), i);

                // sediment transportation
                for (int y = 0; y < heightMap.Height; y++)
                {
                    for (int x = 0; x < heightMap.Width; x++)
                    {
                        var aPoint = new IntVector2(x, y);
                        if (x == 10 && y == 10)
                        {
                            var settings = DebugGetMapValues(x, y, waterMap, waterMap_1, waterMap_2, fluxMap,
                                velocityMap, sedimentMap, sedimentMap_1);
                            int yyy = 2;
                        }
                        var velocity = velocityMap.GetValue(aPoint);
                        sedimentMap.SetValue(aPoint,
                            sedimentMap_1.GetValueWithIndexClamped(aPoint.ToFloatVec() - velocity * deltaT));
                    }
                }

                //evaporation
                for (int y = 0; y < heightMap.Height; y++)
                {
                    for (int x = 0; x < heightMap.Width; x++)
                    {
                        var aPoint = new IntVector2(x, y);
                        if (x == 10 && y == 10)
                        {
                            var settings = DebugGetMapValues(x, y, waterMap, waterMap_1, waterMap_2, fluxMap,
                                velocityMap, sedimentMap, sedimentMap_1);
                            int yyy = 2;
                        }
                        waterMap.SetValue(aPoint, waterMap_2.GetValue(aPoint) * (1 - ke_EvaporationConstant * deltaT));
                    }
                }
            }

            //for (int y = 0; y < heightMap.Height; y++)
            //{
            //    for (int x = 0; x < heightMap.Width; x++)
            //    {
            //        var aPoint = new IntVector2(x,y);
            //        heightMap.AddValue(aPoint, sedimentMap.GetValue(aPoint));
            //    }
            //}
            //Debug.Log("t23: " + sb.ToString());
            return debugOutput;
        }

        private DebMapValues DebugGetMapValues(int x, int y, SimpleHeightArray waterMap, SimpleHeightArray waterMap1,
            SimpleHeightArray waterMap2, MySimpleArray<Vector4> fluxMap, MySimpleArray<Vector2> velocityMap,
            SimpleHeightArray sedimentMap, SimpleHeightArray sedimentMap1)
        {
            return new DebMapValues()
            {
                waterMapVal = waterMap.GetValue(x, y),
                waterMap1Val = waterMap1.GetValue(x, y),
                waterMap2Val = waterMap2.GetValue(x, y),
                fluxMapVal = fluxMap.GetValue(x, y),
                velocityMapVal = velocityMap.GetValue(x, y),
                sedimentMapVal = sedimentMap.GetValue(x, y),
                sedimentMap1Val = sedimentMap1.GetValue(x, y)
            };
        }

        public class DebMapValues
        {
            public float waterMapVal { get; set; }
            public float waterMap1Val { get; set; }
            public float waterMap2Val { get; set; }
            public Vector4 fluxMapVal { get; set; }
            public float sedimentMapVal { get; set; }
            public Vector2 velocityMapVal { get; set; }
            public float sedimentMap1Val { get; set; }
        }


        private float GetFlux(MySimpleArray<Vector4> fluxMap, List<MaiNeighbour> neighbours,
            MaiNeighbourDirection neighbourPosition, MaiNeighbourDirection fluxDirection)
        {
            var requestedElement = neighbours.Where(c => c.DirectionIndex == neighbourPosition).ToList();
            if (!requestedElement.Any())
            {
                return 0;
            }
            else
            {
                return fluxMap.GetValue(requestedElement.First().Position)[(int) fluxDirection];
            }
        }

        private List<MaiNeighbour> GetNeighbours(SimpleHeightArray heightArray, IntVector2 center)
        {
            var heightArrayBoundaries = heightArray.Boundaries;
            var neighbours = new List<MaiNeighbour>()
            {
                new MaiNeighbour()
                {
                    Position = center + new IntVector2(-1, 0),
                    DirectionIndex = MaiNeighbourDirection.LEFT
                },
                new MaiNeighbour()
                {
                    Position = center + new IntVector2(1, 0),
                    DirectionIndex = MaiNeighbourDirection.RIGHT
                },
                new MaiNeighbour()
                {
                    Position = center + new IntVector2(0, 1),
                    DirectionIndex = MaiNeighbourDirection.UP
                },
                new MaiNeighbour()
                {
                    Position = center + new IntVector2(0, -1),
                    DirectionIndex = MaiNeighbourDirection.DOWN
                },
            }.Where(c => { return heightArrayBoundaries.AreValidIndexes(c.Position); }).ToList();
            return neighbours;
        }
    }

    public class MeiHydraulicEroderConfiguration
    {
        public int StepCount = 10;
        public float DeltaT = 0.02f;
        public float ConstantWaterAdding = 0.5f;
        public float A_PipeCrossSection = 0.2f;
        public float L_PipeLength = 0.1f;
        public float GravityAcceleration = 10;
        public float DissolvingConstant = 0.3f;
        public float DepositionConstant = 0.3f;
        public float EvaporationConstant = 0.5f;
        public float SedimentCapacityConstant = 0.5f;
        public Vector2 GridSize = new Vector2(1, 1);
    }


    public class MaiNeighbour
    {
        public IntVector2 Position;
        public MaiNeighbourDirection DirectionIndex;
    }

    public enum MaiNeighbourDirection
    {
        LEFT = 0,
        RIGHT = 1,
        DOWN = 2,
        UP = 3
    }

    public static class MaiNeighbourDirectionExtensions
    {
        public static MaiNeighbourDirection GetOpposite(this MaiNeighbourDirection direction)
        {
            switch (direction)
            {
                case MaiNeighbourDirection.DOWN:
                    return MaiNeighbourDirection.UP;

                case MaiNeighbourDirection.UP:
                    return MaiNeighbourDirection.DOWN;

                case MaiNeighbourDirection.LEFT:
                    return MaiNeighbourDirection.RIGHT;

                case MaiNeighbourDirection.RIGHT:
                    return MaiNeighbourDirection.LEFT;
            }
            Preconditions.Fail("Unnown direction: " + direction);
            return 0;
        }

        public static bool IsHorizontal(this MaiNeighbourDirection direction)
        {
            switch (direction)
            {
                case MaiNeighbourDirection.DOWN:
                    return false;

                case MaiNeighbourDirection.UP:
                    return false;

                case MaiNeighbourDirection.LEFT:
                    return true;

                case MaiNeighbourDirection.RIGHT:
                    return true;
            }
            Preconditions.Fail("Unnown direction: " + direction);
            return false;
        }

        public static bool IsVertical(this MaiNeighbourDirection direction)
        {
            return !IsHorizontal(direction);
        }
    }

    public class TerrainErosionDebugOutput
    {
        private Dictionary<string, List<HeightmapArray>> _arraysDict = new Dictionary<string, List<HeightmapArray>>();

        public void AddArray(string name, HeightmapArray array, int stepNo)
        {
            if (!_arraysDict.ContainsKey(name))
            {
                _arraysDict[name] = new List<HeightmapArray>();
            }
            var clone = MyArrayUtils.DeepClone(array.HeightmapAsArray);
            //for (int x = 10; x < 10 + stepNo * 5; x++)
            //{
            //    for (int y = 10; y < 10 + stepNo * 5; y++)
            //    {
            //        clone[x, y] = array.HeightmapAsArray[10, 10];
            //    }
            //}

            var copyArray = new HeightmapArray(clone);
            _arraysDict[name].Add(copyArray);
        }

        public void NormalizeInGroups()
        {
            foreach (var list in _arraysDict.Values)
            {
                var extremes = list.Select(c => MyArrayUtils.CalculateExtremes(c.HeightmapAsArray)).ToList();
                var extent = new ArrayExtremes(
                    extremes.Min(c => c.Min),
                    extremes.Max(c => c.Max)
                );
                list.ForEach(c => MyArrayUtils.Normalize(c.HeightmapAsArray, extent));
                //foreach (var arr in list)
                //{
                //    //Debug.Log("T67: "+arr.HeightmapAsArray[10, 10]);
                //    //var ext = MyArrayUtils.CalculateExtremes(arr.HeightmapAsArray);
                //    //Debug.Log("T88: min "+ext.Min+" max"+ext.Max);
                //    MyArrayUtils.Normalize(arr.HeightmapAsArray);
                //    //Debug.Log("T000_67: "+arr.HeightmapAsArray[10, 10]);
                //    //var ext2 = MyArrayUtils.CalculateExtremes(arr.HeightmapAsArray);
                //    //Debug.Log("T000_88: min "+ext2.Min+" max"+ext2.Max);
                //    //Debug.Log("___________________________");
                //}
                //list.ForEach(c => MyArrayUtils.Normalize(c.HeightmapAsArray));
            }
        }

        public Dictionary<string, List<HeightmapArray>> ArraysDict => _arraysDict;

        public int OneArrayListLength => _arraysDict.Values.Select(c => c.Count).First();
        public int OneArrayListCount => _arraysDict.Values.Count;
    }


    public class VectorArrayTODO : MySimpleArray<Vector2>
    {
        public VectorArrayTODO(Vector2[,] array) : base(array)
        {
        }

        public VectorArrayTODO(int x, int y) : base(x, y)
        {
        }

        public override void SetValue(int x, int y, Vector2 value)
        {
            Preconditions.Assert(!float.IsNaN(value.magnitude), "");
            base.SetValue(x, y, value);
        }
    }

    public class Vector4ArrayTODO : MySimpleArray<Vector4>
    {
        public Vector4ArrayTODO(Vector4[,] array) : base(array)
        {
        }

        public Vector4ArrayTODO(int x, int y) : base(x, y)
        {
        }

        public override void SetValue(int x, int y, Vector4 value)
        {
            Preconditions.Assert(!float.IsNaN(value.magnitude), "");
            base.SetValue(x, y, value);
        }
    }
}