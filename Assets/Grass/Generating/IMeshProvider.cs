using UnityEngine;

namespace Assets.Grass
{
    internal interface IMeshProvider
    {
        Mesh GetMesh(int lod);
    }
}