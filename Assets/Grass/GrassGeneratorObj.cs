using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Assets.Grass.Billboard;
using Assets.Grass.Container;
using Assets.Grass.Generating;
using Assets.Grass.Generating.SingleGrass;
using Assets.Grass.Instancing;
using Assets.Grass.Lod;
using Assets.Grass.Placing;
using UnityEngine;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;

namespace Assets.Grass
{
    internal class GrassGeneratorObj : MonoBehaviour
    {
        public bool shouldUseInstancing = true;
        private IGrassInstanceContainer _grassInstanceContainer;
        public Material BillboardMaterial;
        public Material ShellMaterial;
        public Material GrassBillboardGeneratorMaterial;
        public RenderTexture RenderTexture;
        public Texture RealTexture;
        public Material fragMaterial;

        public GameObject RenderTextureGameObject;
        public GameObject CameraGameObject;

        private void Start()
        {
            UnityEngine.Random.InitState(DateTime.Now.Second); //todo to provider

            GpuInstancingGrassInstanceContainer gpuInstancingGrassContainer = new GpuInstancingGrassInstanceContainer();
            GameObjectGrassInstanceContainer gameObjectContainer = new GameObjectGrassInstanceContainer();

            _rootInstanceContainer = new RootInstanceContainer(gameObjectContainer, gpuInstancingGrassContainer);
            _grassInstanceContainer = _rootInstanceContainer;
            Material grassMaterial = new Material(Shader.Find("Custom/Vegetation/Grass.Instanced"));

            var meshGenerator = new GrassMeshGenerator();

            var singleGrassMeshProvider = new SingleGrassMeshProvider(meshGenerator);
            var singleGrassUpdater = new SingleGrassEntitySplatUpdater(singleGrassMeshProvider);
            var grassTuftLodEntitySplatGenerator = new GenericEntitySplatGenerator((position) =>
                new LodEntitySplat(position,
                    new SingleGrassLodResolver(),
                    new GpuInstancingEntitySplatGenerator(new GrassTuftGenerator(),
                        new SingleGrassUniformPositionProvider(),
                        new GrassTuftAbstractSettingGenerator(), grassMaterial,
                        new ConstantCount(20), singleGrassMeshProvider, gpuInstancingGrassContainer,
                        new GpuInstancingGrassInstanceGenerator()),
                    singleGrassUpdater));

            var singleGrassLodEntitySplatGenerator = new GenericEntitySplatGenerator((position) =>
                new LodEntitySplat(position,
                    new SingleGrassLodResolver(),
                    new GpuInstancingEntitySplatGenerator(new GrassSingleGenerator(),
                        new SingleGrassUniformPositionProvider(),
                        new GrassSingleAbstractSettingGenerator(), grassMaterial,
                        new ConstantCount(20), singleGrassMeshProvider, gpuInstancingGrassContainer,
                        new GpuInstancingGrassInstanceGenerator()),
                    singleGrassUpdater));


            var lodGroupProvider =
                new LodGroupsProvider(new List<ILodEntitySplatGenerator>
                {
                    singleGrassLodEntitySplatGenerator,
                    grassTuftLodEntitySplatGenerator
                });

            var mapSize = new Vector2(100, 100);
            var splatSize = mapSize / 10;
            var singleLevelDistance = (int) splatSize.x * 2;
            var singleLevelMargin = 0.1f;
            var lodMarginPowCoef = 0.8f;
            _grassLodManager =
                new GrassLodManager(new LodLevelResolver(7, singleLevelDistance, singleLevelMargin, lodMarginPowCoef),
                    new GrassLodManager.MultithreadLodGroupsActioner(lodGroupProvider), mapSize, splatSize);
            _grassLodManager.InitializeGroups(new Vector3(0, 0, 0));
            _grassLodManager.UpdateLod(new Vector3(0, 0, 0));
        }

