using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.NPR.PostProcessing
{
    public class SimplePostProcessingDirectorOC : MonoBehaviour
    {
        public Material PostProcessingMaterial;

        public void OnRenderImage(RenderTexture src, RenderTexture dest)
        {
            Graphics.Blit(src, dest, PostProcessingMaterial);
        }
    }
}
