using System.Runtime.Serialization.Formatters;
using UnityEngine;

namespace Assets.Utils
{
    public class ExtendedFlycam : MonoBehaviour
    {
        public float cameraSensitivity = 90;
        public float climbSpeed = 4;
        public float normalMoveSpeed = 10;
        public float slowMoveFactor = 0.1f;
        public float zRotationSpeed = 1;
        public float fastMoveFactor = 3;

        private float rotationX = 0.0f;
        private float rotationY = 0.0f;
        private float rotationZ = 0.0f;
        public bool RotationEnabled = false;

        void Start()
        {
            rotationX = transform.localRotation.eulerAngles.x;
            rotationY = transform.localRotation.eulerAngles.y;
            rotationZ = transform.localRotation.eulerAngles.z;
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.M))
            {
                RotationEnabled = !RotationEnabled;
            }
            if (RotationEnabled)
            {
                var tempCameraSensivity = cameraSensitivity;
                if (Input.GetKey(KeyCode.Z))
                {
                    tempCameraSensivity /= 100;
                }

                rotationX += Input.GetAxis("Mouse X") * tempCameraSensivity* Time.deltaTime;
                rotationY += Input.GetAxis("Mouse Y") * tempCameraSensivity* Time.deltaTime;
                rotationY = Mathf.Clamp(rotationY, -90, 90);

                if (Input.GetKey(KeyCode.P))
                {
                    rotationX +=  0.01f*tempCameraSensivity* Time.deltaTime;
                }
                if (Input.GetKey(KeyCode.O))
                {
                    rotationX -= 0.01f* tempCameraSensivity* Time.deltaTime;
                }

            }
                var localZRotationSpeed = zRotationSpeed;
                if (Input.GetKey(KeyCode.Z))
                {
                    localZRotationSpeed/= 10;
                }
                if (Input.GetKey(KeyCode.T))
                {
                    rotationZ += localZRotationSpeed;
                }
                if (Input.GetKey(KeyCode.Y))
                {
                    rotationZ -= localZRotationSpeed;
                }

            transform.localRotation = Quaternion.AngleAxis(rotationX, Vector3.up);
            transform.localRotation *= Quaternion.AngleAxis(rotationY, Vector3.left);
            var eulerRotation = transform.localRotation.eulerAngles;
            eulerRotation.z = rotationZ;
            transform.localRotation = Quaternion.Euler(eulerRotation);

            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            {
                transform.position += transform.forward * (normalMoveSpeed * fastMoveFactor) *
                                      Input.GetAxis("Vertical") *
                                      Time.deltaTime;
                transform.position += transform.right * (normalMoveSpeed * fastMoveFactor) *
                                      Input.GetAxis("Horizontal") *
                                      Time.deltaTime;
            }
            //else if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
            else if (Input.GetKey(KeyCode.Z)) 
            {
                transform.position += transform.forward * (normalMoveSpeed * slowMoveFactor) *
                                      Input.GetAxis("Vertical") *
                                      Time.deltaTime;
                transform.position += transform.right * (normalMoveSpeed * slowMoveFactor) *
                                      Input.GetAxis("Horizontal") *
                                      Time.deltaTime;
            }
            else
            {
                transform.position += transform.forward * normalMoveSpeed * Input.GetAxis("Vertical") * Time.deltaTime;
                transform.position += transform.right * normalMoveSpeed * Input.GetAxis("Horizontal") * Time.deltaTime;
            }


            if (Input.GetKey(KeyCode.Q))
            {
                transform.position += transform.up * climbSpeed * Time.deltaTime;
            }
            if (Input.GetKey(KeyCode.E))
            {
                transform.position -= transform.up * climbSpeed * Time.deltaTime;
            }

            if (Input.GetKeyDown(KeyCode.End))
            {
                Screen.lockCursor = (Screen.lockCursor == false) ? true : false;
            }
        }
    }
}