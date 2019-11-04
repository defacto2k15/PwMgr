using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Assets.NPR.Lines;
using Assets.Utils;
using UnityEditor;
using UnityEngine;

namespace Assets.NPR.ShaderCustomizer
{
    public class NPRObjectShaderCustomizerOC : MonoBehaviour
    {
        private const string _nameReplacementPrefix = "Custom/NPR/";
        private string _imitationsDirectoryName = "Imitation";

        public Shader PatternShader;
        public List<ShaderFeatureSetting> FeatureSettings = new List<ShaderFeatureSetting>();
        public List<ShaderAspectSetting> AspectSettings = new List<ShaderAspectSetting>();
        public List<ShaderToggleSetting> ToggleSettings = new List<ShaderToggleSetting>();


        public void Start()
        {
            RecreateShader();
        }

        public void RecreateShader()
        {
#if UNITY_EDITOR
            var oldShaderPath = ShaderCustomizationUtils.RetriveShaderPath(PatternShader);

            var text = System.IO.File.ReadAllText(oldShaderPath);
            var newFileSuffix = GetInstanceID().ToString();
            text = ModifyShaderText(text, newFileSuffix);

            var newShaderPath = ShaderCustomizationUtils.CreateImitationShaderPath(oldShaderPath, newFileSuffix, _imitationsDirectoryName);
            File.WriteAllText(newShaderPath, text);

            AssetDatabase.Refresh();
            var newShader = ShaderCustomizationUtils.LoadAssetAtPath(newShaderPath);
            var newMaterial = new Material(newShader);

            var renderer = GetComponent<Renderer>();
            ShaderCustomizationUtils.TransferProperties(renderer.sharedMaterial, newMaterial);
            SetMaterialAndUpdateBuffers(renderer, newMaterial);
#else
            Preconditions.Fail("RecteateShader is not supported in build ");
#endif

        }


        public void ReturnToPatternShader()
        {
            var newMaterial = new Material(PatternShader);
            var renderer = GetComponent<Renderer>();
            ShaderCustomizationUtils.TransferProperties(renderer.sharedMaterial, newMaterial);
            SetMaterialAndUpdateBuffers(renderer, newMaterial);
        }

        private void SetMaterialAndUpdateBuffers(Renderer renderer, Material newMaterial)
        {
            renderer.sharedMaterial = newMaterial;
            foreach (var injector in GetComponents<CachedShaderBufferInjectOC>())
            {
                injector.RecreateBuffer();
            }
        }

        private string ModifyShaderText(string text, string newFileSuffix)
        {
            text = ShaderCustomizationUtils.AdjustShaderName(text, _nameReplacementPrefix, newFileSuffix);

            text = ShaderCustomizationUtils.ReplaceDefinition(text, "IN_IMITATION", 1);

            var includeLines = new List<string>();
            foreach (var setting in FeatureSettings)
            {
                Preconditions.Assert(setting.Feature.Details().SupportedDetectionModes.Contains(setting.DetectionMode), $"For feature {setting.Feature} mode {setting.DetectionMode} is not supported");
                text = ShaderCustomizationUtils.ReplaceDefinition(text, $"IN_{setting.Feature.Details().Token}_FEATURE_DETECTION_MODE", setting.DetectionMode.Details().Index);
                text = ShaderCustomizationUtils.ReplaceDefinition(text, $"IN_{setting.Feature.Details().Token}_FEATURE_APPLY_MODE", setting.ApplyMode.Details().Index);
                text = ShaderCustomizationUtils.ReplaceDefinition(text, $"IN_{setting.Feature.Details().Token}_TARGET_INDEX", setting.TargetTextureIndex);
                
                includeLines.Add($"#include \"../Features/{setting.Feature.Details().Token}_feature.txt\"");
            }

            foreach (var aspectSetting in AspectSettings)
            {
                Preconditions.Assert(aspectSetting.Aspect.Details().SupportedModes.Contains(aspectSetting.AspectMode), $"For feature {aspectSetting.Aspect} mode {aspectSetting.AspectMode} is not supported");
                text = ShaderCustomizationUtils.ReplaceDefinition(text, $"IN_{aspectSetting.Aspect.Details().Token}_ASPECT_MODE", aspectSetting.AspectMode.Details().Index);

                includeLines.Add($"#include \"../Aspects/{aspectSetting.Aspect.Details().Token}_aspect.txt\"");
            }

            foreach (var toggleSetting in ToggleSettings)
            {
                int valueToSet = 0;
                if (toggleSetting.IsEnabled)
                {
                    valueToSet = 1;
                }
                text = ShaderCustomizationUtils.ReplaceDefinition(text, $"IN_USE_{toggleSetting.Toggle.Details().Token}", valueToSet);

                foreach (var line in toggleSetting.Toggle.Details().LinesToDisablePrefix)
                {
                    text = ShaderCustomizationUtils.SetEnabilityLine(text, line, toggleSetting.IsEnabled);
                }
            }

            string[] lines = text.Split(
                new[] {"\r\n", "\r", "\n"},
                StringSplitOptions.None
            );
            includeLines.ForEach(c => ShaderCustomizationUtils.ReplaceLine(lines, "IMITATION INCLUDE LINE", c));
            ChangeMacroDesignations(lines, FeatureSettings.Select(c => c.Feature).ToList(), AspectSettings.Select(c => c.Aspect).ToList());

            
            var outString = lines.Aggregate((i, j) => i + "\r\n" + j);
            Debug.Log(outString);
            return outString;
        }

