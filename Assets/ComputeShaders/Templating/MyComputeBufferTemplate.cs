using System;
using UnityEngine;

namespace Assets.ComputeShaders.Templating
{
    public class MyComputeBufferTemplate
    {
        public int Count;
        public int Stride;
        public ComputeBufferType Type;
        public Array BufferData;
    }
}