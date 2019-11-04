using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.Utils
{
    class CameraSteeringScript : MonoBehaviour
    {
        [Range(0,2)]
        public float MovingSpeed = 1;
        public void Update()
        {
            var movingAmount = 0.5f*MovingSpeed;
            var rotatingAmount = 0.5f;
            if (Input.GetKey(KeyCode.LeftShift))
            {
                movingAmount *= 10;
                rotatingAmount *= 10;
            }

            Vector3 delta = Vector3.zero;
            if (Input.GetKey(KeyCode.A) == true)
            {
                delta += new Vector3(-movingAmount, 0, 0);
            }
            if (Input.GetKey(KeyCode.D) == true)
            {
                delta += new Vector3(movingAmount, 0, 0);
            }
            if (Input.GetKey(KeyCode.W) == true)
            {
                delta += new Vector3(0, 0, movingAmount);
            }
            if (Input.GetKey(KeyCode.S) == true)
            {
                delta += new Vector3(0, 0, -movingAmount);
            }
            if (Input.GetKey(KeyCode.Q) == true)
            {
                delta += new Vector3(0, -movingAmount, 0);
            }
            if (Input.GetKey(KeyCode.E) == true)
            {
                delta += new Vector3(0, movingAmount, 0);
            }

            transform.localPosition += delta;

            var rotatingDelta = Vector3.zero;
            if (Input.GetKey(KeyCode.Y))
            {
                rotatingDelta += new Vector3(-rotatingAmount, 0, 0);
            }
            if (Input.GetKey(KeyCode.U))
            {
                rotatingDelta += new Vector3(rotatingAmount, 0, 0);
            }
            if (Input.GetKey(KeyCode.H))
            {
                rotatingDelta += new Vector3(0, -rotatingAmount, 0);
            }
            if (Input.GetKey(KeyCode.J))
            {
                rotatingDelta += new Vector3(0, rotatingAmount, 0);
            }
            if (Input.GetKey(KeyCode.N))
            {
                rotatingDelta += new Vector3(0, 0, -rotatingAmount);
            }
            if (Input.GetKey(KeyCode.M))
            {
                rotatingDelta += new Vector3(0, 0, rotatingAmount);
            }
            transform.localEulerAngles += rotatingDelta;
        }
    }
}