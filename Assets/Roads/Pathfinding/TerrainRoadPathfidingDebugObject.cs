using System.Collections.Generic;
using System.Linq;
using Assets.ComputeShaders;
using Assets.Heightmaps.Ring1;
using Assets.Heightmaps.Ring1.Creator;
using Assets.Heightmaps.Ring1.MeshGeneration;
using Assets.Heightmaps.Ring1.RenderingTex;
using Assets.Heightmaps.Ring1.TerrainDescription;
using Assets.Heightmaps.Ring1.TerrainDescription.Cache;
using Assets.Heightmaps.Ring1.TerrainDescription.FeatureGenerating;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.MeshGeneration;
using Assets.Roads.Pathfinding.AStar;
using Assets.Roads.Pathfinding.TerrainPath;
using Assets.TerrainMat.Stain;
using Assets.Utils;
using Assets.Utils.MT;
using Assets.Utils.Services;
using Assets.Utils.TextureRendering;
using Assets.Utils.Textures;
using Assets.Utils.UTUpdating;
using UnityEngine;

namespace Assets.Roads.Pathfinding
{
    public class TerrainRoadPathfidingDebugObject : MonoBehaviour
    {
        public ComputeShaderContainerGameObject ContainerGameObject;

        public float FloatPointDistanceFactor = 3;
        public float FinalLineDistanceFactor = 3;
        public float MinimalGoalValue = 30;
        public float StepDistanceFactor = 0.00004f;
        public float AzimuthFactor = 0.0001f;
        public float HeightRateFactor = 1000000;
        public float WaySeparationDifferenceFactor = 0.0001f;
        public float LineSeparationActivationLength = 6;

        public float NodeAccomplishedDistance = 4;

        public static float RealLifeSubmapSideLength = 90;
        public static float GrateSideCount = 90;
        public static float OneGrateSideLength = GrateSideCount / RealLifeSubmapSideLength;
        public static float GrateToGlobalPositionMultiplier = 100 / GrateSideCount;

        private TerrainShapeDbProxy _terrainShapeDbProxy;

        public void Start()
        {
            var debugTerrainTester = new DebugTerrainTester(ContainerGameObject);
            debugTerrainTester.Start();

            /// CREATING plate
            //////////
            /// 
            var baseCoord = new IntVector2(0, 0);

            var posVectors = new List<IntVector2>();
            for (int x = 7; x < 9; x++)
            {
                for (int y = 8; y < 10; y++)
                {
                    posVectors.Add(new IntVector2(x, y) + baseCoord);
                }
            }

            CreateTerrainRectangles(posVectors,
                debugTerrainTester.TerrainShapeDbProxy);
            //DebugCreateHeightImage(debugTerrainTester.TerrainShapeDbProxy);

            //CreateTerrainRectangles(new List<IntVector2>()
            //{
            //    new IntVector2(0,0) + baseCoord,
            //    new IntVector2(1,0) + baseCoord,
            //    new IntVector2(0,1) + baseCoord,
            //    new IntVector2(1,1) + baseCoord,
            //}, debugTerrainTester.TerrainShapeDbProxy);

            _terrainShapeDbProxy = debugTerrainTester.TerrainShapeDbProxy;
        }

        private void DebugCreateHeightImage(TerrainShapeDbProxy terrainShapeDbProxy)
        {
            var transformer = new TerrainTextureFormatTransformator(new CommonExecutorUTProxy());
            var collageList = new List<List<Texture2D>>();
            for (int x = 7; x < 9; x++)
            {
                var oneLineList = new List<Texture2D>();
                for (int y = 8; y < 10; y++)
                {
                    var startPos = new Vector2(x * 90f * 8 * 8, y * 90f * 8 * 8);
                    var heightTexture = terrainShapeDbProxy.Query(new TerrainDescriptionQuery()
                        {
                            QueryArea = new MyRectangle(startPos.x, startPos.y, 90 * 8 * 8, 90 * 8 * 8),
                            RequestedElementDetails = new List<TerrainDescriptionQueryElementDetail>()
                            {
                                new TerrainDescriptionQueryElementDetail()
                                {
                                    Type = TerrainDescriptionElementTypeEnum.HEIGHT_ARRAY,
                                    Resolution = TerrainCardinalResolution.MIN_RESOLUTION
                                }
                            }
                        }).Result.GetElementOfType(TerrainDescriptionElementTypeEnum.HEIGHT_ARRAY).TokenizedElement
                        .DetailElement.Texture;
                    var tex2d = transformer.PlainToEncodedHeightTextureAsync(heightTexture).Result;
                    oneLineList.Add(tex2d);
                }
                collageList.Add(oneLineList);
            }

            var stitched = StitchTextures(collageList);
            SavingFileManager.SaveTextureToPngFile($@"C:\inz\cont\collage5.png", stitched);
        }

