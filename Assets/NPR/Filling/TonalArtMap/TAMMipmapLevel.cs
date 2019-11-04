using System.Collections.Generic;

namespace Assets.NPRResources.TonalArtMap
{
    public class TAMMipmapLevel
    {
        private TAMMipmapLevel _lowerLevel;
        private int _levelIndex;
        private int _mipmapLevelsCount;

        public TAMMipmapLevel(int levelIndex, int mipmapLevelsCount, TAMMipmapLevel lowerLevel = null)
        {
            _levelIndex = levelIndex;
            _lowerLevel = lowerLevel;
            _mipmapLevelsCount = mipmapLevelsCount;
        }

        public bool IsLowestLevel => _lowerLevel == null;
        public TAMMipmapLevel LowerLevel => _lowerLevel;

        public int LevelIndex => _levelIndex;

        public int MipmapLevelsCount => _mipmapLevelsCount;

        public static List<TAMMipmapLevel> CreateList(int count)
        {
            var outList = new List<TAMMipmapLevel>();
            TAMMipmapLevel lastMipmapLevel = null;
            for (int i = 0; i < count; i++)
            {
                var newMipmapLevel = new TAMMipmapLevel(i, count, lastMipmapLevel);
                outList.Add(newMipmapLevel);
                lastMipmapLevel = newMipmapLevel;
            }

            return outList;
        }
    }
}