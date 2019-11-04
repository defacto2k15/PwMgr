using System.Collections.Generic;
using Assets.Heightmaps.Ring1;
using Assets.Heightmaps.Ring1.MeshGeneration;
using Assets.Heightmaps.Ring1.Painter;
using Assets.Heightmaps.Ring1.treeNodeListener;
using Assets.Heightmaps.Ring1.TerrainDescription;
using Assets.Heightmaps.Welding;
using Assets.Repositioning;
using Assets.Ring2.GRuntimeManagementOtherThread;
using Assets.Ring2.RuntimeManagementOtherThread.Finalizer;
using UnityEngine;

namespace Assets.Heightmaps.GRing
{
    public class GCompositeRing2NodeTerrainCreator : INewGRingListenersCreator
    {
        private readonly Ring1PaintingOrderGrabber _orderGrabber;
        private readonly GameObject _parentObject;
        private readonly MeshGeneratorUTProxy _meshGenerator;
        private readonly ITerrainShapeDb _terrainShapeDb;
        private readonly UnityCoordsCalculator _coordsCalculator;
        private readonly GRing2PatchesCreatorProxy _patchesCreator;
        private readonly GRingSpotUpdater _spotUpdater;
        private readonly HeightArrayWeldingPack _weldingPack;
        private Ring2PatchStamplingOverseerFinalizer _patchStamper;
        private GRingGroundShapeProviderConfiguration _groundShapeProviderConfiguration;
        private GRingTerrainMeshProviderConfiguration _terrainMeshProviderConfiguration;

        public GCompositeRing2NodeTerrainCreator(
            Ring1PaintingOrderGrabber orderGrabber,
            GameObject parentObject,
            MeshGeneratorUTProxy meshGenerator,
            ITerrainShapeDb terrainShapeDb,
            UnityCoordsCalculator coordsCalculator,
            GRing2PatchesCreatorProxy patchesCreator,
            GRingSpotUpdater spotUpdater,
            HeightArrayWeldingPack weldingPack,
            Ring2PatchStamplingOverseerFinalizer patchStamper,
            GRingGroundShapeProviderConfiguration groundShapeProviderConfiguration,
            GRingTerrainMeshProviderConfiguration terrainMeshProviderConfiguration)
        {
            _orderGrabber = orderGrabber;
            _parentObject = parentObject;
            _meshGenerator = meshGenerator;
            _terrainShapeDb = terrainShapeDb;
            _coordsCalculator = coordsCalculator;
            _patchesCreator = patchesCreator;
            _spotUpdater = spotUpdater;
            _weldingPack = weldingPack;
            _patchStamper = patchStamper;
            _groundShapeProviderConfiguration = groundShapeProviderConfiguration;
            _terrainMeshProviderConfiguration = terrainMeshProviderConfiguration;
        }

        public IAsyncGRingNodeListener CreateNewListener(Ring1Node node, FlatLod flatLod)
        {
            var inGamePosition = _coordsCalculator.CalculateGlobalObjectPosition(node.Ring1Position);

            GRingTerrainMeshProvider terrainMeshProvider = new GRingTerrainMeshProvider(
                _meshGenerator,
                flatLod,
                _terrainMeshProviderConfiguration
            );
            GRingGroundShapeProvider groundShapeProvider =
                new GRingGroundShapeProvider(
                    _terrainShapeDb,
                    flatLod,
                    inGamePosition,
                    _spotUpdater,
                    _groundShapeProviderConfiguration);

            IGRingSurfaceProvider surfaceProvider = new CompositeGRingSurfaceProvider(new List<IGRingSurfaceProvider>()
            {
                new GRing2SurfaceProvider(_patchesCreator, inGamePosition, flatLod),
                new GStampedRing2SurfaceProvider(_patchesCreator, inGamePosition, _patchStamper, flatLod),
            });

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
}