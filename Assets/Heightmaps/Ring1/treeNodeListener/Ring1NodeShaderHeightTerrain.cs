using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Assets.Heightmaps.Ring1.MeshGeneration;
using Assets.Heightmaps.Ring1.Painter;
using Assets.Heightmaps.Ring1.TerrainDescription;
using Assets.Heightmaps.Ring1.VisibilityTexture;
using Assets.MeshGeneration;
using Assets.Ring2.BaseEntities;
using Assets.ShaderUtils;
using Assets.TerrainMat.Stain;
using Assets.Utils;
using UnityEngine;

namespace Assets.Heightmaps.Ring1.treeNodeListener
{
    public class Ring1NodeShaderHeightTerrain : Ring1NodeTerrain
    {
        private readonly Texture2D _visibilityTexture;
        private readonly UnityCoordsCalculator _coordsCalculator;
        private TerrainShapeDbProxy _terrainShapeDb;
        private readonly MeshGeneratorUTProxy _meshGenerator;
        private StainTerrainServiceProxy _stainTerrainServiceProxy;

        public Ring1NodeShaderHeightTerrain(
            Ring1Node ring1Node,
            Ring1VisibilityTextureChangeGrabber ring1VisibilityTextureChangeGrabber,
            Texture2D visibilityTexture,
            GameObject terrainParentObject,
            UnityCoordsCalculator coordsCalculator,
            Ring1PaintingOrderGrabber orderGrabber,
            TerrainShapeDbProxy terrainShapeDb,
            MeshGeneratorUTProxy meshGenerator,
            StainTerrainServiceProxy stainTerrainServiceProxy)
            : base(ring1Node, ring1VisibilityTextureChangeGrabber, orderGrabber, terrainParentObject)
        {
            _visibilityTexture = visibilityTexture;
            _coordsCalculator = coordsCalculator;
            _terrainShapeDb = terrainShapeDb;
            _meshGenerator = meshGenerator;
            _stainTerrainServiceProxy = stainTerrainServiceProxy;
        }

        protected override async Task<UInt32> CreateTerrainAsync()
        {
            //todo DOPISZ REAGOWANIE NA ZNIKNIeCIE elementu terenu - trzeba odeslac do TerrainShapeDb informacje o odblokowaniu tekstury!!
            var creationDetails = CalculateQueryDetails(Ring1Node.QuadLodLevel);
            var gameObjectMeshResolution = creationDetails.TerrainMeshResolution; //new IntVector2(241, 241); 
            var inGamePosition = _coordsCalculator.CalculateGlobalObjectPosition(Ring1Node.Ring1Position);

            var mesh = await _meshGenerator.AddOrder(
                () => PlaneGenerator.CreateFlatPlaneMesh(gameObjectMeshResolution.X, gameObjectMeshResolution.Y));

            var debugTestDivider = 240f;

            var triplet = new MyTransformTriplet(
                new Vector3(inGamePosition.X / debugTestDivider, 0, inGamePosition.Y / debugTestDivider), Vector3.zero,
                new Vector3(inGamePosition.Width / debugTestDivider, 20,
                    inGamePosition.Height / debugTestDivider)); //todo!

            var stainResourceWithCoords = await _stainTerrainServiceProxy.RetriveResource(inGamePosition);

            UniformsPack pack = await CreateUniformsPack(creationDetails.QueryElementDetails, creationDetails.Pack,
                stainResourceWithCoords);
            var creationTemplate = new Ring1GroundPieceCreationTemplate()
            {
                Name = "Ring1 terrain object " + inGamePosition.ToString(),
                ParentGameObject = ParentObject,
                PieceMesh = mesh,
                ShaderName = "Custom/Terrain/Ring1",
                TransformTriplet = triplet,
                Uniforms = pack,
                ShaderKeywordSet = creationDetails.KeywordSet
            };
            var objectId = OrderGrabber.AddCreationOrder(creationTemplate);
            return objectId;
        }

