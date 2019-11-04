using System.Collections.Generic;
using System.Linq;
using Assets.ComputeShaders;
using Assets.Heightmaps.Ring1.TerrainDescription.FeatureGenerating;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.Random.Fields;
using Assets.Utils;
using Assets.Utils.MT;
using Assets.Utils.Services;
using UnityEngine;

namespace Assets.Trees.Placement.Domain
{
    public class FloraDomainIntensityGeneratorDebugObject : MonoBehaviour
    {
        public ComputeShaderContainerGameObject ComputeShaderContainer;

        public void Start()
        {
            TaskUtils.SetGlobalMultithreading(false);

            List<FloraDomainCreationTemplate> creationTemplates = new List<FloraDomainCreationTemplate>()
            {
                new FloraDomainCreationTemplate()
                {
                    MaxIntensity = 1f,
                    MinIntensity = 0.0f,
                    PositionMultiplier = 1f,
                    Type = FloraDomainType.Domain1
                },
                new FloraDomainCreationTemplate()
                {
                    MaxIntensity = 0.7f,
                    MinIntensity = 0.0f,
                    PositionMultiplier = 1f,
                    Type = FloraDomainType.Domain2
                },
                new FloraDomainCreationTemplate()
                {
                    MaxIntensity = 0.6f,
                    MinIntensity = 0.0f,
                    PositionMultiplier = 1f,
                    Type = FloraDomainType.Domain3
                },
            };

            var generator = new FloraDomainIntensityGenerator(creationTemplates, ComputeShaderContainer,
                new UnityThreadComputeShaderExecutorObject(), new CommonExecutorUTProxy(), 44,
                new FloraDomainIntensityGeneratorConfiguration()
                {
                    PixelsPerUnit = 1f / 3f
                });

            var res = generator.GeneratePartAsync(new MyRectangle(0, 0, 90 * 8, 90 * 8)).Result;

            res.Part.DomainIntensityFigures.Select((c, i) =>
            {
                CreateDebugTextureShowingObject(c.Value, i);
                return 1;
            }).ToList();
        }

        private void CreateDebugTextureShowingObject(IntensityFieldFigure figure, int i)
        {
            var tex = new Texture2D(figure.Width, figure.Height, TextureFormat.ARGB32, false, false);
            for (int x = 0; x < figure.Width; x++)
            {
                for (int y = 0; y < figure.Height; y++)
                {
                    var intensity = figure.GetPixel(x, y);
                    var color = new Color(intensity, 0, 0, 1);
                    tex.SetPixel(x, y, color);
                }
            }
            tex.Apply();

            var material = new Material(Shader.Find("Custom/Debug/TextureArray"));
            material.SetTexture("_MainTex", tex);

            var go = GameObject.CreatePrimitive(PrimitiveType.Plane);
            go.GetComponent<MeshRenderer>().material = material;

            go.transform.localPosition = new Vector3(i * 12, 0, 0);
        }
    }
}