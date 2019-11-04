using System;
using System.Collections.Generic;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.Utils;
using Assets.Utils.ArrayUtils;

namespace Assets.Heightmaps.Ring1.VisibilityTexture
{
    public class Ring1VisibilityTextureChangeGrabber
    {
        private int _sidePixelsCount;
        private bool[,] _lastFrameVisibility;
        private bool[,] _currentFrameVisibility;

        public Ring1VisibilityTextureChangeGrabber(int sidePixelsCount = 16)
        {
            _sidePixelsCount = sidePixelsCount;
            _lastFrameVisibility = new bool[_sidePixelsCount, _sidePixelsCount];
            _currentFrameVisibility = new bool[_sidePixelsCount, _sidePixelsCount];
            MyArrayUtils.PopulateArray(_lastFrameVisibility, false);
            MyArrayUtils.PopulateArray(_currentFrameVisibility, false);
        }

        public void SetVisible(MyRectangle ring1Position)
        {
            var positions = RetrivePixelPositions(ring1Position);

            for (int y = positions.Y; y < positions.Y + positions.Height; y++)
            {
                for (int x = positions.X; x < positions.X + positions.Width; x++)
                {
                    if (!_lastFrameVisibility[x, y])
                    {
                        _currentFrameVisibility[x, y] = true;
                    }
                }
            }
        }

        private Positions2D<int> RetrivePixelPositions(MyRectangle ring1Position)
        {
            int baseX = (int) Math.Floor(ring1Position.X * _sidePixelsCount);
            int baseY = (int) Math.Floor(ring1Position.Y * _sidePixelsCount);
            int width = (int) Math.Floor(ring1Position.Width * _sidePixelsCount);
            int height = (int) Math.Floor(ring1Position.Height * _sidePixelsCount);

            return new Positions2D<int>(baseX, baseY, width, height);
        }

        public Ring1VisibilityTextureDelta RetriveVisibilityChanges()
        {
            var changes = new Dictionary<IntVector2, bool>();
            for (int x = 0; x < _lastFrameVisibility.GetLength(0); x++)
            {
                for (int y = 0; y < _lastFrameVisibility.GetLength(1); y++)
                {
                    var last = _lastFrameVisibility[x, y];
                    var current = _currentFrameVisibility[x, y];
                    if (last != current)
                    {
                        changes[new IntVector2(x, y)] = current;
                    }
                }
            }

            _lastFrameVisibility = _currentFrameVisibility;
            _currentFrameVisibility = MyArrayUtils.DeepClone(_lastFrameVisibility);
            changes = new Dictionary<IntVector2, bool>();
            return new Ring1VisibilityTextureDelta(changes);
        }
    }
}