        private async Task<UniformsPack> CreateUniformsPack(
            List<TerrainDescriptionQueryElementDetail> queryElementDetails,
            UniformsPack uniformsPack,
            StainTerrainResourceWithCoords stainResourceWithCoords)
        {
            var terrainDetailAreaPosition = _coordsCalculator.CalculateGlobalObjectPosition(Ring1Node.Ring1Position);

            var terrainOutput = await _terrainShapeDb.Query(new TerrainDescriptionQuery()
            {
                QueryArea = terrainDetailAreaPosition,
                RequestedElementDetails = queryElementDetails
            });

            var heightmapTexture = terrainOutput.GetElementOfType(TerrainDescriptionElementTypeEnum.HEIGHT_ARRAY);
            uniformsPack.SetTexture("_HeightmapTex",
                heightmapTexture.TokenizedElement.DetailElement.Texture.Texture);
            uniformsPack.SetUniform("_HeightmapUv", heightmapTexture.UvBase.ToVector4());

            if (terrainOutput.HasElementOfType(TerrainDescriptionElementTypeEnum.NORMAL_ARRAY))
            {
                var normalAsTexture = terrainOutput.GetElementOfType(TerrainDescriptionElementTypeEnum.NORMAL_ARRAY);
                uniformsPack.SetTexture("_NormalmapTex",
                    normalAsTexture.TokenizedElement.DetailElement.Texture.Texture);
                uniformsPack.SetUniform("_NormalmapUv", normalAsTexture.UvBase.ToVector4());
            }

            uniformsPack.SetTexture("_PaletteTex", stainResourceWithCoords.Resource.TerrainPaletteTexture);
            uniformsPack.SetTexture("_PaletteIndexTex", stainResourceWithCoords.Resource.PaletteIndexTexture);
            uniformsPack.SetTexture("_ControlTex", stainResourceWithCoords.Resource.ControlTexture);
            uniformsPack.SetUniform("_TerrainStainUv", stainResourceWithCoords.Coords.ToVector4());

            uniformsPack.SetUniform("_TerrainTextureSize", stainResourceWithCoords.Resource.TerrainTextureSize);
            uniformsPack.SetUniform("_PaletteMaxIndex", stainResourceWithCoords.Resource.PaletteMaxIndex);
            return uniformsPack;
        }

        private GroundPieceCreationDetails CalculateQueryDetails(int quadLodLevel)
        {
            TerrainCardinalResolution detailResolution = TerrainCardinalResolution.MIN_RESOLUTION;
            int meshLodPower = 0;
            bool directNormalCalculation = false;

            if (quadLodLevel == 1)
            {
                detailResolution = TerrainCardinalResolution.MIN_RESOLUTION;
                meshLodPower = 3;
            }
            else if (quadLodLevel == 2)
            {
                detailResolution = TerrainCardinalResolution.MIN_RESOLUTION;
                meshLodPower = 3;
            }
            else if (quadLodLevel == 3)
            {
                detailResolution = TerrainCardinalResolution.MIN_RESOLUTION;
                meshLodPower = 2;
            }
            else if (quadLodLevel == 4)
            {
                detailResolution = TerrainCardinalResolution.MID_RESOLUTION;
                meshLodPower = 2;
            }
            else if (quadLodLevel == 5)
            {
                detailResolution = TerrainCardinalResolution.MID_RESOLUTION;
                meshLodPower = 1;
            }
            else if (quadLodLevel == 6)
            {
                detailResolution = TerrainCardinalResolution.MID_RESOLUTION;
                meshLodPower = 0;
                directNormalCalculation = true;
            }
            else if (quadLodLevel == 7)
            {
                detailResolution = TerrainCardinalResolution.MAX_RESOLUTION;
                meshLodPower = 0;
                directNormalCalculation = true;
            }
            //else if (lodLevel == 8)
            //{
            //    detailResolution = TerrainCardinalResolution.MAX_RESOLUTION;
            //    meshLodPower = 0;
            //}
            else
            {
                Preconditions.Fail("Unsupported lod level: " + quadLodLevel);
            }

            int baseMeshResolution = 240;
            int finalMeshResolution = Mathf.RoundToInt(baseMeshResolution / Mathf.Pow(2, meshLodPower));
            IntVector2 terrainMeshResolution = new IntVector2(finalMeshResolution + 1, finalMeshResolution + 1);

            var queryElementDetails = new List<TerrainDescriptionQueryElementDetail>()
            {
                new TerrainDescriptionQueryElementDetail()
                {
                    Resolution = detailResolution,
                    Type = TerrainDescriptionElementTypeEnum.HEIGHT_ARRAY
                },
            };
            if (!directNormalCalculation)
            {
                queryElementDetails.Add(
                    new TerrainDescriptionQueryElementDetail()
                    {
                        Resolution = detailResolution,
                        Type = TerrainDescriptionElementTypeEnum.NORMAL_ARRAY
                    }
                );
            }


            var pack = new UniformsPack();
            pack.SetUniform("_LodLevel", quadLodLevel);
            pack.SetUniform("_HeightmapLodOffset", meshLodPower);

            List<string> keywords = new List<string>();
            if (directNormalCalculation)
            {
                keywords.Add("DYNAMIC_NORMAL_GENERATION");
            }

            return new GroundPieceCreationDetails()
            {
                KeywordSet = new ShaderKeywordSet(keywords),
                QueryElementDetails = queryElementDetails,
                Pack = pack,
                TerrainMeshResolution = terrainMeshResolution
            };
        }

        private class GroundPieceCreationDetails
        {
            public List<TerrainDescriptionQueryElementDetail> QueryElementDetails;
            public ShaderKeywordSet KeywordSet;
            public UniformsPack Pack;
            public IntVector2 TerrainMeshResolution;
        }
    }
}