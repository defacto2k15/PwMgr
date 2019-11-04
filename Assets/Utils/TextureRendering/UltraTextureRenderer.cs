using Assets.Heightmaps.Ring1.TerrainDescription.FeatureGenerating;
using Assets.Ring2;
using UnityEngine;

namespace Assets.Utils.TextureRendering
{
    public class UltraTextureRenderer
    {
        private static Texture2D _blankInputTexture;

        private static Texture2D BlankInputTexture
        {
            get
            {
                if (_blankInputTexture == null)
                {
                    _blankInputTexture = new Texture2D(2, 2, TextureFormat.ARGB32, false);
                }
                return _blankInputTexture;
            }
        }

        public static Texture2D RenderTextureAtOnce(Material renderMaterial,
            RenderTextureInfo renderTextureInfo, ConventionalTextureInfo outTextureinfo)
        {
            var renderTexture = new RenderTexture(renderTextureInfo.Width, renderTextureInfo.Height, 0,
                renderTextureInfo.Format);
            if (renderTextureInfo.UseMipMaps)
            {
                renderTexture.useMipMap = true;
            }

            Graphics.Blit(BlankInputTexture, renderTexture, renderMaterial);

            Texture2D aTexture = new Texture2D(outTextureinfo.Width, outTextureinfo.Height, outTextureinfo.Format,
                outTextureinfo.Mipmaps);
            aTexture.ReadPixels(
                new Rect(outTextureinfo.X, outTextureinfo.Y, outTextureinfo.Width, outTextureinfo.Height), 0, 0);
            aTexture.Apply();

            return aTexture;
        }

        public static Texture2D RenderTextureToTexture2D(RenderTexture sourceRenderTexture)
        {
            Texture2D aTexture = new Texture2D(sourceRenderTexture.width, sourceRenderTexture.height,
                ToTextureFormat(sourceRenderTexture.format),
                sourceRenderTexture.useMipMap);
            var oldActive = RenderTexture.active;
            RenderTexture.active = sourceRenderTexture;
            aTexture.ReadPixels(
                new Rect(0, 0, sourceRenderTexture.width, sourceRenderTexture.height), 0, 0);
            aTexture.Apply();
            RenderTexture.active = oldActive;

            return aTexture;
        }

        public static void RenderIntoExistingTexture2D(RenderTexture sourceRenderTexture, Texture2D existingTexture)
        {
            var oldActive = RenderTexture.active;
            RenderTexture.active = sourceRenderTexture;
            existingTexture.ReadPixels(
                new Rect(0, 0, sourceRenderTexture.width, sourceRenderTexture.height), 0, 0);
            existingTexture.Apply();
            RenderTexture.active = oldActive;
        }

        public static TextureFormat ToTextureFormat(RenderTextureFormat rtf)
        {
            if (rtf == RenderTextureFormat.ARGB32)
            {
                return TextureFormat.ARGB32;
            }
            else
            {
                Debug.LogWarning("Cannot transform renderTextureFormat " + rtf + " to textureFormat. TODO repair. Defaulting to ARGB32");
                return TextureFormat.ARGB32;
            }
        }

        public static RenderTextureFormat ToRenderTextureFormat(TextureFormat rtf)
        {
            if (rtf == TextureFormat.ARGB32)
            {
                return RenderTextureFormat.ARGB32;
            }
            else
            {
                Preconditions.Fail("Cannot transform textureFormat " + rtf + " to renderTextureFormat. TODO repair");
                return RenderTextureFormat.ARGB32;
            }
        }

        public static Texture2D MoveRenderTextureToNormalTexture(RenderTexture renderTexture,
            ConventionalTextureInfo outTextureinfo)
        {
            Texture2D aTexture = new Texture2D(outTextureinfo.Width, outTextureinfo.Height, outTextureinfo.Format,
                outTextureinfo.Mipmaps);
            RenderTexture.active = renderTexture;
            aTexture.ReadPixels(
                new Rect(outTextureinfo.X, outTextureinfo.Y, outTextureinfo.Width, outTextureinfo.Height), 0, 0);
            aTexture.Apply();
            return aTexture;
        }

        public static RenderTexture CreateRenderTexture(Material renderMaterial, RenderTextureInfo renderTextureInfo)
        {
            var renderTexture = new RenderTexture(renderTextureInfo.Width, renderTextureInfo.Height, 0,
                renderTextureInfo.Format);
            if (renderTextureInfo.UseMipMaps)
            {
                renderTexture.useMipMap = true;
            }

            Graphics.Blit(BlankInputTexture, renderTexture, renderMaterial);

            return renderTexture;
        }

