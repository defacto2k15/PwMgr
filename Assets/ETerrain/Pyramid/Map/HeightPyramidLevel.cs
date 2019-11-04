namespace Assets.ETerrain.Pyramid.Map
{
    public enum HeightPyramidLevel
    {
        Top = 0, Mid = 1, Bottom = 2
    }

    public static class PyramidSegmentLevelUtils
    {
        public static int GetIndex(this HeightPyramidLevel level)
        {
            return (int) level;
        }

        public static HeightPyramidLevel? GetLowerLevel(this HeightPyramidLevel level)
        {
            switch (level)
            {
                case HeightPyramidLevel.Bottom:
                    return null;
                case HeightPyramidLevel.Mid:
                    return HeightPyramidLevel.Bottom;
                case HeightPyramidLevel.Top:
                    return HeightPyramidLevel.Mid;
            }

            return null;
        }

        public static HeightPyramidLevel? GetHigherLevel(this HeightPyramidLevel level)
        {
            switch (level)
            {
                case HeightPyramidLevel.Bottom:
                    return HeightPyramidLevel.Mid;
                case HeightPyramidLevel.Mid:
                    return HeightPyramidLevel.Top;
                case HeightPyramidLevel.Top:
                    return null;
            }

            return null;
        }
    
    }
}