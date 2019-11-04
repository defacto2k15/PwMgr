using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Assets.Heightmaps.Preparment.Simplyfying;
using Assets.Heightmaps.Preparment.Slicing;
using Assets.Heightmaps.Ring1.MeshGeneration;
using Assets.Heightmaps.Ring1.Painter;
using Assets.Heightmaps.Ring1.TerrainDescription;
using Assets.Heightmaps.Ring1.VisibilityTexture;
using Assets.MeshGeneration;
using Assets.ShaderUtils;
using Assets.Utils;
using UnityEngine;

namespace Assets.Heightmaps.Ring1.treeNodeListener
{
    public class Ring1NodeDirectHeightTerrain : Ring1NodeTerrain //todo remove
    {
        private TerrainShapeDbProxy _terrainShapeDb;
        private readonly Texture2D _visibilityTexture;
        private readonly UnityCoordsCalculator _coordsCalculator;
        private readonly HeightmapArray _globalHeightmap;
        private readonly MeshGeneratorUTProxy _meshGenerator;

        private HeightmapSimplyfyer simplyfyer = new HeightmapSimplyfyer();
        private TerrainSlicer slicer = new TerrainSlicer();

        public Ring1NodeDirectHeightTerrain(
            Ring1Node ring1Node,
            Ring1VisibilityTextureChangeGrabber ring1VisibilityTextureChangeGrabber,
            Texture2D visibilityTexture,
            GameObject terrainParentObject,
            UnityCoordsCalculator coordsCalculator,
            Ring1PaintingOrderGrabber orderGrabber,
            TerrainShapeDbProxy terrainShapeDb,
            HeightmapArray globalHeightmap,
            MeshGeneratorUTProxy meshGenerator
        )
            : base(ring1Node, ring1VisibilityTextureChangeGrabber, orderGrabber, terrainParentObject)
        {
            _visibilityTexture = visibilityTexture;
            _coordsCalculator = coordsCalculator;
            _globalHeightmap = globalHeightmap;
            _meshGenerator = meshGenerator;
            _terrainShapeDb = terrainShapeDb;
        }

        protected override async Task<UInt32> CreateTerrainAsync()
        {
            var ring1Position = Ring1Node.Ring1Position;

            var xOffset = _globalHeightmap.Width * ring1Position.X;
            var yOffset = _globalHeightmap.Height * ring1Position.Y;
            var width = _globalHeightmap.Width * ring1Position.Width;
            var height = _globalHeightmap.Height * ring1Position.Height;

            var subArray = slicer.GetSubarrayWithEmptyMarginsSafe(_globalHeightmap.HeightmapAsArray, (int) xOffset,
                (int) yOffset,
                (int) width, (int) height);

            Point2D newGameObjectSize = new Point2D(33, 33);

            var simplifiedMap = simplyfyer.SimplyfyHeightmap(new HeightmapArray(subArray), newGameObjectSize.X - 1,
                newGameObjectSize.Y - 1);

            var mesh = await _meshGenerator.AddOrder(
                () => PlaneGenerator.CreatePlaneMesh(1, 1, simplifiedMap.HeightmapAsArray));

            UniformsPack pack = await CreateUniformsPack(Ring1Node);

            var inGamePosition = _coordsCalculator.CalculateGlobalObjectPosition(Ring1Node.Ring1Position);

            var triplet = new MyTransformTriplet(new Vector3(inGamePosition.X, 0, inGamePosition.Y), Vector3.zero,
                new Vector3(inGamePosition.Width, 1, inGamePosition.Height));

            var creationTemplate = new Ring1GroundPieceCreationTemplate()
            {
                Name = "Ring1 terrain object " + inGamePosition.ToString(),
                ParentGameObject = ParentObject,
                PieceMesh = mesh,
                ShaderName = "Custom/Terrain/Ring1DirectHeight",
                TransformTriplet = triplet,
                Uniforms = pack
            };
            var objectId = OrderGrabber.AddCreationOrder(creationTemplate);
            return (objectId);
        }

        private async Task<UniformsPack> CreateUniformsPack(Ring1Node ring1Node)
        {
            UniformsPack pack = new UniformsPack();


            var terrainOutput = await _terrainShapeDb.Query(new TerrainDescriptionQuery()
            {
                QueryArea = Ring1Node.Ring1Position,
                RequestedElementDetails = new List<TerrainDescriptionQueryElementDetail>()
                {
                    new TerrainDescriptionQueryElementDetail()
                    {
                        //PixelsPerMeter = 10, //todo
                        Resolution = TerrainCardinalResolution.FromRing1NodeLodLevel(Ring1Node.QuadLodLevel),
                        Type = TerrainDescriptionElementTypeEnum.HEIGHT_ARRAY
                    },
                    new TerrainDescriptionQueryElementDetail()
                    {
                        //PixelsPerMeter = 10, //todo
                        Resolution = TerrainCardinalResolution.FromRing1NodeLodLevel(Ring1Node.QuadLodLevel),
                        Type = TerrainDescriptionElementTypeEnum.TESSALATION_REQ_ARRAY
                    },
                }
            });

            var uvPosition =
                _coordsCalculator.CalculateUvPosition(Ring1Node.Ring1Position); //_submapTextures.UvPosition;
            var heightmapTexture = terrainOutput.GetElementOfType(TerrainDescriptionElementTypeEnum.HEIGHT_ARRAY);
            var tessalationTexture =
                terrainOutput.GetElementOfType(TerrainDescriptionElementTypeEnum.TESSALATION_REQ_ARRAY);

            pack.SetUniform("_TerrainTextureUvPositions",
                new Vector4(uvPosition.X, uvPosition.Y, uvPosition.Width, uvPosition.Height));
            pack.SetTexture("_HeightmapTex", heightmapTexture.TokenizedElement.DetailElement.Texture.Texture);
            pack.SetTexture("_TessalationTex", tessalationTexture.TokenizedElement.DetailElement.Texture.Texture);

            pack.SetTexture("_LodTexture", _visibilityTexture);
            pack.SetUniform("_MaxHeight", 100);
            pack.SetUniform("_LodTextureUvOffset",
                _coordsCalculator.CalculateTextureUvLodOffset(ring1Node.Ring1Position));
            pack.SetUniform("_BaseTrianglesCount",
                _coordsCalculator.CalculateGameObjectSize(ring1Node.Ring1Position).X - 1);
            return pack;
        }
    }
}