        private void ChangeMacroDesignations(string[] lines, List<ShaderFeature> features, List<ShaderAspect> aspects)
        {
            var q = lines.Select((c, i) => new {c, i}).Where(c => c.c.Contains("IN FEATURE MACRO START")).ToList();
            int featureMacroStart = lines.Select((c,i) => new {c,i}).Where(c => c.c.Contains("IN FEATURE MACRO START")).Select(c => c.i).First();
            Preconditions.Assert(featureMacroStart != 0, "cannot find line with IN FEATURE MACRO START");
            int featureMacroEnd = lines.Select((c,i) => new {c,i}).Where(c => c.c.Contains("IN FEATURE MACRO END")).Select(c => c.i).First();
            Preconditions.Assert(featureMacroEnd != 0, "cannot find line with IN FEATURE MACRO END");
            var featureMacroLines = lines.Select((c, i) => new {c, i}).Where(c => c.c.StartsWith("#define FEATURE"))
                .Where(c => c.i > featureMacroStart && c.i < featureMacroEnd).Select(c => c.i).ToList();
            Preconditions.Assert(featureMacroLines.Count == 11, $"Cannot find 11 feature maro lines, found only {featureMacroLines.Count}");

            for (int j = 0; j < featureMacroLines.Count; j++)
            {
                var lineIdx = featureMacroLines[j];
                if ( j < features.Count)
                {
                    lines[lineIdx] = $"#define FEATURE{j + 1}( a )  a({features[j].Details().Token})";
                }
                else
                {
                    lines[lineIdx] = $"#define FEATURE{j + 1}( a )  ";
                }
            }

            int aspectMacroStart = lines.Select((c,i) => new {c,i}).Where(c => c.c.Contains("IN ASPECT MACRO START")).Select(c => c.i).First();
            Preconditions.Assert(aspectMacroStart != 0, "cannot find line with IN ASPECT MACRO START");
            int aspectMacroEnd = lines.Select((c,i) => new {c,i}).Where(c => c.c.Contains("IN ASPECT MACRO END")).Select(c => c.i).First();
            Preconditions.Assert(aspectMacroEnd != 0, "cannot find line with IN ASPECT MACRO END");
            var aspectMacroLines = lines.Select((c, i) => new {c, i}).Where(c => c.c.StartsWith("#define ASPECT"))
                .Where(c => c.i > aspectMacroStart && c.i < aspectMacroEnd).Select(c => c.i).ToList();
            Preconditions.Assert(aspectMacroLines.Count == 5, $"Cannot find 5 aspect macro lines, found only {aspectMacroLines.Count}");

            for (int j = 0; j < aspectMacroLines.Count; j++)
            {
                var lineIdx = aspectMacroLines[j];
                if ( j < aspects.Count)
                {
                    lines[lineIdx] = $"#define ASPECT{j + 1}( a ) a({aspects[j].Details().Token})";
                }
                else
                {
                    lines[lineIdx] = $"#define ASPECT{j + 1}( a ) ";
                }
            }
        }
    }

    [Serializable]
    public class ShaderFeatureSetting
    {
        public ShaderFeature Feature;
        public ShaderFeatureDetectionMode DetectionMode;
        public ShaderFeatureApplyMode ApplyMode;
        public int TargetTextureIndex;
    }

    public enum ShaderFeature
    {
        Ridge, Valley, Silhouette, ObjectId, DepthNormal, HybridApparentRidgesPP,  PrincipalHighlights, SuggestiveContours, SuggestiveContoursPP, SuggestiveHighlights
    }

