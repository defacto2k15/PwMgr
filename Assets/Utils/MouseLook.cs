using UnityEngine;

namespace Assets.Utils
{
    public class MouseLook : MonoBehaviour //todo z https://answers.unity.com/questions/29741/mouse-look-script.html
    {
        public float mouseSensitivity = 100.0f;
        public float clampAngle = 80.0f;

        private float rotY = 0.0f; // rotation around the up/y axis
        private float rotX = 0.0f; // rotation around the right/x axis

        private bool _rotatingEnabled = true;

        void Start()
        {
            Vector3 rot = transform.localRotation.eulerAngles;
            rotY = rot.y;
            rotX = rot.x;
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.M))
            {
                _rotatingEnabled = !_rotatingEnabled;
            }

            if (Input.GetKeyDown(KeyCode.G))
            {
                var extendedFlycam = GetComponent<ExtendedFlycam>();
                extendedFlycam.enabled = !extendedFlycam.enabled;
            }

            if (_rotatingEnabled)
            {
                float mouseX = Input.GetAxis("Mouse X");
                float mouseY = -Input.GetAxis("Mouse Y");

                rotY += mouseX * mouseSensitivity * Time.deltaTime;
                rotX += mouseY * mouseSensitivity * Time.deltaTime;

                rotX = Mathf.Clamp(rotX, -clampAngle, clampAngle);

                Quaternion localRotation = Quaternion.Euler(rotX, rotY, 0.0f);
                transform.rotation = localRotation;
            }
        }
    }
}