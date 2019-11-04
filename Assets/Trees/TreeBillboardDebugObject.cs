using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.Trees.Generation;
using Assets.Utils;
using UnityEngine;
using UnityEngine.Rendering;

namespace Assets.Trees
{
    public class TreeBillboardDebugObject : MonoBehaviour
    {
        public Texture CollageTex;
        public float BillboardCount;
        public float ColumnsCount;
        public float RowsCount;
        private Mesh _mesh;
        private Material _material;
        private Matrix4x4[] _maticesArray;
        private MaterialPropertyBlock _block;

        private Vector3 _headScale;

        [Range(5, 80)] public int TreeBoxSideLength = 20;
        [Range(1, 30)] public float FlatSizemingFactor = 20;
        [Range(0, 360)] public float XRotation = 0;
        [Range(0, 360)] public float YRotation = 0;
        [Range(0, 360)] public float ZRotation = 0;
        [Range(0, 1)] public float AlphaCutoff = 0;


        public void Start()
        {
            TreePrefabManager prefabManager = new TreePrefabManager();
            var loadedClan = prefabManager.LoadTreeClan("clan1");
            var pyramid = loadedClan.Pyramids[0];

            _material = new Material(Shader.Find("Custom/Vegetation/GenericBillboard.Instanced"));
            _mesh = GameObject.CreatePrimitive(PrimitiveType.Quad).GetComponent<MeshFilter>().mesh;

            _headScale = pyramid.CollageTexture.ScaleOffsets;

            _block = new MaterialPropertyBlock();
            //_block.SetTexture("_CollageTex", CollageTex);
            //_block.SetFloat("_BillboardCount", BillboardCount);
            //_block.SetFloat("_ColumnsCount", ColumnsCount);
            //_block.SetFloat("_RowsCount", RowsCount);

            _block.SetTexture("_CollageTex", pyramid.CollageTexture.Collage);
            _block.SetFloat("_BillboardCount", pyramid.CollageTexture.SubTexturesCount);
            _block.SetFloat("_ColumnsCount", pyramid.CollageTexture.ColumnsCount);
            _block.SetFloat("_RowsCount", pyramid.CollageTexture.LinesCount);
        }

        public void Update()
        {
            List<Matrix4x4> maticesList = new List<Matrix4x4>();
            for (int x = 0; x < TreeBoxSideLength; x++)
            {
                for (int y = 0; y < TreeBoxSideLength; y++)
                {
                    maticesList.Add(
                        TransformUtils.GetLocalToWorldMatrixWithEulerAngles(
                            new Vector3(x * FlatSizemingFactor, 0, y * FlatSizemingFactor),
                            new Vector3(XRotation, YRotation, ZRotation),
                            _headScale));
                }
            }
            _maticesArray = maticesList.ToArray();
            _block.SetFloat("_AlphaCutoff", AlphaCutoff);
            Graphics.DrawMeshInstanced(_mesh, 0, _material, _maticesArray, _maticesArray.Length, _block,
                ShadowCastingMode.Off);

            //var rotationOffsetList = new List<float>();
            //for (int x = 0; x < TreeBoxSideLength; x++)
            //{
            //        maticesList.Add(
            //            TransformUtils.GetLocalToWorldMatrixWithEulerAngles(new Vector3(x * FlatSizemingFactor+1.1f, 0, 0 * FlatSizemingFactor),
            //            new Vector3(XRotation, YRotation, ZRotation),
            //            _headScale));
            //        rotationOffsetList.Add(0.0f + 0.2f * (x * 1f/TreeBoxSideLength));
            //}
            //_maticesArray = maticesList.ToArray();
            //var rotationArray = rotationOffsetList.ToArray();
            //_block.SetFloatArray("_BaseYRotation", rotationArray);

            //Graphics.DrawMeshInstanced(_mesh, 0, _material, _maticesArray, _maticesArray.Length, _block, ShadowCastingMode.Off);

            //rotationOffsetList = new List<float>();
            //maticesList.Clear();
            //for (int x = 0; x < TreeBoxSideLength; x++)
            //{
            //        maticesList.Add(
            //            TransformUtils.GetLocalToWorldMatrixWithEulerAngles(new Vector3(x * FlatSizemingFactor, 0, 0.2f * FlatSizemingFactor),
            //            new Vector3(XRotation, YRotation, ZRotation),
            //            _headScale));
            //        rotationOffsetList.Add(0.8f + 0.2f * (x * 1f/TreeBoxSideLength));
            //}
            //_maticesArray = maticesList.ToArray();
            //rotationArray = rotationOffsetList.ToArray();
            //_block.SetFloatArray("_BaseYRotation", rotationArray);
            //Graphics.DrawMeshInstanced(_mesh, 0, _material, _maticesArray, _maticesArray.Length, _block, ShadowCastingMode.Off);
        }
    }
}