using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.ComputeShaders;
using Assets.Heightmaps.Ring1;
using Assets.Heightmaps.Ring1.TerrainDescription;
using Assets.Heightmaps.Ring1.TerrainDescription.FeatureGenerating;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.Heightmaps.TextureUtils;
using Assets.MeshGeneration;
using Assets.Roads.Pathfinding;
using Assets.Utils;
using Assets.Utils.Services;
using Assets.Utils.Textures;
using NetTopologySuite.IO;
using UnityEngine;

namespace Assets.Heightmaps.Welding
{
    public class WeldTextureManagerDebugObject : MonoBehaviour
    {
        public ComputeShaderContainerGameObject ComputeShaderContainer;

        public void Start()
        {
            RootTest();
        }

        private void RootTest()
        {
            //RunTest(
            //    new DebugOneSideSpecification()
            //    {
            //        QueryArea = new UnityCoordsPositions2D(0, 0, 5760, 5760),
            //        ConstantCoord = 240,
            //        FullLodSidePixelsRange = new IntVector2(0, 241),
            //        LodLevel = 0,
            //        MeshResolution = new IntVector2(240+1, 240+1),
            //        SideType = WeldSideType.Right,
            //        TerrainWeldUvs = new TerrainWeldUvs()
            //        {
            //            RightUv = new Vector4(0, 0, 240 / 1024f, 0)
            //        }
            //    },
            //new DebugOneSideSpecification()
            //{
            //    QueryArea = new UnityCoordsPositions2D(5760, 0, 5760, 5760),
            //    ConstantCoord = 0,
            //    FullLodSidePixelsRange = new IntVector2(0, 241),
            //    LodLevel = 0,
            //    MeshResolution = new IntVector2(240+1, 240+1),
            //    SideType = WeldSideType.Left,
            //    TerrainWeldUvs = new TerrainWeldUvs()
            //    {
            //        LeftUv = new Vector4(0, 0, 240 / 1024f, 0)
            //    }
            //});

            //RunTest(
            //    new DebugOneSideSpecification()
            //    {
            //        QueryArea = new UnityCoordsPositions2D(0, 0, 5760, 5760),
            //        ConstantCoord = 240,
            //        FullLodSidePixelsRange = new IntVector2(0, (240 / 8) + 1),
            //        LodLevel = 3,
            //        MeshResolution = new IntVector2((240 / 8) + 1, (240 / 8) + 1),
            //        SideType = WeldSideType.Right,
            //        TerrainWeldUvs = new TerrainWeldUvs()
            //        {
            //            RightUv = new Vector4(0, 0, 240 / 1024f, 0)
            //        }
            //    },
            //new DebugOneSideSpecification()
            //{
            //    QueryArea = new UnityCoordsPositions2D(5760, 0, 5760, 5760),
            //    ConstantCoord = 0,
            //    FullLodSidePixelsRange = new IntVector2(0, 241),
            //    LodLevel = 0,
            //    MeshResolution = new IntVector2(240 + 1, 240 + 1),
            //    SideType = WeldSideType.Left,
            //    TerrainWeldUvs = new TerrainWeldUvs()
            //    {
            //        LeftUv = new Vector4(0, 0, 240 / 1024f, 0)
            //    }
            //});


            //RunTest(
            //    new DebugOneSideSpecification()
            //    {
            //        QueryArea = new UnityCoordsPositions2D(0, 0, 5760, 5760),
            //        ConstantCoord = 240,
            //        FullLodSidePixelsRange = new IntVector2(0, (240 / 1) + 1),
            //        LodLevel = 0,
            //        MeshResolution = new IntVector2((240 / 1)+1, (240 / 1)+1),
            //        SideType = WeldSideType.Right,
            //        TerrainWeldUvs = new TerrainWeldUvs()
            //        {
            //            RightUv = new Vector4(0, 0, 240 / 1024f, 0)
            //        }
            //    },
            //new DebugOneSideSpecification()
            //{
            //    QueryArea = new UnityCoordsPositions2D(5760, 0, 5760 / 2f, 5760 / 2f),
            //    ConstantCoord = 0,
            //    FullLodSidePixelsRange = new IntVector2(0, 121),
            //    LodLevel = 0,
            //    MeshResolution = new IntVector2(121, 121),
            //    SideType = WeldSideType.Left,
            //    TerrainWeldUvs = new TerrainWeldUvs()
            //    {
            //        LeftUv = new Vector4(0, 0, 120 / 1024f, 0)
            //    }
            //});

            //RunTest(
            //    new DebugOneSideSpecification()
            //    {
            //        QueryArea = new UnityCoordsPositions2D(0, 0, 5760, 5760),
            //        ConstantCoord = 240,
            //        FullLodSidePixelsRange = new IntVector2(0, (240 / 1) + 1),
            //        LodLevel = 0,
            //        MeshResolution = new IntVector2((240 / 1)+1, (240 / 1)+1),
            //        SideType = WeldSideType.Right,
            //        TerrainWeldUvs = new TerrainWeldUvs()
            //        {
            //            RightUv = new Vector4(0, 0, 240 / 1024f, 0)
            //        }
            //    },
            //new DebugOneSideSpecification()
            //{
            //    QueryArea = new UnityCoordsPositions2D(5760, 0, 5760 / 2f, 5760 / 2f),
            //    ConstantCoord = 0,
            //    FullLodSidePixelsRange = new IntVector2(0, 121),
            //    LodLevel = 0,
            //    MeshResolution = new IntVector2(121, 121),
            //    SideType = WeldSideType.Left,
            //    TerrainWeldUvs = new TerrainWeldUvs()
            //    {
            //        LeftUv = new Vector4(0, 0, 120 / 1024f, 0)
            //    }
            //});

            RunTest2(new List<Debug2TerrainSpecification>()
                {
                    new Debug2TerrainSpecification()
                    {
                        QueryArea = new MyRectangle(0, 0, 5760, 5760),
                        LodLevel = 0,
                        MeshResolution = new IntVector2(240 + 1, 240 + 1),
                        TerrainWeldUvs = new TerrainWeldUvs()
                        {
                            RightUv = new Vector4(0, 0, 240 / 1024f, 0),
                            TopUv = new Vector4(1 / 1024f, 256 / 1024f, (256 + 240) / 1024f, 0),
                        }
                    },
                    new Debug2TerrainSpecification()
                    {
                        QueryArea = new MyRectangle(5760, 0, 5760 / 2, 5760 / 2),
                        LodLevel = 2,
                        MeshResolution = new IntVector2(30 + 1, 30 + 1),
                        TerrainWeldUvs = new TerrainWeldUvs()
                        {
                            LeftUv = new Vector4(0, 0, 120 / 1024f, 0)
                        }
                    },
                    new Debug2TerrainSpecification()
                    {
                        QueryArea = new MyRectangle(5760, 5760 / 2, 5760 / 2, 5760 / 2),
                        LodLevel = 0,
                        MeshResolution = new IntVector2(120 + 1, 120 + 1),
                        TerrainWeldUvs = new TerrainWeldUvs()
                        {
                            LeftUv = new Vector4(0, 120 / 1024f, 240 / 1024f, 0),
                            TopUv = new Vector4(3 / 1024f, (0) / 1024f, (120) / 1024f, 0),
                        }
                    },
                    new Debug2TerrainSpecification() // 3 - big up
                    {
                        QueryArea = new MyRectangle(0, 5760, 5760, 5760),
                        LodLevel = 1,
                        MeshResolution = new IntVector2(120 + 1, 120 + 1),
                        TerrainWeldUvs = new TerrainWeldUvs()
                        {
                            BottomUv = new Vector4(1 / 1024f, 256 / 1024f, (256 + 240) / 1024f, 0),
                            RightUv = new Vector4(1 / 1024f, 0 / 1024f, (240) / 1024f, 0),
                        }
                    },
                    new Debug2TerrainSpecification() // 4 - big up-right
                    {
                        QueryArea = new MyRectangle(5760, 5760, 5760, 5760),
                        LodLevel = 3,
                        MeshResolution = new IntVector2(30 + 1, 30 + 1),
                        TerrainWeldUvs = new TerrainWeldUvs()
                        {
                            LeftUv = new Vector4(1 / 1024f, 0 / 1024f, (240) / 1024f, 0),
                            BottomUv = new Vector4(3 / 1024f, (0) / 1024f, (240) / 1024f, 0),
                        }
                    },
                }, new List<Debug2WeldSpecification>()
                {
                    new Debug2WeldSpecification()
                    {
                        ColumnIndex = 0,
                        WeldRange = new IntVector2(0, 120),
                        Index1 = 0,

                        Side1ConstantCoord = 240,
                        Side1FullLodSidePixelsRange = new IntVector2(0, 121),
                        Side1SideType = WeldSideType.Right,
                        Side1SamplingDistance = 4,

                        Index2 = 1,
                        Side2ConstantCoord = 0,
                        Side2FullLodSidePixelsRange = new IntVector2(0, 31),
                        Side2SideType = WeldSideType.Left,
                        Side2SamplingDistance = 4,
                    },
                    new Debug2WeldSpecification()
                    {
                        ColumnIndex = 0,
                        WeldRange = new IntVector2(120, 240),

                        Index1 = 0,
                        Side1ConstantCoord = 240,
                        Side1FullLodSidePixelsRange = new IntVector2(120, 241),
                        Side1SideType = WeldSideType.Right,
                        Side1SamplingDistance = 1,

                        Index2 = 2,
                        Side2ConstantCoord = 0,
                        Side2FullLodSidePixelsRange = new IntVector2(120, 241),
                        Side2SideType = WeldSideType.Left,
                        Side2SamplingDistance = 1
                    },
                    new Debug2WeldSpecification()
                    {
                        ColumnIndex = 1,
                        WeldRange = new IntVector2(256, 256 + 240),

                        Index1 = 0,
                        Side1ConstantCoord = 240,
                        Side1FullLodSidePixelsRange = new IntVector2(0, 241),
                        Side1SideType = WeldSideType.Top,
                        Side1SamplingDistance = 2,

                        Index2 = 3,
                        Side2ConstantCoord = 0,
                        Side2FullLodSidePixelsRange = new IntVector2(0, 121),
                        Side2SideType = WeldSideType.Bottom,
                        Side2SamplingDistance = 2
                    },
                    new Debug2WeldSpecification()
                    {
                        ColumnIndex = 1,
                        WeldRange = new IntVector2(0, 240),

                        Index1 = 3,
                        Side1ConstantCoord = 120,
                        Side1FullLodSidePixelsRange = new IntVector2(0, 121),
                        Side1SideType = WeldSideType.Right,
                        Side1SamplingDistance = 8,

                        Index2 = 4,
                        Side2ConstantCoord = 0,
                        Side2FullLodSidePixelsRange = new IntVector2(0, 31),
                        Side2SideType = WeldSideType.Left,
                        Side2SamplingDistance = 8
                    },
                    new Debug2WeldSpecification()
                    {
                        ColumnIndex = 3,
                        WeldRange = new IntVector2(0, 120),

                        Index1 = 2,
                        Side1ConstantCoord = 120,
                        Side1FullLodSidePixelsRange = new IntVector2(0, 121),
                        Side1SideType = WeldSideType.Top,
                        Side1SamplingDistance = 8,

                        Index2 = 4,
                        Side2ConstantCoord = 0,
                        Side2FullLodSidePixelsRange = new IntVector2(0, 16),
                        Side2SideType = WeldSideType.Bottom,
                        Side2SamplingDistance = 8,
                    }
                }
            );
        }

