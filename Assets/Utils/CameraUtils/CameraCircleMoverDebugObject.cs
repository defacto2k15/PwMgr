using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.Utils
{
    public class CameraCircleMoverDebugObject : MonoBehaviour
    {
        [Range(0, 800)] public float TimeMovementMultiplier = 800f;
        public int MaxZ = 10;

        private float _timeSum = 0;

        public void Start()
        {
            SetUpPosition();
        }

        public void Update()
        {
            SetUpPosition();
        }

        public void SetUpPosition()
        {
            _timeSum += Time.deltaTime * TimeMovementMultiplier;

            var center = new Vector3(MaxZ, 0, MaxZ);

            var angle = _timeSum / 100;
            var radAngle = Mathf.Deg2Rad * angle;

            var newPosition = new Vector3(Mathf.Sin(radAngle), 0, Mathf.Cos(radAngle)) * MaxZ;
            newPosition += center;

            Camera.main.transform.position = newPosition;
            Camera.main.transform.eulerAngles = new Vector3(0, 45, 0);
        }
    }
}