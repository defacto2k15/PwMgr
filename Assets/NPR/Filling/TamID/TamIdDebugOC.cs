using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.Utils;
using UnityEngine;

namespace Assets.NPR.Filling.TamID
{
    public class TamIdDebugOC : MonoBehaviour
    {
        private RunOnceBox _once;

        public void Start()
        {
            _once = new RunOnceBox(() =>
            {
                var tamIdPostProcessingDirectorOc = FindObjectOfType<TamIdPostProcessingDirectorOC>();
                var tamTex = GenerateMockTexture(tamIdPostProcessingDirectorOc.FragmentTexWidth);
                var mat = GetComponent<MeshRenderer>().material;
                mat.SetTexture("_TamIdTex", tamTex);

                mat.SetBuffer("_AppendBuffer", tamIdPostProcessingDirectorOc.TamidFragmentBuffer);
                mat.SetInt("_FragmentTexWidth", tamIdPostProcessingDirectorOc.FragmentTexWidth);
            });
        }

        public void Update()
        {
            _once.Update();
        }

        private Texture2D GenerateMockTexture(int fragmentTexWidth)
        {
            var width = 256;
            var tex = new Texture2D(width,width,TextureFormat.ARGB32,false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < width; y++)
                {
                    tex.SetPixel(x,y, new Color(0,0,0,0));
                }
            }

            IntVector2 strokeSize = new IntVector2(64, 4);

            for (uint i = 0; i < 64; i++)
            {
                PlaceStroke(i, new IntVector2(UnityEngine.Random.Range(0, 250), UnityEngine.Random.Range(0, 250)), strokeSize, tex, fragmentTexWidth);
            }
            //PlaceStroke(0, new IntVector2(220, 170), strokeSize, tex);
            ////PlaceStroke(1, new IntVector2(64, 64), strokeSize, tex);
            ////PlaceStroke(2, new IntVector2(300, 128), strokeSize, tex);

            return tex;
        }

        private static void PlaceStroke(uint id, IntVector2 offset, IntVector2 strokeSize, Texture2D tex, int fragmentTexWidth)
        {
            for (int x = offset.X; x < offset.X + strokeSize.X; x++)
            {
                for (int y = offset.Y; y < offset.Y + strokeSize.Y; y++)
                {
                    var t = (x - offset.X) / (float) strokeSize.X;
                    if (x < tex.width && y < tex.height)
                    {
                        tex.SetPixel(x, y, new Color(t, id%fragmentTexWidth / (float)(fragmentTexWidth-1), Mathf.FloorToInt(id/(float)fragmentTexWidth) / (float)(fragmentTexWidth-1), 1));
                    }
                }
            }

            tex.Apply();
        }
    }
}