        private void RunTest(DebugOneSideSpecification side1Specification, DebugOneSideSpecification side2Specification)
        {
            var dtt = new DebugTerrainTester(ComputeShaderContainer);
            dtt.Start(false, true);

            var ter1 = side1Specification.HeightTextureWithUvBase;
            if (ter1 == null)
            {
                var realTerrain = dtt.TerrainShapeDbProxy.Query(new TerrainDescriptionQuery()
                {
                    QueryArea = side1Specification.QueryArea,
                    RequestedElementDetails = new List<TerrainDescriptionQueryElementDetail>()
                    {
                        new TerrainDescriptionQueryElementDetail()
                        {
                            Type = TerrainDescriptionElementTypeEnum.HEIGHT_ARRAY,
                            Resolution = TerrainCardinalResolution.MIN_RESOLUTION
                        }
                    }
                }).Result.GetElementOfType(TerrainDescriptionElementTypeEnum.HEIGHT_ARRAY);
                ter1 = new HeightTextureWithUvBase()
                {
                    Texture = realTerrain.TokenizedElement.DetailElement.Texture,
                    UvBase = realTerrain.UvBase
                };
            }

            var ter2 = side2Specification.HeightTextureWithUvBase;
            if (ter2 == null)
            {
                var realTerrain = dtt.TerrainShapeDbProxy.Query(new TerrainDescriptionQuery()
                {
                    QueryArea = side2Specification.QueryArea,
                    RequestedElementDetails = new List<TerrainDescriptionQueryElementDetail>()
                    {
                        new TerrainDescriptionQueryElementDetail()
                        {
                            Type = TerrainDescriptionElementTypeEnum.HEIGHT_ARRAY,
                            Resolution = TerrainCardinalResolution.MIN_RESOLUTION
                        }
                    }
                }).Result.GetElementOfType(TerrainDescriptionElementTypeEnum.HEIGHT_ARRAY);
                ter2 = new HeightTextureWithUvBase()
                {
                    Texture = realTerrain.TokenizedElement.DetailElement.Texture,
                    UvBase = realTerrain.UvBase
                };
            }

            var weldTexture = new RenderTexture(1024, 1024, 0, RenderTextureFormat.RFloat);
            weldTexture.wrapMode = TextureWrapMode.Clamp;
            weldTexture.enableRandomWrite = true;
            weldTexture.filterMode = FilterMode.Point;
            weldTexture.Create();
            var weldTextureManager = new WeldingExecutor(ComputeShaderContainer,
                new UnityThreadComputeShaderExecutorObject(), weldTexture);


            WeldTextureDrawingOrder weldingOrder = new WeldTextureDrawingOrder()
            {
                WeldOnTextureInfo = new WeldOnTextureInfo()
                {
                    ColumnIndex = 0,
                    WeldRange = new IntVector2(0, 240),
                },
                FirstSideInfo = new WeldTextureDrawingSideInfo()
                {
                    ConstantCoord = side1Specification.ConstantCoord,
                    FullLodSidePixelsRange = side1Specification.FullLodSidePixelsRange,
                    HeightTexture = ter1.Texture,
                    LodLevel = side1Specification.LodLevel,
                    SideType = side1Specification.SideType,
                },
                SecondSideInfo = new WeldTextureDrawingSideInfo()
                {
                    ConstantCoord = side2Specification.ConstantCoord,
                    FullLodSidePixelsRange = side2Specification.FullLodSidePixelsRange,
                    HeightTexture = ter2.Texture,
                    LodLevel = side2Specification.LodLevel,
                    SideType = side2Specification.SideType,
                },
            };
            weldTextureManager.RenderWeld(weldingOrder).Wait(5000);

            CreateDebugObject(new DebugObjectSpecification()
                {
                    HeightTexture = ter1.Texture,
                    HeightmapUv = ter1.UvBase.ToVector4(),
                    HeightmapLodOffset = side1Specification.LodLevel,
                    StartPos = side1Specification.QueryArea.DownLeftPoint,
                    MeshResolution = side1Specification.MeshResolution,
                    LocalScale = new Vector3(5760 * ter1.UvBase.Width, 2300, 5760 * ter1.UvBase.Height)
                },
                weldTexture,
                side1Specification.TerrainWeldUvs
            );

            CreateDebugObject(new DebugObjectSpecification()
                {
                    HeightTexture = ter2.Texture,
                    HeightmapUv = ter2.UvBase.ToVector4(),
                    HeightmapLodOffset = side2Specification.LodLevel,
                    StartPos = side2Specification.QueryArea.DownLeftPoint,
                    MeshResolution = side2Specification.MeshResolution,
                    LocalScale = new Vector3(5760 * ter2.UvBase.Width, 2300, 5760 * ter2.UvBase.Height)
                },
                weldTexture,
                side2Specification.TerrainWeldUvs
            );
        }

