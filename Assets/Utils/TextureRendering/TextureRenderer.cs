using System;
using System.Collections.Generic;
using Assets.ShaderUtils;
using UnityEngine;

namespace Assets.Utils.TextureRendering
{
    public class TextureRenderer
    {
        public Texture2D RenderTexture(String shaderName, Texture inputTexture, UniformsPack uniforms,
            RenderTextureInfo renderTextureInfo, ConventionalTextureInfo outTextureInfo)
        {
            UseShaderToRenderTexture(shaderName, inputTexture, renderTextureInfo, uniforms);
            return CreateConventionalTextures(new List<ConventionalTextureInfo> {outTextureInfo})[0];
        }

        public Texture2D RenderTexture(Material renderMaterial, Texture inputTexture,
            RenderTextureInfo renderTextureInfo, ConventionalTextureInfo outTextureInfo)
        {
            var renderTexture = new RenderTexture(renderTextureInfo.Width, renderTextureInfo.Height, 0,
                renderTextureInfo.Format);
            renderMaterial.SetTexture("_MainTex", renderTexture);
            renderMaterial.SetVector("_InputAndOutputTextureSize",
                new Vector4(renderTextureInfo.Width, renderTextureInfo.Height, inputTexture.width,
                    inputTexture.height));
            Graphics.Blit(inputTexture, renderTexture, renderMaterial);
            return CreateConventionalTextures(new List<ConventionalTextureInfo> {outTextureInfo})[0];
        }

        private static void UseShaderToRenderTexture(string shaderName, Texture inputTexture,
            RenderTextureInfo renderTextureInfo, UniformsPack uniforms)
        {
            var renderTexture = new RenderTexture(renderTextureInfo.Width, renderTextureInfo.Height, 0,
                renderTextureInfo.Format);
            var renderMaterial = new Material(Shader.Find(shaderName));
            renderMaterial.SetTexture("_MainTex", renderTexture);
            renderMaterial.SetVector("_InputAndOutputTextureSize",
                new Vector4(renderTextureInfo.Width, renderTextureInfo.Height, inputTexture.width,
                    inputTexture.height));
            uniforms.SetUniformsToMaterial(renderMaterial);
            Graphics.Blit(inputTexture, renderTexture, renderMaterial);
        }

        public List<Texture2D> RenderTextures(string shaderName, Texture2D inputTexture, UniformsPack uniformsPack,
            RenderTextureInfo renderTextureInfo, List<ConventionalTextureInfo> outTextureInfos)
        {
            UseShaderToRenderTexture(shaderName, inputTexture, renderTextureInfo, uniformsPack);
            return CreateConventionalTextures(outTextureInfos);
        }

        private List<Texture2D> CreateConventionalTextures(List<ConventionalTextureInfo> outTextureInfos)
        {
            var outList = new List<Texture2D>();
            foreach (var aTextureInfo in outTextureInfos)
            {
                Texture2D aTexture = new Texture2D(aTextureInfo.Width, aTextureInfo.Height, aTextureInfo.Format,
                    aTextureInfo.Mipmaps);
                aTexture.ReadPixels(new Rect(aTextureInfo.X, aTextureInfo.Y, aTextureInfo.Width, aTextureInfo.Height),
                    0, 0);
                aTexture.Apply();
                outList.Add(aTexture);
            }
            return outList;
        }
    }
}