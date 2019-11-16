using Assets.Repositioning;
using Assets.Utils;
using Assets.Utils.UTUpdating;
using UnityEngine;

namespace Assets.Heightmaps.Ring1
{
    public class FovData
    {
        private Vector3 _cameraPosition;
        private Plane[] _frustumPlanes;

        public FovData(Vector3 cameraPosition, Plane[] frustumPlanes)
        {
            this._cameraPosition = cameraPosition;
            this._frustumPlanes = frustumPlanes;
        }

        public Vector3 CameraPosition
        {
            get { return _cameraPosition; }
        }

        public bool IsIn(Bounds bounds)
        {
            //bool isIn = GeometryUtility.TestPlanesAABB(_frustumPlanes, bounds);//old way
            //if (isIn)
            //{
            //    BoundDebugging.AddBoundsToDraw(bounds, nodeData.LodLevel);
            //}
            //return isIn;

            return true; // todo find multithreading solution!
        }

        public static FovData FromCamera(Camera camera, Repositioner repositioner = null)
        {
            var lastPosition = camera.transform.position;

            var newPosition = new Vector3(lastPosition.x, 1, lastPosition.z);
            if (repositioner != null)
            {
                newPosition = repositioner.InvMove(newPosition);
            }
            camera.transform.position = newPosition;
            var data = new FovData(camera.transform.position, GeometryUtility.CalculateFrustumPlanes(camera));
            camera.transform.position = lastPosition;
            return data;
        }

        public static FovData FromCamera(ICameraForUpdate camera, Repositioner repositioner = null)
        {
            var lastPosition = camera.Position;

            var newPosition = new Vector3(lastPosition.x, 1, lastPosition.z);
            if (repositioner != null)
            {
                newPosition = repositioner.InvMove(newPosition);
            }
            camera.Position = newPosition;
            //BoundDebugging.SetCamera(camera);
            var data = new FovData(camera.Position, camera.CalculateFrustumPlanes(camera) );
            camera.Position = lastPosition;
            return data;
        }
    }
}