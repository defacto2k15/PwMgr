﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.NPR.TamID
{
    // from https://cheneyshen.com/category/all/gpu/compute-shader/
    public class TamIdTest1OC : MonoBehaviour
    {
        public Material postProcessingMaterial;
        public Shader appendBufferShader;

        const int width = 32;
        const float size = 5.0f;

        ComputeBuffer buffer;
        ComputeBuffer argBuffer;

        void Start()
        {
            Material mat = new Material(appendBufferShader);
            mat.hideFlags = HideFlags.HideAndDontSave;

            RenderTexture tex = new RenderTexture(width, width, 0);

            buffer = new ComputeBuffer(width * width, sizeof(float) * 3, ComputeBufferType.Append);
            buffer.SetCounterValue(0);

            mat.SetBuffer("appendBuffer", buffer);
            mat.SetFloat("size", size);
            mat.SetFloat("width", width);

            Graphics.SetRandomWriteTarget(1, buffer);
            Graphics.Blit(null, tex, mat, 0);
            Graphics.ClearRandomWriteTargets();


            argBuffer = new ComputeBuffer(4, sizeof(int), ComputeBufferType.IndirectArguments);

            int[] args = new int[] {0, 1, 0, 0};
            argBuffer.SetData(args);

            ComputeBuffer.CopyCount(buffer, argBuffer, 0);
            argBuffer.GetData(args);

            Debug.Log("vertex count " + args[0]);
            Debug.Log("instance count " + args[1]);
            Debug.Log("start vertex " + args[2]);
            Debug.Log("start instance " + args[3]);
        }

        void OnPostRender()
        {
            postProcessingMaterial.SetPass(0);
            postProcessingMaterial.SetBuffer("buffer", buffer);
            //material.SetColor("col", Color.blue);
            Graphics.DrawProceduralIndirectNow(MeshTopology.Points, argBuffer,0);
        }

        void OnDestroy()
        {
            buffer.Release();
            argBuffer.Release();
        }
    }
}