        private void RunTest2(List<Debug2TerrainSpecification> terrainSpecifications,
            List<Debug2WeldSpecification> weldSpecifications)
        {
            var dtt = new DebugTerrainTester(ComputeShaderContainer);
            dtt.Start(false, true);

            var terrains = terrainSpecifications.Select(s =>
            {
                var realTerrain = dtt.TerrainShapeDbProxy.Query(new TerrainDescriptionQuery()
                {
                    QueryArea = s.QueryArea,
                    RequestedElementDetails = new List<TerrainDescriptionQueryElementDetail>()
                    {
                        new TerrainDescriptionQueryElementDetail()
                        {
                            Type = TerrainDescriptionElementTypeEnum.HEIGHT_ARRAY,
                            Resolution = TerrainCardinalResolution.MIN_RESOLUTION
                        }
                    }
                }).Result.GetElementOfType(TerrainDescriptionElementTypeEnum.HEIGHT_ARRAY);
                return new HeightTextureWithUvBase()
                {
                    Texture = realTerrain.TokenizedElement.DetailElement.Texture,
                    UvBase = realTerrain.UvBase
                };
            }).ToList();


            var weldTexture = new RenderTexture(1024, 1024, 0, RenderTextureFormat.RFloat);
            weldTexture.wrapMode = TextureWrapMode.Clamp;
            weldTexture.enableRandomWrite = true;
            weldTexture.filterMode = FilterMode.Point;
            weldTexture.Create();
            var weldTextureManager = new WeldingExecutor(ComputeShaderContainer,
                new UnityThreadComputeShaderExecutorObject(), weldTexture);

            foreach (var weldSpecification in weldSpecifications)
            {
                var side1Specification = terrainSpecifications[weldSpecification.Index1];
                var side2Specification = terrainSpecifications[weldSpecification.Index2];

                WeldTextureDrawingOrder weldingOrder = new WeldTextureDrawingOrder()
                {
                    WeldOnTextureInfo = new WeldOnTextureInfo()
                    {
                        ColumnIndex = weldSpecification.ColumnIndex,
                        WeldRange = weldSpecification.WeldRange
                    },
                    FirstSideInfo = new WeldTextureDrawingSideInfo()
                    {
                        ConstantCoord = weldSpecification.Side1ConstantCoord,
                        FullLodSidePixelsRange = weldSpecification.Side1FullLodSidePixelsRange,
                        HeightTexture = terrains[weldSpecification.Index1].Texture,
                        LodLevel = side1Specification.LodLevel,
                        SideType = weldSpecification.Side1SideType,
                        SamplingDistance = weldSpecification.Side1SamplingDistance
                    },
                    SecondSideInfo = new WeldTextureDrawingSideInfo()
                    {
                        ConstantCoord = weldSpecification.Side2ConstantCoord,
                        FullLodSidePixelsRange = weldSpecification.Side2FullLodSidePixelsRange,
                        HeightTexture = terrains[weldSpecification.Index2].Texture,
                        LodLevel = side2Specification.LodLevel,
                        SideType = weldSpecification.Side2SideType,
                        SamplingDistance = weldSpecification.Side2SamplingDistance
                    },
                };
                weldTextureManager.RenderWeld(weldingOrder).Wait(5000);
            }

            for (int i = 0; i < terrainSpecifications.Count; i++)
            {
                var specification = terrainSpecifications[i];
                var terrain = terrains[i];

                CreateDebugObject(new DebugObjectSpecification()
                    {
                        HeightTexture = terrain.Texture,
                        HeightmapUv = terrain.UvBase.ToVector4(),
                        HeightmapLodOffset = specification.LodLevel,
                        StartPos = specification.QueryArea.DownLeftPoint,
                        MeshResolution = specification.MeshResolution,
                        LocalScale = new Vector3(5760 * terrain.UvBase.Width, 2300, 5760 * terrain.UvBase.Height)
                    },
                    weldTexture,
                    specification.TerrainWeldUvs
                );
            }
        }

