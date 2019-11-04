using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.ETerrain.TestUtils;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.MeshGeneration;
using Assets.Utils;
using Assets.Utils.MT;
using Assets.Utils.Textures;
using UnityEngine;

namespace Assets.Heightmaps.Ring1.TerrainDescription.CornerMerging
{
    public class TerrainShapeDBWithMergingDEO : MonoBehaviour
    {
        private TerrainShapeDbUnderTest _shapeDb;

        public void Start()
        {
            string filePath = @"c:\testUnityCache\";

            TaskUtils.SetGlobalMultithreading(false);
            _shapeDb = new TerrainShapeDbUnderTest(true, true, filePath);

            ClearUnityCache(filePath);
            //TaskUtils.DebuggerAwareWait(Test1());
            ////TaskUtils.DebuggerAwareWait(Test2());
            //TaskUtils.DebuggerAwareWait(Test4());
            //TaskUtils.DebuggerAwareWait(Test4());
            TaskUtils.DebuggerAwareWait(Test5());
        }

        private void ClearUnityCache(string path)
        {
            System.IO.DirectoryInfo di = new DirectoryInfo(path);

            foreach (FileInfo file in di.GetFiles())
            {
                file.Delete();
            }
            foreach (DirectoryInfo dir in di.GetDirectories())
            {
                dir.Delete(true);
            }
        }

        public async Task Test1() //simply retrive non-merged
        {
            var heightTexture = (await _shapeDb.ShapeDb.QueryAsync(new TerrainDescriptionQuery()
            {
                QueryArea = new MyRectangle(5760f, 5760f, 5760f, 5760f),
                RequestedElementDetails = new List<TerrainDescriptionQueryElementDetail>()
                {
                    new TerrainDescriptionQueryElementDetail()
                    {
                        RequiredMergeStatus = RequiredCornersMergeStatus.NOT_IMPORTANT,
                        Resolution = TerrainCardinalResolution.MIN_RESOLUTION,
                        Type = TerrainDescriptionElementTypeEnum.HEIGHT_ARRAY
                    }
                }
            })).GetElementOfType(TerrainDescriptionElementTypeEnum.HEIGHT_ARRAY).TokenizedElement.DetailElement.Texture.Texture;

            CreateTerrainObject(heightTexture, new Vector2(5760, 5760));
        }

        public async Task Test2() //simply retrive merged
        {
            var heightTexture = (await _shapeDb.ShapeDb.QueryAsync(new TerrainDescriptionQuery()
            {
                QueryArea = new MyRectangle(5760f, 5760f, 5760f, 5760f),
                RequestedElementDetails = new List<TerrainDescriptionQueryElementDetail>()
                {
                    new TerrainDescriptionQueryElementDetail()
                    {
                        RequiredMergeStatus = RequiredCornersMergeStatus.MERGED,
                        Resolution = TerrainCardinalResolution.MIN_RESOLUTION,
                        Type = TerrainDescriptionElementTypeEnum.HEIGHT_ARRAY
                    }
                }
            })).GetElementOfType(TerrainDescriptionElementTypeEnum.HEIGHT_ARRAY).TokenizedElement.DetailElement.Texture.Texture;

            CreateTerrainObject(heightTexture, new Vector2(5760, 5760));
        }

        public async Task Test3() //simply retrive merged but of max resolution
        {
            var heightTexture = (await _shapeDb.ShapeDb.QueryAsync(new TerrainDescriptionQuery()
            {
                QueryArea = new MyRectangle(5760f + 90*3, 5760f + 90*3, 90f, 90f),
                RequestedElementDetails = new List<TerrainDescriptionQueryElementDetail>()
                {
                    new TerrainDescriptionQueryElementDetail()
                    {
                        RequiredMergeStatus = RequiredCornersMergeStatus.MERGED,
                        Resolution = TerrainCardinalResolution.MAX_RESOLUTION,
                        Type = TerrainDescriptionElementTypeEnum.HEIGHT_ARRAY
                    }
                }
            })).GetElementOfType(TerrainDescriptionElementTypeEnum.HEIGHT_ARRAY).TokenizedElement.DetailElement.Texture.Texture;

            CreateTerrainObject(heightTexture, new Vector2(5760+90f, 5760+90f));
        }

