using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.Measuring;
using Assets.Utils;
using UnityEngine;

namespace Assets.NPR.Filling.TamID
{
    public class TamIssPostProcessingDirectorOC : MonoBehaviour, INprRenderingPostProcessingDirector
    {
        public Material AggregatingFragmentsRenderingMaterial;
        public Material MaxMinFragmentsRenderingMaterials;
        public Material StrokeRenderingMaterial;
        public Material TamIssFragmentGathering;

        private ComputeBuffer _tamidFragmentBuffer;
        private ComputeBuffer _tamidArgBuffer;

        private RenderTexture _aggregationTexture;
        private RenderTexture _dummySizedTexture;
        private RenderTexture _minMaxTexture;

        private int _fragmentTexWidth = 256;

        private RenderTexture _fragmentsRenderTarget;
        private RenderTexture _worldPositionRenderTarget;

        public ComputeBuffer TamidFragmentBuffer => _tamidFragmentBuffer;
        public int FragmentTexWidth => _fragmentTexWidth;
        public bool DrawUnderlyingHatchesOnFrame = false;

        public MeasurementRenderTargetsSet MeasurementRenderTargets;
        public bool AutonomicRendering;

        public void Start()
        {
            if (AutonomicRendering)
            {
                StartInternal();
            }
        }

        public void StartInternal()
        {
            InitializeMeasurementRenderTargets();
            InitializeRenderTargets();
            //UploadRenderTargetsToCamera();
            TamIssFragmentGathering.SetTexture("_FragmentsTex", _fragmentsRenderTarget);
            TamIssFragmentGathering.SetTexture("_WorldPositionTex",_worldPositionRenderTarget);

            IntVector2 fragmentsGridSize = new IntVector2(_fragmentTexWidth, _fragmentTexWidth);
            int aggregationTextureStride = 6;

            _tamidFragmentBuffer = new ComputeBuffer(Screen.width * Screen.height*2,System.Runtime.InteropServices.Marshal.SizeOf(typeof(AppendBufferFragment)), ComputeBufferType.Append);

            _tamidArgBuffer = new ComputeBuffer(4, sizeof(int), ComputeBufferType.IndirectArguments);

            _aggregationTexture = new RenderTexture(fragmentsGridSize.X*aggregationTextureStride, fragmentsGridSize.Y, 0, RenderTextureFormat.ARGBFloat);
            _aggregationTexture.filterMode = FilterMode.Point;
            _aggregationTexture.wrapMode = TextureWrapMode.Clamp;
            _aggregationTexture.Create();

            _dummySizedTexture = new RenderTexture(Screen.width, Screen.height, 32, RenderTextureFormat.ARGB32);
            _dummySizedTexture.Create();

            _minMaxTexture = new RenderTexture(fragmentsGridSize.X, fragmentsGridSize.Y, 0, RenderTextureFormat.RGFloat);
            _minMaxTexture.filterMode = FilterMode.Point;
            _minMaxTexture.Create();

            StrokeRenderingMaterial.SetTexture("_FragmentsTex", _fragmentsRenderTarget);
            StrokeRenderingMaterial.SetTexture("_WorldPositionTex", _worldPositionRenderTarget);
            StrokeRenderingMaterial.SetTexture("_AggregateTex", _aggregationTexture);
            StrokeRenderingMaterial.SetTexture("_AggregateTex2", _aggregationTexture);
            StrokeRenderingMaterial.SetTexture("_MinMaxTex", _minMaxTexture);

            AggregatingFragmentsRenderingMaterial.SetInt("_FragmentTexWidth", _fragmentTexWidth);
            MaxMinFragmentsRenderingMaterials.SetInt("_FragmentTexWidth", _fragmentTexWidth);
            StrokeRenderingMaterial.SetInt("_FragmentTexWidth", _fragmentTexWidth);
        }

        public void SetMeasurementRenderTargets(MeasurementRenderTargetsSet set)
        {
            MeasurementRenderTargets = set;
        }

        public void SetAutonomicRendering(bool autonomicRendering)
        {
            AutonomicRendering = autonomicRendering;
        }

        private void InitializeMeasurementRenderTargets()
        {
            if (AutonomicRendering)
            {
                var artisticMainRenderTexture = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32);
                artisticMainRenderTexture.filterMode = FilterMode.Point;

                var hatchMainRenderTexture = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32);
                hatchMainRenderTexture.filterMode = FilterMode.Point;

                var idRenderTexture = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32);
                idRenderTexture.filterMode = FilterMode.Point;

