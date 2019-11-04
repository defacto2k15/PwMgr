using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.Heightmaps.Ring1.TerrainDescription;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.TerrainMat;
using Assets.Trees.Placement.BiomesMap;
using Assets.Utils;
using Assets.Utils.Quadtree;
using Assets.Utils.Services;
using Assets.Utils.Spatial;
using Assets.Utils.Textures;
using GeoAPI.Geometries;

namespace Assets.Heightmaps.Ring1.HeightArrayDbs
{
    public class HeightArrayDb : ITerrainHeightArrayProvider
    {
        private Dictionary<TerrainCardinalResolution, SpatialDb<MySimpleArray<float>>> _resolutionDbs =
            new Dictionary<TerrainCardinalResolution, SpatialDb<MySimpleArray<float>>>();

        public HeightArrayDb(TerrainShapeDbProxy terrainShapeDb, CommonExecutorUTProxy commonExecutor)
        {
            TerrainTextureFormatTransformator transformator = new TerrainTextureFormatTransformator(commonExecutor);
            foreach (var resolution in TerrainCardinalResolution.AllResolutions)
            {
                SpatialDbConfiguration configuration = new SpatialDbConfiguration()
                {
                    QueryingCellSize =
                        VectorUtils.FillVector2(TerrainDescriptionConstants.DetailCellSizesPerResolution[resolution])
                };
                IStoredPartsGenerator<MySimpleArray<float>> partsGenerator =
                    new HeightArrayGenerator(terrainShapeDb, transformator, resolution);

                _resolutionDbs[resolution] = new SpatialDb<MySimpleArray<float>>(partsGenerator, configuration);
            }
        }

        public TerrainHeightArrayWithUvBase RetriveTerrainHeightInfo(MyRectangle queryArea,
            TerrainCardinalResolution resolution)
        {
            var retrived = _resolutionDbs[resolution].ProvidePartsAt(queryArea).Result;
            return new TerrainHeightArrayWithUvBase()
            {
                HeightArray = retrived.CoordedPart.Part,
                UvBase = retrived.Uv
            };
        }
    }

    public class CoordedHeightArrayInTree : IHasEnvelope, ICanTestIntersect
    {
        public CoordedHeightArray CoordedHeightArray;

        public Envelope CalculateEnvelope()
        {
            return MyNetTopologySuiteUtils.ToEnvelope(CoordedHeightArray.Coords);
        }

        public bool Intersects(IGeometry geometry)
        {
            return MyNetTopologySuiteUtils.ToGeometryEnvelope(CoordedHeightArray.Coords).Intersects(geometry);
        }
    }

    public class CoordedHeightArray
    {
        public MySimpleArray<float> HeightArray;
        public MyRectangle Coords;
    }

    public class HeightArrayGenerator : IStoredPartsGenerator<MySimpleArray<float>>
    {
        private TerrainShapeDbProxy _terrainShapeDb;
        private TerrainCardinalResolution _resolution;
        private TerrainTextureFormatTransformator _transformator;

        public HeightArrayGenerator(TerrainShapeDbProxy terrainShapeDb, TerrainTextureFormatTransformator transformator,
            TerrainCardinalResolution resolution)
        {
            _terrainShapeDb = terrainShapeDb;
            _transformator = transformator;
            _resolution = resolution;
        }

        public async Task<CoordedPart<MySimpleArray<float>>> GeneratePartAsync(MyRectangle queryArea)
        {
            var heightTexture = (await _terrainShapeDb.Query(new TerrainDescriptionQuery()
            {
                QueryArea = queryArea,
                RequestedElementDetails = new List<TerrainDescriptionQueryElementDetail>()
                {
                    new TerrainDescriptionQueryElementDetail()
                    {
                        Type = TerrainDescriptionElementTypeEnum.HEIGHT_ARRAY,
                        Resolution = _resolution
                    }
                }
            })).GetElementOfType(TerrainDescriptionElementTypeEnum.HEIGHT_ARRAY);

            var encodedTexture = await _transformator.PlainToEncodedHeightTextureAsync(
                heightTexture.TokenizedElement.DetailElement.Texture);

            var heightArray = HeightmapUtils.EncodedHeightToArray(encodedTexture);

            return new CoordedPart<MySimpleArray<float>>()
            {
                Coords = queryArea,
                Part = heightArray
            };
        }
    }
}