        public async Task Test4() //RetriveSeveralMergedTerrains in Min resolution
        {
            float terrainWidth = 5760f;
            TerrainCardinalResolution resolution = TerrainCardinalResolution.MIN_RESOLUTION;
            await CreateMergedGriddedTerrain(new IntVector2(1, 1),terrainWidth, resolution);

            await CreateMergedGriddedTerrain(new IntVector2(0, 0),terrainWidth, resolution);
            await CreateMergedGriddedTerrain(new IntVector2(1, 0),terrainWidth, resolution);
            await CreateMergedGriddedTerrain(new IntVector2(2, 0),terrainWidth, resolution);

            await CreateMergedGriddedTerrain(new IntVector2(0, 1),terrainWidth, resolution);
            await CreateMergedGriddedTerrain(new IntVector2(2, 1),terrainWidth, resolution);

            await CreateMergedGriddedTerrain(new IntVector2(0, 2),terrainWidth, resolution);
            await CreateMergedGriddedTerrain(new IntVector2(1, 2),terrainWidth, resolution);
            await CreateMergedGriddedTerrain(new IntVector2(2, 2),terrainWidth, resolution);
        }

        public async Task Test5() //RetriveSeveralMergedTerrains in Max resolution
        {
            float terrainWidth = 90;
            TerrainCardinalResolution resolution = TerrainCardinalResolution.MAX_RESOLUTION;
            await CreateMergedGriddedTerrain(new IntVector2(10+1, 10+1),terrainWidth, resolution);

            await CreateMergedGriddedTerrain(new IntVector2(10, 10),terrainWidth, resolution);
            await CreateMergedGriddedTerrain(new IntVector2(11, 10),terrainWidth, resolution);
            await CreateMergedGriddedTerrain(new IntVector2(12, 10),terrainWidth, resolution);

            await CreateMergedGriddedTerrain(new IntVector2(10, 11),terrainWidth, resolution);
            await CreateMergedGriddedTerrain(new IntVector2(12, 11),terrainWidth, resolution);

            await CreateMergedGriddedTerrain(new IntVector2(10, 12),terrainWidth, resolution);
            await CreateMergedGriddedTerrain(new IntVector2(11, 12),terrainWidth, resolution);
            await CreateMergedGriddedTerrain(new IntVector2(12, 12),terrainWidth, resolution);
        }

        private async Task CreateMergedGriddedTerrain(IntVector2 gridPosition, float terrainWidth, TerrainCardinalResolution resolution)
        {
            var startPosition = gridPosition * terrainWidth;
            var heightTexture = (await _shapeDb.ShapeDb.QueryAsync(new TerrainDescriptionQuery()
            {
                QueryArea = new MyRectangle(startPosition.x, startPosition.y, terrainWidth, terrainWidth),
                RequestedElementDetails = new List<TerrainDescriptionQueryElementDetail>()
                {
                    new TerrainDescriptionQueryElementDetail()
                    {
                        RequiredMergeStatus = RequiredCornersMergeStatus.MERGED,
                        Resolution = resolution,
                        Type = TerrainDescriptionElementTypeEnum.HEIGHT_ARRAY
                    }
                }
            })).GetElementOfType(TerrainDescriptionElementTypeEnum.HEIGHT_ARRAY).TokenizedElement.DetailElement.Texture.Texture;

            CreateTerrainObject(heightTexture, gridPosition*90f);
        }

        public void CreateTerrainObject(Texture texture, Vector2 startPosition)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Plane);
            go.GetComponent<MeshFilter>().mesh = PlaneGenerator.CreateFlatPlaneMesh(241, 241);

            var material = new Material(Shader.Find("Custom/Debug/SimpleTerrain"));
            material.SetTexture("_HeightmapTex",texture);
            material.SetFloat("_HeightmapTexWidth", 241);

            go.GetComponent<MeshRenderer>().material = material;
            go.transform.localPosition = new Vector3(startPosition.x, 0, startPosition.y);
            go.transform.localScale = new Vector3(90,100,90);
            go.name = $"TerrainAt"+startPosition;
        }
    }
}
