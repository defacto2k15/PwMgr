using UnityEngine;

namespace Assets.Utils.CameraUtils
{
    public class CameraMouseRotationOC : MonoBehaviour
    {
        public Transform Target;
        [Range(0f, 10f)] public float TurnSpeed =1f;
        [Range(0f, 10f)] public float ZoomSpeed =1f;

        private Vector3 _offset;
        public bool MovingEnabled = true;

        public void Start()
        {
            _offset = Target.position + transform.position;
        }

        public void LateUpdate()
        {
            if (Input.GetKeyDown(KeyCode.M))
            {
                MovingEnabled = !MovingEnabled;
            }
            if (!MovingEnabled)
            {
                return;
            }
            _offset = Quaternion.AngleAxis(Input.GetAxis("Mouse X") * TurnSpeed, Vector3.up) *
                     Quaternion.AngleAxis(Input.GetAxis("Mouse Y") * TurnSpeed, Vector3.right) * _offset;

            _offset *= 1 + Input.GetAxis("Mouse ScrollWheel")*ZoomSpeed;

            transform.position = Target.position + _offset;
            transform.LookAt(Target.position);
        }
    }
}
