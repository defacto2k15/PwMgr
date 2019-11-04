using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.NPR.Filling.Solid
{
    public class SolidPatternTextureInjectorOC : MonoBehaviour
    {
        public Texture3D TextureToInject;

        public void Start()
        {
            GetComponent<MeshRenderer>().material.SetTexture("_SolidTex", TextureToInject);
        }
    }
}
