using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Assets.Grass;
using Assets.Random.Fields;
using Assets.Ring2;
using Assets.ShaderUtils;
using Assets.Trees.RuntimeManagement.SubjectsInstancesContainer.GpuInstancing;
using Assets.Trees.RuntimeManagement.SubjectsInstancesContainer.GpuInstancing.Global;
using Assets.Trees.RuntimeManagement.SubjectsInstancesContainer.GpuInstancing.InstanceThread;
using Assets.Trees.RuntimeManagement.SubjectsInstancesContainer.GpuInstancing.UnityThread;
using Assets.Utils;
using Assets.Utils.TextureRendering;
using UnityEngine;

namespace Assets.Grass2
{
    public class Grass2DebugObject : MonoBehaviour
    {
        private GlobalGpuInstancingContainer _globalGpuInstancingContainer;

        public void Start()
        {
            var meshGenerator = new GrassMeshGenerator();
            var mesh = meshGenerator.GetGrassBladeMesh(1);

            var material = new Material(Shader.Find("Custom/Vegetation/Grass"));
            material.SetFloat("_BendingStrength", 0.2f);
            material.SetFloat("_InitialBendingValue", 0.4f);
            material.SetFloat("_PlantBendingStiffness", 0.3f);
            material.SetVector("_WindDirection", Vector4.one);
            material.SetVector("_PlantDirection", Vector4.one);
            material.SetColor("_Color", Color.green);
            material.SetFloat("_RandSeed", 44.4f);

            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.GetComponent<MeshFilter>().mesh = mesh;
            go.GetComponent<MeshRenderer>().material = material;

            var instancingMaterial = new Material(Shader.Find("Custom/Vegetation/Grass.Instanced"));
            instancingMaterial.enableInstancing = true;


            _globalGpuInstancingContainer = new GlobalGpuInstancingContainer();

            var commonUniforms = new UniformsPack();
            commonUniforms.SetUniform("_BendingStrength", 0.8f);
            commonUniforms.SetUniform("_WindDirection", Vector4.one);

            var instancingContainer = new GpuInstancingVegetationSubjectContainer(
                new GpuInstancerCommonData(mesh, instancingMaterial, commonUniforms),
                new GpuInstancingUniformsArrayTemplate(new List<GpuInstancingUniformTemplate>()
                {
                    new GpuInstancingUniformTemplate("_Color", GpuInstancingUniformType.Vector4),
                    new GpuInstancingUniformTemplate("_InitialBendingValue", GpuInstancingUniformType.Float),
                    new GpuInstancingUniformTemplate("_PlantBendingStiffness", GpuInstancingUniformType.Float),
                    new GpuInstancingUniformTemplate("_PlantDirection", GpuInstancingUniformType.Vector4),
                    new GpuInstancingUniformTemplate("_RandSeed", GpuInstancingUniformType.Float),
                })
            );
            var bucketId = _globalGpuInstancingContainer.CreateBucket(instancingContainer);

            var uniforms = new UniformsPack();
            uniforms.SetUniform("_InitialBendingValue", 0.4f);
            uniforms.SetUniform("_PlantBendingStiffness", 0.3f);
            uniforms.SetUniform("_PlantDirection", Vector4.one);
            uniforms.SetUniform("_Color", Color.green);
            uniforms.SetUniform("_RandSeed", 44.4f);

            var msw = new MyStopWatch();
            msw.StartSegment("AddingClassic");
            for (int x = 0; x < 300; x++)
            {
                for (int y = 0; y < 300; y++)
                {
                    var myTriplet = new MyTransformTriplet(new Vector3(x, 0, y), Vector3.zero, Vector3.one);
                    _globalGpuInstancingContainer.AddInstance(bucketId, myTriplet.ToLocalToWorldMatrix(), uniforms);
                }
            }

            _globalGpuInstancingContainer.StartThread();
            _globalGpuInstancingContainer.FinishUpdateBatch();
            Debug.Log("It took: " + msw.CollectResults());
        }

        private bool once = false;

        public void Update()
        {
            _globalGpuInstancingContainer.DrawFrame();
        }
    }
}