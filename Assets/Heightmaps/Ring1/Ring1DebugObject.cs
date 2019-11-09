using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.Caching;
using Assets.FinalExecution;
using Assets.Heightmaps.Preparment;
using Assets.Heightmaps.Ring1.Creator;
using Assets.Heightmaps.Ring1.MeshGeneration;
using Assets.Heightmaps.Ring1.MT;
using Assets.Heightmaps.Ring1.Painter;
using Assets.Heightmaps.Ring1.Ring1HeightArrayModifier;
using Assets.Heightmaps.Ring1.treeNodeListener;
using Assets.Heightmaps.Ring1.TerrainDescription;
using Assets.Heightmaps.Ring1.TerrainDescription.Cache;
using Assets.Heightmaps.Ring1.VisibilityTexture;
using Assets.Ring2;
using Assets.Utils;
using Assets.Utils.MT;
using Assets.Utils.Services;
using Assets.Utils.Textures;
using Assets.Utils.UTUpdating;
using UnityEngine;

namespace Assets.Heightmaps.Ring1
{
    public class Ring1DebugObject : MonoBehaviour
    {
        private Ring1Tree _ring1Tree;
        private UpdatableContainer _updatableContainer;
        private List<OtherThreadProxyAndActionPair> _otherThreadActionPairs = new List<OtherThreadProxyAndActionPair>();

        // Use this for initialization
        void Start()
        {
            _updatableContainer = new UpdatableContainer();
            TaskUtils.SetGlobalMultithreading(true);

            int minSubmapWidth = 256; //(int)Math.Floor((double)filePixelWidth/subTerrainCount)-1 ;
            var ring1Array = SavingFileManager.LoadFromFile("map.dat", 2048, 2048);
            ring1Array = BasicHeightmapModifier.Multiply(1000, ring1Array);
            _ring1Tree = new Ring1Tree();

            //////////////////

            var tessalationRequirementTextureGenerator = new TessalationRequirementTextureGenerator();
            var tessalationReqTexture =
                tessalationRequirementTextureGenerator.GenerateTessalationRequirementTexture(ring1Array, 64);

            var heightmapBundle = SavingFileManager.LoadHeightmapBundlesFromFiles("heightmapBundle", 4, 2048);

            var submapTextures = new Ring1SubmapTextures(heightmapBundle, tessalationReqTexture);

            /// /// VISIBILITY TEXTURE
            var visibilityTextureSideLength = 16;
            var visibilityTexture = new Texture2D(visibilityTextureSideLength, visibilityTextureSideLength,
                TextureFormat.RFloat, false);
            visibilityTexture.filterMode = FilterMode.Point;

            var visibilityTextureProcessorProxy =
                new Ring1VisibilityTextureProcessorUTProxy(new Ring1VisibilityTextureProcessor(visibilityTexture));
            _updatableContainer.AddUpdatableElement(visibilityTextureProcessorProxy);


            var visibilityTextureChangeGrabber = new Ring1VisibilityTextureChangeGrabber();

            var terrainParentGameObject = new GameObject("TerrainParent");

            var unityCoordsCalculator = new UnityCoordsCalculator(new Vector2(256, 256));
            var orderGrabber = new Ring1PaintingOrderGrabber();

            var painterProxy = new RingTerrainPainterUTProxy(new RingTerrainPainter());
            _updatableContainer.AddUpdatableElement(painterProxy);

            painterProxy.Update();

            var mainRespondingProxy = new Ring1NodeEventMainRespondingProxy(new Ring1NodeEventMainResponder());
            _otherThreadActionPairs.Add(new OtherThreadProxyAndActionPair()
            {
                Proxy = mainRespondingProxy,
                EveryPostAction =
                    () =>
                    {
                        var delta = visibilityTextureChangeGrabber.RetriveVisibilityChanges();

                        if (delta.AnyChange)
                        {
                            var visibilityTextureChagnes = visibilityTextureChangeGrabber.RetriveVisibilityChanges();
                            visibilityTextureProcessorProxy.AddOrder(visibilityTextureChagnes);
                        }

                        if (orderGrabber.IsAnyOrder)
                        {
                            painterProxy.AddOrder(orderGrabber.RetriveOrderAndClear());
                        }
                    }
            });

            var commonExecutor = new CommonExecutorUTProxy();
            _updatableContainer.AddUpdatableElement(commonExecutor);

            TerrainShapeDbProxy terrainShapeDbProxy = new TerrainShapeDbProxy(
                FETerrainShapeDbInitialization.CreateTerrainShapeDb(null /*todo here*/, commonExecutor, new TerrainDetailAlignmentCalculator(240),
                    false, false, false, null));

            _otherThreadActionPairs.Add(new OtherThreadProxyAndActionPair()
            {
                Proxy = terrainShapeDbProxy
            });


            var meshGeneratorProxy = new MeshGeneratorUTProxy(new MeshGeneratorService());
            _updatableContainer.AddUpdatableElement(meshGeneratorProxy);

            //var eventCollector = new Ring1NodeEventCollector((node) =>
            //    new Ring1NodeDirectHeightTerrain(
            //        node,
            //        visibilityTextureChangeGrabber,
            //        visibilityTexture,
            //        terrainParentGameObject,
            //        unityCoordsCalculator,
            //        orderGrabber,
            //        terrainShapeDbProxy,
            //        ring1Array,
            //        meshGeneratorProxy));
            var eventCollector = new Ring1NodeEventCollector(new FromLambdaListenersCreator((node) =>
                new Ring1ListenerToGRingListener(
                    new Ring1NodeShaderHeightTerrain(
                        node,
                        visibilityTextureChangeGrabber,
                        visibilityTexture,
                        terrainParentGameObject,
                        unityCoordsCalculator,
                        orderGrabber,
                        terrainShapeDbProxy,
                        meshGeneratorProxy,
                        null)))); //todo if you want this to work

            var ring1Proxy = new Ring1TreeProxy(_ring1Tree);
            _otherThreadActionPairs.Add(new OtherThreadProxyAndActionPair()
            {
                Proxy = ring1Proxy,
                EveryPostAction =
                    () =>
                    {
                        if (eventCollector.Any)
                        {
                            mainRespondingProxy.AddOrder(eventCollector.RetriveOrderAndClear());
                        }
                    }
            });

            StartThreading();
            ring1Proxy.CreateHeightmap(
                new Ring1Tree.RootNodeCreationParameters()
                {
                    UnityCoordsCalculator = unityCoordsCalculator,
                    NodeListener = eventCollector,
                    PrecisionDistances = Ring1TestDefaults.PrecisionDistances,
                    InitialCameraPosition = Vector3.zero,
                });
        }

