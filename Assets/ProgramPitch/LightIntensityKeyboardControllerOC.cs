using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.ProgramPitch
{
    public class LightIntensityKeyboardControllerOC : MonoBehaviour
    {
        private float _changeAmount = 0.03f;
        private Light _lightComponent;

        public void Start()
        {
            _lightComponent = GetComponent<Light>();
        }

        public void Update()
        {
            var intensity = _lightComponent.intensity;
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                intensity = intensity - _changeAmount;
            }
            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                intensity = intensity + _changeAmount;
            }

            intensity = Mathf.Min(1f, intensity);
            intensity = Mathf.Max(0, intensity);

            float epsilon = 0.01f;
            if (Mathf.Abs(_lightComponent.intensity - intensity) > epsilon)
            {
                _lightComponent.intensity = intensity;
                Debug.Log("Light intensity is "+intensity);
            }
        }

    }
}
