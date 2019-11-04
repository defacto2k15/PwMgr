using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Assets.NPR.Lines;
using Assets.Utils;
using UnityEditor;
using UnityEngine;

namespace Assets.NPR.PostProcessing.PPShaderCustomization
{
    public class NprPPShaderCustomizerOC : MonoBehaviour
    {
        public List<NprPPShaderFactorLocation> FactorLocations = new List<NprPPShaderFactorLocation>();
        public List<NprPPShaderUsageSettings> UsageSettings = new List<NprPPShaderUsageSettings>();

        private const string _nameReplacementPrefix = "Custom/NPR/";
        private string _imitationsDirectoryName = "Imitation";


        public Material RecreateShaderAndCreateMaterial(Shader patternShader)
        {
#if UNITY_EDITOR
            var oldShaderPath = ShaderCustomizationUtils.RetriveShaderPath(patternShader);
            var text = System.IO.File.ReadAllText(oldShaderPath);
            text = ModifyShaderText(text);

            var newFileSuffix = GetInstanceID().ToString();
            var newShaderPath = ShaderCustomizationUtils.CreateImitationShaderPath(oldShaderPath, newFileSuffix, _imitationsDirectoryName);
            File.WriteAllText(newShaderPath, text);

            AssetDatabase.Refresh();
            var newShader = ShaderCustomizationUtils.LoadAssetAtPath(newShaderPath);
            var newMaterial = new Material(newShader);
            return newMaterial;
#else
            Preconditions.Fail("RecreateShaderAndCreateMaterial is not supported in build ");
            return null;
#endif
        }

        private string ModifyShaderText(String text)
        {
            var locationsDict = FactorLocations.ToDictionary(c => c.Factor, c => c);
            text = ShaderCustomizationUtils.AdjustShaderName(text, "Custom/NPR/", GetInstanceID().ToString());
            text = ShaderCustomizationUtils.ReplaceDefinition(text, $"IN_IMITATION", 1);

            Preconditions.Assert(UsageSettings.Count <= 5, "E864 Maximum usage count is 5");
            int usageIndex = 0;
            var generationLines = new List<string>();
            var includeLines = new List<string>();
            var applyLines = new List<string>();
            var usageLines = new List<string>();

            foreach (var pair in locationsDict)
            {
                usageLines.Add($"#define IN_{pair.Key.Details().Token}_TEXTURE_INDEX {pair.Value.TextureIndex.ToString()}");
                usageLines.Add($"#define IN_{pair.Key.Details().Token}_TEXTURE_SUFFIX {pair.Value.Suffix}");
                if (!pair.Key.Details().IsFilter)
                {
                    includeLines.Add($"#include \"../PPFeatures/{pair.Key.Details().Token}_ppFeature.txt\"");
                }
            }

            foreach (var usage in UsageSettings)
            {
                if (usage.Factor.Details().IsFilter)
                {
                    var newGenerationLine = $"pp_generate_filter_{usage.Filter.Details().Token}({usage.Factor.Details().Token})";
                    if (!generationLines.Contains(newGenerationLine))
                    {
                        generationLines.Add(newGenerationLine);
                    }
                    usageLines.Add($"#define IN_filter{usageIndex}_COLOR {Vector4ToShaderString(usage.Color)}");
                    if (usage.UseDebugSliderForTreshold)
                    {
                        usageLines.Add($"#define IN_filter{usageIndex}_TRESHOLD _DebugSlider");
                    }
                    else
                    {
                        usageLines.Add($"#define IN_filter{usageIndex}_TRESHOLD {usage.Treshold}");
                    }
                    usageLines.Add($"#define IN_filter{usageIndex}_DESTINATION_TEXTURE_INDEX {usage.DestinationTextureIndex}");
                    usageLines.Add($"#define IN_filter{usageIndex}_DESTINATION_TEXTURE_SUFFIX {usage.DestinationTextureSuffix}");

                    applyLines.Add($"pp_apply_usage({usageIndex}, {usage.Factor.Details().Token}, {usage.Filter.Details().Token}, i.uv, inColors);");
                }
                else
                {
                    usageLines.Add($"#define IN_{usage.Factor.Details().Token}_DESTINATION_TEXTURE_INDEX {usage.DestinationTextureIndex}");
                    usageLines.Add($"#define IN_{usage.Factor.Details().Token}_DESTINATION_TEXTURE_SUFFIX {usage.DestinationTextureSuffix}");
                    applyLines.Add($"{usage.Factor.Details().Token}_ppApplication(uv, inColors);");
                }

                usageIndex++;
            }

            string[] lines = text.Split(
                new[] {"\r\n", "\r", "\n"},
                StringSplitOptions.None
            );
            generationLines.ForEach(c => ShaderCustomizationUtils.ReplaceLine(lines, "IMITATION GENERATION LINE", c));
            includeLines.ForEach(c => ShaderCustomizationUtils.ReplaceLine(lines, "IMITATION INCLUDE LINE", c));
            applyLines.ForEach(c => ShaderCustomizationUtils.ReplaceLine(lines, "IMITATION APPLY LINE", c));
            usageLines.ForEach(c => ShaderCustomizationUtils.ReplaceLine(lines, "IMITATION USAGE LINE", c));

            return lines.Aggregate((i, j) => i + "\r\n" + j);
        }

        private string Vector4ToShaderString(Vector4 input)
        {
            return $"float4({input[0]}, {input[1]}, {input[2]}, {input[3]})";
        }

        //private static string ReplaceDefinition(string text, string name, string newValue)
        //{
        //    var pattern = @"#define\s*" + name + @"\s*.*";
        //    Preconditions.Assert(Regex.IsMatch(text,pattern), "There was no match during replacement with pattern "+pattern);
        //    var toReturn = Regex.Replace(text, pattern, $"#define {name} {newValue}");
        //    return toReturn;
        //}
    }

    [Serializable]
    public class NprPPShaderUsageSettings
    {
        public NprPPShaderFactor Factor;
        public Vector4 Color;
        public NprPPShaderFilter Filter;
        public float Treshold;
        public bool UseDebugSliderForTreshold;
        public int DestinationTextureIndex;
        public string DestinationTextureSuffix;
    }

    [Serializable]
    public class NprPPShaderFactorLocation
    {
        public NprPPShaderFactor Factor;
        public int TextureIndex;
        public string Suffix;
    }
}