        private void CreateTerrainRectangles(List<IntVector2> startPositionsNo, TerrainShapeDbProxy terrainShapeDbProxy)
        {
            var mesh = PlaneGenerator.CreateFlatPlaneMesh(241, 241);
            var parentGo = new GameObject("terrainParent");


            foreach (var startPosNo in startPositionsNo)
            {
                var terrainPos = new Vector2(startPosNo.X * 100f, startPosNo.Y * 100);
                var startPos = new Vector2(startPosNo.X * 90f * 8 * 8, startPosNo.Y * 90f * 8 * 8);
                var terrainObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
                terrainObject.GetComponent<MeshFilter>().mesh = mesh;
                terrainObject.transform.localScale = new Vector3(100, 4000 / (8 * 8), 100);
                terrainObject.transform.position = new Vector3(terrainPos.x, -107.5f, terrainPos.y);
                terrainObject.transform.SetParent(parentGo.transform);

                var heightTexture = terrainShapeDbProxy.Query(new TerrainDescriptionQuery()
                    {
                        QueryArea = new MyRectangle(startPos.x, startPos.y, 90 * 8 * 8, 90 * 8 * 8),
                        RequestedElementDetails = new List<TerrainDescriptionQueryElementDetail>()
                        {
                            new TerrainDescriptionQueryElementDetail()
                            {
                                Type = TerrainDescriptionElementTypeEnum.HEIGHT_ARRAY,
                                Resolution = TerrainCardinalResolution.MIN_RESOLUTION
                            }
                        }
                    }).Result.GetElementOfType(TerrainDescriptionElementTypeEnum.HEIGHT_ARRAY).TokenizedElement
                    .DetailElement.Texture.Texture;
                var material = new Material(Shader.Find("Custom/Terrain/TestTerrainDirectPlain"));
                material.SetTexture("_HeightmapTex", heightTexture);
                terrainObject.GetComponent<MeshRenderer>().material = material;
                terrainObject.name = "testTerrain " + startPos;
            }
        }

        private static Texture2D StitchTextures(List<List<Texture2D>> textures)
        {
            var texSize = new IntVector2(textures[0][0].width - 1, textures[0][0].height - 1);
            var tex = new Texture2D(texSize.X * textures.Count, texSize.Y * textures[0].Count, TextureFormat.ARGB32,
                false);

            for (int bx = 0; bx < textures.Count; bx++)
            {
                var oneLine = textures[bx];
                for (int by = 0; by < oneLine.Count; by++)
                {
                    var oneTex = oneLine[by];
                    var offset = new IntVector2(bx * texSize.X, by * texSize.Y);
                    for (int x = 0; x < texSize.X; x++)
                    {
                        for (int y = 0; y < texSize.Y; y++)
                        {
                            tex.SetPixel(x + offset.X, y + offset.Y, oneTex.GetPixel(x, y));
                        }
                    }
                }
            }
            return tex;
        }

        public void ResetPathObjects()
        {
            ///////// NOW FINDING PATH
            var pathfinder = new TerrainPathfinder(
                new TerrainPathfinder.TerrainPathfinderConfiguration()
                {
                    HeightRateFactor = HeightRateFactor,
                    AzimuthFactor = AzimuthFactor,
                    FinalLineDistanceFactor = FinalLineDistanceFactor,
                    MinimalGoalValue = MinimalGoalValue,
                    LineSeparationActivationLength = LineSeparationActivationLength,
                    FloatPointDistanceFactor = FloatPointDistanceFactor,
                    NodeAccomplishedDistance = NodeAccomplishedDistance,
                    StepDistanceFactor = StepDistanceFactor,
                    WaySeparationDifferenceFactor = WaySeparationDifferenceFactor
                },
                OneGrateSideLength,
                _terrainShapeDbProxy,
                DebugActionOnNodeCreation,
                DebugPerSegmentCompleted
            );

            var nodePositions = new List<Vector2>
            {
                //new Vector2(50, 50),
                //new Vector2(100, 50),
                //new Vector2(50, 180),
                //new Vector2(120, 120),
                //new Vector2(120, 180),
                new Vector2(30, 10),
                new Vector2(150, 70),
                new Vector2(50, 180),
                new Vector2(10, 120),
                new Vector2(200, 10),
            };

            pathfinder.GeneratePath(nodePositions);
        }

