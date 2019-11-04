using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Assets.Heightmaps.Ring1;
using Assets.Heightmaps.Ring1.MeshGeneration;
using Assets.Heightmaps.Ring1.Painter;
using Assets.Heightmaps.Ring1.treeNodeListener;
using Assets.MeshGeneration;
using Assets.Repositioning;
using Assets.Ring2.BaseEntities;
using Assets.ShaderUtils;
using Assets.Utils;
using Assets.Utils.MT;
using UnityEngine;

namespace Assets.Heightmaps.GRing
{
    public class GDebugLodNodeTerrain : IAsyncGRingNodeListener
    {
        private readonly Ring1PaintingOrderGrabber _orderGrabber;
        private readonly GameObject _parentObject;

        private readonly GRingTripletProvider _tripletProvider;
        private readonly FlatLod _flatLod;

        private GRingTerrainId _terrainId = null;
        private MeshGeneratorUTProxy _meshGeneratorUtProxy;

        public GDebugLodNodeTerrain(
            Ring1PaintingOrderGrabber orderGrabber,
            GameObject parentObject,
            GRingTripletProvider tripletProvider,
            FlatLod flatLod,
            MeshGeneratorUTProxy meshGeneratorUtProxy)
        {
            _orderGrabber = orderGrabber;
            _parentObject = parentObject;
            _tripletProvider = tripletProvider;
            _flatLod = flatLod;
            _meshGeneratorUtProxy = meshGeneratorUtProxy;
        }

        private bool _isActive = false;

        public Task DoNotDisplayAsync()
        {
            if (_terrainId != null)
            {
                _isActive = false;
                foreach (var id in _terrainId.ElementsIds)
                {
                    _orderGrabber.SetActive(id, false);
                }
            }
            return TaskUtils.EmptyCompleted();
        }

        public Task Destroy()
        {
            return TaskUtils.EmptyCompleted();
        }

        public Task UpdateAsync()
        {
            if (!_isActive)
            {
                _isActive = true;
                if (_terrainId != null)
                {
                    foreach (var id in _terrainId.ElementsIds)
                    {
                        _orderGrabber.SetActive(id, true);
                    }
                }
            }
            return TaskUtils.EmptyCompleted();
        }

        public async Task CreatedNewNodeAsync()
        {
            _terrainId = await CreateTerrainAsync();
        }

        public static int LastId = 0;

        private async Task<GRingTerrainId> CreateTerrainAsync()
        {
            var triplet = _tripletProvider.ProvideTriplet();

            var mesh = await _meshGeneratorUtProxy.AddOrder(() => PlaneGenerator.CreateFlatPlaneMesh(17, 17));
            var creationTemplatesList = new List<Ring1GroundPieceCreationTemplate>();
            int layerIndex = 0;

            UniformsPack pack = new UniformsPack();
            pack.SetUniform("_LodLevel", _flatLod.ScalarValue);
            pack.SetUniform("_NodeId", LastId);

            creationTemplatesList.Add(
                new Ring1GroundPieceCreationTemplate()
                {
                    Name = $"TerrainElementLayer l:{layerIndex} fl:{_flatLod.ScalarValue}, X:{LastId++}",
                    ParentGameObject = _parentObject,
                    PieceMesh = mesh,
                    TransformTriplet = triplet,
                    ShaderName = "Custom/Terrain/DebugLod",
                    ShaderKeywordSet = new ShaderKeywordSet(),
                    Uniforms = pack,
                    Modifier = new Ring1GroundPieceModifier()
                });


            var toReturn = new GRingTerrainId()
            {
                ElementsIds = creationTemplatesList.Select(c => _orderGrabber.AddCreationOrder(c)).ToList()
            };

            _isActive = true;
            return toReturn;
        }
    }

    public class GDebugLodNodeTerrainCreator : INewGRingListenersCreator
    {
        private readonly Ring1PaintingOrderGrabber _orderGrabber;
        private readonly GameObject _parentObject;
        private readonly UnityCoordsCalculator _coordsCalculator;
        private readonly MeshGeneratorUTProxy _meshGeneratorUtProxy;

        public GDebugLodNodeTerrainCreator(Ring1PaintingOrderGrabber orderGrabber, GameObject parentObject,
            UnityCoordsCalculator coordsCalculator, MeshGeneratorUTProxy meshGeneratorUtProxy)
        {
            _orderGrabber = orderGrabber;
            _parentObject = parentObject;
            _coordsCalculator = coordsCalculator;
            _meshGeneratorUtProxy = meshGeneratorUtProxy;
        }

        public IAsyncGRingNodeListener CreateNewListener(Ring1Node node, FlatLod flatLod)
        {
            var inGamePosition = _coordsCalculator.CalculateGlobalObjectPosition(node.Ring1Position);

            GRingTripletProvider tripletProvider = new GRingTripletProvider(
                inGamePosition, Repositioner.Default, HeightDenormalizer.Default);

            return new GDebugLodNodeTerrain(_orderGrabber, _parentObject, tripletProvider, flatLod,
                _meshGeneratorUtProxy);
        }
    }
}