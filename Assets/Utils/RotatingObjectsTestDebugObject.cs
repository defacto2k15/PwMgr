using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.Utils
{
    public class RotatingObjectsTestDebugObject : MonoBehaviour
    {
        private GameObject _testObject;
        [Range(0, 300)] public float Angle;

        [Range(0, 2)] public float First;

        [Range(0, 2)] public float Second;

        [Range(0, 2)] public float Third;

        public void Start()
        {
            _testObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            _testObject.transform.localScale = new Vector3(1, 10, 1);
        }

        public void Update()
        {
            var normal = new Vector3(First, Second, Third).normalized;

            var normalRotation = Quaternion.LookRotation(normal);
            var nnr = normalRotation.eulerAngles;
            var normalFlatSpin = Quaternion.AngleAxis(Angle, Vector3.up);
            var normalFlatSpin0 = Quaternion.AngleAxis(0.1f, Vector3.up);
            var normalFlatSpin1 = Quaternion.AngleAxis(0.3f, Vector3.up);
            var normalFlatSpin2 = Quaternion.AngleAxis(0.7f, Vector3.up);

            var finalRotation = (normalRotation /** normalFlatSpin*/);
            _testObject.transform.rotation = finalRotation;
        }
    }
}