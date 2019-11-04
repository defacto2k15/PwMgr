using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.ComputeShaders;
using Assets.Heightmaps.Ring1.Creator;
using Assets.MeshGeneration;
using Assets.Random;
using Assets.Utils;
using Assets.Utils.ArrayUtils;
using Assets.Utils.MT;
using Assets.Utils.TextureRendering;
using UnityEngine;

namespace Assets.Heightmaps.Ring1.Erosion
{
    public class ShaderComputeErosionDebugObject : MonoBehaviour
    {
        public ComputeShader MyShader;
        public ComputeShader HydraulicErosionShader;
        public ComputeShader MeiErosionShader;
        public ComputeShader TweakedThermalErosionShader;

        public GameObject OutputShowingObject;
        public HeightmapArray _currentHeightmap;
        private Texture _conventionalOutputTexture;
        private GameObject _go;
        private IntVector2 _sizeOfTexture = new IntVector2(240, 240);

        private UnityThreadComputeShaderExecutorObject ShaderExecutorObject =
            new UnityThreadComputeShaderExecutorObject();

        public void Start()
        {
            TaskUtils.SetGlobalMultithreading(false);
            //DiamondSquareCreator creator = new DiamondSquareCreator(new RandomProvider(22));
            //_currentHeightmap = creator.CreateDiamondSquareNoiseArray(_sizeOfTexture, 64);
            //MyArrayUtils.Multiply(_currentHeightmap.HeightmapAsArray, 0.1f);
            var heightTex = SavingFileManager.LoadPngTextureFromFile(@"C:\inz\cont\smallCut.png", 240, 240,
                TextureFormat.RGBA32, true, true);
            _currentHeightmap = HeightmapUtils.CreateHeightmapArrayFromTexture(heightTex);

            MyArrayUtils.Normalize(_currentHeightmap.HeightmapAsArray);
            MyArrayUtils.InvertNormalized(_currentHeightmap.HeightmapAsArray);

            CreateComparisionObject();
            //MyArrayUtils.Multiply(_currentHeightmap.HeightmapAsArray, 400);

            //float[,] newHeightArray = new float[256,256];
            //for (int x = 0; x < 256; x++)
            //{
            //    for (int y = 0; y < 256; y++)
            //    {
            //        var distance = (new Vector2(x,y) - new Vector2(128, 128)).magnitude;
            //        newHeightArray[x, y] = distance;
            //    }
            //}
            //MyArrayUtils.Normalize(newHeightArray);
            //_currentHeightmap = new HeightmapArray(newHeightArray);

            //Hydraulic_RenderComputeShader();

            Thermal_RenderComputeShader();
            //Hydraulic_RenderComputeShader();
        }

