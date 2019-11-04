using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Assets.ComputeShaders;
using Assets.Heightmaps.Ring1.TerrainDescription;
using Assets.Heightmaps.Ring1.TerrainDescription.FeatureGenerating;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.MeshGeneration;
using Assets.Roads.Engraving;
using Assets.Roads.Files;
using Assets.Roads.Pathfinding;
using Assets.Utils;
using Assets.Utils.MT;
using Assets.Utils.Services;
using Assets.Utils.Spatial;
using Assets.Utils.Textures;
using Assets.Utils.UTUpdating;
using UnityEngine;
using BaseOtherThreadProxy = Assets.Utils.MT.BaseOtherThreadProxy;

namespace Assets.Roads.TerrainFeature
{
    public class RoadEngravingFeatureApplierDebugObject : MonoBehaviour
    {
        public ComputeShaderContainerGameObject ComputeShaderContainer;
        private DebugTerrainTester _debugTerrainTester;

        public void Start()
        {
            _debugTerrainTester = new DebugTerrainTester(ComputeShaderContainer);
            _debugTerrainTester.Start(true);

            RoadDatabaseProxy roadDatabaseProxy = new RoadDatabaseProxy(new RoadDatabase(@"C:\inz\wrt-1\"));
            roadDatabaseProxy.StartThreading();
            UnityThreadComputeShaderExecutorObject shaderExecutorObject = new UnityThreadComputeShaderExecutorObject();
            TextureConcieverUTProxy textureConcieverUtProxy = new TextureConcieverUTProxy();
            _debugTerrainTester.AddUpdatableElement(textureConcieverUtProxy);
            _debugTerrainTester.AddUpdatableElement(shaderExecutorObject);

            RoadEngraver roadEngraver = new RoadEngraver(
                ComputeShaderContainer,
                shaderExecutorObject,
                new RoadEngraver.RoadEngraverConfiguration());

            PathProximityTextureGenerator proximityTextureGenerator = new PathProximityTextureGenerator(
                textureConcieverUtProxy,
                new PathProximityTextureGenerator.PathProximityTextureGeneratorConfiguration());
            PathProximityArrayGenerator proximityArrayGenerator = new PathProximityArrayGenerator(
                new PathProximityArrayGenerator.PathProximityArrayGeneratorConfiguration());

            var pathProximityTextureDbProxy = new PathProximityTextureDbProxy(new SpatialDb<TextureWithSize>(
                new PathProximityTexturesProvider(roadDatabaseProxy, proximityTextureGenerator,
                    proximityArrayGenerator, new PathProximityTextureProviderConfiguration()
                    {
                        MaxProximity = 5
                    }),
                new SpatialDbConfiguration()
                {
                    QueryingCellSize = new Vector2(90, 90)
                }));
            pathProximityTextureDbProxy.StartThreading();

            _roadEngravingTerrainFeatureApplier = new RoadEngravingTerrainFeatureApplier(
                pathProximityTextureDbProxy,
                roadEngraver,
                new RoadEngravingTerrainFeatureApplierConfiguration());

            var basePos = new IntVector2(524, 583);
            var startPos = new List<IntVector2>()
            {
                basePos + new IntVector2(0, 0),
                basePos + new IntVector2(0, 1),
                basePos + new IntVector2(1, 0),
                basePos + new IntVector2(1, 1),
            };

            var commonExecutor = new OtherThreadExecutorProxy();
            commonExecutor.StartThreading(() => { });

            commonExecutor.PostAction(async () =>
            {
                var outList = new List<TextureWithCoords>();
                var terrainCoords = startPos.Select(c => new MyRectangle(c.X * 90, c.Y * 90, 90, 90));
                foreach (var aTerrainCoord in terrainCoords)
                {
                    var baseHeightTexture = (await _debugTerrainTester.TerrainShapeDbProxy.Query(
                        new TerrainDescriptionQuery()
                        {
                            QueryArea = aTerrainCoord,
                            RequestedElementDetails = new List<TerrainDescriptionQueryElementDetail>()
                            {
                                new TerrainDescriptionQueryElementDetail()
                                {
                                    Type = TerrainDescriptionElementTypeEnum.HEIGHT_ARRAY,
                                    Resolution = TerrainCardinalResolution.MAX_RESOLUTION
                                }
                            }
                        }));
                    var terrainTexture = baseHeightTexture
                        .GetElementOfType(TerrainDescriptionElementTypeEnum.HEIGHT_ARRAY).TokenizedElement.DetailElement
                        .Texture;

                    var engravedHeightTexture =
                        await _roadEngravingTerrainFeatureApplier.ApplyFeatureAsync(
                            new TextureWithCoords(terrainTexture, aTerrainCoord),
                            TerrainCardinalResolution.MAX_RESOLUTION, true);
                    outList.Add(engravedHeightTexture);
                }

                outList.ForEach(c => _terrainTextures.Add(c));
            });
        }

        private ConcurrentBag<TextureWithCoords> _terrainTextures = new ConcurrentBag<TextureWithCoords>();
        private RoadEngravingTerrainFeatureApplier _roadEngravingTerrainFeatureApplier;


        public void Update()
        {
            _debugTerrainTester.Update();
            TextureWithCoords outTex = null;
            if (_terrainTextures.TryTake(out outTex))
            {
                CreateTerrainObject(outTex.Texture,
                    new Vector2(outTex.Coords.X, outTex.Coords.Y), 90);
            }
        }


        public static GameObject CreateTerrainObject(Texture heightTexture, Vector2 startPos, float baseTerrainLength)
        {
            var mesh = PlaneGenerator.CreateFlatPlaneMesh(241, 241);

            var terrainPos = startPos; //new Vector2(startPosNo.X * 100f, startPosNo.Y * 100);
            var terrainObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
            terrainObject.GetComponent<MeshFilter>().mesh = mesh;
            terrainObject.transform.localScale = new Vector3(baseTerrainLength, 4000, baseTerrainLength);
            terrainObject.transform.position = new Vector3(terrainPos.x, -20f, terrainPos.y);
            //terrainObject.transform.SetParent(parentGo.transform);

            var material = new Material(Shader.Find("Custom/Terrain/TestTerrainDirectPlain"));
            material.SetTexture("_HeightmapTex", heightTexture);
            terrainObject.GetComponent<MeshRenderer>().material = material;
            terrainObject.name = "testTerrain " + startPos;

            return terrainObject;
        }

        public class DEBUGTODOCommonAsyncExecutorUtProxy : LegacyBaseUTProxy
        {
            private SingleThreadSynchronizationContext _singleThreadSynchronizationContext;

            public void StartThreading(Action perEveryPostAction = null)
            {
                if (!TaskUtils.GetGlobalMultithreading())
                {
                    return;
                }
                _singleThreadSynchronizationContext = new SingleThreadSynchronizationContext();
                SingleThreadSynchronizationContext.SetSynchronizationContext(_singleThreadSynchronizationContext);
            }

            public override void InternalUpdate()
            {
                if (_singleThreadSynchronizationContext != null)
                {
                    _singleThreadSynchronizationContext.OneMessageLoop();
                }
            }

            public void AddAction(Action action)
            {
                if (!TaskUtils.GetGlobalMultithreading() || TaskUtils.GetMultithreadingOverride())
                {
                    action();
                }
                else
                {
                    _singleThreadSynchronizationContext.Post(
                        (unused) => action(), null);
                }
            }
        }

        public class OtherThreadExecutorProxy : BaseOtherThreadProxy
        {
            public OtherThreadExecutorProxy() : base("OtherThreadExecutorProxyThread", false)
            {
            }
        }
    }
}