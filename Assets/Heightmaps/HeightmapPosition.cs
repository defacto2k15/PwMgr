using System;
using Assets.Utils;

namespace Assets.Heightmaps
{
    class HeightmapPosition //todo delete
    {
        private readonly int _xPos;
        private readonly int _yPos;
        private readonly int _heightmapSizeWidth;
        private readonly int _heightmapSizeHeight;

        public HeightmapPosition(int xPos, int yPos, int heightmapSizeWidth, int heightmapSizeHeight)
        {
            Preconditions.Assert(xPos >= 0 && xPos < heightmapSizeWidth,
                " xPos must be between 0 and heightmapSizeWidth");
            Preconditions.Assert(yPos >= 0 && yPos < heightmapSizeHeight,
                " yPos must be between 0 and heightmapSizeHeight");

            _xPos = xPos;
            _yPos = yPos;
            _heightmapSizeWidth = heightmapSizeWidth;
            _heightmapSizeHeight = heightmapSizeHeight;
        }

        public HeightmapPosition GetPositionOnOtherHeightmap(int otherHeightmapWidth, int otherHeightmapHeight)
        {
            Preconditions.Assert(otherHeightmapHeight <= _heightmapSizeHeight,
                " Prawopodobnie margin jest barny z bardziej skomplikowanej heightmapy do mniej skomplikowanej.");
            Preconditions.Assert(otherHeightmapWidth <= _heightmapSizeWidth,
                " Prawopodobnie margin jest barny z bardziej skomplikowanej heightmapy do mniej skomplikowanej.");
            int xDivisor = _heightmapSizeWidth / otherHeightmapWidth;
            int yDivisor = _heightmapSizeHeight / otherHeightmapHeight;

            return new HeightmapPosition((int) Math.Floor((double) _xPos / xDivisor),
                (int) Math.Floor((double) _yPos / yDivisor), otherHeightmapWidth, otherHeightmapHeight);
        }

        public int X
        {
            get { return _xPos; }
        }

        public int Y
        {
            get { return _yPos; }
        }
    }
}