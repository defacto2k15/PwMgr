using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.Heightmaps.Ring1;
using Assets.Heightmaps.Ring1.Painter;
using Assets.Heightmaps.Ring1.treeNodeListener;
using Assets.Heightmaps.Ring1.TerrainDescription;
using Assets.Heightmaps.Ring1.VisibilityTexture;
using Assets.Heightmaps.Welding;
using Assets.Ring2.BaseEntities;
using Assets.ShaderUtils;
using Assets.Utils;
using Assets.Utils.MT;
using UnityEngine;

namespace Assets.Heightmaps.GRing
{
    public class GRingNodeTerrain : ImprovedAsyncGRingNodeListener
    {
        //todo: reimplement visibilityTexture?
        private readonly Ring1PaintingOrderGrabber _orderGrabber;

        private readonly GameObject _parentObject;

        private readonly GRingTerrainMeshProvider _terrainMeshProvider;
        private readonly GRingGroundShapeProvider _groundShapeProvider;
        private readonly IGRingSurfaceProvider _surfaceProvider;
        private readonly GRingTripletProvider _tripletProvider;
        private readonly FlatLod _flatLod;
        private readonly GRingWeldingUpdater _weldingUpdater;

        private GRingTerrainId _terrainId = null;
        private IGroundShapeToken _groundShapeToken = null;

        public GRingNodeTerrain(
            Ring1PaintingOrderGrabber orderGrabber,
            GameObject parentObject,
            GRingTerrainMeshProvider terrainMeshProvider,
            GRingGroundShapeProvider groundShapeProvider,
            IGRingSurfaceProvider surfaceProvider,
            GRingTripletProvider tripletProvider,
            FlatLod flatLod, GRingWeldingUpdater weldingUpdater)
        {
            _orderGrabber = orderGrabber;
            _parentObject = parentObject;
            _terrainMeshProvider = terrainMeshProvider;
            _groundShapeProvider = groundShapeProvider;
            _surfaceProvider = surfaceProvider;
            _tripletProvider = tripletProvider;
            _flatLod = flatLod;
            _weldingUpdater = weldingUpdater;
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
                if (_weldingUpdater.WeldsActive)
                {
                    _weldingUpdater.RemoveTerrain();
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
            if (_weldingUpdater.WeldsActive)
            {
                _weldingUpdater.RemoveTerrain();
            }
            return TaskUtils.EmptyCompleted(); //todo
        }

        private async Task<GRingTerrainId> CreateTerrainAsync()
        {
            //Debug.Log("G34 Start");
            var triplet = _tripletProvider.ProvideTriplet();
            var meshDetails = await _terrainMeshProvider.ProvideMeshDetailsAsync();
            //Debug.Log("G35 Mesh detail");
            var groundShapeDetails = await _groundShapeProvider.ProvideGroundTextureDetail();
            //Debug.Log("G36 Ground shape detail");
            var surfaceProviderDetails = await _surfaceProvider.ProvideSurfaceDetail();
            var weldingDetails =
                _weldingUpdater.ProvideWeldingDetails(groundShapeDetails.HeightDetailOutput, meshDetails.HeightmapLod);

            _groundShapeToken = groundShapeDetails.GroundShapeToken;

            var creationTemplatesList = new List<Ring1GroundPieceCreationTemplate>();
            int layerIndex = surfaceProviderDetails.Count;
            foreach (var surfaceDetail in surfaceProviderDetails)
            {
                UniformsPack pack = new UniformsPack();
                pack.MergeWith(meshDetails.Uniforms);
                pack.MergeWith(groundShapeDetails.Uniforms);
                pack.MergeWith(surfaceDetail.UniformsWithKeywords.Uniforms);
                pack.MergeWith(weldingDetails.UniformsPack);

                var keywordsSet = ShaderKeywordSet.Merge(groundShapeDetails.ShaderKeywordSet,
                    surfaceDetail.UniformsWithKeywords.Keywords);

                var layerTriplet = triplet.Clone();
                var oldPosition = layerTriplet.Position;
                layerTriplet.Position = new Vector3(oldPosition.x, oldPosition.y + layerIndex / 2000f, oldPosition.z);

                creationTemplatesList.Add(
                    new Ring1GroundPieceCreationTemplate()
                    {
                        Name =
                            $"TerrainElementLayer l:{layerIndex} ql:{_flatLod.SourceQuadLod} fl:{_flatLod.ScalarValue} wd {weldingDetails.WeldingIndex}",
                        ParentGameObject = _parentObject,
                        PieceMesh = meshDetails.Mesh,
                        TransformTriplet = layerTriplet,
                        ShaderName = surfaceDetail.ShaderName,
                        ShaderKeywordSet = keywordsSet,
                        Uniforms = pack,
                        Modifier = weldingDetails.Modifier
                    });

                layerIndex--;
            }

            return new GRingTerrainId()
            {
                ElementsIds = creationTemplatesList.Select(c => _orderGrabber.AddCreationOrder(c)).ToList()
            };
        }
    }

