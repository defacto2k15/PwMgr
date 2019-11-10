using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Assets.Caching;
using Assets.Heightmaps.Ring1.Creator;
using Assets.Heightmaps.Ring1.TerrainDescription.Cache;
using Assets.Heightmaps.Ring1.TerrainDescription.CornerMerging;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.Utils;
using Assets.Utils.MT;
using Assets.Utils.Services;
using Assets.Utils.TextureRendering;
using Assets.Utils.Textures;
using UnityEngine;

namespace Assets.Heightmaps.Ring1.TerrainDescription
{
    public class TerrainDetailFileManager
    {
        private string _mainDictionaryPath;
        private CommonExecutorUTProxy _commonExecutor;
        private string _extension = ".png"; //TODO to configuration

        public TerrainDetailFileManager(string mainDictionaryPath, CommonExecutorUTProxy commonExecutor)
        {
            _mainDictionaryPath = mainDictionaryPath;
            _commonExecutor = commonExecutor;
        }

        public async Task SaveHeightDetailElementAsync(string filename, TextureWithSize textureWithSize)
        {
            Debug.Log("G88 Saving!");
            var path = _mainDictionaryPath + filename + _extension;
            var texture2d = await ChangeHeightTextureToTexture2DAsync(textureWithSize.Texture);
            Debug.Log("R8 Final path:! " + path);
            await _commonExecutor.AddAction(() => SavingFileManager.SaveTextureToPngFile(path, texture2d));
        }

        public async Task SaveNormalDetailElementAsync(string filename, TextureWithSize textureWithSize)
        {
            var path = _mainDictionaryPath + filename + _extension;
            var texture2d = await ChangeNormalTextureToTexture2DAsync(textureWithSize.Texture);
            await _commonExecutor.AddAction(() => SavingFileManager.SaveTextureToPngFile(path, texture2d));
        }

        private Task<Texture2D> ChangeHeightTextureToTexture2DAsync(Texture inputTexture)
        {
            var transformator = new TerrainTextureFormatTransformator(_commonExecutor);
            return transformator.PlainToEncodedHeightTextureAsync(new TextureWithSize()
            {
                Texture = inputTexture,
                Size = new IntVector2(241, 241)
            });
        }

        private async Task<Texture2D> ChangeNormalTextureToTexture2DAsync(Texture inputTexture)
        {
            if (inputTexture is Texture2D)
            {
                return inputTexture as Texture2D;
            }

            if (inputTexture is RenderTexture)
            {
                return await _commonExecutor.AddAction(
                    () => UltraTextureRenderer.RenderTextureToTexture2D(inputTexture as RenderTexture));
            }
            else
            {
                Preconditions.Fail("Cannot change texture " + inputTexture + " to texture2D");
                return null;
            }
        }

        public Task<List<string>> RetriveAllTerrainDetailFilesListAsync()
        {
            return _commonExecutor.AddAction(() =>
                Directory.GetFiles(_mainDictionaryPath).Select(c => c.TrimEndString(_extension).Replace(_mainDictionaryPath, "")).ToList());
        }

        public async Task<TextureWithSize> RetriveHeightDetailElementAsync(string filename)
        {
            IntVector2 textureSize = new IntVector2(241, 241);
            var path = _mainDictionaryPath + filename + _extension;
            Preconditions.Assert(File.Exists(path), $"Cannot retrive heightDetailElement of path {path} as it does not exist");
            {
                var texture = await _commonExecutor.AddAction(() =>
                {
                    var tex = SavingFileManager.LoadPngTextureFromFile(path, textureSize.X, textureSize.Y,
                        TextureFormat.ARGB32, true, true);
                    tex.wrapMode = TextureWrapMode.Clamp;
                    return tex;
                });

                var transformator = new TerrainTextureFormatTransformator(_commonExecutor);
                var plainTexture = await transformator.EncodedHeightTextureToPlainAsync(new TextureWithSize()
                {
                    Texture = texture,
                    Size = textureSize
                });

                return new TextureWithSize()
                {
                    Size = textureSize,
                    Texture = plainTexture
                };
            }
        }

        public async Task<TextureWithSize> RetriveNormalDetailElementAsync(string filename)
        {
            IntVector2 textureSize = new IntVector2(241, 241);
            var path = _mainDictionaryPath + filename + _extension;
            Preconditions.Assert(File.Exists(path), $"Cannot retrive normalDetailElement of path {path} as it does not exist");
            var texture = await _commonExecutor.AddAction(() =>
                SavingFileManager.LoadPngTextureFromFile(path, textureSize.X, textureSize.Y,
                    TextureFormat.ARGB32, true, true));

            return new TextureWithSize()
            {
                Size = textureSize,
                Texture = texture
            };
        }
    }

    public class CachingTerrainDetailFileManager : IAssetCachingFileManager<InternalTerrainDetailElementToken, TextureWithSize>
    {
        private TerrainDetailFileManager _fileManager;

        public CachingTerrainDetailFileManager(TerrainDetailFileManager fileManager)
        {
            _fileManager = fileManager;
        }

        public Task<TextureWithSize> RetriveAssetAsync(string filename, InternalTerrainDetailElementToken query)
        {
            if (query.Type == TerrainDescriptionElementTypeEnum.HEIGHT_ARRAY)
            {
                return (_fileManager.RetriveHeightDetailElementAsync(filename));
            }
            else if (query.Type == TerrainDescriptionElementTypeEnum.NORMAL_ARRAY)
            {
                return (_fileManager.RetriveNormalDetailElementAsync(filename));
            }
            else
            {
                Preconditions.Fail("Not supported detailelement type " + query.Type);
                return (null);
            }
        }

        public Task SaveAssetAsync(string filename, InternalTerrainDetailElementToken query, TextureWithSize asset)
        {
            if (query.Type == TerrainDescriptionElementTypeEnum.HEIGHT_ARRAY)
            {
                return (_fileManager.SaveHeightDetailElementAsync(filename, asset));
            }else if (query.Type == TerrainDescriptionElementTypeEnum.NORMAL_ARRAY)
            {
                return (_fileManager.SaveNormalDetailElementAsync(filename, asset));
            }
            else
            {
                Preconditions.Fail("Not supported detailelement type "+query.Type);
                return (null);
            }
        }

        public Task<List<string>> RetriveAllAssetFilenamesAsync()
        {
            return _fileManager.RetriveAllTerrainDetailFilesListAsync();
        }
    }
}