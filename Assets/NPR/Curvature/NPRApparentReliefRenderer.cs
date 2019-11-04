using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.Cameras;
using UnityEngine;

namespace Assets.NPR.Curvature
{
    public class NPRApparentReliefRenderer : MonoBehaviour
    {
        public Material RenderMaterial;
        public Shader ApparentReliefPassShader;
        public ReplacementShaderCameraOC ReplacementShaderCameraOc;
        private string _replacementTag = "RenderType";

        public void Start()
        {
            ReplacementShaderCameraOc.MySetReplacementShader(ApparentReliefPassShader, _replacementTag);
        }

        public void Update()
        {
            ReplacementShaderCameraOc.RenderToTarget();
        }

        public void OnRenderImage(RenderTexture src, RenderTexture dest) 
        {
            RenderMaterial.SetTexture("_ApparentReliefTex", ReplacementShaderCameraOc.TargetTexture);
            Graphics.Blit(src, dest, RenderMaterial);
        }
    }
}
