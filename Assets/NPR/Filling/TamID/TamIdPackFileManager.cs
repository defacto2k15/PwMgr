using System.Collections.Generic;
using System.Linq;
using Assets.Heightmaps.Ring1.Creator;

namespace Assets.NPRResources.TonalArtMap
{
    public class TamIdPackFileManager
    {
        public void Save(string directoryPath, List<TAMTone> tones, List<TAMMipmapLevel> levels, int layersCount, TamIdSoleImagesPack pack)
        {
            for (int toneIndex = 0; toneIndex < tones.Count; toneIndex++)
            {
                for (int levelIndex = 0; levelIndex < levels.Count; levelIndex++)
                {
                    for (int layerIndex = 0; layerIndex < layersCount; layerIndex++)
                    {
                        var image = pack.Columns[tones[toneIndex]][levels[levelIndex]][layerIndex];
                        SavingFileManager.SaveTextureToPngFile(directoryPath + CreateFileNameToSoleImage(toneIndex, levelIndex, layerIndex), image);
                    }
                }
            }
        }

        public TamIdSoleImagesPack Load(string directoryPath, List<TAMTone> tones, List<TAMMipmapLevel> levels, int layersCount)
        {
            var columns = Enumerable.Range(0, tones.Count).ToDictionary(
                toneIndex => tones[toneIndex],
                toneIndex =>
                    Enumerable.Range(0, levels.Count).ToDictionary(
                        levelIndex => levels[levelIndex],
                        levelIndex => 
                            Enumerable.Range(0, layersCount).Select(layerIndex => 
                            {
                                var path = directoryPath+CreateFileNameToSoleImage(toneIndex, levelIndex,layerIndex);
                                return SavingFileManager.LoadPngTextureFromFile(path);
                            }).ToList())
            );
            return new TamIdSoleImagesPack(columns);
        }

        private string CreateFileNameToSoleImage(int toneIndex, int levelIndex, int layer)
        {
            return $"TamIdImage-{toneIndex}-{levelIndex}-{layer}.png";
        }
    }

}