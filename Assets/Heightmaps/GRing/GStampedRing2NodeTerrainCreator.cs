﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Assets.Heightmaps.Ring1;
using Assets.Heightmaps.Ring1.MeshGeneration;
using Assets.Heightmaps.Ring1.Painter;
using Assets.Heightmaps.Ring1.treeNodeListener;
using Assets.Heightmaps.Ring1.TerrainDescription;
using Assets.Heightmaps.Welding;
using Assets.Repositioning;
using Assets.Ring2.GRuntimeManagementOtherThread;
using Assets.Ring2.RuntimeManagementOtherThread.Finalizer;
using Assets.Utils;
using UnityEngine;

namespace Assets.Heightmaps.GRing
{
    public class GStampedRing2NodeTerrainCreator : INewGRingListenersCreator
    {
        private readonly Ring1PaintingOrderGrabber _orderGrabber;
        private readonly GameObject _parentObject;
        private readonly MeshGeneratorUTProxy _meshGenerator;
        private readonly ITerrainShapeDb _terrainShapeDb;
        private readonly UnityCoordsCalculator _coordsCalculator;
        private readonly GRing2PatchesCreatorProxy _patchesCreator;
        private readonly Ring2PatchStamplingOverseerFinalizer _patchStamper;
        private readonly GRingSpotUpdater _spotUpdater;
        private readonly HeightArrayWeldingPack _weldingPack;
        private readonly GRingGroundShapeProviderConfiguration _groundShapeProviderConfiguration;
        private readonly GRingTerrainMeshProviderConfiguration _terrainMeshProviderConfiguration;

        public GStampedRing2NodeTerrainCreator(
            Ring1PaintingOrderGrabber orderGrabber,
            GameObject parentObject,
            MeshGeneratorUTProxy meshGenerator,
            ITerrainShapeDb terrainShapeDb,
            UnityCoordsCalculator coordsCalculator,
            GRing2PatchesCreatorProxy patchesCreator,
            Ring2PatchStamplingOverseerFinalizer patchStamper,
            GRingSpotUpdater spotUpdater,
            HeightArrayWeldingPack weldingPack,
            GRingGroundShapeProviderConfiguration groundShapeProviderConfiguration,
            GRingTerrainMeshProviderConfiguration terrainMeshProviderConfiguration)
        {
            _orderGrabber = orderGrabber;
            _parentObject = parentObject;
            _meshGenerator = meshGenerator;
            _terrainShapeDb = terrainShapeDb;
            _coordsCalculator = coordsCalculator;
            _patchesCreator = patchesCreator;
            _patchStamper = patchStamper;
            _spotUpdater = spotUpdater;
            _weldingPack = weldingPack;
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
            GRingGroundShapeProvider groundShapeProvider = new GRingGroundShapeProvider(
                _terrainShapeDb,
                flatLod,
                inGamePosition,
                _spotUpdater,
                _groundShapeProviderConfiguration);

            GStampedRing2SurfaceProvider surfaceProvider = new GStampedRing2SurfaceProvider(
                _patchesCreator,
                inGamePosition,
                _patchStamper,
                flatLod);

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