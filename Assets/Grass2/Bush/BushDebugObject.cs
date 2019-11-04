using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.Grass;
using Assets.Grass2.Billboards;
using Assets.Heightmaps.Ring1.RenderingTex;
using Assets.Heightmaps.Ring1.TerrainDescription.FeatureGenerating;
using Assets.Ring2.Devising;
using Assets.Utils;
using Assets.Utils.MT;
using Assets.Utils.TextureRendering;
using UnityEngine;

namespace Assets.Grass2.Bush
{
    public class BushDebugObject : MonoBehaviour
    {
        public ComputeShaderContainerGameObject ComputeShaderContainer;

        public void Start2()
        {
            TaskUtils.SetGlobalMultithreading(false);

            var meshGenerator = new GrassMeshGenerator();
            var mesh = meshGenerator.GetGrassBillboardMesh(0, 1);


            var generator = new Grass2BillboardGenerator(new UTTextureRendererProxy(new TextureRendererService(
                new MultistepTextureRenderer(ComputeShaderContainer), new TextureRendererServiceConfiguration()
                {
                    StepSize = new Vector2(10, 10)
                })), new Grass2BillboardGenerator.Grass2BillboardGeneratorConfiguration()
            {
                BillboardSize = new IntVector2(256, 256)
            });

            var tex = generator.GenerateBillboardImageAsync(40, 1).Result;
            tex.filterMode = FilterMode.Point;

            var material = new Material(Shader.Find("Custom/Vegetation/GrassBushBillboard"));
            material.SetTexture("_MainTex", tex);

            for (int x = 0; x < 30; x++)
            {
                for (int y = 0; y < 30; y++)
                {
                    var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                    go.transform.localPosition = new Vector3(x, 0, y);
                    go.GetComponent<MeshFilter>().mesh = mesh;
                    go.GetComponent<MeshRenderer>().material = material;
                }
            }
        }

        public void Start()
        {
            var meshGenerator = new GrassMeshGenerator();
            var mesh = meshGenerator.GetGrassBillboardMesh(0, 1);

            var normal = new Vector3(0.5f, 0.5f, 0).normalized;
            var rotationA = Quaternion.LookRotation(normal);
            var transf = new MyTransformTriplet(
                new Vector3(0, 0, 0),
                rotationA.eulerAngles * Mathf.Deg2Rad,
                new Vector3(1, 1, 1));

            var matrix = transf.ToLocalToWorldMatrix();

            for (int i = 0; i < 360; i += 30)
            {
                CreateObjectDebug(mesh, matrix, i);
            }

            //var transf2 = new MyTransformTriplet(new Vector3(0,0,0), new Vector3(0, 60*Mathf.Deg2Rad, 0), new Vector3(1,1,1)  );
            //var matrix2 = transf2.ToLocalToWorldMatrix();

            //var finalMatrix = matrix * matrix2;

            //go.transform.localPosition = finalMatrix.ExtractPosition();
            //go.transform.rotation = finalMatrix.ExtractRotation();
            //go.transform.localScale = finalMatrix.ExtractScale();
        }

        private void CreateObjectDebug(Mesh mesh, Matrix4x4 baseMatrix, float rotation)
        {
            var transf2 = new MyTransformTriplet(new Vector3(0, 0, 0), new Vector3(0, rotation * Mathf.Deg2Rad, 0),
                new Vector3(1, 1, 1));
            var matrix2 = transf2.ToLocalToWorldMatrix();
            var finalMatrix = baseMatrix * matrix2;

            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.transform.localPosition = new Vector3(0, 0, 0);
            go.GetComponent<MeshFilter>().mesh = mesh;
            go.name = rotation.ToString();

            go.transform.localPosition = finalMatrix.ExtractPosition();
            go.transform.rotation = finalMatrix.ExtractRotation();
            go.transform.localScale = finalMatrix.ExtractScale();
        }


        public void Start4()
        {
            var normal = new Vector3(0.5f, 0.5f, 0).normalized;
            var rotationA = Quaternion.LookRotation(normal);
            var a1 = rotationA.eulerAngles;

            var tt1 = new MyTransformTriplet(Vector3.zero, rotationA.eulerAngles * Mathf.Deg2Rad,
                new Vector3(1, 14, 7));

            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);

            var finalMatrix = tt1.ToLocalToWorldMatrix();
            go.transform.localPosition = finalMatrix.ExtractPosition();
            go.transform.rotation = finalMatrix.ExtractRotation();
            go.transform.localScale = finalMatrix.ExtractScale();

            var go2 = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go2.transform.rotation = rotationA;
            go2.transform.localScale = new Vector3(1, 14, 7);

            Debug.Log("Rot1: " + a1);
        }
    }
}