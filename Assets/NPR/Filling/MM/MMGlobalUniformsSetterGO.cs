using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.Utils;
using UnityEngine;

namespace Assets.NPR.Filling.MM
{
    public class MMGlobalUniformsSetterGO : MonoBehaviour
    {
        [Range(0.2f,1.5f)]
        public float GlobalMMultiplier;
        public int VectorQuantCount = 8;
        [Range(0.01f,0.2f)]
        public float FastSeedSamplingMultiplier;

        [Range(0.0f, 3.14f)] public float AngleOffset;
        [Range(0.0f, 3.14f)] public float AngleOffset2;

        private List<Material> _materialsToAlign = new List<Material>();
        private RunOnceBox _collectAlignableMaterialsBox;

        public void Start()
        {
            _collectAlignableMaterialsBox = new RunOnceBox(() =>
            {
                var materialsFromMMProcessor =
                    FindObjectOfType<MyMethodPostProcessingDirectorOC>().RenderingMaterials;
                _materialsToAlign.AddRange(materialsFromMMProcessor);
            }, 3);
        }

        public void Update()
        {
            _collectAlignableMaterialsBox.Update();
            Shader.SetGlobalFloat("_GlobalMMultiplier", GlobalMMultiplier);
            Shader.SetGlobalFloat("_GlobalAngleOffset", AngleOffset);
            Shader.SetGlobalFloat("_GlobalAngleOffset2", AngleOffset2);
            _materialsToAlign.ForEach(UpdateUniforms);
        }

        private void UpdateUniforms(Material mat)
        {
            mat.SetFloat("_SeedSamplingMultiplier", FastSeedSamplingMultiplier);
            mat.SetInt("_VectorQuantCount", VectorQuantCount);
        }
    }
}
