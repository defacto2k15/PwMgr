using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.Utils.TextureRendering
{
    public class MultistepTextureRenderingDebugObject : MonoBehaviour
    {
        private MultistepTextureRenderer _renderer;

        public void Start()
        {
            //_renderer = new MultistepTextureRenderer();
            //MultistepTextureRenderingInput input = new MultistepTextureRenderingInput()
            //{
            //    MultistepCoordUniform = new MultistepRenderingCoordUniform(new Vector4(0,0,20,20), "_Coord" ),
            //    OutTextureinfo = new ConventionalTextureInfo(200,200,TextureFormat.ARGB32, false),
            //    RenderMaterial = renderMaterial,
            //    RenderTextureInfoFormat = RenderTextureFormat.ARGB32,
            //    StepSize = new Vector2(20, 20)
            //};
            //_renderer.StartRendering(input);
        }
    }
}