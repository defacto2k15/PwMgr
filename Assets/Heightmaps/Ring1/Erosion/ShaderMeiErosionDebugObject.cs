using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.ComputeShaders;
using Assets.Heightmaps.Ring1.Creator;
using Assets.MeshGeneration;
using Assets.Utils;
using Assets.Utils.ArrayUtils;
using Assets.Utils.MT;
using UnityEngine;

namespace Assets.Heightmaps.Ring1.Erosion
{
    public class ShaderMeiErosionDebugObject : MonoBehaviour
    {
        public ComputeShader MeiErosionShader;
        public GameObject OutputShowingObject;
        public HeightmapArray _currentHeightmap;
        private Texture _conventionalOutputTexture;
        private GameObject _go;
        private IntVector2 _sizeOfTexture = new IntVector2(240, 240);

        private UnityThreadComputeShaderExecutorObject ShaderExecutorObject =
            new UnityThreadComputeShaderExecutorObject();

        public int StepCount = 50;

        [Range(0.05f * 0.1f, 0.05f * 5)] public float A_PipeCrossSection = 0.05f;

        [Range((1 / 64f) * 0.1f, (1 / 64f) * 5)] public float ConstantWaterAdding = 1f / 64;

        [Range(9.81f * 0.1f, 9.81f * 5)] public float GravityAcceleration = 9.81f;

        [Range(1f * 0.1f, 1f * 5)] public float DeltaT = 1f;

        [Range(0.0001f * 12 * 0.1f, 0.0001f * 12 * 5)] public float DepositionConstant = 0.0001f * 12;

        [Range(0.0001f * 12 * 0.1f, 0.0001f * 12 * 5)] public float DissolvingConstant = 0.0001f * 12;

        [Range(0.05f * 0.1f, 0.05f * 5)] public float EvaporationConstant = 0.05f * 10;

        public Vector2 GridSize = new Vector2(1, 1);

        [Range(0.1f, 10)] public float L_PipeLength = 1;

        [Range(250f * 0f, 250f * 5)] public float SedimentCapacityConstant = 250;


        public void Start()
        {
            TaskUtils.SetGlobalMultithreading(false);
            var heightTex = SavingFileManager.LoadPngTextureFromFile(@"C:\inz\cont\smallCut.png", 240, 240,
                TextureFormat.RGBA32, true, true);
            _currentHeightmap = HeightmapUtils.CreateHeightmapArrayFromTexture(heightTex);

            MyArrayUtils.Normalize(_currentHeightmap.HeightmapAsArray);
            MyArrayUtils.InvertNormalized(_currentHeightmap.HeightmapAsArray);

            //for (int x = 0; x < 40; x++)
            //{
            //    for (int y = 0; y < 40; y++)
            //    {
            //        _currentHeightmap.HeightmapAsArray[90+x, 90+y] = 0;
            //    }
            //}

            CreateComparisionObject();
            MyArrayUtils.Multiply(_currentHeightmap.HeightmapAsArray, 400);


            Mei_RenderComputeShader();
        }

