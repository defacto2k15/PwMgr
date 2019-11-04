using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Utils.FontMap
{
    public class FontMapInjectOC : MonoBehaviour
    {
        public Texture2D FontMap;

        public void Start()
        {
            Shader.SetGlobalTexture("_G_FontMap", FontMap);            
        }
    }
}
