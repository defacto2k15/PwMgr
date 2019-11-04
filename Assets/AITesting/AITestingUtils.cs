using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;

namespace Assets.AITesting
{
    public static class AITestingUtils
    {
        public static void RebuildNavSurfacesInScene()
        {
            var surfaces = Object.FindObjectsOfType<NavMeshSurface>();
            foreach (var aSurface in surfaces)
            {
                aSurface.BuildNavMesh();
            }
        }
    }
}
