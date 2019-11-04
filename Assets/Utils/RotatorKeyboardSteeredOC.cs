using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Utils
{
    public class RotatorKeyboardSteeredOC : MonoBehaviour
    {
        public GameObject ObjectToRotate;
        public float RotationSpeed = 1;
        public KeyCode PositiveKey;
        public KeyCode NegativeKey;

        public void Update()
        {
            var eulerRotation = ObjectToRotate.transform.rotation.eulerAngles;
            if (Input.GetKey(PositiveKey))
            {
                ObjectToRotate.transform.rotation = Quaternion.Euler(eulerRotation.x + Time.deltaTime * RotationSpeed, eulerRotation.y, eulerRotation.z);
            }
            if (Input.GetKey(NegativeKey))
            {
                ObjectToRotate.transform.rotation = Quaternion.Euler(eulerRotation.x + Time.deltaTime * RotationSpeed*-1, eulerRotation.y, eulerRotation.z);
            }

        }
    }
}