        public static MultistepTextureRenderingState MultistepRenderTexture(MultistepTextureRenderingInput input,
            MultistepTextureRenderingState state = null)
        {
            if (state == null)
            {
                Texture newOutTexture = null;
                if (input.CreateTexture2D)
                {
                    newOutTexture = new Texture2D(input.OutTextureinfo.Width, input.OutTextureinfo.Height,
                        input.OutTextureinfo.Format,
                        input.OutTextureinfo.Mipmaps);
                }
                else
                {
                    var outRenderTexture = new RenderTexture(input.OutTextureinfo.Width, input.OutTextureinfo.Height,0,
                        ToRenderTextureFormat(input.OutTextureinfo.Format));
                    outRenderTexture.enableRandomWrite = true;
                    outRenderTexture.Create();
                    newOutTexture = outRenderTexture;
                }

                state = new MultistepTextureRenderingState(newOutTexture);
            }
            int stepIndex = state.StepIndex;

            var stepSize = input.StepSize;
            var outTextureWidth = input.OutTextureinfo.Width;

            int oneLineStepsCount = Mathf.CeilToInt(outTextureWidth / stepSize.x);
            int stepLineNo = Mathf.FloorToInt((float) stepIndex / oneLineStepsCount);
            int stepColumnNo = stepIndex - stepLineNo * oneLineStepsCount;

            int yStart = Mathf.RoundToInt(stepLineNo * stepSize.y);
            int xStart = Mathf.RoundToInt(stepColumnNo * stepSize.x);

            int xEnd = (int) Mathf.Min(xStart + stepSize.x, input.OutTextureinfo.Width);
            int width = xEnd - xStart;
            int yEnd = (int) Mathf.Min(yStart + stepSize.y, input.OutTextureinfo.Height);
            int height = yEnd - yStart;

            Vector4 absoluteValues = new Vector4(xStart, yStart, width, height);

            Vector4 uvStep = new Vector4(
                absoluteValues.x / input.OutTextureinfo.Width,
                absoluteValues.y / input.OutTextureinfo.Height,
                absoluteValues[2] / input.OutTextureinfo.Width,
                absoluteValues[3] / input.OutTextureinfo.Height
            );

            var renderTexture = new RenderTexture((int) width, (int) height, 0,
                input.RenderTextureInfoFormat, RenderTextureReadWrite.Default);
                renderTexture.enableRandomWrite = true;

            var renderMaterial = input.RenderMaterial;
            input.MultistepCoordUniform.SetUniformForStep(uvStep, renderMaterial);

            Graphics.Blit(BlankInputTexture, renderTexture, renderMaterial);

            Texture outTexture = null;
            if (input.CreateTexture2D)
            {
                var outTexture2D = state.OutTexture as Texture2D;
                outTexture2D.ReadPixels(
                    new Rect(0, 0, (int) width, (int) height),
                    (int) absoluteValues.x, (int) absoluteValues.y);
                outTexture = outTexture2D;
            }
            else
            {
                ComputeShaderContainerGameObject computeShadersContainer = input.ComputeShadersContainer;

                ComputeShader shader = computeShadersContainer.RenderTextureMovingShader;
                var kernel = shader.FindKernel("CSTextureMoving_Move");

                shader.SetTexture(kernel, "SourceTexture", renderTexture);
                shader.SetTexture(kernel, "DestTexture", state.OutTexture);
                shader.SetInt("g_offsetX", (int) absoluteValues.x);
                shader.SetInt("g_offsetY", (int) absoluteValues.y);

                shader.Dispatch(kernel, (int) width, (int) height, 1);
            }


            if (xEnd >= input.OutTextureinfo.Width && yEnd >= input.OutTextureinfo.Height)
            {
                if (input.CreateTexture2D)
                {
                    (outTexture as Texture2D).Apply();
                }
                RenderTexture.active = null;
                GameObject.Destroy(renderTexture);
                state.SetRenderingCompleted();
            }
            state.IncrementStep();
            return state;
        }

        public static void ModifyRenderTexture(Material material, IntRectangle renderingRectangle, IntVector2 targetTextureSize,
            RenderTexture renderTexture)
        {
            RenderTexture.active = renderTexture;
            GL.PushMatrix();
            GL.LoadPixelMatrix(0, targetTextureSize.X, 0, targetTextureSize.Y);
            Graphics.DrawTexture(renderingRectangle.ToRect(), renderTexture, material);
            GL.PopMatrix();
            RenderTexture.active = null;
        }
    }

    public class MultistepTextureRenderingInput
    {
        public ConventionalTextureInfo OutTextureinfo;
        public Vector2 StepSize;
        public MultistepRenderingCoordUniform MultistepCoordUniform;
        public RenderTextureFormat RenderTextureInfoFormat;
        public Material RenderMaterial;
        public bool CreateTexture2D = true;
        public ComputeShaderContainerGameObject ComputeShadersContainer;
    }

    public class MultistepTextureRenderingState
    {
        private Texture _outTexture;
        private int _stepIndex;
        private bool _renderingCompleted;

        public MultistepTextureRenderingState(Texture outTexture)
        {
            _outTexture = outTexture;
            _stepIndex = 0;
            _renderingCompleted = false;
        }

        public int StepIndex => _stepIndex;

        public Texture OutTexture => _outTexture;

        public bool RenderingCompleted => _renderingCompleted;

        public void SetRenderingCompleted()
        {
            _renderingCompleted = true;
        }

        public void IncrementStep()
        {
            _stepIndex++;
        }
    }

    public class MultistepRenderingCoordUniform
    {
        private Vector4 _originalCoordUniform;
        private readonly string _uniformName;

        public MultistepRenderingCoordUniform(Vector4 originalCoordUniform, string uniformName)
        {
            _originalCoordUniform = originalCoordUniform;
            _uniformName = uniformName;
        }

        public void SetUniformForStep(Vector4 uvStep, Material material)
        {
            Vector4 finalUniform = new Vector4(
                _originalCoordUniform.x + _originalCoordUniform[2] * uvStep.x,
                _originalCoordUniform.y + _originalCoordUniform[3] * uvStep.y,
                _originalCoordUniform[2] * uvStep[2],
                _originalCoordUniform[3] * uvStep[3]
            );
            material.SetVector(_uniformName, finalUniform);
        }
    }
}