        private void StartThreading()
        {
            if (TaskUtils.GetGlobalMultithreading())
            {
                foreach (var pair in _otherThreadActionPairs)
                {
                    pair.Proxy.StartThreading(pair.EveryPostAction);
                }
            }
        }

        public void Update()
        {
            _updatableContainer.Update();
            if (!TaskUtils.GetGlobalMultithreading())
            {
                foreach (var pair in _otherThreadActionPairs)
                {
                    pair.EveryPostAction?.Invoke();
                }
            }
        }
    }

    public class OtherThreadProxyAndActionPair
    {
        public BaseOtherThreadProxy Proxy;
        public Action EveryPostAction;
    }

    public class Ring1ListenerToGRingListener : IAsyncRing1NodeListener
    {
        private readonly IAsyncGRingNodeListener _listener;

        public Ring1ListenerToGRingListener(IAsyncGRingNodeListener listener)
        {
            _listener = listener;
        }

        public Task CreatedNewNodeAsync()
        {
            Debug.Log("Ytr CreatedNewNode");
            return _listener.CreatedNewNodeAsync();
        }

        public Task DoNotDisplayAsync()
        {
            Debug.Log("Ytr DoNotDisplay");
            return _listener.DoNotDisplayAsync();
        }

        public Task UpdateAsync(Vector3 cameraPosition)
        {
            Debug.Log("Ytr Update");
            return _listener.UpdateAsync();
        }
    }
}