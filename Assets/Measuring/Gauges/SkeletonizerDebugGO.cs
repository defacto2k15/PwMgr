using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.Utils.TextureRendering;
using Assets.Utils.Textures;
using UnityEngine;

namespace Assets.Measuring.Gauges
{
    public class SkeletonizerDebugGO : MonoBehaviour
    {
        public Material SkeletonizerMaterial;
        public Texture2D InputTexture;
        public RenderTexture OutTexture0;
        public RenderTexture OutTexture1;

        private int _currentOutTexture = 0;


        public void Start()
        {
            OutTexture0 = new RenderTexture(InputTexture.height, InputTexture.width, 0, RenderTextureFormat.ARGBFloat);
            OutTexture0.filterMode = FilterMode.Point;
            OutTexture0.wrapMode = TextureWrapMode.Clamp;

            OutTexture1 = new RenderTexture(InputTexture.height, InputTexture.width, 0, RenderTextureFormat.ARGBFloat);
            OutTexture1.filterMode = FilterMode.Point;
            OutTexture1.wrapMode = TextureWrapMode.Clamp;

            Graphics.Blit(InputTexture, OutTexture0, SkeletonizerMaterial, 1);

            GetComponent<MeshRenderer>().material.SetTexture("_MainTex", OutTexture0);
        }

        public void Update()
        {
            if (Input.GetKeyDown(KeyCode.A))
            {
                var src = CurrentRenderTexture;
                _currentOutTexture = (_currentOutTexture + 1) % 2;
                var dst = CurrentRenderTexture;
                Graphics.Blit(src,dst, SkeletonizerMaterial, 0);
                GetComponent<MeshRenderer>().material.SetTexture("_MainTex", dst);
            }
        }

        public RenderTexture CurrentRenderTexture
        {
            get
            {
                if (_currentOutTexture == 0)
                {
                    return OutTexture0;
                }
                else
                {
                    return OutTexture1;
                }
            }
        }
    }

    public class HatchSkeletonizer
    {
        private Material _skeletonizerMaterial;
        private int _tapsCount;
        private RenderTexture _outTexture0;
        private RenderTexture _outTexture1;
        private int _currentOutTexture = 0;

        public HatchSkeletonizer(Material skeletonizerMaterial, int tapsCount)
        {
            _skeletonizerMaterial = skeletonizerMaterial;
            _tapsCount = tapsCount;
        }

        public void Initialize()
        {
            _outTexture0 = new RenderTexture(Screen.width,Screen.height, 0, RenderTextureFormat.ARGB32);
            _outTexture0.filterMode = FilterMode.Point;
            _outTexture0.wrapMode = TextureWrapMode.Clamp;

            _outTexture1 = new RenderTexture(Screen.width,Screen.height, 0, RenderTextureFormat.ARGB32);
            _outTexture1.filterMode = FilterMode.Point;
            _outTexture1.wrapMode = TextureWrapMode.Clamp;
        }

        public LocalTexture Skeletonize(Texture hatchMainTexture)
        {
            Graphics.Blit(hatchMainTexture, _outTexture0, _skeletonizerMaterial, 1);

            for (int i = 0; i < _tapsCount; i++)
            {
                var src = CurrentRenderTexture;
                _currentOutTexture = (_currentOutTexture + 1) % 2;
                var dst = CurrentRenderTexture;
                Graphics.Blit(src,dst, _skeletonizerMaterial, 0);
            }

            return LocalTexture.FromTexture2D(UltraTextureRenderer.RenderTextureToTexture2D(CurrentRenderTexture));
        }

        private RenderTexture CurrentRenderTexture
        {
            get
            {
                if (_currentOutTexture == 0)
                {
                    return _outTexture0;
                }
                else
                {
                    return _outTexture1;
                }
            }
        }

    }
}
