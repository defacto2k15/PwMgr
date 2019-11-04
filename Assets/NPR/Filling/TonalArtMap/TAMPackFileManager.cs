using System.Collections.Generic;
using System.Linq;
using Assets.Heightmaps.Ring1.Creator;

namespace Assets.NPRResources.TonalArtMap
{
    public class TAMPackFileManager
    {
        public void Save(string directoryPath, List<TAMTone> tones, List<TAMMipmapLevel> levels, TAMSoleImagesPack pack)
        {
            for (int toneIndex = 0; toneIndex < tones.Count; toneIndex++)
            {
                for (int levelIndex = 0; levelIndex < levels.Count; levelIndex++)
                {
                    var image = pack.Columns[tones[toneIndex]][levels[levelIndex]];
                    SavingFileManager.SaveTextureToPngFile(directoryPath+CreateFileNameToSoleImage(toneIndex, levelIndex), image);
                }
            }
        }

        public TAMSoleImagesPack Load(string directoryPath, List<TAMTone> tones, List<TAMMipmapLevel> levels)
        {
            var columns = Enumerable.Range(0, tones.Count).ToDictionary(
                toneIndex => tones[toneIndex],
                toneIndex =>

                    Enumerable.Range(0, levels.Count).ToDictionary(
                        levelIndex => levels[levelIndex],
                        levelIndex =>
                        {
                            var path = directoryPath+CreateFileNameToSoleImage(toneIndex, levelIndex);
                            return SavingFileManager.LoadPngTextureFromFile(path);
                        })
            );
            return new TAMSoleImagesPack(columns);
        }

        private string CreateFileNameToSoleImage(int toneIndex, int levelIndex)
        {
            return $"TAMSoleImage-{toneIndex}-{levelIndex}.png";
        }
    }
}