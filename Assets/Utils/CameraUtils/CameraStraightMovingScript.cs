using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.Utils
{
    class CameraStraightMovingScript : MonoBehaviour
    {
        [Range(0, 4)] public float TimeMovementMultiplier = 4f;

        public Vector3 DeltaVector = new Vector3(0.25f, 0, 0.25f);

        public void Update()
        {
            var oldPos = Camera.main.transform.position;
            var newPos = oldPos + Time.deltaTime * DeltaVector.normalized * TimeMovementMultiplier;
            Camera.main.transform.position = newPos;
        }
    }
}