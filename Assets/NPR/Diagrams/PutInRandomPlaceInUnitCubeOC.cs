using UnityEngine;

namespace Assets.Scripts
{
    [ExecuteInEditMode]
    public class   PutInRandomPlaceInUnitCubeOC : MonoBehaviour
    {
        public int Reloader;
        public float CubeSide;

        public void Start()
        {
            UpdatePosition();
        }

        public void OnValidate()
        {
            UpdatePosition();
        }

        private void UpdatePosition()
        {
            var seed = gameObject.GetInstanceID() + Reloader;
            var rand = new System.Random(seed);
            var pos = new Vector3((float) rand.NextDouble(), (float) rand.NextDouble(), (float) rand.NextDouble());
            pos *= 2;
            pos = pos - new Vector3(1,1,1);

            pos = pos * CubeSide;
            transform.localPosition = pos;
        }
    }
}