using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.Heightmaps.Ring1.Creator;
using Assets.TerrainMat.Stain;
using Assets.Utils;
using Assets.Utils.Services;
using UnityEngine;

namespace Assets.TerrainMat
{
    public class StainTerrainResourceFileManager
    {
        private readonly CommonExecutorUTProxy _commonExecutor;
        private readonly StainTerrainResourceFileManagerPathsGenerator _pathsGenerator;

        public StainTerrainResourceFileManager(string path, CommonExecutorUTProxy commonExecutor)
        {
            _commonExecutor = commonExecutor;
            _pathsGenerator = new StainTerrainResourceFileManagerPathsGenerator(path);
        }

        public void SaveResources(StainTerrainResource resource)
        {
            var info = new StainTerrainResourceInfoJson()
            {
                PaletteMaxIndex = resource.PaletteMaxIndex,
                TerrainTextureSize = resource.TerrainTextureSize,
                TerrainPaletteTextureSize = resource.TerrainPaletteTexture.ToMySize(),
                ControlTextureSize = resource.ControlTexture.ToMySize(),
                PaletteIndexTextureSize = resource.PaletteIndexTexture.ToMySize()
            };
            File.WriteAllText(_pathsGenerator.InfoFilePath, JsonUtility.ToJson(info));
            SavingFileManager.SaveTextureToPngFile(_pathsGenerator.TerrainPaletteTexturePath,
                resource.TerrainPaletteTexture);
            SavingFileManager.SaveTextureToPngFile(_pathsGenerator.ControlTexturePath, resource.ControlTexture);
            SavingFileManager.SaveTextureToPngFile(_pathsGenerator.PaletteIndexTexturePath,
                resource.PaletteIndexTexture);
        }

        private StainTerrainResource _cachedResources;
        public async Task<StainTerrainResource> LoadResources()
        {
            if (_cachedResources != null)
            {
                return _cachedResources;
            }
            var info =
                JsonUtility.FromJson<StainTerrainResourceInfoJson>(File.ReadAllText(_pathsGenerator.InfoFilePath));

            var terrainPaletteTexture = await _commonExecutor.AddAction(() => LoadTexture(
                _pathsGenerator.TerrainPaletteTexturePath,
                info.TerrainPaletteTextureSize, TextureFormat.RGB24));

            var controlTexture = await _commonExecutor.AddAction(() => LoadTexture(_pathsGenerator.ControlTexturePath, info.ControlTextureSize,
                TextureFormat.RGB24, FilterMode.Bilinear));

            var paletteIndexTexture = await _commonExecutor.AddAction( () => LoadTexture(_pathsGenerator.PaletteIndexTexturePath,
                info.PaletteIndexTextureSize, TextureFormat.RHalf));

            _cachedResources = new StainTerrainResource()
            {
                PaletteIndexTexture = paletteIndexTexture,
                ControlTexture = controlTexture,
                TerrainPaletteTexture = terrainPaletteTexture,
                PaletteMaxIndex = info.PaletteMaxIndex,
                TerrainTextureSize = info.TerrainTextureSize
            };
            return _cachedResources;
        }

        private Texture2D LoadTexture(string path, IntVector2 size, TextureFormat format,
            FilterMode filterMode = FilterMode.Point)
        {
            
            var tex = SavingFileManager.LoadPngTextureFromFile(path, size.X, size.Y, format, true, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = filterMode;
            return tex;
        }

        [Serializable]
        public class StainTerrainResourceInfoJson
        {
            public float PaletteMaxIndex;
            public float TerrainTextureSize;
            public IntVector2 ControlTextureSize;
            public IntVector2 PaletteIndexTextureSize;
            public IntVector2 TerrainPaletteTextureSize;
        }

        private class StainTerrainResourceFileManagerPathsGenerator
        {
            private string _path;

            public StainTerrainResourceFileManagerPathsGenerator(string path)
            {
                _path = path;
            }

            public string ControlTexturePath => _path + $"/controlTexture.dat";
            public string PaletteIndexTexturePath => _path + $"/paletteIndexTexture.dat";
            public string TerrainPaletteTexturePath => _path + $"/terrainPaletteTexture.dat";
            public string InfoFilePath => _path + $"/info.json";
        }
    }
}