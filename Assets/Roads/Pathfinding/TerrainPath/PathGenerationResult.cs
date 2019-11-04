using System.Collections.Generic;
using UnityEngine;

namespace Assets.Roads.Pathfinding.TerrainPath
{
    public class PathGenerationResult
    {
        public List<List<Vector2>> PathSegments;
        public bool GenerationSucceded;
    }
}