        private static void DebugActionOnNodeCreation(TerrainPathfindingNodeDetails details,
            TerrainPathfindingNode parent)
        {
            var position = details.Position;
            var multip = TerrainRoadPathfidingDebugObject.GrateToGlobalPositionMultiplier;

            if (parent != null)
            {
                TerrainRoadPathfidingDebugObject.CurrentPathfindingSteps.Add(new TestPathfindingStep()
                {
                    Start = new Vector3(parent.Position.X * multip, TerrainRoadPathfidingDebugObject.DebugLinesHeight,
                        parent.Position.Y * multip),
                    Finish = new Vector3(position.X * multip, TerrainRoadPathfidingDebugObject.DebugLinesHeight,
                        position.Y * multip),
                });
            }
        }

        private void DebugPerSegmentCompleted(List<Vector2> stepsList)
        {
            PathfindingResults.Add(new SinglePairPathfindingResult()
            {
                FinalStepList = stepsList.Select(
                    c => new Vector3(c.x * GrateToGlobalPositionMultiplier,
                        DebugLinesHeight, c.y * GrateToGlobalPositionMultiplier)).ToList(),
                PathfindingSteps = CurrentPathfindingSteps
            });
            CurrentPathfindingSteps = new List<TestPathfindingStep>();
        }

        public void Update()
        {
            if (Input.GetKeyDown(KeyCode.J))
            {
                Step();
            }
        }

        public void SolvePath()
        {
            while (_solvingResult == AStarSolverState.Searching)
            {
                Step();
            }
        }

        public void ResetPath()
        {
            DebugGameObjects.ForEach(c => GameObject.Destroy(c));
            DebugGameObjects.Clear();
            CurrentPathfindingSteps.Clear();
            _solvingResult = AStarSolverState.Searching;
            PathfindingResults.Clear();
            ResetPathObjects();
        }

        private static List<GameObject> DebugGameObjects = new List<GameObject>();

        private static List<TestPathfindingStep> CurrentPathfindingSteps = new List<TestPathfindingStep>();
        private List<SinglePairPathfindingResult> PathfindingResults = new List<SinglePairPathfindingResult>();

        private static float DebugLinesHeight = 3f;


        private AStarSolverState _solvingResult = AStarSolverState.Searching;

        public void Step()
        {
            //if (_solvingResult == AStarSolverState.Searching)
            //{
            //    List<int> indexesToRemove = new List<int>();
            //    int i = 0;
            //    foreach (var obj in DebugGameObjects)
            //    {
            //        var scale = obj.transform.localScale.x;
            //        scale = scale * 0.9f;
            //        obj.transform.localScale = new Vector3(scale, scale, scale);
            //        if (scale < 0.1f)
            //        {
            //            indexesToRemove.Add(i);
            //        }
            //        i++;
            //    }
            //    indexesToRemove.Reverse();
            //    indexesToRemove.ForEach(k => GameObject.Destroy(DebugGameObjects[k]));
            //    indexesToRemove.ForEach(k => DebugGameObjects.RemoveAt(k));

            //    _solvingResult = _aStarSolver.Step();
            //    if (_solvingResult != AStarSolverState.Searching)
            //    {
            //        Debug.Log($"T6: {_solvingResult}");
            //        var nodesPath = _aStarSolver.GetPath().Cast<TerrainPathfindingNode>();
            //        var finalStepsList = nodesPath
            //            .Select(c => c.Position)
            //            .Select(p => new Vector3(p.X * GrateToGlobalPositionMultiplier, DebugLinesHeight, p.Y*GrateToGlobalPositionMultiplier))
            //            .ToList();

            //        PathfindingResults.Add( new SinglePairPathfindingResult()
            //        {
            //            FinalStepList = finalStepsList,
            //            PathfindingSteps = CurrentPathfindingSteps
            //        });
            //        CurrentPathfindingSteps = new List<TestPathfindingStep>();
            //    }
            //}
        }

        public void OnDrawGizmosSelected()
        {
            int k = 0;
            List<Color> baseColors = new List<Color>()
            {
                new Color(0, 1, 0),
                new Color(0, 0, 1),
                new Color(0, 1, 1),
                new Color(0, 0.5f, 1),
                new Color(0, 1, 0.5f)
            };
            foreach (var aPathfindingResult in PathfindingResults)
            {
                var currentColor = baseColors[k % baseColors.Count];

                var pathfindingSteps = aPathfindingResult.PathfindingSteps;
                int i = 0;
                foreach (var step in pathfindingSteps)
                {
                    float percent = (float) (i % 200) / CurrentPathfindingSteps.Count;
                    Gizmos.color = currentColor * percent;
                    Gizmos.DrawLine(step.Start, step.Finish);
                    i++;
                }

                var finalStepsList = aPathfindingResult.FinalStepList;
                Gizmos.color = Color.red;
                for (int l = 0; l < finalStepsList.Count - 1; l++)
                {
                    var current = finalStepsList[l];
                    var next = finalStepsList[l + 1];
                    Gizmos.DrawLine(current, next);
                }

                k++;
            }
        }

