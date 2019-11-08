using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.ComputeShaders;
using Assets.Heightmaps.Ring1.Creator;
using Assets.Heightmaps.Ring1.RenderingTex;
using Assets.Heightmaps.Ring1.TerrainDescription.CornerMerging;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.MeshGeneration;
using Assets.Ring2;
using Assets.ShaderUtils;
using Assets.Utils;
using Assets.Utils.MT;
using Assets.Utils.Services;
using Assets.Utils.TextureRendering;
using Assets.Utils.Textures;
using UnityEngine;

namespace Assets.Heightmaps.Ring1.TerrainDescription.FeatureGenerating
{
    public class TerrainDetailProvider_Normals_DebugObject : MonoBehaviour
    {
        public ComputeShaderContainerGameObject ContainerGameObject;
        private UTTextureRendererProxy _utTextureRendererProxy;
        private TerrainCardinalResolution _terrainResolution = TerrainCardinalResolution.MID_RESOLUTION;

        public void Start()
        {
            TaskUtils.SetGlobalMultithreading(false);

            var rgbaMainTexture = SavingFileManager.LoadPngTextureFromFile(@"C:\inz\cont\n49_e019_1arc_v3.png", 3600,
                3600,
                TextureFormat.ARGB32, true, false);


            TerrainTextureFormatTransformator transformator =
                new TerrainTextureFormatTransformator(new CommonExecutorUTProxy());
            var mainTexture = transformator.EncodedHeightTextureToPlainAsync(new TextureWithSize()
            {
                Size = new IntVector2(3600, 3600),
                Texture = rgbaMainTexture
            }).Result;


            var gameObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
            var material = new Material(Shader.Find("Custom/Terrain/Ring1"));

            TerrainAndNormalTexture pair = CreateTerrainAndNormalTexture(mainTexture);
            material.SetTexture("_HeightmapTex", pair.HeightTexture);
            material.SetTexture("_NormalmapTex", pair.NormalTexture);

            gameObject.GetComponent<MeshRenderer>().material = material;
            gameObject.name = "Terrain";
            gameObject.transform.localRotation = Quaternion.Euler(0, 0, 0);

            var unitySideLength = 10f;
            var realSideLength = 240 * 24;
            var metersPerUnit = realSideLength / unitySideLength;

            if (_terrainResolution == TerrainCardinalResolution.MIN_RESOLUTION)
            {
                gameObject.transform.localScale = new Vector3(10, (6500 / metersPerUnit), 10);
                gameObject.transform.localPosition = new Vector3(0, 0, 0);
            }
            else if (_terrainResolution == TerrainCardinalResolution.MID_RESOLUTION)
            {
                gameObject.transform.localScale = new Vector3(10, (6500 / metersPerUnit) * 8, 10);
                gameObject.transform.localPosition = new Vector3(0, 0, 0);
            }
            else
            {
                gameObject.transform.localScale = new Vector3(10, (6500 / metersPerUnit) * 8 * 8, 10);
                gameObject.transform.localPosition = new Vector3(0, -30, 0);
            }
            gameObject.GetComponent<MeshFilter>().mesh = PlaneGenerator.CreateFlatPlaneMesh(240, 240);
        }

        private TerrainAndNormalTexture CreateTerrainAndNormalTexture(RenderTexture mainTexture)
        {
            TextureRendererServiceConfiguration rendererServiceConfiguration = new TextureRendererServiceConfiguration()
            {
                StepSize = new Vector2(500, 500)
            };
            _utTextureRendererProxy = new UTTextureRendererProxy(
                new TextureRendererService(new MultistepTextureRenderer(ContainerGameObject),
                    rendererServiceConfiguration));

            var provider =
                CreateTerrainDetailProvider(
                    TerrainDetailProviderDebugUtils.CreateFeatureAppliers(_utTextureRendererProxy, ContainerGameObject,
                        new CommonExecutorUTProxy(), new UnityThreadComputeShaderExecutorObject()), mainTexture);

            MyRectangle queryArea = null;
            if (_terrainResolution == TerrainCardinalResolution.MIN_RESOLUTION)
            {
                queryArea = new MyRectangle(0, 0, 24 * 240, 24 * 240);
            }
            else if (_terrainResolution == TerrainCardinalResolution.MID_RESOLUTION)
            {
                queryArea = new MyRectangle(3 * 240, 3 * 240, 3 * 240, 3 * 240);
            }
            else
            {
                queryArea =
                    new MyRectangle(5 * 0.375f * 240, 5 * 0.375f * 240, 0.375f * 240, 0.375f * 240);
            }

            var outTex = provider.GenerateHeightDetailElementAsync(queryArea, _terrainResolution,CornersMergeStatus.NOT_MERGED).Result.Texture;
            var outNormal = provider.GenerateNormalDetailElementAsync(queryArea, _terrainResolution, CornersMergeStatus.NOT_MERGED).Result.Texture;
            return new TerrainAndNormalTexture()
            {
                HeightTexture = outTex.Texture,
                NormalTexture = outNormal.Texture
            };
        }

        private TerrainDetailProvider CreateTerrainDetailProvider(List<RankedTerrainFeatureApplier> featureAppliers,
            Texture mainTexture)
        {
            var terrainDetailProviderConfiguration = new TerrainDetailProviderConfiguration()
            {
                UseTextureSavingToDisk = false
            };
            var terrainDetailFileManager =
                new TerrainDetailFileManager("C:\\unityCache\\", new CommonExecutorUTProxy());


            TerrainDetailGeneratorConfiguration generatorConfiguration = new TerrainDetailGeneratorConfiguration()
            {
                TerrainDetailImageSideDisjointResolution = 240
            };
            TextureWithCoords fullFundationData = new TextureWithCoords(new TextureWithSize()
            {
                Texture = mainTexture,
                Size = new IntVector2(mainTexture.width, mainTexture.height)
            }, new MyRectangle(0, 0, 3601 * 24, 3601 * 24));

            TerrainDetailGenerator generator =
                new TerrainDetailGenerator(generatorConfiguration, _utTextureRendererProxy, fullFundationData,
                    featureAppliers, new CommonExecutorUTProxy());

            TerrainDetailProvider provider =
                new TerrainDetailProvider(terrainDetailProviderConfiguration, generator, null, new TerrainDetailAlignmentCalculator(240));
            generator.SetBaseTerrainDetailProvider(BaseTerrainDetailProvider.CreateFrom(provider));
            return provider;
        }

        private class TerrainAndNormalTexture
        {
            public Texture HeightTexture;
            public Texture NormalTexture;
        }
    }
}