    public static class ShaderFeaturesUtils
    {
        private static Dictionary<ShaderFeature, ShaderFeatureDetails> _details = new Dictionary<ShaderFeature, ShaderFeatureDetails>()
        {
            {ShaderFeature.Ridge, new ShaderFeatureDetails() {
                Token = "ridge",
                SupportedDetectionModes = new List<ShaderFeatureDetectionMode>() {ShaderFeatureDetectionMode.Off, ShaderFeatureDetectionMode.Vertex, ShaderFeatureDetectionMode.Geometry},
                SupportedApplyModes = new List<ShaderFeatureApplyMode>() {ShaderFeatureApplyMode.Fins, ShaderFeatureApplyMode.SurfaceLine },
            } },
            {ShaderFeature.Valley, new ShaderFeatureDetails() {
                Token = "valley",
                SupportedDetectionModes = new List<ShaderFeatureDetectionMode>() {ShaderFeatureDetectionMode.Off, ShaderFeatureDetectionMode.Vertex, ShaderFeatureDetectionMode.Geometry},
                SupportedApplyModes = new List<ShaderFeatureApplyMode>() {ShaderFeatureApplyMode.Fins, ShaderFeatureApplyMode.SurfaceLine },
            } },
            {ShaderFeature.Silhouette, new ShaderFeatureDetails() {
                Token = "silhouette",
                SupportedDetectionModes = new List<ShaderFeatureDetectionMode>() {ShaderFeatureDetectionMode.Off, ShaderFeatureDetectionMode.Geometry},
                SupportedApplyModes = new List<ShaderFeatureApplyMode>() {ShaderFeatureApplyMode.Fins, ShaderFeatureApplyMode.SurfaceLine },
            } },
            {ShaderFeature.ObjectId, new ShaderFeatureDetails() {
                Token = "obj",
                SupportedDetectionModes = new List<ShaderFeatureDetectionMode>() {ShaderFeatureDetectionMode.Off, ShaderFeatureDetectionMode.Pixel},
                SupportedApplyModes = new List<ShaderFeatureApplyMode>() {ShaderFeatureApplyMode.Filling},
            } },
            {ShaderFeature.DepthNormal, new ShaderFeatureDetails() {
                Token = "dn",
                SupportedDetectionModes = new List<ShaderFeatureDetectionMode>() {ShaderFeatureDetectionMode.Off, ShaderFeatureDetectionMode.Pixel},
                SupportedApplyModes = new List<ShaderFeatureApplyMode>() {ShaderFeatureApplyMode.Filling},
            } },
            {ShaderFeature.HybridApparentRidgesPP, new ShaderFeatureDetails() {
                Token = "happ",
                SupportedDetectionModes = new List<ShaderFeatureDetectionMode>() {ShaderFeatureDetectionMode.Off, ShaderFeatureDetectionMode.Pixel},
                SupportedApplyModes = new List<ShaderFeatureApplyMode>() {ShaderFeatureApplyMode.Filling},
            } },
            {ShaderFeature.PrincipalHighlights, new ShaderFeatureDetails() {
                Token = "ph",
                SupportedDetectionModes = new List<ShaderFeatureDetectionMode>() {ShaderFeatureDetectionMode.Off, ShaderFeatureDetectionMode.Vertex},
                SupportedApplyModes = new List<ShaderFeatureApplyMode>() {ShaderFeatureApplyMode.Filling, ShaderFeatureApplyMode.LineFilling},
            } },
            {ShaderFeature.SuggestiveContours, new ShaderFeatureDetails() {
                Token = "sc",
                SupportedDetectionModes = new List<ShaderFeatureDetectionMode>() {ShaderFeatureDetectionMode.Off, ShaderFeatureDetectionMode.Vertex},
                SupportedApplyModes = new List<ShaderFeatureApplyMode>() {ShaderFeatureApplyMode.Filling, ShaderFeatureApplyMode.LineFilling},
            } },
            {ShaderFeature.SuggestiveContoursPP, new ShaderFeatureDetails() {
                Token = "scpp",
                SupportedDetectionModes = new List<ShaderFeatureDetectionMode>() {ShaderFeatureDetectionMode.Off, ShaderFeatureDetectionMode.Pixel},
                SupportedApplyModes = new List<ShaderFeatureApplyMode>() {ShaderFeatureApplyMode.Filling},
            } },
            {ShaderFeature.SuggestiveHighlights, new ShaderFeatureDetails() {
                Token = "sh",
                SupportedDetectionModes = new List<ShaderFeatureDetectionMode>() {ShaderFeatureDetectionMode.Off, ShaderFeatureDetectionMode.Vertex},
                SupportedApplyModes = new List<ShaderFeatureApplyMode>() {ShaderFeatureApplyMode.Filling, ShaderFeatureApplyMode.LineFilling},
            } },
        };

        public static ShaderFeatureDetails Details(this ShaderFeature feature)
        {
            return _details[feature];
        }
    }

    public class ShaderFeatureDetails
    {
        public string Token;
        public List<ShaderFeatureDetectionMode> SupportedDetectionModes;
        public List<ShaderFeatureApplyMode> SupportedApplyModes;
    }

    [Serializable]
    public class ShaderAspectSetting
    {
        public ShaderAspect Aspect;
        public ShaderFeatureDetectionMode AspectMode;
    }

    [Serializable]
    public class ShaderToggleSetting
    {
        public ShaderToggle Toggle;
        public bool IsEnabled;
    }
}
