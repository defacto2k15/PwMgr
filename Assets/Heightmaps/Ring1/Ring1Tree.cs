using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Assets.Grass;
using Assets.Heightmaps.Ring1.Creator;
using Assets.Heightmaps.Ring1.treeNodeListener;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.Heightmaps.Ring1.VisibilityTexture;
using Assets.Random;
using Assets.ShaderUtils;
using Assets.Utils;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Assets.Heightmaps.Ring1
{
    public class Ring1Tree
    {
        private Ring1TreeConfiguration _configuration;

        private Ring1Root _rootNode;
        private Ring1VisibilityResolver _visibilityResolver;
        private NodeSplitController _nodeSplitController;

        public Ring1Tree(Ring1TreeConfiguration configuration = null)
        {
            if (configuration == null)
            {
                configuration = new Ring1TreeConfiguration();
            }
            _configuration = configuration;
        }

        public void
            CreateHeightmap_TODO_DELETE(
                HeightmapArray heightmapArray) //input heightmap has 256x256 map, spans 5,7x5,7km p22.3m real life terrain
        {
            var tessalationRequirementTextureGenerator = new TessalationRequirementTextureGenerator();
            var tessalationReqTexture =
                tessalationRequirementTextureGenerator.GenerateTessalationRequirementTexture(heightmapArray, 64);

            var heightmapBundle = SavingFileManager.LoadHeightmapBundlesFromFiles("heightmapBundle", 4, 2048);

            var submapTextures = new Ring1SubmapTextures(heightmapBundle, tessalationReqTexture);
            _rootNode = CreateRootNode_TODO_DELETE(submapTextures);
            _rootNode.UpdateLod();
        }

        public void CreateHeightmap(RootNodeCreationParameters parameters)
        {
            _rootNode = CreateRootNode(parameters);
            Update(new FovData(parameters.InitialCameraPosition, null));
        }

        private Ring1Root CreateRootNode(RootNodeCreationParameters parameters)
        {
            int maximumLodLevel = parameters.PrecisionDistances.Values.Max();
            int minimumLodLevel = parameters.PrecisionDistances.Values.Min();
            _visibilityResolver =
                new Ring1VisibilityResolver(new Ring1BoundsCalculator(parameters.UnityCoordsCalculator), parameters.InitialCameraPosition);
            _visibilityResolver.SetFovDataOverride(true);

            _nodeSplitController = new NodeSplitController(
                maximumLodLevel: maximumLodLevel,
                minimumLodLevel: minimumLodLevel,
                precisionDistances: parameters.PrecisionDistances,
                coordsCalculator: parameters.UnityCoordsCalculator
            );
            //_nodeSplitController.SetCameraPosition(parameters.InitialCameraPosition);
            //Debug.Log("F11 First camera position "+parameters.InitialCameraPosition);

            return new Ring1Root(
                new Ring1Node(
                    _nodeSplitController,
                    parameters.NodeListener,
                    _visibilityResolver,
                    new MyRectangle(0, 0, 0.5f, 0.5f),
                    1, Ring1NodePositionEnum.DOWN_LEFT),
                new Ring1Node(
                    _nodeSplitController,
                    parameters.NodeListener,
                    _visibilityResolver,
                    new MyRectangle(0.5f, 0, 0.5f, 0.5f),
                    1, Ring1NodePositionEnum.DOWN_RIGHT),
                new Ring1Node(
                    _nodeSplitController,
                    parameters.NodeListener,
                    _visibilityResolver,
                    new MyRectangle(0, 0.5f, 0.5f, 0.5f),
                    1, Ring1NodePositionEnum.TOP_LEFT),
                new Ring1Node(
                    _nodeSplitController,
                    parameters.NodeListener,
                    _visibilityResolver,
                    new MyRectangle(0.5f, 0.5f, 0.5f, 0.5f),
                    1, Ring1NodePositionEnum.TOP_RIGHT),
                parameters.NodeListener
            );
        }

        public class RootNodeCreationParameters
        {
            public IRing1NodeListener NodeListener;
            public UnityCoordsCalculator UnityCoordsCalculator;
            public Dictionary<float, int> PrecisionDistances;
            public Vector3 InitialCameraPosition;
        }

        Ring1Root CreateRootNode_TODO_DELETE(Ring1SubmapTextures submapTextures)
        {
            var terrainParentGameObject = new GameObject("TerrainParent");

            var unityCoordsCalculator = new UnityCoordsCalculator(new Vector2(256, 256));
            var nodeListener = new Ring1NodeObjects(
                (node) => new List<IRing1NodeListener>()
                {
                    //todo
                    //new Ring1NodeShaderHeightTerrain(node, _ring1LodData, terrainParentGameObject, submapTextures,
                    //    unityCoordsCalculator, null) //todo this null
//                      new Ring1NodeDirectHeightTerrain(node, _ring1LodData, terrainParentGameObject, submapTextures, unityCoordsCalculator, heightmapArray)
                }
            );

            _visibilityResolver = new Ring1VisibilityResolver(new Ring1BoundsCalculator(unityCoordsCalculator));
            _visibilityResolver.SetFovDataOverride(true);

            _nodeSplitController = new NodeSplitController(
                maximumLodLevel: MyConstants.MAXIMUM_LOD_LEVEL,
                minimumLodLevel: 0,
                precisionDistances: new Dictionary<float, int>
                {
                    {50, 3},
                    {100, 2},
                    {200, 1}
                },
                coordsCalculator: unityCoordsCalculator
            );

            return new Ring1Root(
                new Ring1Node(
                    _nodeSplitController,
                    nodeListener,
                    _visibilityResolver,
                    new MyRectangle(0, 0, 0.5f, 0.5f),
                    1, Ring1NodePositionEnum.DOWN_LEFT),
                new Ring1Node(
                    _nodeSplitController,
                    nodeListener,
                    _visibilityResolver,
                    new MyRectangle(0.5f, 0, 0.5f, 0.5f),
                    1, Ring1NodePositionEnum.DOWN_RIGHT),
                new Ring1Node(
                    _nodeSplitController,
                    nodeListener,
                    _visibilityResolver,
                    new MyRectangle(0, 0.5f, 0.5f, 0.5f),
                    1, Ring1NodePositionEnum.TOP_LEFT),
                new Ring1Node(
                    _nodeSplitController,
                    nodeListener,
                    _visibilityResolver,
                    new MyRectangle(0.5f, 0.5f, 0.5f, 0.5f),
                    1, Ring1NodePositionEnum.TOP_RIGHT),
                nodeListener
            );
        }

        public Texture2D RegenerateTextureShowingObject(UniformsPack uniforms)
        {
            throw new NotImplementedException();
        }

        public void RegenerateTerrainShowingObject(UniformsPack uniforms)
        {
            throw new NotImplementedException();
        }

        public void UpdateLod(FovData fovData)
        {
            Update(fovData);
        }

        private void Update(FovData fovData)
        {
            var moveDelta = _nodeSplitController.CameraLastPosition.FlatDistance(fovData.CameraPosition);

            if (moveDelta > _configuration.MinimumTreeUpdateDelta)
            {
                _nodeSplitController.SetCameraPosition(fovData.CameraPosition);
                _visibilityResolver.SetFovDataOverride(null);
                _visibilityResolver.SetFovData(fovData);
                _rootNode.UpdateLod();
            }
        }
    }

    public class Ring1TreeConfiguration
    {
        public float MinimumTreeUpdateDelta = -1f; // will updateLod allways!
    }
}