using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Assets.Heightmaps.Ring1;
using Assets.Heightmaps.Ring1.MeshGeneration;
using Assets.Heightmaps.Ring1.Painter;
using Assets.Heightmaps.Ring1.treeNodeListener;
using Assets.Heightmaps.Ring1.TerrainDescription;
using Assets.Heightmaps.Welding;
using Assets.MeshGeneration;
using Assets.Repositioning;
using Assets.Ring2.BaseEntities;
using Assets.ShaderUtils;
using Assets.Trees.SpotUpdating;
using Assets.Utils;
using Assets.Utils.MT;
using UnityEngine;

namespace Assets.Heightmaps.GRing
{
    public class GDebugTerrainedLodNodeTerrain : ImprovedAsyncGRingNodeListener
    {
        private readonly Ring1PaintingOrderGrabber _orderGrabber;
        private readonly GameObject _parentObject;

        private readonly GRingTripletProvider _tripletProvider;
        private readonly FlatLod _flatLod;
        private MeshGeneratorUTProxy _meshGeneratorUtProxy;
        private GRingGroundShapeProvider _groundShapeProvider;

        private GRingTerrainId _terrainId = null;
        private IGroundShapeToken _groundShapeToken;

        public GDebugTerrainedLodNodeTerrain(
            Ring1PaintingOrderGrabber orderGrabber,
            GameObject parentObject,
            GRingTripletProvider tripletProvider,
            FlatLod flatLod,
            MeshGeneratorUTProxy meshGeneratorUtProxy, GRingGroundShapeProvider groundShapeProvider)
        {
            _orderGrabber = orderGrabber;
            _parentObject = parentObject;
            _tripletProvider = tripletProvider;
            _flatLod = flatLod;
            _meshGeneratorUtProxy = meshGeneratorUtProxy;
            _groundShapeProvider = groundShapeProvider;
        }

        public override Task ShowNodeAsync()
        {
            if (_terrainId != null)
            {
                _groundShapeToken.GroundActive();
                foreach (var id in _terrainId.ElementsIds)
                {
                    _orderGrabber.SetActive(id, true);
                }
            }
            return TaskUtils.EmptyCompleted();
        }

        public override Task UpdateNodeAsync()
        {
            return TaskUtils.EmptyCompleted();
        }

        public override Task HideNodeAsync()
        {
            if (_terrainId != null)
            {
                foreach (var id in _terrainId.ElementsIds)
                {
                    _orderGrabber.SetActive(id, false);
                }
            }
            return TaskUtils.EmptyCompleted();
        }

        public override async Task CreatedNewNodeAsync()
        {
            _terrainId = await CreateTerrainAsync();
        }

        public override Task Destroy()
        {
            return TaskUtils.EmptyCompleted(); //todo
        }

        public static int LastId = 0;

        private async Task<GRingTerrainId> CreateTerrainAsync()
        {
            var triplet = _tripletProvider.ProvideTriplet();

            var mesh = await _meshGeneratorUtProxy.AddOrder(() => PlaneGenerator.CreateFlatPlaneMesh(65, 65));
            var creationTemplatesList = new List<Ring1GroundPieceCreationTemplate>();
            int layerIndex = 0;

            UniformsPack pack = new UniformsPack();
            pack.SetUniform("_LodLevel", _flatLod.ScalarValue);
            pack.SetUniform("_NodeId", LastId);

            var groundShapeDetails = await _groundShapeProvider.ProvideGroundTextureDetail();
            _groundShapeToken = groundShapeDetails.GroundShapeToken;
            pack.MergeWith(groundShapeDetails.Uniforms);

            creationTemplatesList.Add(
                new Ring1GroundPieceCreationTemplate()
                {
                    Name = $"TerrainElementLayer l:{layerIndex} fl:{_flatLod.ScalarValue}, X:{LastId++}",
                    ParentGameObject = _parentObject,
                    PieceMesh = mesh,
                    TransformTriplet = triplet,
                    ShaderName = "Custom/Terrain/DebugTerrainedLod",
                    //ShaderName = "Custom/Terrain/Ring0",
                    ShaderKeywordSet = groundShapeDetails.ShaderKeywordSet,
                    Uniforms = pack,
                    Modifier = new Ring1GroundPieceModifier()
                });


            var toReturn = new GRingTerrainId()
            {
                ElementsIds = creationTemplatesList.Select(c => _orderGrabber.AddCreationOrder(c)).ToList()
            };

            return toReturn;
        }
    }

    public class GDebugTerrainedLodNodeTerrainCreator : INewGRingListenersCreator
    {
        private readonly Ring1PaintingOrderGrabber _orderGrabber;
        private readonly GameObject _parentObject;
        private readonly UnityCoordsCalculator _coordsCalculator;
        private readonly MeshGeneratorUTProxy _meshGeneratorUtProxy;
        private readonly ITerrainShapeDb _terrainShapeDb;
        private readonly GRingSpotUpdater _gRingSpotUpdater;
        private GRingGroundShapeProviderConfiguration _groundShapeProviderConfiguration;

        public GDebugTerrainedLodNodeTerrainCreator(
            Ring1PaintingOrderGrabber orderGrabber, GameObject parentObject,
            UnityCoordsCalculator coordsCalculator, MeshGeneratorUTProxy meshGeneratorUtProxy,
            ITerrainShapeDb terrainShapeDb, GRingGroundShapeProviderConfiguration groundShapeProviderConfiguration,
            GRingSpotUpdater gRingSpotUpdater = null)
        {
            _orderGrabber = orderGrabber;
            _parentObject = parentObject;
            _coordsCalculator = coordsCalculator;
            _meshGeneratorUtProxy = meshGeneratorUtProxy;
            _terrainShapeDb = terrainShapeDb;
            _groundShapeProviderConfiguration = groundShapeProviderConfiguration;
            _gRingSpotUpdater = gRingSpotUpdater;
        }

        public IAsyncGRingNodeListener CreateNewListener(Ring1Node node, FlatLod flatLod)
        {
            var inGamePosition = _coordsCalculator.CalculateGlobalObjectPosition(node.Ring1Position);

            GRingTripletProvider tripletProvider = new GRingTripletProvider(
                inGamePosition, Repositioner.Default, HeightDenormalizer.Default);

            GRingGroundShapeProvider groundShapeProvider = new GRingGroundShapeProvider(
                _terrainShapeDb,
                flatLod,
                inGamePosition,
                _gRingSpotUpdater,
                _groundShapeProviderConfiguration);

            return new GDebugTerrainedLodNodeTerrain(
                _orderGrabber, _parentObject, tripletProvider, flatLod, _meshGeneratorUtProxy, groundShapeProvider);
        }
    }
}