        private class SinglePairPathfindingResult
        {
            public List<TestPathfindingStep> PathfindingSteps;
            public List<Vector3> FinalStepList;
        }
    }

    public class DebugTerrainTester
    {
        private ComputeShaderContainerGameObject _containerGameObject;

        private UpdatableContainer _updatableContainer;
        private TerrainShapeDbProxy _terrainShapeDbProxy;

        private readonly List<OtherThreadProxyAndActionPair> _otherThreadActionPairs =
            new List<OtherThreadProxyAndActionPair>();

        public DebugTerrainTester(ComputeShaderContainerGameObject containerGameObject)
        {
            _containerGameObject = containerGameObject;
        }

        public void Start(bool multithreading = false, bool useTextureSavingToDisk = false)
        {
            _updatableContainer = new UpdatableContainer();
            TaskUtils.SetGlobalMultithreading(multithreading);

            //////////////////
            var rgbaMainTexture = SavingFileManager.LoadPngTextureFromFile(@"C:\inz\cont\n49_e019_1arc_v3.png", 3600,
                3600,
                TextureFormat.ARGB32, true, false);

            CommonExecutorUTProxy commonExecutorUtProxy = new CommonExecutorUTProxy();
            _updatableContainer.AddUpdatableElement(commonExecutorUtProxy);
            TerrainTextureFormatTransformator transformator =
                new TerrainTextureFormatTransformator(commonExecutorUtProxy);
            var mirroredImage = transformator.MirrorHeightTexture(new TextureWithSize()
            {
                Size = new IntVector2(3600, 3600),
                Texture = rgbaMainTexture
            });
            var globalHeightTexture = transformator.EncodedHeightTextureToPlain(new TextureWithSize()
            {
                Size = new IntVector2(3600, 3600),
                Texture = mirroredImage
            });

            var unityCoordsCalculator = new UnityCoordsCalculator(new Vector2(24 * 240 * 2, 24 * 240 * 2));

            UnityThreadComputeShaderExecutorObject computeShaderExecutorObject =
                new UnityThreadComputeShaderExecutorObject();
            _updatableContainer.AddUpdatableElement(computeShaderExecutorObject);
            _updatableContainer.AddUpdatableElement(commonExecutorUtProxy);

            UTTextureRendererProxy textureRendererProxy = new UTTextureRendererProxy(new TextureRendererService(
                new MultistepTextureRenderer(_containerGameObject), new TextureRendererServiceConfiguration()
                {
                    StepSize = new Vector2(500, 500)
                }));
            _updatableContainer.AddUpdatableElement(textureRendererProxy);

            TerrainDetailGenerator terrainDetailGenerator = Ring1DebugObjectV2.CreateTerrainDetailGenerator(
                globalHeightTexture, textureRendererProxy, commonExecutorUtProxy, computeShaderExecutorObject,
                _containerGameObject);
            TerrainDetailProvider terrainDetailProvider = Ring1DebugObjectV2.CreateTerrainDetailProvider(
                terrainDetailGenerator, commonExecutorUtProxy, useTextureSavingToDisk);

            var terrainShapeDb = new TerrainShapeDb(
                new CachedTerrainDetailProvider(
                    terrainDetailProvider,
                    () => new TerrainDetailElementsCache(commonExecutorUtProxy,
                        new TerrainDetailElementCacheConfiguration())),
                new TerrainDetailAlignmentCalculator(240));
            _terrainShapeDbProxy = new TerrainShapeDbProxy(terrainShapeDb);
            terrainDetailGenerator.SetBaseTerrainDetailProvider(BaseTerrainDetailProvider.CreateFrom(terrainShapeDb));

            _otherThreadActionPairs.Add(new OtherThreadProxyAndActionPair()
            {
                Proxy = _terrainShapeDbProxy
            });

            var meshGeneratorProxy = new MeshGeneratorUTProxy(new MeshGeneratorService());
            _updatableContainer.AddUpdatableElement(meshGeneratorProxy);

            var stainTerrainResourceCreatorUtProxy =
                new StainTerrainResourceCreatorUTProxy(new StainTerrainResourceCreator());
            _updatableContainer.AddUpdatableElement(stainTerrainResourceCreatorUtProxy);

            Ring1DebugObjectV2.StartThreading(_otherThreadActionPairs);
        }

        public void AddUpdatableElement(IUpdatable element)
        {
            _updatableContainer.AddUpdatableElement(element);
        }

        public TerrainShapeDbProxy TerrainShapeDbProxy => _terrainShapeDbProxy;

        public void Update()
        {
            _updatableContainer.Update();
        }
    }
}