using UnityEngine;

namespace Assets.Utils.CameraUtils
{
    [ExecuteInEditMode]
    public class CameraMouseRotatorByAnglesOC : MonoBehaviour
    {
        public Transform Target;

        [Range(0f, 6f)] public float DistanceFromCamera =1f;

        [Range(0f, 360f)] public float XRotation;
        [Range(0f, 360f)] public float YRotation;

        private Vector3 _offset;
        public bool MovingEnabled = true;

        public void Start()
        {
            var delta = (transform.position - Target.position);
            _offset = Target.position + delta.normalized * DistanceFromCamera;
        }

        public void LateUpdate()
        {
            if (!MovingEnabled)
            {
                return;
            }
            var newOffset = Quaternion.AngleAxis(XRotation , Vector3.up) *
                      Quaternion.AngleAxis(YRotation , Vector3.right) * _offset;

            newOffset *=  DistanceFromCamera;

            transform.position = Target.position + newOffset;
            transform.LookAt(Target.position);
        }
    }
}