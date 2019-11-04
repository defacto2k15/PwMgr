using System;
using System.Collections.Generic;
using Assets.Random;
using UnityEngine;

namespace Assets.TerrainMat.BiomeGen
{
    public class BiomeInstanceDetailGenerator
    {
        private readonly Dictionary<BiomeType, BiomeInstanceDetailTemplate> _detailTemplateDict;

        public BiomeInstanceDetailGenerator(Dictionary<BiomeType, BiomeInstanceDetailTemplate> detailTemplateDict)
        {
            _detailTemplateDict = detailTemplateDict;
        }

        public Vector4 GenerateControlValues(int x, int y, BiomeType type, BiomeInstanceId id)
        {
            var template = _detailTemplateDict[type].ControlTemplate;
            var random = new RandomProvider(unchecked ( (int) id.Id * 333 + 11 + x ^ 312 + y * 2123));

            var index = Mathf.FloorToInt(random.Next(0, template.BaseControlValues.Count - 0.0001f));
            Vector4 baseControl = template.BaseControlValues[index];

            var newControl = new Vector4(0, 0, 0, 0);

            for (int i = 0; i < 4; i++)
            {
                newControl[i] = (float) (baseControl[i] + random.RandomGaussian(template.DeltaControls));
            }
            newControl.Normalize();
            return newControl;
        }

        public BiomeMapColorsLexicon GenerateColorsLexicon(List<BiomeInstanceCharacteristics> instanceInfos)
        {
            var workDict = new Dictionary<ColorPack, List<BiomeInstanceId>>();
            foreach (var info in instanceInfos)
            {
                var random = new RandomProvider((int) info.InstanceId.Id * 333 + 11);
                var template = _detailTemplateDict[info.Type].ColorTemplate;

                var baseColorIndex = Mathf.FloorToInt(random.Next(0, template.BaseColors.Count) - 0.0001f);
                var baseColors = template.BaseColors[baseColorIndex];

                Color[] outColorsArray = new Color[4];
                for (int i = 0; i < 4; i++)
                {
                    var color = RandomizeColor(baseColors[i], template.DeltaCharacteristics, random);
                    outColorsArray[i] = color;
                }
                var newPack = new ColorPack(outColorsArray);

                if (!workDict.ContainsKey(newPack))
                {
                    workDict[newPack] = new List<BiomeInstanceId>();
                }
                workDict[newPack].Add(info.InstanceId);
            }


            Dictionary<BiomeInstanceId, int> instanceIdToColorPackId = new Dictionary<BiomeInstanceId, int>();
            List<ColorPack> colorPacks = new List<ColorPack>();

            int k = 0;
            foreach (var pair in workDict)
            {
                colorPacks.Add(pair.Key);
                foreach (var id in pair.Value)
                {
                    instanceIdToColorPackId[id] = k;
                }
                k++;
            }

            return new BiomeMapColorsLexicon(colorPacks, instanceIdToColorPackId);
        }

        private Color RandomizeColor(Color startColor, RandomCharacteristics colorRandomCharacteristics,
            RandomProvider random)
        {
            Color outColor = new Color(0, 0, 0, 0);
            for (int i = 0; i < 4; i++)
            {
                outColor[i] = startColor[i] + (float) random.RandomGaussian(colorRandomCharacteristics);
            }
            return outColor;
        }
    }
}