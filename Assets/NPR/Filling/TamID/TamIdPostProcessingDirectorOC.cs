using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.Utils;
using UnityEngine;

namespace Assets.NPR.Filling.TamID
{
    public class TamIdPostProcessingDirectorOC : MonoBehaviour
    {
        public Material AggregatingFragmentsRenderingMaterial;
        public Material MaxMinFragmentsRenderingMaterials;
        public Material TestAggregateTexRenderingMaterial;
        public Material TestCoefficientsSolverMaterial;
        public Material StrokeRenderingMaterial;

        private ComputeBuffer _tamidFragmentBuffer;
        private ComputeBuffer _tamidArgBuffer;

        private RenderTexture _aggregationTexture;
        private RenderTexture _maxMinTexture;
        private RenderTexture _dummySizedTexture;
        private RenderTexture _minMaxTexture;

        private int _fragmentTexWidth = 128;

        public ComputeBuffer TamidFragmentBuffer => _tamidFragmentBuffer;

        public void Start()
        {
            IntVector2 fragmentsGridSize = new IntVector2(_fragmentTexWidth, _fragmentTexWidth);
            int aggregationTextureStride = 6;

            _tamidFragmentBuffer = new ComputeBuffer(Screen.width * Screen.height,System.Runtime.InteropServices.Marshal.SizeOf(typeof(AppendBufferFragment)), ComputeBufferType.Append);

            _tamidArgBuffer = new ComputeBuffer(4, sizeof(int), ComputeBufferType.IndirectArguments);

            _aggregationTexture = new RenderTexture(fragmentsGridSize.X*aggregationTextureStride, fragmentsGridSize.Y, 0, RenderTextureFormat.ARGBFloat);
            _aggregationTexture.filterMode = FilterMode.Point;
            _aggregationTexture.wrapMode = TextureWrapMode.Clamp;
            _aggregationTexture.Create();

            _dummySizedTexture= new RenderTexture(fragmentsGridSize.X, fragmentsGridSize.Y, 0, RenderTextureFormat.ARGBFloat);
            _dummySizedTexture.Create();

            _minMaxTexture = new RenderTexture(fragmentsGridSize.X, fragmentsGridSize.Y, 0, RenderTextureFormat.RGFloat);
            _minMaxTexture.filterMode = FilterMode.Point;
            _minMaxTexture.Create();

            TestAggregateTexRenderingMaterial.SetTexture("_AggregateTex", _aggregationTexture);
            TestCoefficientsSolverMaterial.SetTexture("_AggregateTex", _aggregationTexture);
            TestCoefficientsSolverMaterial.SetTexture("_MinMaxTex", _minMaxTexture);

            StrokeRenderingMaterial.SetTexture("_AggregateTex", _aggregationTexture);
            StrokeRenderingMaterial.SetTexture("_MinMaxTex", _minMaxTexture);

            AggregatingFragmentsRenderingMaterial.SetInt("_FragmentTexWidth", _fragmentTexWidth);
            MaxMinFragmentsRenderingMaterials.SetInt("_FragmentTexWidth", _fragmentTexWidth);
            TestAggregateTexRenderingMaterial.SetInt("_FragmentTexWidth", _fragmentTexWidth);
            TestCoefficientsSolverMaterial.SetInt("_FragmentTexWidth", _fragmentTexWidth);
            StrokeRenderingMaterial.SetInt("_FragmentTexWidth", _fragmentTexWidth);
        }

        public int FragmentTexWidth => _fragmentTexWidth;

        public void Update()
        {
            int[] args = new int[]{0,1,0,0};
            _tamidArgBuffer.SetData(args);

            ComputeBuffer.CopyCount(_tamidFragmentBuffer, _tamidArgBuffer, 0);
            _tamidArgBuffer.GetData(args);
            Debug.Log($"A: {args[0]} {args[1]} {args[2]} {args[3]}");

            _tamidFragmentBuffer.SetCounterValue(0);
        }

        public void OnPreRender()
        {
            Graphics.SetRandomWriteTarget(1, _tamidFragmentBuffer);
        }

        public void OnPostRender()
        {
            Graphics.ClearRandomWriteTargets();
            var oldFrameBuffer = RenderTexture.active;

                AggregatingFragmentsRenderingMaterial.SetPass(0);
                AggregatingFragmentsRenderingMaterial.SetBuffer("_FragmentsBuffer", _tamidFragmentBuffer);
                RenderTexture.active = _aggregationTexture;
                // clear aggregate texture before summation
                GL.Clear(false, true, new Color(0,0,0,0));
                Graphics.DrawProceduralIndirectNow(MeshTopology.Points, _tamidArgBuffer, 0);

                MaxMinFragmentsRenderingMaterials.SetPass(0);
                MaxMinFragmentsRenderingMaterials.SetBuffer("_FragmentsBuffer", _tamidFragmentBuffer);
                RenderTexture.active = _minMaxTexture;
                // clear aggregate texture before summation
                GL.Clear(false, true, new Color(0,0,0,0));
                Graphics.DrawProceduralIndirectNow(MeshTopology.Points, _tamidArgBuffer, 0);

            RenderTexture.active = oldFrameBuffer;
            //Graphics.Blit(oldFrameBuffer, oldFrameBuffer, TestAggregateTexRenderingMaterial, 0);
            //Graphics.Blit(oldFrameBuffer, oldFrameBuffer, TestCoefficientsSolverMaterial, 0);
            //Graphics.Blit(oldFrameBuffer, oldFrameBuffer, StrokeRenderingMaterial, 0);

            StrokeRenderingMaterial.SetPass(0);
            Graphics.DrawProceduralNow(MeshTopology.Points, _fragmentTexWidth*_fragmentTexWidth);
            //Graphics.DrawProceduralNow(MeshTopology.LineStrip, 3);
        }

        void OnDestroy()
        {
            _tamidFragmentBuffer.Release();
            _tamidArgBuffer.Release();
        }

        private struct AppendBufferFragment
        {
            public  uint XAndY;
            public  uint TAndId;
        }
    }
}
