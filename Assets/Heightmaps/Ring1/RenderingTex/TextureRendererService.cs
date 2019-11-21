using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.Ring2;
using Assets.Ring2.BaseEntities;
using Assets.ShaderUtils;
using Assets.Utils;
using Assets.Utils.TextureRendering;
using Assets.Utils.UTUpdating;
using UnityEngine;

namespace Assets.Heightmaps.Ring1.RenderingTex
{
    public class TextureRendererService
    {
        private MultistepTextureRenderer _multistepTextureRenderer;
        private TextureRendererServiceConfiguration _configuration;
        private TextureRenderingOrder _multistepOrder;

        public TextureRendererService(MultistepTextureRenderer multistepTextureRenderer,
            TextureRendererServiceConfiguration configuration)
        {
            _multistepTextureRenderer = multistepTextureRenderer;
            _configuration = configuration;
        }

        public void AddMultistepOrder(TextureRenderingOrder order)
        {
            var template = order.Template;
            Preconditions.Assert(template.RenderingRectangle==null,"E781: Cannot render multistep when filling RenderTexture!");

            var outTextureInfo = template.OutTextureInfo;
            var shaderName = template.ShaderName;
            var pack = template.UniformPack;
            var material = new Material(Shader.Find(shaderName));
            template.Keywords.EnableInMaterial(material);
            pack.SetUniformsToMaterial(material);
            var renderTextureFormat = template.RenderTextureFormat;

            MultistepTextureRenderingInput input = new MultistepTextureRenderingInput()
            {
                MultistepCoordUniform = new MultistepRenderingCoordUniform(
                    new Vector4(
                        template.Coords.X,
                        template.Coords.Y,
                        template.Coords.Width,
                        template.Coords.Height
                    ), "_Coords"),
                OutTextureinfo = outTextureInfo,
                RenderTextureInfoFormat = renderTextureFormat,
                RenderMaterial = material,
                StepSize = _configuration.StepSize,
                CreateTexture2D = template.CreateTexture2D
            };
            _multistepTextureRenderer.StartRendering(input);
            _multistepOrder = order;
        }

        public Texture FufillOrder(TextureRenderingTemplate template)
        {
            var outTextureInfo = template.OutTextureInfo;
            var shaderName = template.ShaderName;
            var pack = template.UniformPack;
            var material = new Material(Shader.Find(shaderName));
            template.Keywords.EnableInMaterial(material);

            if (template.Coords != null)
            {
                pack.SetUniform("_Coords", template.Coords.ToVector4());
            }
            pack.SetUniformsToMaterial(material);

            var renderTextureFormat = template.RenderTextureFormat;


            Texture outTexture = null;
            if (template.CreateTexture2D)
            {
                Preconditions.Assert(!template.RenderTextureArraySlice.HasValue, "E629 RenderTextureArrays are not supported here");
                RenderTextureInfo renderTextureInfo = new RenderTextureInfo(outTextureInfo.Width, outTextureInfo.Height,
                    renderTextureFormat, template.RenderTextureMipMaps);
                outTexture = UltraTextureRenderer.RenderTextureAtOnce(material, renderTextureInfo, outTextureInfo);
            }
            else
            {
                if (template.RenderingRectangle == null)
                {
                    Preconditions.Assert(!template.RenderTextureArraySlice.HasValue, "E629 RenderTextureArrays are not supported here");
                    RenderTextureInfo renderTextureInfo = new RenderTextureInfo(outTextureInfo.Width,
                        outTextureInfo.Height,
                        renderTextureFormat, template.RenderTextureMipMaps);
                    outTexture = UltraTextureRenderer.CreateRenderTexture(material, renderTextureInfo);
                }
                else
                {
                    if (template.RenderTextureArraySlice.HasValue)
                    {
                        UltraTextureRenderer.ModifyRenderTextureArray(material, template.RenderingRectangle, template.RenderTargetSize
                            , template.RenderTextureToModify, template.RenderTextureArraySlice.Value);
                    }
                    else
                    {
                        UltraTextureRenderer.ModifyRenderTexture(material, template.RenderingRectangle, template.RenderTargetSize,
                            template.RenderTextureToModify);
                    }

                    return template.RenderTextureToModify;
                }
            }
            //GameObject.Destroy(material);
            return outTexture;
        }

        public bool CurrentlyRendering
        {
            get { return _multistepTextureRenderer.IsActive; }
        }

        public void Update()
        {
            if (_multistepTextureRenderer.IsActive)
            {
                if (_multistepTextureRenderer.RenderingCompleted())
                {
                    var texture = _multistepTextureRenderer.RetriveRenderedTexture();
                    _multistepOrder.Tcs.SetResult(texture);
                    _multistepOrder = null;
                }
                else
                {
                    _multistepTextureRenderer.Update();
                }
            }
        }
    }


    public class TextureRenderingTemplate //todo refactior it!!!
    {
        public bool CanMultistep { get; set; }
        public ConventionalTextureInfo OutTextureInfo { get; set; }
        public string ShaderName { get; set; }
        public UniformsPack UniformPack { get; set; }
        public RenderTextureFormat RenderTextureFormat { get; set; }
        public MyRectangle Coords { get; set; }
        public bool CreateTexture2D { get; set; } = true;
        public bool RenderTextureMipMaps { get; set; } = false;
        public ShaderKeywordSet Keywords { get; set; } = new ShaderKeywordSet();

        public IntRectangle RenderingRectangle;
        public RenderTexture RenderTextureToModify;
        public IntVector2 RenderTargetSize;
        public int? RenderTextureArraySlice;
    }

    public class TextureRendererServiceConfiguration
    {
        public Vector2 StepSize;
    }

    public class UTTextureRendererProxy : BaseUTTransformProxy<Texture, TextureRenderingTemplate>
    {
        private TextureRendererService _service;

        public UTTextureRendererProxy(TextureRendererService service) : base(false)
        {
            _service = service;
        }

        public Task<Texture> AddOrder(TextureRenderingTemplate template)
        {
            return BaseUtAddOrder(template);
        }

        public override bool InternalHasWorkToDo()
        {
            return _service.CurrentlyRendering;
        }

        protected override void InternalUpdate()
        {
            if (_service.CurrentlyRendering)
            {
                _service.Update();
            }
            else
            {
                // lets add neOrder order;
                UTProxyTransformOrder<Texture, TextureRenderingTemplate> transformOrder = TryGetNextFromQueue();
                if (transformOrder == null)
                {
                    return;
                }
                else
                {
                    if (transformOrder.Order.CanMultistep)
                    {
                        _service.AddMultistepOrder(new TextureRenderingOrder()
                        {
                            Tcs = transformOrder.Tcs,
                            Template = transformOrder.Order
                        });
                    }
                    else
                    {
                        transformOrder.Tcs.SetResult(_service.FufillOrder(transformOrder.Order));
                    }
                }
            }
        }

        protected override Texture ExecuteOrder(TextureRenderingTemplate order)
        {
            return _service.FufillOrder(order);
        }
    }

    public class TextureRenderingOrder
    {
        public TaskCompletionSource<Texture> Tcs;
        public TextureRenderingTemplate Template;
    }
}