        public void Update()
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                TweakedThermal_RenderComputeShader();
                //Mei_RenderComputeShader();
            }
        }

        private void CreateComparisionObject()
        {
            _go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            var material = new Material(Shader.Find("Custom/Terrain/Terrain_Debug"));
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
        }

        public void Hydraulic_RenderComputeShader()
        {
            //var configuration =
            //    new HydraulicEroderConfiguration()
            //    {
            //        StepCount = 20,
            //        NeighbourFinder = NeighbourFinders.Big9Finder,
            //        kr_ConstantWaterAddition = 0.0001f/5,
            //        ks_GroundToSedimentFactor = 1f,
            //        ke_WaterEvaporationFactor = 0.05f,
            //        kc_MaxSedimentationFactor = 0.8f,
            //        FinalSedimentationToGround = false,
            //        WaterGenerator = HydraulicEroderWaterGenerator.FirstFrame,
            //        DestinationFinder = HydraulicEroderWaterDestinationFinder.OnlyBest
            //    };

            //MultistepComputeShader computeShader = new MultistepComputeShader(HydraulicErosionShader, _sizeOfTexture, 8 );
            //var kernel_water = computeShader.AddKernel("CSHydraulicErosion_Water");
            //var kernel_erostion = computeShader.AddKernel("CSHydraulicErosion_Erosion");
            //var kernel_deltaSum = computeShader.AddKernel("CSHydraulicErosion_DeltaSum");
            //var kernel_clearDelta = computeShader.AddKernel("CSHydraulicErosion_ClearDelta");
            //var kernel_evaporation = computeShader.AddKernel("CSHydraulicErosion_Evaporation");
            //var kernel_sedimentationToGround = computeShader.AddKernel("CSHydraulicErosion_SedimentationToGround");

            //computeShader.SetGlobalUniform("g_sideLength", _sizeOfTexture.X);
            //computeShader.SetGlobalUniform("g_krParam", configuration.kr_ConstantWaterAddition);
            //computeShader.SetGlobalUniform("g_ksParam", configuration.ks_GroundToSedimentFactor);
            //computeShader.SetGlobalUniform("g_keParam", configuration.ke_WaterEvaporationFactor);
            //computeShader.SetGlobalUniform("g_kcParam", configuration.kc_MaxSedimentationFactor);


            //var heightMap = new MyComputeBuffer("HeightMap", _sizeOfTexture.X*_sizeOfTexture.Y, 4);
            //computeShader.SetBuffer(heightMap,
            //    new List<MyKernelHandle>()
            //    {
            //        kernel_water,
            //        kernel_erostion,
            //        kernel_evaporation,
            //        kernel_sedimentationToGround
            //    });

            //heightMap.SetData(_currentHeightmap.HeightmapAsArray);

            //var waterMap = new MyComputeBuffer("WaterMap", _sizeOfTexture.X*_sizeOfTexture.Y, 4);
            //computeShader.SetBuffer(waterMap,
            //    new List<MyKernelHandle>()
            //    {
            //        kernel_water,
            //        kernel_erostion,
            //        kernel_deltaSum,
            //        kernel_evaporation,
            //    });

            //var deltaBuffer = new MyComputeBuffer("DeltaBuffer", _sizeOfTexture.X*_sizeOfTexture.Y, 4*2*9);
            //computeShader.SetBuffer(deltaBuffer, 
            //    new List<MyKernelHandle>()
            //    {
            //        kernel_erostion,
            //        kernel_deltaSum,
            //        kernel_clearDelta
            //    });

            //var sedimentMap = new MyComputeBuffer("SedimentMap", _sizeOfTexture.X*_sizeOfTexture.Y, 4);
            //computeShader.SetBuffer(sedimentMap,
            //    new List<MyKernelHandle>()
            //{
            //    kernel_water,
            //    kernel_erostion,
            //    kernel_deltaSum,
            //    kernel_evaporation,
            //    kernel_sedimentationToGround
            //});

            //var DebugTexture = new MyRenderTexture("DebugTexture", new RenderTexture(_sizeOfTexture.X,_sizeOfTexture.Y,24, RenderTextureFormat.ARGB32), true);
            //computeShader.SetTexture(DebugTexture, 
            //    new List<MyKernelHandle>()
            //{
            //    kernel_water,
            //    kernel_erostion,
            //    kernel_deltaSum,
            //    kernel_clearDelta,
            //    kernel_evaporation,
            //    kernel_sedimentationToGround
            //});

            //var msw = new MyStopWatch();
            //msw.StartSegment("SHADERING");

            //var loopedKernels = new List<MyKernelHandle>()
            //{
            //    kernel_water,
            //    kernel_erostion,
            //    kernel_deltaSum,
            //    kernel_clearDelta,
            //    kernel_evaporation,
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
            //                    DispatchCount = 30,
            //                    KernelHandles = loopedKernels,
            //                },
            //                new ComputeShaderDispatchLoop()
            //                {
            //                    DispatchCount = 1,
            //                    KernelHandles = new List<MyKernelHandle>() {kernel_sedimentationToGround}
            //                }
            //            },
            //            Shader = computeShader
            //        }
            //    }
            //}).Wait();

            //float [,] outArray = new float[_sizeOfTexture.X,_sizeOfTexture.Y];
            //heightMap.GetData(outArray);

            //Debug.Log("T531 "+msw.CollectResults());

            //_conventionalOutputTexture = UltraTextureRenderer.MoveRenderTextureToNormalTexture(DebugTexture.AsRenderTexture,
            //    new ConventionalTextureInfo(_sizeOfTexture.X, _sizeOfTexture.Y, TextureFormat.ARGB32, false));

            //OutputShowingObject.GetComponent<MeshRenderer>().material.SetTexture("_MainTex", _conventionalOutputTexture);

            //var newOutTexture = HeightmapUtils.CreateTextureFromHeightmap(new HeightmapArray(outArray));
            //_go.GetComponent<MeshRenderer>().material.SetTexture("_HeightmapTex1",newOutTexture);
        }

        public void TweakedThermal_RenderComputeShader()
        {
            //MultistepComputeShader computeShader = new MultistepComputeShader(TweakedThermalErosionShader, _sizeOfTexture, 8 );
            //var kernel1 = computeShader.AddKernel("CSTweakedThermal_Precalculation");
            //var kernel2 = computeShader.AddKernel("CSTweakedThermal_Erosion");


            //computeShader.SetGlobalUniform("g_tParam", 0.01f/1.6f);
            //computeShader.SetGlobalUniform("g_cParam", 0.4f);
            //computeShader.SetGlobalUniform("g_sideLength", _sizeOfTexture.X);


            //var MyHeightBuffer0 = new MyComputeBuffer("HeightBuffer0", _sizeOfTexture.X*_sizeOfTexture.Y, 4); 
            //computeShader.SetBuffer(MyHeightBuffer0, new List<MyKernelHandle>() { kernel1, kernel2});

            //MyHeightBuffer0.SetData(_currentHeightmap.HeightmapAsArray);

            //var MyHeightBuffer1 = new MyComputeBuffer("HeightBuffer1", _sizeOfTexture.X*_sizeOfTexture.Y, 4); 
            //computeShader.SetBuffer(MyHeightBuffer1, new List<MyKernelHandle>() { kernel1, kernel2});

            //var OutTex0 = new MyRenderTexture("OutputHeightSum", new RenderTexture(_sizeOfTexture.X,_sizeOfTexture.Y,24, RenderTextureFormat.ARGB32), true);
            //computeShader.SetTexture(OutTex0, new List<MyKernelHandle>() {kernel1, kernel2});


            //var MyMidTextureBuffer = new MyComputeBuffer("MidTextureBuffer", _sizeOfTexture.X*_sizeOfTexture.Y, 4*2); 
            //computeShader.SetBuffer(MyMidTextureBuffer, new List<MyKernelHandle>() { kernel1, kernel2});

            //var msw = new MyStopWatch();
            //msw.StartSegment("shadering!");

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
            //                    DispatchCount = 70,
            //                    KernelHandles = new List<MyKernelHandle>() {kernel1, kernel2},
            //                },
            //            },
            //            Shader = computeShader
            //        }
            //    }
            //}).Wait();

            //float [,] outArray = new float[_sizeOfTexture.X,_sizeOfTexture.Y];
            //MyHeightBuffer0.GetData(outArray);

            //Debug.Log("T531 "+msw.CollectResults());

            //OutputShowingObject.GetComponent<MeshRenderer>().material.SetTexture("_MainTex", OutTex0.AsRenderTexture);

            //var newOutTexture = HeightmapUtils.CreateTextureFromHeightmap(new HeightmapArray(outArray));
            //_go.GetComponent<MeshRenderer>().material.SetTexture("_HeightmapTex1",newOutTexture);
        }

        public void Thermal_RenderComputeShader()
        {
            //MultistepComputeShader computeShader = new MultistepComputeShader(MyShader, _sizeOfTexture, 8 );
            //var kernel1 = computeShader.AddKernel("CSThermal_Precalculation");
            //var kernel2 = computeShader.AddKernel("CSThermal_Erosion");
            //var kernel3 = computeShader.AddKernel("CSThermal_TransferHeightToTexture");

            //computeShader.SetGlobalUniform("g_tParam", 0.05f);
            //computeShader.SetGlobalUniform("g_cParam", 0.04f);
            //computeShader.SetGlobalUniform("g_sideLength", _sizeOfTexture.X);


            //var MyHeightBuffer0 = new MyComputeBuffer("HeightBuffer0", _sizeOfTexture.X*_sizeOfTexture.Y, 4); 
            //computeShader.SetBuffer(MyHeightBuffer0, new List<MyKernelHandle>() { kernel1, kernel2, kernel3});

            //MyHeightBuffer0.SetData(_currentHeightmap.HeightmapAsArray);

            //var MyHeightBuffer1 = new MyComputeBuffer("HeightBuffer1", _sizeOfTexture.X*_sizeOfTexture.Y, 4); 
            //computeShader.SetBuffer(MyHeightBuffer1, new List<MyKernelHandle>() { kernel1, kernel2});

            //var OutTex0 = new MyRenderTexture("OutputHeightSum", new RenderTexture(_sizeOfTexture.X,_sizeOfTexture.Y,24, RenderTextureFormat.ARGB32), true);
            //computeShader.SetTexture(OutTex0, new List<MyKernelHandle>() { kernel3});


            //var MyMidTextureBuffer = new MyComputeBuffer("MidTextureBuffer", _sizeOfTexture.X*_sizeOfTexture.Y, 4*2); 
            //computeShader.SetBuffer(MyMidTextureBuffer, new List<MyKernelHandle>() { kernel1, kernel2});

            //var msw = new MyStopWatch();
            //msw.StartSegment("shadering!");

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
            //                    DispatchCount = 30,
            //                    KernelHandles = new List<MyKernelHandle>() {kernel1, kernel2},
            //                },
            //            },
            //            Shader = computeShader
            //        }
            //    }
            //}).Wait();

            //float [,] outArray = new float[_sizeOfTexture.X,_sizeOfTexture.Y];
            //MyHeightBuffer0.GetData(outArray);

            //Debug.Log("T531 "+msw.CollectResults());

            //_conventionalOutputTexture = UltraTextureRenderer.MoveRenderTextureToNormalTexture(OutTex0.AsRenderTexture,
            //    new ConventionalTextureInfo(_sizeOfTexture.X, _sizeOfTexture.Y, TextureFormat.ARGB32, false));

            //OutputShowingObject.GetComponent<MeshRenderer>().material.SetTexture("_MainTex", _conventionalOutputTexture);

            //var newOutTexture = HeightmapUtils.CreateTextureFromHeightmap(new HeightmapArray(outArray));
            //_go.GetComponent<MeshRenderer>().material.SetTexture("_HeightmapTex1",newOutTexture);
        }

        public void Mei_RenderComputeShader()
        {
            //var configuration = new MeiHydraulicEroderConfiguration()
            //{
            //    StepCount = 50,
            //    A_PipeCrossSection = 0.05f,
            //    ConstantWaterAdding = 1 / 64f,
            //    GravityAcceleration = 9.81f,
            //    DeltaT = 1f,
            //    DepositionConstant = 0.0001f * 12,
            //    DissolvingConstant = 0.0001f * 12 ,
            //    EvaporationConstant = 0.05f * 10,
            //    GridSize = new Vector2(1, 1),
            //    L_PipeLength = 1,
            //    SedimentCapacityConstant = 250
            //};

            //MultistepComputeShader computeShader = new MultistepComputeShader(MeiErosionShader, _sizeOfTexture, 8 );

            //var kernel_bufferInitialization = computeShader.AddKernel("CSMei_InitializeBuffers");
            //var kernel_waterIncrement = computeShader.AddKernel("CSMei_WaterIncrement");
            //var kernel_flowSimulation = computeShader.AddKernel("CSMei_FlowSimulation");
            //var kernel_velocityCalculation = computeShader.AddKernel("CSMei_VelocityCalculation");
            //var kernel_sedimentCalculation = computeShader.AddKernel("CSMei_SedimentCalculation");
            //var kernel_sedimentTransportation = computeShader.AddKernel("CSMei_SedimentTransportation");
            //var kernel_evaporation = computeShader.AddKernel("CSMei_Evaporation");

            //computeShader.SetGlobalUniform("g_sideLength", _sizeOfTexture.X);
            //computeShader.SetGlobalUniform("deltaT", configuration.DeltaT);
            //computeShader.SetGlobalUniform("constantWaterAdding", configuration.ConstantWaterAdding);
            //computeShader.SetGlobalUniform("A_pipeCrossSection", configuration.A_PipeCrossSection);
            //computeShader.SetGlobalUniform("l_pipeLength", configuration.L_PipeLength);
            //computeShader.SetGlobalUniform("g_GravityAcceleration", configuration.GravityAcceleration);
            //computeShader.SetGlobalUniform("ks_DissolvingConstant", configuration.DissolvingConstant);
            //computeShader.SetGlobalUniform("kd_DepositionConstant", configuration.DepositionConstant);
            //computeShader.SetGlobalUniform("ke_EvaporationConstant", configuration.EvaporationConstant);
            //computeShader.SetGlobalUniform("kc_SedimentCapacityConstant", configuration.SedimentCapacityConstant);
            //computeShader.SetGlobalUniform("gridSideSize", configuration.GridSize.x);

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
            //                    DispatchCount = 1,
            //                    KernelHandles = new List<MyKernelHandle>() {kernel_bufferInitialization},
            //                },
            //                new ComputeShaderDispatchLoop()
            //                {
            //                    DispatchCount = configuration.StepCount,
            //                    KernelHandles = loopedKernels
            //                },
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

            //Debug.Log("T531 "+msw.CollectResults());

            //OutputShowingObject.GetComponent<MeshRenderer>().material.SetTexture("_MainTex", DebugTexture.AsRenderTexture);
        }
    }
}