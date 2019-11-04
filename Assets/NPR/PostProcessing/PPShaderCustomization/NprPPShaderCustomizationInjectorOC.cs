using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.Utils;
using UnityEngine;

namespace Assets.NPR.PostProcessing.PPShaderCustomization
{
    public class NprPPShaderCustomizationInjectorOC : MonoBehaviour
    {
        public Shader PatternShader;

        public void RecreateShader()
        {
            var newMaterial = GetComponent<NprPPShaderCustomizerOC>().RecreateShaderAndCreateMaterial(PatternShader);
            var director = GetComponents<NPRPostProcessingDirectorOC>()[0];

            ShaderCustomizationUtils.TransferProperties(director.PostprocessingMaterial, newMaterial);
            director.PostprocessingMaterial = newMaterial;
        }

        public void ReturnToPatternShader()
        {
            var newMaterial = new Material(PatternShader);
            var director = GetComponents<NPRPostProcessingDirectorOC>()[0];

            ShaderCustomizationUtils.TransferProperties(director.PostprocessingMaterial, newMaterial);
            director.PostprocessingMaterial = newMaterial;
        }
    }
}
