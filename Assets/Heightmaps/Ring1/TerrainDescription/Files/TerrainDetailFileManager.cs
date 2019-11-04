using System;
using System.IO;
using System.Threading.Tasks;
using Assets.Heightmaps.Ring1.Creator;
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

        public TerrainDetailFileManager(string mainDictionaryPath, CommonExecutorUTProxy commonExecutor)
        {
            _mainDictionaryPath = mainDictionaryPath;
            _commonExecutor = commonExecutor;
        }

        public async Task<RetrivedTerrainDetailTexture> TryRetriveHeightDetailElementAsync(MyRectangle area,
            TerrainCardinalResolution resolution)
        {
            string textureName = GenerateTextureName("Heightmap_", area, resolution);
            var texture = await TryRetriveHeightTextureAsync(textureName);
            if (texture == null)
            {
                return null;
            }
            else
            {
                var mergeStatusJson = await TryRetriveMergeStatusJson(textureName);
                return new RetrivedTerrainDetailTexture()
                {
                    CornersMergeStatus = mergeStatusJson.MergeStatus,
                    TextureWithSize = texture
                };
            }
        }

        public async Task<RetrivedTerrainDetailTexture> TryRetriveNormalDetailElementAsync(MyRectangle area,
            TerrainCardinalResolution resolution)
        {
            string textureName = GenerateTextureName("Normalmap_", area, resolution);
            var texture = await TryRetriveNormalTextureAsync(textureName);
            if (texture == null)
            {
                return null;
            }
            else
            {
                var mergeStatusJson = await TryRetriveMergeStatusJson(textureName);
                return new RetrivedTerrainDetailTexture()
                {
                    CornersMergeStatus = mergeStatusJson.MergeStatus,
                    TextureWithSize = texture
                };
            }
        }

        private async Task<TextureWithSize> TryRetriveHeightTextureAsync(string textureName)
        {
            IntVector2 textureSize = new IntVector2(241, 241);
            var path = _mainDictionaryPath + textureName;
            if (File.Exists(path))
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
            else
            {
                return null;
            }
        }

        private Task<TerrainDetailElementCornersMergeStatusJson> TryRetriveMergeStatusJson(string textureName)
        {
            var path = _mainDictionaryPath + textureName + ".json";
            if (!File.Exists(path))
            {
                Debug.Log($"W871 Nie ma mergeStatusJson o ścieżce "+path);
                return TaskUtils.MyFromResult(new TerrainDetailElementCornersMergeStatusJson()
                {
                    MergeStatus = CornersMergeStatus.NOT_MERGED
                });
            }
            return _commonExecutor.AddAction(() => JsonUtility.FromJson<TerrainDetailElementCornersMergeStatusJson>(File.ReadAllText(path)));
        }


        private async Task<TextureWithSize> TryRetriveNormalTextureAsync(string textureName)
        {
            IntVector2 textureSize = new IntVector2(241, 241);
            var path = _mainDictionaryPath + textureName;
            if (File.Exists(path))
            {
                var texture = await _commonExecutor.AddAction(() =>
                    SavingFileManager.LoadPngTextureFromFile(path, textureSize.X, textureSize.Y,
                        TextureFormat.ARGB32, true, true));

                return new TextureWithSize()
                {
                    Size = textureSize,
                    Texture = texture
                };
            }
            else
            {
                return null;
            }
        }

        public async Task SaveHeightDetailElementAsync(Texture texture, MyRectangle area, TerrainCardinalResolution resolution, CornersMergeStatus mergeStatus)
        {
            Debug.Log("G88 Saving!");
            string textureName = GenerateTextureName("Heightmap_", area, resolution);
            var path = _mainDictionaryPath + textureName;
            var texture2d = await ChangeHeightTextureToTexture2DAsync(texture);
            Debug.Log("R8 Final path:! " + path);
            await _commonExecutor.AddAction(() => SavingFileManager.SaveTextureToPngFile(path, texture2d));

            var jsonPath = _mainDictionaryPath + textureName + ".json";
            await _commonExecutor.AddAction(() =>
                File.WriteAllText(jsonPath, JsonUtility.ToJson(new TerrainDetailElementCornersMergeStatusJson()
                {
                    MergeStatus = mergeStatus
                })));
        }

        public async Task SaveNormalDetailElementAsync(Texture texture, MyRectangle area,
            TerrainCardinalResolution resolution, CornersMergeStatus mergeStatus)
        {
            string textureName = GenerateTextureName("Normalmap_", area, resolution);
            var path = _mainDictionaryPath + textureName;
            var texture2d = await ChangeNormalTextureToTexture2DAsync(texture);
            Debug.Log("L8 Final path:! " + path);
            await _commonExecutor.AddAction(() => SavingFileManager.SaveTextureToPngFile(path, texture2d));

            var jsonPath = _mainDictionaryPath + textureName + ".json";
            await _commonExecutor.AddAction(() =>
                File.WriteAllText(jsonPath, JsonUtility.ToJson(new TerrainDetailElementCornersMergeStatusJson()
                {
                    MergeStatus = mergeStatus
                })));
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

        private string GenerateTextureName(string prefix, MyRectangle area,
            TerrainCardinalResolution resolution)
        {
            return prefix + (int) area.X + "x" + (int) area.Y + "xx" + (int) area.Width + "x" +
                   (int) area.Height + "xx" + resolution.DetailResolution.MetersPerPixel.ToString("0.00") + ".tex";
        }
    }

    [Serializable]
    public class TerrainDetailElementCornersMergeStatusJson
    {
        public CornersMergeStatus MergeStatus;
    }
}