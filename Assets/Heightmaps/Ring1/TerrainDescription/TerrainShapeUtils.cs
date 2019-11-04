using Assets.Heightmaps.Ring1.valTypes;
using Assets.Ring2;
using Assets.Utils;
using UnityEngine;

namespace Assets.Heightmaps.Ring1.TerrainDescription
{
    public static class TerrainShapeUtils
    {
        public static MyRectangle ComputeUvOfSubElement(MyRectangle subElement, MyRectangle mainElement)
        {
            Vector2 startOffset = new Vector2(subElement.X - mainElement.X, subElement.Y - mainElement.Y);
            Vector2 endOffset = new Vector2(subElement.X + subElement.Width - mainElement.X,
                subElement.Y + subElement.Height - mainElement.Y);
            Vector2 widths = endOffset - startOffset;

            Vector2 uvStart = new Vector2(startOffset.x / mainElement.Width, startOffset.y / mainElement.Height);
            var uvWidth = new Vector2(widths.x / mainElement.Width, widths.y / mainElement.Height);
            return new MyRectangle(uvStart.x, uvStart.y, uvWidth.x, uvWidth.y);
        }

        public static IntVector2 RetriveTextureSize(MyRectangle area, TerrainCardinalResolution resolution)
        {
            return new IntVector2
            (
                Mathf.RoundToInt(area.Width * resolution.DetailResolution.PixelsPerMeter) + 1,
                Mathf.RoundToInt(area.Width * resolution.DetailResolution.PixelsPerMeter) + 1
            );
        }

        public static MyRectangle GetAlignedTerrainArea(MyRectangle queryArea,
            TerrainCardinalResolution cardinalResolution, int terrainDetailImageSideResolution)
        {
            var alignLength = cardinalResolution.DetailResolution.MetersPerPixel * terrainDetailImageSideResolution;
            var startX = Mathf.FloorToInt(queryArea.X / (float) alignLength) * alignLength;
            var startY = Mathf.FloorToInt(queryArea.Y / (float) alignLength) * alignLength;

            return new MyRectangle(startX, startY, alignLength, alignLength);
        }

        public static IntVector2 GetGriddedTerrainArea(MyRectangle alignedArea,
            TerrainCardinalResolution cardinalResolution, int terrainDetailImageSideResolution)
        {
            var alignLength = cardinalResolution.DetailResolution.MetersPerPixel * terrainDetailImageSideResolution;
            var startX = Mathf.RoundToInt(alignedArea.X / (float) alignLength);
            var startY = Mathf.RoundToInt(alignedArea.Y / (float) alignLength);
            return new IntVector2(startX, startY);
        }

        public static MyRectangle GetAlignedTerrainArea(IntVector2 griddedArea,
            TerrainCardinalResolution cardinalResolution, int terrainDetailImageSideResolution)
        {
            var alignLength = cardinalResolution.DetailResolution.MetersPerPixel * terrainDetailImageSideResolution;
            return new MyRectangle(griddedArea.X*alignLength, griddedArea.Y*alignLength, alignLength, alignLength);
        }

    }
}