        public void Update()
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                Mei_RenderComputeShader();
            }
            if (Input.GetKeyDown(KeyCode.O))
            {
                StepCount--;
                Mei_RenderComputeShader();
            }
            if (Input.GetKeyDown(KeyCode.P))
            {
                StepCount++;
                Mei_RenderComputeShader();
            }
        }

        public void RecreateWithStepChange(int stepDelta)
        {
            StepCount += stepDelta;
            Mei_RenderComputeShader();
        }

        private void CreateComparisionObject()
        {
            _go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            var material = new Material(Shader.Find("Custom/Terrain/Terrain_Debug_V2"));
            _go.GetComponent<MeshRenderer>().material = material;
            _go.name = "Terrain";
            _go.transform.localRotation = Quaternion.Euler(0, 0, 0);
            _go.transform.localScale = new Vector3(10, 1, 10);
            _go.transform.localPosition = new Vector3(0, 0, 0);
            _go.GetComponent<MeshFilter>().mesh = PlaneGenerator.CreateFlatPlaneMesh(240, 240);

            var doneTexture =
                HeightmapUtils.CreateTextureFromHeightmap(_currentHeightmap);
            _go.GetComponent<MeshRenderer>().material.SetTexture("_HeightmapTex0", doneTexture);
            _go.GetComponent<MeshRenderer>().material.SetTexture("_HeightmapTex1", doneTexture);
            _go.GetComponent<MeshRenderer>().material.SetFloat("_HeightmapSelectionScalar", 1);
        }

        public void Mei_RenderComputeShader()
        {
            //MultistepComputeShader computeShader = new MultistepComputeShader(MeiErosionShader, _sizeOfTexture, 8 ); //todo
            //var kernel_bufferInitialization = computeShader.AddKernel("CSMei_InitializeBuffers");
            //var kernel_waterIncrement = computeShader.AddKernel("CSMei_WaterIncrement");
            //var kernel_flowSimulation = computeShader.AddKernel("CSMei_FlowSimulation");
            //var kernel_velocityCalculation = computeShader.AddKernel("CSMei_VelocityCalculation");
            //var kernel_sedimentCalculation = computeShader.AddKernel("CSMei_SedimentCalculation");
            //var kernel_sedimentTransportation = computeShader.AddKernel("CSMei_SedimentTransportation");
            //var kernel_evaporation = computeShader.AddKernel("CSMei_Evaporation");

            //computeShader.SetGlobalUniform("g_sideLength", _sizeOfTexture.X);
            //computeShader.SetGlobalUniform("deltaT", DeltaT);
            //computeShader.SetGlobalUniform("constantWaterAdding", ConstantWaterAdding);
            //computeShader.SetGlobalUniform("A_pipeCrossSection", A_PipeCrossSection);
            //computeShader.SetGlobalUniform("l_pipeLength", L_PipeLength);
            //computeShader.SetGlobalUniform("g_GravityAcceleration", GravityAcceleration);
            //computeShader.SetGlobalUniform("ks_DissolvingConstant", DissolvingConstant);
            //computeShader.SetGlobalUniform("kd_DepositionConstant", DepositionConstant);
            //computeShader.SetGlobalUniform("ke_EvaporationConstant", EvaporationConstant);
            //computeShader.SetGlobalUniform("kc_SedimentCapacityConstant", SedimentCapacityConstant);
            //computeShader.SetGlobalUniform("gridSideSize",GridSize.x);

            //var allKernels = 
            //    new List<MyKernelHandle>()
            //{
            //        kernel_waterIncrement,
            //        kernel_flowSimulation,
            //        kernel_velocityCalculation,
            //        kernel_sedimentCalculation,
            //        kernel_sedimentTransportation,
            //        kernel_evaporation,
            //        kernel_bufferInitialization
            //};

            //var heightMap = new MyComputeBuffer("HeightMap", _sizeOfTexture.X*_sizeOfTexture.Y, 4);
            //heightMap.SetData(_currentHeightmap.HeightmapAsArray);
            //computeShader.SetBuffer(heightMap, allKernels);

            //var waterMap = new MyComputeBuffer("WaterMap", _sizeOfTexture.X*_sizeOfTexture.Y, 4);
            //computeShader.SetBuffer(waterMap, allKernels);

            //var waterMap1 = new MyComputeBuffer("WaterMap_1", _sizeOfTexture.X*_sizeOfTexture.Y, 4);
            //computeShader.SetBuffer(waterMap1, allKernels);

            //var waterMap2 = new MyComputeBuffer("WaterMap_2", _sizeOfTexture.X*_sizeOfTexture.Y, 4);
            //computeShader.SetBuffer(waterMap2, allKernels);

            //var fluxMap = new MyComputeBuffer("FluxMap", _sizeOfTexture.X*_sizeOfTexture.Y, 4*4);
            //computeShader.SetBuffer(fluxMap, allKernels);

            //var velocityMap = new MyComputeBuffer("VelocityMap", _sizeOfTexture.X*_sizeOfTexture.Y, 4*2);
            //computeShader.SetBuffer(velocityMap, allKernels);

            //var sedimentMap = new MyComputeBuffer("SedimentMap", _sizeOfTexture.X*_sizeOfTexture.Y, 4);
            //computeShader.SetBuffer(sedimentMap, allKernels);

            //var sedimentMap1 = new MyComputeBuffer("SedimentMap_1", _sizeOfTexture.X*_sizeOfTexture.Y, 4);
            //computeShader.SetBuffer(sedimentMap1, allKernels);

            //var DebugTexture = new MyRenderTexture("DebugTexture", new RenderTexture(_sizeOfTexture.X,_sizeOfTexture.Y,24, RenderTextureFormat.ARGB32), true);
            //computeShader.SetTexture(DebugTexture, allKernels);

            //var msw = new MyStopWatch();
            //msw.StartSegment("SHADERING");
            //Debug.Log("T222 START!");

            //var loopedKernels = 
            //    new List<MyKernelHandle>()
            //{
            //        kernel_waterIncrement,
            //        kernel_flowSimulation,
            //        kernel_velocityCalculation,
            //        kernel_sedimentCalculation,
            //        kernel_sedimentTransportation,
            //        kernel_evaporation,
            //};

            //ShaderExecutorObject.DispatchComputeShader(new ComputeShaderOrder()
            //{
            //    WorkPacks = new List<ComputeShaderWorkPack>()
            //    {
            //        new ComputeShaderWorkPack()
            //        {
            //            DispatchLoops = new List<ComputeShaderDispatchLoop>()
            //            {
            //                new ComputeShaderDispatchLoop()
            //                {
            //                    DispatchCount = StepCount,
            //                    KernelHandles = loopedKernels,

            //                }
            //            },
            //            Shader = computeShader
            //        }
            //    }
            //}).Wait();

            //float [,] outArray = new float[_sizeOfTexture.X,_sizeOfTexture.Y];
            //heightMap.GetData(outArray);
            //MyArrayUtils.Multiply(outArray, 1/400f);


            //var newOutTexture = HeightmapUtils.CreateTextureFromHeightmap(new HeightmapArray(outArray));
            //_go.GetComponent<MeshRenderer>().material.SetTexture("_HeightmapTex1",newOutTexture);
            //_go.GetComponent<MeshRenderer>().material.SetTexture("_OverlayTex", DebugTexture.AsRenderTexture);

            //Debug.Log("T531 "+msw.CollectResults());

            //OutputShowingObject.GetComponent<MeshRenderer>().material.SetTexture("_MainTex", DebugTexture.AsRenderTexture);
        }
    }
}