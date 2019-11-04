using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts
{
    [ExecuteInEditMode]
    public class   CartoonMaterialPropSetterOC : MonoBehaviour
    {
        public Color BaseColor;
        [Range(0,1)]
        public float Alpha;
        public String OurTag;

        public void Start()
        {
                UpdateColor();
        }

        public void OnValidate()
        {
            UpdateColor();
        }
        private void UpdateColor()
        {
            var renderers = GetComponentsInChildren<Transform>().Where(c => c.tag.Equals(OurTag)).Select(c => c.gameObject.GetComponent<MeshRenderer>()).Where(c => c != null)
                .ToList();
                //var renderers = GetComponentsInChildren<MeshRenderer>();
                //var renderers = GetComponentsInChildren<MeshRenderer>();
            foreach (var aRenderer in renderers)
            {
                MaterialPropertyBlock properties = new MaterialPropertyBlock();

                aRenderer.GetPropertyBlock(properties);
                properties.SetColor("_BaseColor", BaseColor);
                properties.SetFloat("_Alpha", Alpha);
                aRenderer.SetPropertyBlock(properties);
            }
        }
    }
}
