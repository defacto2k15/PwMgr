using System.Collections.Generic;
using System.Threading.Tasks;
using Assets.Heightmaps.Ring1;
using Assets.Heightmaps.Ring1.MeshGeneration;
using Assets.Heightmaps.Ring1.Painter;
using Assets.Heightmaps.Ring1.treeNodeListener;
using Assets.Heightmaps.Ring1.TerrainDescription;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.Heightmaps.Welding;
using Assets.Repositioning;
using Assets.Ring2.BaseEntities;
using Assets.ShaderUtils;
using Assets.Utils;
using Assets.Utils.MT;
using UnityEngine;

namespace Assets.Heightmaps.GRing
{
    public class GRing0NodeTerrainCreator : INewGRingListenersCreator
    {
        private readonly Ring1PaintingOrderGrabber _orderGrabber;
        private readonly GameObject _parentObject;
        private readonly MeshGeneratorUTProxy _meshGenerator;
        private readonly ITerrainShapeDb _terrainShapeDb;
        private readonly UnityCoordsCalculator _coordsCalculator;
        private readonly GRingSpotUpdater _spotUpdater;
        private readonly HeightArrayWeldingPack _weldingPack;
        private GRingGroundShapeProviderConfiguration _groundShapeProviderConfiguration;
        private GRingTerrainMeshProviderConfiguration _terrainMeshProviderConfiguration;

        public GRing0NodeTerrainCreator(
            Ring1PaintingOrderGrabber orderGrabber,
            GameObject parentObject,
            MeshGeneratorUTProxy meshGenerator,
            ITerrainShapeDb terrainShapeDb,
            UnityCoordsCalculator coordsCalculator, GRingSpotUpdater spotUpdater,
            HeightArrayWeldingPack weldingPack,
            GRingGroundShapeProviderConfiguration groundShapeProviderConfiguration,
            GRingTerrainMeshProviderConfiguration terrainMeshProviderConfiguration)
        {
            _orderGrabber = orderGrabber;
            _parentObject = parentObject;
            _meshGenerator = meshGenerator;
            _terrainShapeDb = terrainShapeDb;
            _coordsCalculator = coordsCalculator;
            _spotUpdater = spotUpdater;
            _groundShapeProviderConfiguration = groundShapeProviderConfiguration;
            _terrainMeshProviderConfiguration = terrainMeshProviderConfiguration;
            _weldingPack = weldingPack;
        }

        public IAsyncGRingNodeListener CreateNewListener(Ring1Node node, FlatLod flatLod)
        {
            var inGamePosition = _coordsCalculator.CalculateGlobalObjectPosition(node.Ring1Position);

            GRingTerrainMeshProvider terrainMeshProvider = new GRingTerrainMeshProvider(
                _meshGenerator,
                flatLod,
                _terrainMeshProviderConfiguration
            );
            GRingGroundShapeProvider groundShapeProvider = new GRingGroundShapeProvider(
                _terrainShapeDb,
                flatLod,
                inGamePosition,
                _spotUpdater,
                _groundShapeProviderConfiguration);

            var surfaceProvider = new GRing0SurfaceProvider(
                inGamePosition);

            GRingTripletProvider tripletProvider = new GRingTripletProvider(
                inGamePosition, Repositioner.Default, HeightDenormalizer.Default);

            return new GRingNodeTerrain(
                _orderGrabber,
                _parentObject,
                terrainMeshProvider,
                groundShapeProvider,
                surfaceProvider,
                tripletProvider,
                flatLod,
                new GRingWeldingUpdater(_weldingPack)
            );
        }
    }

    public class GRing0SurfaceProvider : IGRingSurfaceProvider
    {
        public GRing0SurfaceProvider(MyRectangle inGamePosition)
        {
        }

        public Task<List<GRingSurfaceDetail>> ProvideSurfaceDetail()
        {
            return TaskUtils.MyFromResult(new List<GRingSurfaceDetail>()
            {
                new GRingSurfaceDetail()
                {
                    ShaderName = "Custom/Terrain/Ring0",
                    UniformsWithKeywords = new UniformsWithKeywords()
                    {
                        Keywords = new ShaderKeywordSet(),
                        Uniforms = new UniformsPack()
                    }
                }
            });
        }
    }
}