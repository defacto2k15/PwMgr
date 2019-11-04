using System.Collections.Generic;
using Assets.Heightmaps;
using Assets.Heightmaps.Ring1;
using Assets.Heightmaps.Ring1.TerrainDescription;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.Utils;
using Assets.Utils.ArrayUtils;
using Assets.Utils.Services;
using UnityEngine;

namespace Assets.Roads.Pathfinding.TerrainPath
{
    public class TerrainSamplingSource
    {
        private readonly TerrainShapeDbProxy _terrainShapeDb;
        private Dictionary<IntVector2, HeightmapArray> _heightArraysDict = new Dictionary<IntVector2, HeightmapArray>();
        private IntVector2 _heightArraySize = new IntVector2(90, 90);
        private readonly GratePositionCalculator _gratePositionCalculator;

        public TerrainSamplingSource(TerrainShapeDbProxy terrainShapeDb,
            GratePositionCalculator gratePositionCalculator)
        {
            _terrainShapeDb = terrainShapeDb;
            _gratePositionCalculator = gratePositionCalculator;
        }

        private IntVector2 CalculateStartCoordsOfArray(Vector2 globalPosition)
        {
            var xPos = Mathf.FloorToInt(globalPosition.x / _heightArraySize.X) * _heightArraySize.X;
            var yPos = Mathf.FloorToInt(globalPosition.y / _heightArraySize.Y) * _heightArraySize.Y;
            return new IntVector2(xPos, yPos);
        }

        private HeightmapArrayWithCoords RetriveHeightArray(Vector2 globalPosition)
        {
            var startCoordsOfArray = CalculateStartCoordsOfArray(globalPosition);
            var arrayCoords = CalculateCoordsOfHeightmapArray(startCoordsOfArray.ToFloatVec());
            if (!_heightArraysDict.ContainsKey(startCoordsOfArray))
            {
                _heightArraysDict.Add(startCoordsOfArray, LoadTexture(arrayCoords));
            }
            return new HeightmapArrayWithCoords()
            {
                Array = _heightArraysDict[startCoordsOfArray],
                Coords = arrayCoords
            };
        }

        private MyRectangle CalculateCoordsOfHeightmapArray(Vector2 startPosition)
        {
            return new MyRectangle(startPosition.x, startPosition.y, _heightArraySize.X, _heightArraySize.Y);
        }

        public HeightmapArray LoadTexture(MyRectangle arrayCoords)
        {
            var queriedElement = _terrainShapeDb.Query(new TerrainDescriptionQuery()
            {
                QueryArea = arrayCoords,
                RequestedElementDetails = new List<TerrainDescriptionQueryElementDetail>()
                {
                    new TerrainDescriptionQueryElementDetail()
                    {
                        Type = TerrainDescriptionElementTypeEnum.HEIGHT_ARRAY,
                        Resolution = TerrainCardinalResolution.MAX_RESOLUTION
                    }
                }
            }).Result;

            var textureWithSize = queriedElement.GetElementOfType(TerrainDescriptionElementTypeEnum.HEIGHT_ARRAY)
                .TokenizedElement.DetailElement.Texture;

            var transformer = new TerrainTextureFormatTransformator(new CommonExecutorUTProxy()); //todo
            var encodedTerrainTexture = transformer.PlainToEncodedHeightTextureAsync(textureWithSize).Result;

            return HeightmapUtils.CreateHeightmapArrayFromTexture(encodedTerrainTexture);
        }


        public float SamplePosition(Vector2 globalPosition)
        {
            var heightArrayWithCoords = RetriveHeightArray(globalPosition);
            var terrainUv = RectangleUtils.CalculateSubelementUv(heightArrayWithCoords.Coords, globalPosition);
            var toReturn = MyArrayUtils.GetValueWith01Uv(heightArrayWithCoords.Array.HeightmapAsArray, terrainUv);
            return toReturn;
        }

        public float SamplePosition(IntVector2 gratePosition)
        {
            return SamplePosition(_gratePositionCalculator.ToGlobalPosition(gratePosition));
        }

        private class HeightmapArrayWithCoords
        {
            public HeightmapArray Array;
            public MyRectangle Coords;
        }
    }
}