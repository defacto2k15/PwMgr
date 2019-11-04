using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Assets.ShaderUtils;
using UnityEditor;
using UnityEngine;

namespace Assets.Utils
{
    public static class ShaderCustomizationUtils
    {
        public static string CreateImitationShaderPath(string oldShaderPath, string newFileSuffix, string imitationsDirectoryName)
        {
            return Path.Combine(
                       Path.GetDirectoryName(oldShaderPath),
                       imitationsDirectoryName,
                       Path.GetFileNameWithoutExtension(oldShaderPath) + newFileSuffix
                   ) + Path.GetExtension(oldShaderPath);
        }

        public static string RetriveShaderPath(Shader shader)
        {
#if UNITY_EDITOR
            return Application.dataPath + AssetDatabase.GetAssetPath(shader).Remove(0, "Assets".Length);
#else
            Preconditions.Fail("RetriveShaderPath is Not supported in build ");
            return null;
#endif
        }

        public static void TransferProperties(Material oldMaterial, Material newMaterial)
        {
#if UNITY_EDITOR
            for (int i = 0; i < ShaderUtil.GetPropertyCount(oldMaterial.shader); i++)
            {
                var name = ShaderUtil.GetPropertyName(oldMaterial.shader, i);
                var type = ShaderUtil.GetPropertyType(oldMaterial.shader, i);
                switch (type)
                {
                    case ShaderUtil.ShaderPropertyType.Color:
                        newMaterial.SetColor(name, oldMaterial.GetColor(name));
                        break;
                    case ShaderUtil.ShaderPropertyType.Float:
                    case ShaderUtil.ShaderPropertyType.Range:
                        newMaterial.SetFloat(name, oldMaterial.GetFloat(name));
                        break;
                    case ShaderUtil.ShaderPropertyType.Vector:
                        newMaterial.SetVector(name, oldMaterial.GetVector(name));
                        break;
                    case ShaderUtil.ShaderPropertyType.TexEnv:
                        newMaterial.SetTexture(name, oldMaterial.GetTexture(name));
                        break;
                    default:
                        Debug.LogError("Cannot transfer variable ! "+name+" :"+type);                    
                        break;
                }
            }
#endif
        }

        public static UniformsPack RetriveUniforms(Material material)
        {
            var pack = new UniformsPack();
#if UNITY_EDITOR
            for (int i = 0; i < ShaderUtil.GetPropertyCount(material.shader); i++)
            {
                var name = ShaderUtil.GetPropertyName(material.shader, i);
                var type = ShaderUtil.GetPropertyType(material.shader, i);
                switch (type)
                {
                    case ShaderUtil.ShaderPropertyType.Color:
                        pack.SetUniform(name, material.GetColor(name));
                        break;
                    case ShaderUtil.ShaderPropertyType.Float:
                    case ShaderUtil.ShaderPropertyType.Range:
                        pack.SetUniform(name, material.GetFloat(name));
                        break;
                    case ShaderUtil.ShaderPropertyType.Vector:
                        pack.SetUniform(name, material.GetVector(name));
                        break;
                    case ShaderUtil.ShaderPropertyType.TexEnv:
                        pack.SetTexture(name, material.GetTexture(name));
                        break;
                    default:
                        Debug.LogError("Cannot transfer variable ! "+name+" :"+type);                    
                        break;
                }
            }
#endif
            return pack;
        }

        public static UniformsPack FilterNonPresentUniforms(Material material, UniformsPack pack)
        {
            UniformsPack outPack = new UniformsPack();
#if UNITY_EDITOR
            for (int i = 0; i < ShaderUtil.GetPropertyCount(material.shader); i++)
            {
                var name = ShaderUtil.GetPropertyName(material.shader, i);
                var type = ShaderUtil.GetPropertyType(material.shader, i);
                switch (type)
                {
                    case ShaderUtil.ShaderPropertyType.Color:
                    case ShaderUtil.ShaderPropertyType.Vector:
                        if (pack.Vector4Uniforms.ContainsKey(name))
                        {
                        outPack.SetUniform(name, pack.Vector4Uniforms[name].Get());
                        }
                        break;
                    case ShaderUtil.ShaderPropertyType.Float:
                    case ShaderUtil.ShaderPropertyType.Range:
                        if (pack.FloatUniforms.ContainsKey(name))
                        {
                            outPack.SetUniform(name, pack.FloatUniforms[name].Get());
                        }

                        break;
                    case ShaderUtil.ShaderPropertyType.TexEnv:
                        if (pack.Textures.ContainsKey(name))
                        {
                            outPack.SetTexture(name, pack.Textures[name]);
                        }
                        break;
                    default:
                        Debug.LogError("Cannot transfer variable ! "+name+" :"+type);                    
                        break;
                }
            }
#endif
            return outPack;
        }

        public static Shader LoadAssetAtPath(string newShaderPath)
        {
#if UNITY_EDITOR
            return AssetDatabase.LoadAssetAtPath<Shader>(newShaderPath.Substring(newShaderPath.IndexOf("Assets")));
#else
            Preconditions.Fail("LoadAssetAtPath is Not supported in build ");
            return null;
#endif

        }

        public static string AdjustShaderName(string text, string nameReplacementPrefix, string newSuffix)
        {
            Preconditions.Assert(text.Contains(nameReplacementPrefix), "There is no nameReplacementText in text");
            text = text.Replace(nameReplacementPrefix, nameReplacementPrefix + "IMITATION/");

            var lines = text.Split("\n".ToCharArray(), 2);
            var firstLine = lines[0];
            var pattern = "\"\\s*{";
            Preconditions.Assert(Regex.IsMatch(firstLine,pattern), "There was no match during replacement with pattern "+pattern);
            firstLine = Regex.Replace(firstLine, pattern, "-" + newSuffix + "\" {");
            return firstLine + "\n" + lines[1];
        }

        public static string ReplaceDefinition(string text, string name, int newValue)
        {
            var pattern = @"#define\s*" + name + @"\s*.*";
            Preconditions.Assert(Regex.IsMatch(text,pattern), "There was no match during replacement with pattern "+pattern);
            var toReturn = Regex.Replace(text, pattern, $"#define {name} ({newValue})");
            return toReturn;
        }

        public static string SetEnabilityLine(string text, string lineAsEnabled, bool shouldBeEnabled)
        {
            Debug.Log("REPAIR IT");
            return text;
            var patternOn = "^\\s*"+lineAsEnabled;
            var patternOff = @"^\\s*//" + lineAsEnabled;

            var isOn = Regex.IsMatch(text, patternOn);
            var isOff = Regex.IsMatch(text, patternOff);

            Preconditions.Assert(isOn || isOff , "There was no match during replacement with prefix "+lineAsEnabled);
            Preconditions.Assert( !(isOn && isOff) , $"line with prefix "+lineAsEnabled+" is both on and off");
            if (shouldBeEnabled)
            {
                if (isOff)
                {
                    return Regex.Replace(text, patternOff, patternOn);
                }
                else
                {
                    return text;
                }
            }
            else
            {
                if (isOn)
                {
                    return Regex.Replace(text, patternOn, patternOff);
                }
                else
                {
                    return text;
                }
            }
        }

        public static void ReplaceLine(string[] lines,  string designationString, string newLine)
        {
            var pair = lines.Select((c, i) => new {c, i}).FirstOrDefault(c => c.c.Contains(designationString));
            Preconditions.Assert(pair != null, "Cannot find line with designation "+designationString);
            var idx = pair.i;
            lines[idx] = newLine;
        }
    }
}
