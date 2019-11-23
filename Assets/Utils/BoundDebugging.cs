using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.Utils
{
    public class BoundDebugging : MonoBehaviour
    {
        private static List<BoundsToDraw> _boundsToDraw = new List<BoundsToDraw>();
        private static Camera _camera;
        public bool IsEnabled = true;
        public bool DrawBoundsOfAllObjects = false;

        public static void AddBoundsToDraw(Bounds bounds, int lodLevel)
        {
            Color colorToDraw;
            colorToDraw = ColorToDrawFromLod(lodLevel);
            _boundsToDraw.Add(new BoundsToDraw(colorToDraw, bounds));
        }

        private static Color ColorToDrawFromLod(int lodLevel)
        {
            Color colorToDraw;
            switch (lodLevel)
            {
                case 0:
                    colorToDraw = Color.white;
                    break;
                case 1:
                    colorToDraw = Color.blue;
                    break;
                case 2:
                    colorToDraw = Color.red;
                    break;
                case 3:
                    colorToDraw = Color.green;
                    break;
                default:
                    colorToDraw = Color.gray;
                    break;
            }

            return colorToDraw;
        }

        public void OnDrawGizmos()
        {
            if (IsEnabled)
            {
                if (DrawBoundsOfAllObjects)
                {
                    int i = 0;
                    foreach (var bounds in FindObjectsOfType<Renderer>().Select(c => c.bounds))
                    {
                        Gizmos.color = ColorToDrawFromLod(i%5);
                        var modifiedBounds = new Bounds(bounds.center,new Vector3(bounds.size.x, bounds.size.y + i, bounds.size.z));
                        Gizmos.DrawCube(modifiedBounds.center, modifiedBounds.size);
                        i++;
                    }
                }
                else
                {
                    foreach (BoundsToDraw btd in _boundsToDraw)
                    {
                        Gizmos.color = btd.colorToDraw;
                        Gizmos.DrawCube(btd.bounds.center, btd.bounds.size);
                    }
                }

                if (_camera != null)
                {
                    Matrix4x4 old = Gizmos.matrix;
                    Gizmos.matrix = Matrix4x4.TRS(_camera.transform.position, _camera.transform.rotation, Vector3.one);
                    Gizmos.DrawFrustum(Vector3.zero, _camera.fieldOfView, 100, 0.1f, _camera.aspect);
                    Gizmos.matrix = old;
                }
            }
        }

        public static void SetCamera(Camera camera)
        {
            _camera = camera;
        }
    }

    internal class BoundsToDraw
    {
        public Color colorToDraw;
        public Bounds bounds;

        public BoundsToDraw(Color colorToDraw, Bounds bounds)
        {
            this.colorToDraw = colorToDraw;
            this.bounds = bounds;
        }
    }
}