        private void generateShells()
        {
            var layersCount = 10;
            var grassHeight = 0.2f;
            GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            for (int i = 0; i < layersCount; i++)
            {
                var shellObj = new GameObject("grassShell " + i);
                shellObj.transform.localPosition = new Vector3(10, (grassHeight / layersCount) * i, 10);
                var rend = shellObj.AddComponent<MeshRenderer>();
                rend.transform.localScale = new Vector3(1, 1, 1);
                rend.material = ShellMaterial;
                rend.material.SetFloat("_LayerHeight", i * (1 / (float) layersCount));

                rend.material.SetFloat("_BendingStrength", 0.2f);
                rend.material.SetVector("_WindDirection", new Vector4(1, 1, 1, 1));
                rend.material.SetFloat("__Scale", 30.0f);
                shellObj.AddComponent<MeshFilter>().mesh = plane.GetComponent<MeshFilter>().mesh;
            }
        }

        private void generateBillboardTexture()
        {
            Graphics.Blit(RealTexture, RenderTexture, fragMaterial);
        }

        private GrassEntitiesWithMaterials generateTurf(GrassBillboardGenerator billboardGenerator)
        {
            var meshGenerator = new GrassMeshGenerator();
            var mesh = meshGenerator.GetGrassBillboardMesh(0, 1);
            var xgenerateTriangleTurf = billboardGenerator.GenerateTriangleTurf();
            //todo : use grass entities set and rotate
            //xgenerateTriangleTurf.Rotation = (MyMathUtils.DegToRad(new Vector3(0, 90, 0)));
            //xgenerateTriangleTurf.Position = new Vector3(2,2,2);
            return new GrassEntitiesWithMaterials(xgenerateTriangleTurf.EntitiesAfterTransform, BillboardMaterial, mesh,
                ContainerType.GameObject);
        }

        private static float a = 0;
        private static float angl = 0;
        private static float windStrength = 0;
        private IEntitySplat _entitySplat;
        private GrassLodManager _manager;
        private RootInstanceContainer _rootInstanceContainer;
        private GrassLodManager _grassLodManager;

        private void Update()
        {
            //if (Input.GetKey(KeyCode.UpArrow))
            //{
            //    _rootInstanceContainer.SetGlobalColor("_Color", new Color(a%1.0f, (a + 0.5f)%1.0f, (a + 0.3f)%1.0f));
            //    a += 0.1f;
            //}

            //if (Input.GetKey(KeyCode.Q))
            //{
            //    _rootInstanceContainer.SetGlobalUniform(GrassShaderUniformName._BendingStrength, windStrength);
            //    windStrength += 0.1f;
            //    windStrength = Mathf.Clamp01(windStrength);
            //}
            //if (Input.GetKey(KeyCode.W))
            //{
            //    _rootInstanceContainer.SetGlobalUniform(GrassShaderUniformName._BendingStrength, windStrength);
            //    windStrength -= 0.1f;
            //    windStrength = Mathf.Clamp01(windStrength);
            //}
            //if (Input.GetKey(KeyCode.T))
            //{
            //    grassSplat.Remove();
            //}

            ////if (Time.frameCount%100 == 0)
            ////{
            ////   _manager.UpdateLod(new Vector3(0, 0, 0));

            ////}
            //if (windStrength < 1)
            //{
            //    //_manager.UpdateLod(new Vector3(0,0,0));
            //    //windStrength = 2;
            //}
            float move = 0.05f;
            if (Input.GetKey(KeyCode.L))
            {
                CameraGameObject.transform.position += new Vector3(move, 0, 0);
            }
            if (Input.GetKey(KeyCode.I))
            {
                CameraGameObject.transform.position += new Vector3(0, 0, move);
            }
            if (Input.GetKey(KeyCode.K))
            {
                CameraGameObject.transform.position += new Vector3(0, 0, -move);
            }
            if (Input.GetKey(KeyCode.J))
            {
                CameraGameObject.transform.position += new Vector3(-move, 0, 0);
            }

            if (Time.frameCount % 100 == 0)
            {
                _grassLodManager.UpdateLod(CameraGameObject.transform.position);
            }
            _grassLodManager.UpdateNewGroups();
            _rootInstanceContainer.Draw();
        }

        private Vector4 GetWindVector(float angle)
        {
            return new Vector4((float) Math.Sin(angle), 0, (float) Math.Cos(angle));
        }
    }
}