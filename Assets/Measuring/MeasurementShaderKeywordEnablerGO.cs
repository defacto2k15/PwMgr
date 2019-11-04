using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Measuring
{
    public class MeasurementShaderKeywordEnablerGO : MonoBehaviour
    {
        public bool MeasurementKeywordOn;
        public bool LightShadingKeywordOn;
        public bool DirectionPerLight;

        public void Start()
        {
            UpdateMeasurementKeyword();
        }

        public void OnValidate()
        {
            UpdateMeasurementKeyword();
        }

        private void UpdateMeasurementKeyword()
        {
            if (MeasurementKeywordOn)
            {
                Shader.EnableKeyword("MEASUREMENT");
            }
            else
            {
                Shader.DisableKeyword("MEASUREMENT");
            }

            if (LightShadingKeywordOn)
            {
                Shader.EnableKeyword("LIGHT_SHADING_ON");
            }
            else
            {
                Shader.DisableKeyword("LIGHT_SHADING_ON");
            }

            if (DirectionPerLight)
            {
                Shader.EnableKeyword("DIRECTION_PER_LIGHT");
            }
            else
            {
                Shader.DisableKeyword("DIRECTION_PER_LIGHT");
            }
        }
    }
}