                var worldPos1RenderTexture = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32);
                worldPos1RenderTexture.filterMode = FilterMode.Point;

                var worldPos2RenderTexture = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32);
                worldPos2RenderTexture.filterMode = FilterMode.Point;

                MeasurementRenderTargets = new MeasurementRenderTargetsSet()
                {
                    ArtisticMainTexture = artisticMainRenderTexture,
                    HatchMainTexture = hatchMainRenderTexture,
                    HatchIdTexture = idRenderTexture,
                    WorldPosition1Texture = worldPos1RenderTexture,
                    WorldPosition2Texture = worldPos2RenderTexture
                };
            }
        }

        private void UploadRenderTargetsToCamera()
        {
            var renderBuffers = new List<RenderTexture>
            {
                MeasurementRenderTargets.ArtisticMainTexture,
                _fragmentsRenderTarget,
                _worldPositionRenderTarget,

                MeasurementRenderTargets.HatchMainTexture,
                MeasurementRenderTargets.HatchIdTexture,
                MeasurementRenderTargets.WorldPosition1Texture,
                MeasurementRenderTargets.WorldPosition2Texture
            }.Select(c => c.colorBuffer).ToArray();
            GetComponent<Camera>().SetTargetBuffers(renderBuffers, MeasurementRenderTargets.ArtisticMainTexture.depthBuffer);
        }

        private void InitializeRenderTargets()
        {
            _worldPositionRenderTarget = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGBFloat)
            {
                useMipMap = false, filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp
            };
            _worldPositionRenderTarget.Create();

            _fragmentsRenderTarget= new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB2101010)
            {
                useMipMap = false, filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp
            };
            _fragmentsRenderTarget.Create();
        }

        private void ClearRenderTargets()
        {
            var renderTextures = new List<RenderTexture>
            {
                _fragmentsRenderTarget,
                _worldPositionRenderTarget,
            }.ToList();
            var oldRenderTarget = RenderTexture.active;
            for (int i = 0; i < renderTextures.Count; i++)
            {
                RenderTexture.active = renderTextures[i];
                GL.Clear(true, true, Color.clear);
            }

            RenderTexture.active = oldRenderTarget;
        }

        public void OnPreRender()
        {
            if (AutonomicRendering)
            {
                OnPreRenderInternal();
            }
        }

        public void OnPreRenderInternal()
        {
            //Graphics.SetRandomWriteTarget(1, _tamidFragmentBuffer);
            UploadRenderTargetsToCamera();
        }

        public void OnRenderImage(RenderTexture src, RenderTexture dest)
        {
            if (AutonomicRendering)
            {
                OnRenderImageInternal(src,dest);
                Graphics.Blit(MeasurementRenderTargets.ArtisticMainTexture, dest);
            }
            else
            {
                Graphics.Blit(src,dest);
            }
        }

        public void OnRenderImageInternal( RenderTexture src, RenderTexture dest)
        {
            int[] args = new int[]{0,1,0,0};
            _tamidArgBuffer.SetData(args);
            ComputeBuffer.CopyCount(_tamidFragmentBuffer, _tamidArgBuffer, 0);
            _tamidArgBuffer.GetData(args);
            Debug.Log($"A: {args[0]} {args[1]} {args[2]} {args[3]}");
            _tamidFragmentBuffer.SetCounterValue(0);

            Graphics.ClearRandomWriteTargets();
            Graphics.SetRandomWriteTarget(1, _tamidFragmentBuffer);

            var localTarget = _dummySizedTexture;
            if (DrawUnderlyingHatchesOnFrame)
            {
                localTarget = MeasurementRenderTargets.ArtisticMainTexture;
            }
            Graphics.Blit(RenderTexture.active, localTarget, TamIssFragmentGathering);
            Graphics.ClearRandomWriteTargets();

            AggregatingFragmentsRenderingMaterial.SetPass(0);
            AggregatingFragmentsRenderingMaterial.SetBuffer("_FragmentsBuffer", _tamidFragmentBuffer);
            RenderTexture.active = _aggregationTexture;
            // clear aggregate texture before summation
            GL.Clear(false, true, new Color(0, 0, 0, 0));
            Graphics.DrawProceduralIndirectNow(MeshTopology.Points, _tamidArgBuffer, 0);

            MaxMinFragmentsRenderingMaterials.SetPass(0);
            MaxMinFragmentsRenderingMaterials.SetBuffer("_FragmentsBuffer", _tamidFragmentBuffer);
            RenderTexture.active = _minMaxTexture;
            // clear aggregate texture before summation
            GL.Clear(false, true, new Color(0, 0, 0, 0));
            Graphics.DrawProceduralIndirectNow(MeshTopology.Points, _tamidArgBuffer, 0);

            Graphics.SetRenderTarget(new RenderBuffer[]
            {
                //_colorRenderTarget.colorBuffer,
                MeasurementRenderTargets.ArtisticMainTexture.colorBuffer,
                MeasurementRenderTargets.HatchMainTexture.colorBuffer,
                MeasurementRenderTargets.HatchIdTexture.colorBuffer,
                MeasurementRenderTargets.WorldPosition1Texture.colorBuffer,
                MeasurementRenderTargets.WorldPosition2Texture.colorBuffer
            }, MeasurementRenderTargets.ArtisticMainTexture.depthBuffer);

            StrokeRenderingMaterial.SetPass(0);
            Graphics.DrawProceduralNow(MeshTopology.Points, _fragmentTexWidth * _fragmentTexWidth);
        }

        void OnDestroy()
        {
            _tamidFragmentBuffer.Release();
            _tamidArgBuffer.Release();
        }

        private struct AppendBufferFragment
        {
            public uint XAndY;
            public uint TAndId;
        }

    }
}