        private void CreateDebugObject(DebugObjectSpecification specification, Texture weldTexture, TerrainWeldUvs uvs)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Plane);
            go.GetComponent<MeshFilter>().mesh =
                PlaneGenerator.CreateFlatPlaneMesh(specification.MeshResolution.X, specification.MeshResolution.Y);
            Material material = new Material(Shader.Find("Custom/Test/TerrainWelding"));
            material.SetTexture("_HeightmapTex", specification.HeightTexture.Texture);
            material.SetVector("_HeightmapUv", specification.HeightmapUv);
            material.SetFloat("_HeightmapLodOffset", specification.HeightmapLodOffset);
            material.SetTexture("_WeldTexture", weldTexture);

            material.SetVector("_LeftWeldTextureUvRange", uvs.LeftUv);
            material.SetVector("_RightWeldTextureUvRange", uvs.RightUv);
            material.SetVector("_TopWeldTextureUvRange", uvs.TopUv);
            material.SetVector("_BottomWeldTextureUvRange", uvs.BottomUv);

            go.GetComponent<MeshRenderer>().material = material;
            go.transform.localPosition = new Vector3(specification.StartPos.x, 0, specification.StartPos.y);
            go.transform.localScale = specification.LocalScale;
        }


        class DebugObjectSpecification
        {
            public TextureWithSize HeightTexture;
            public Vector2 StartPos;
            public int HeightmapLodOffset;
            public IntVector2 MeshResolution;
            public Vector4 HeightmapUv;
            public Vector3 LocalScale;
        }

        class DebugOneSideSpecification
        {
            public int ConstantCoord;
            public IntVector2 FullLodSidePixelsRange;
            public WeldSideType SideType;
            public MyRectangle QueryArea;
            public int LodLevel;
            public IntVector2 MeshResolution;
            public TerrainWeldUvs TerrainWeldUvs;
            public HeightTextureWithUvBase HeightTextureWithUvBase;
        }

        class HeightTextureWithUvBase
        {
            public TextureWithSize Texture;
            public MyRectangle UvBase;
        }

        class Debug2TerrainSpecification
        {
            public MyRectangle QueryArea;
            public int LodLevel;
            public IntVector2 MeshResolution;
            public TerrainWeldUvs TerrainWeldUvs;
        }

        class Debug2WeldSpecification
        {
            public int ColumnIndex { get; set; }
            public IntVector2 WeldRange { get; set; }

            public int Index1 { get; set; }
            public int Side1ConstantCoord { get; set; }
            public IntVector2 Side1FullLodSidePixelsRange { get; set; }
            public WeldSideType Side1SideType { get; set; }
            public int Side1SamplingDistance;

            public int Index2 { get; set; }
            public int Side2ConstantCoord { get; set; }
            public IntVector2 Side2FullLodSidePixelsRange { get; set; }
            public WeldSideType Side2SideType { get; set; }
            public int Side2SamplingDistance;
        }
    }

    public class TerrainWeldUvs
    {
        public Vector4 LeftUv = new Vector4(-1, -1, -1, -1);
        public Vector4 RightUv = new Vector4(-1, -1, -1, -1);
        public Vector4 TopUv = new Vector4(-1, -1, -1, -1);
        public Vector4 BottomUv = new Vector4(-1, -1, -1, -1);

        public static TerrainWeldUvs CreateFrom(WeldSideType sideType, Vector4 weldUv)
        {
            Vector4 leftUv = new Vector4(-1, -1, -1, -1);
            Vector4 rightUv = new Vector4(-1, -1, -1, -1);
            Vector4 topUv = new Vector4(-1, -1, -1, -1);
            Vector4 bottomUv = new Vector4(-1, -1, -1, -1);
            if (sideType == WeldSideType.Left)
            {
                leftUv = weldUv;
            }
            else if (sideType == WeldSideType.Right)
            {
                rightUv = weldUv;
            }
            else if (sideType == WeldSideType.Top)
            {
                topUv = weldUv;
            }
            else if (sideType == WeldSideType.Bottom)
            {
                bottomUv = weldUv;
            }
            else
            {
                Preconditions.Fail("Unsupported sideType: " + sideType);
            }
            return new TerrainWeldUvs()
            {
                BottomUv = bottomUv,
                RightUv = rightUv,
                TopUv = topUv,
                LeftUv = leftUv
            };
        }

        public void Merge(TerrainWeldUvs weldUv)
        {
            if (weldUv.LeftUv.x > -0.5f)
            {
                LeftUv = weldUv.LeftUv;
            }
            if (weldUv.TopUv.x > -0.5f)
            {
                TopUv = weldUv.TopUv;
            }
            if (weldUv.RightUv.x > -0.5f)
            {
                RightUv = weldUv.RightUv;
            }
            if (weldUv.BottomUv.x > -0.5f)
            {
                BottomUv = weldUv.BottomUv;
            }
        }

        public void SetToMaterial(Material material)
        {
            material.SetVector("_LeftWeldTextureUvRange", LeftUv);
            material.SetVector("_RightWeldTextureUvRange", RightUv);
            material.SetVector("_TopWeldTextureUvRange", TopUv);
            material.SetVector("_BottomWeldTextureUvRange", BottomUv);
        }

        public override string ToString()
        {
            return
                $"{nameof(LeftUv)}: {LeftUv}, {nameof(RightUv)}: {RightUv}, {nameof(TopUv)}: {TopUv}, {nameof(BottomUv)}: {BottomUv}";
        }
    }


    public class WeldTextureTestingUtils
    {
        public static Texture CreateHeightTextureA()
        {
            return BaseCreateHeightTexture((x, y) => (float) (Mathf.Max(x, y) / 240.0));
        }

        public static Texture BaseCreateHeightTexture(Func<int, int, float> heightResolver)
        {
            var traditionalHeightMap = new Texture2D(241, 241, TextureFormat.ARGB32, true);
            for (int x = 0; x < 241; x++)
            {
                for (int y = 0; y < 241; y++)
                {
                    float height = heightResolver(x, y);
                    traditionalHeightMap.SetPixel(x, y, HeightColorTransform.EncodeHeight(height));
                }
            }
            traditionalHeightMap.Apply(true);

            var transformator = new TerrainTextureFormatTransformator(new CommonExecutorUTProxy());
            return transformator.EncodedHeightTextureToPlain(new TextureWithSize()
            {
                Texture = traditionalHeightMap,
                Size = new IntVector2(241, 241)
            });
        }
    }
}