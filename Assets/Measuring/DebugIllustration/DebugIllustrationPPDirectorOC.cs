using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Measuring.DebugIllustration
{
    public class DebugIllustrationPPDirectorOC : MonoBehaviour
    {
        public Material DebugIllustrationMaterialPP;
        private bool _useIllustrationMaterial = false;

        public void ShowIllustrations(Texture screenshotTex, Texture resultTex)
        {
            DebugIllustrationMaterialPP.SetTexture("_BackgroundTex", screenshotTex);
            DebugIllustrationMaterialPP.SetTexture("_ResultTex", resultTex);
            _useIllustrationMaterial = true;
        }

        public void OnRenderImage(RenderTexture src, RenderTexture dest)
        {
            if (_useIllustrationMaterial)
            {
                Graphics.Blit(src, dest, DebugIllustrationMaterialPP, 0);
            }
            else
            {
                Graphics.Blit(src, dest);
            }
        }
    }
}