    public class GRingTerrainId
    {
        public List<UInt32> ElementsIds;
    }

    //////////////////////////

    public class GRingWeldingUpdater
    {
        private HeightArrayWeldingPack _weldingPack;
        private int _weldingTerrainIndex = -1;

        public GRingWeldingUpdater(HeightArrayWeldingPack weldingPack)
        {
            _weldingPack = weldingPack;
        }

        public bool WeldsActive => _weldingTerrainIndex != -1;

        public GRingWeldingDetail ProvideWeldingDetails(TerrainDetailElementOutput heightmapDetail, int heightmapOffset)
        {
            if (!_weldingPack.WeldingEnabled)
            {
                return new GRingWeldingDetail()
                {
                    Modifier = new Ring1GroundPieceModifier(),
                    UniformsPack = new UniformsPack()
                };
            }


            if (_weldingPack == null)
            {
                Debug.LogError("WARNING. Welding pack not set!!. Only for testing!!");
                return new GRingWeldingDetail()
                {
                    Modifier = new Ring1GroundPieceModifier(),
                    UniformsPack = new UniformsPack()
                };
            }
            var groundPieceModifier = new Ring1GroundPieceModifier();

            _weldingTerrainIndex = _weldingPack.RegisterTerrain(new WeldingRegistrationTerrain()
            {
                Texture = heightmapDetail.TokenizedElement.DetailElement.Texture,
                DetailGlobalArea = heightmapDetail.TokenizedElement.DetailElement.DetailArea,
                Resolution = heightmapDetail.TokenizedElement.DetailElement.Resolution,
                TerrainLod = heightmapOffset,
                UvCoordsPositions2D = heightmapDetail.UvBase
            }, (t) => groundPieceModifier.ModifyMaterial((material) =>
            {
                t.SetToMaterial(material);
            }));

            var pack = new UniformsPack();
            pack.SetTexture("_WeldTexture", _weldingPack.WeldTexture.Texture);

            var emptyUvs = new TerrainWeldUvs();
            pack.SetUniform("_LeftWeldTextureUvRange", emptyUvs.LeftUv);
            pack.SetUniform("_RightWeldTextureUvRange", emptyUvs.RightUv);
            pack.SetUniform("_TopWeldTextureUvRange", emptyUvs.TopUv);
            pack.SetUniform("_BottomWeldTextureUvRange", emptyUvs.BottomUv);

            return new GRingWeldingDetail()
            {
                UniformsPack = pack,
                Modifier = groundPieceModifier,
                WeldingIndex = _weldingTerrainIndex
            };
        }

        public void RemoveTerrain()
        {
            _weldingPack.RemoveTerrain(_weldingTerrainIndex);
            _weldingTerrainIndex = -1;
        }
    }

    public class GRingWeldingDetail
    {
        public Ring1GroundPieceModifier Modifier;
        public UniformsPack UniformsPack;
        public int WeldingIndex;
    }
}