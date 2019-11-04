using System;
using UnityEngine;

namespace Assets.NPR
{
    [Serializable]
    public class ShaderBufferSE : ScriptableObject
    {
        [SerializeField] public float[] Data;
    }
}