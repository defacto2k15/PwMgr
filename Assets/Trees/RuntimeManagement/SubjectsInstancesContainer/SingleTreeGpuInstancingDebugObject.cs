using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.Grass;
using Assets.Utils;
using UnityEngine;

namespace Assets.Trees.RuntimeManagement.SubjectsInstancesContainer
{
    public class SingleTreeGpuInstancingDebugObject : MonoBehaviour
    {
        public Tree SomeTree;
        public Texture MainTexture;

        //public Color _Color;
        //public Texture _MainTex;
        //public Texture _BumpSpecMap;
        //public Texture _TranslucencyMap;
        //public Color _SpecColor;
        //public Vector4 _TreeInstanceColor;
        //public Vector4 _TreeInstanceScale;
        //public float _SquashAmount;

        public Texture _MainTex;
        public Texture _ShadowTex;
        public Texture _BumpSpecMap;
        public Texture _TranslucencyMap;

        private Mesh _mesh;
        private Matrix4x4[] maticesArray = new Matrix4x4[MyConstants.MaxInstancesPerPack];
        private Material _material;
        private MaterialPropertyBlock _block;

        public void Start()
        {
            _mesh = SomeTree.GetComponent<MeshFilter>().mesh;

            var renderer = SomeTree.GetComponent<MeshRenderer>();
            maticesArray[0] = (TransformUtils.GetLocalToWorldMatrix(Vector3.zero, Vector3.zero, Vector3.one));
            //_material = new Material(Shader.Find("Custom/Nature/Tree Creator Bark Optimized"));
            _material = new Material(Shader.Find("Custom/Nature/Tree Creator Leaves Optimized Ugly"));

            var oldMaterial = renderer.sharedMaterials[1];

            _MainTex = oldMaterial.GetTexture("_MainTex");
            _ShadowTex = oldMaterial.GetTexture("_ShadowTex");
            _BumpSpecMap = oldMaterial.GetTexture("_BumpSpecMap");
            _TranslucencyMap = oldMaterial.GetTexture("_TranslucencyMap");

            //_Color = oldMaterial.GetColor("_Color");
            //_MainTex = oldMaterial.GetTexture("_MainTex");
            //_BumpSpecMap = oldMaterial.GetTexture("_BumpSpecMap");
            //_TranslucencyMap = oldMaterial.GetTexture("_TranslucencyMap");

            //_SpecColor = oldMaterial.GetColor("_SpecColor");
            //_TreeInstanceColor = oldMaterial.GetVector("_TreeInstanceColor");
            //_TreeInstanceScale = oldMaterial.GetVector("_TreeInstanceScale");
            //_SquashAmount = oldMaterial.GetFloat("_SquashAmount");

            _block = new MaterialPropertyBlock();
        }

        public void Update()
        {
            //_block.SetColor("_Color", _Color);
            //_block.SetTexture("_MainTex", _MainTex);
            //_block.SetTexture("_BumpSpecMap", _BumpSpecMap);
            //_block.SetTexture("_TranslucencyMap", _TranslucencyMap);

            //_block.SetColor("_SpecColor", _SpecColor);
            //_block.SetVector("_TreeInstanceColor", _TreeInstanceColor);
            //_block.SetVector("_TreeInstanceScale", _TreeInstanceScale);
            //_block.SetFloat("_SquashAmount", _SquashAmount);


            _block.SetTexture("_MainTex", _MainTex);
            _block.SetTexture("_ShadowTex", _ShadowTex);
            _block.SetTexture("_BumpSpecMap", _BumpSpecMap);
            _block.SetTexture("_TranslucencyMap", _TranslucencyMap);

            Graphics.DrawMeshInstanced(_mesh, 1, _material, maticesArray, 1, _block);
        }
    }
}
//_Color ("Main Color", Color) = (1,1,1,1)
//_MainTex ("Base (RGB) Alpha (A)", 2D) = "white" {}
//_BumpSpecMap ("Normalmap (GA) Spec (R)", 2D) = "bump" {}
//_TranslucencyMap ("Trans (RGB) Gloss(A)", 2D) = "white" {}

//// These are here only to provide default values
//_SpecColor ("Specular Color", Color) = (0.5, 0.5, 0.5, 1)
//[HideInInspector] _TreeInstanceColor ("TreeInstanceColor", Vector) = (1,1,1,1)
//[HideInInspector] _TreeInstanceScale ("TreeInstanceScale", Vector) = (1,1,1,1)
//[HideInInspector] _SquashAmount ("Squash", Float) = 1