using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using AOT;

namespace Assets.NPR.Curvature
{
    public class PrincipalCurvatureDll
    {
        [DllImport("principalCurvature_bin")]
        public static extern int compute_principal_curvature(float[] vertices, int verticesCount, int[] triangles, int triangleCount,
            float[] outDirection1, float[] outDirection2, float[] outValues1, float[] outValues2, int radius, bool useKring);

        [DllImport("principalCurvature_bin")]
        public static extern int trimesh2_compute_principal_curvature(float[] vertices, int verticesCount, int[] triangles, int triangleCount,
            float[] outArray);

        [DllImport("principalCurvature_bin")]
        public static extern int compute_arapUv(float[] vertices, int verticesCount, int []triangles, int triangleCount, float []outUv, int iterations);

        [DllImport("principalCurvature_bin")]
        public static extern int random_points_on_mesh(float[] vertices, int verticesCount, int[] triangles, int triangleCount, int samplesCount, float[] outSamplesArray, int[] outOwnersArray);

        [DllImport("principalCurvature_bin")]
        private static extern void SetLoggingCallback(debugCallback cb);

        delegate void debugCallback(IntPtr request);

        private static bool loggingEnabled = false;

        public static void EnableLogging()
        {
            if (!loggingEnabled)
            {
                loggingEnabled = true;
                SetLoggingCallback(OnDebugCallback);
            }
        }

        private static int r = 0;
        [MonoPInvokeCallback(typeof(debugCallback))]
        static void OnDebugCallback(IntPtr request)
        {
            r++;
            string debugString = Marshal.PtrToStringAnsi(request);
            UnityEngine.Debug.Log("D1: " + debugString);
        }
    }
}
