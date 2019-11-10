using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Assets.Caching;
using Assets.FinalExecution;
using Assets.Heightmaps.GRing;
using Assets.Heightmaps.Ring1.Creator;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.Ring2;
using Assets.Utils;
using Assets.Utils.Services;
using Assets.Utils.TextureRendering;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Assets.ESurface
{
    public class CachedESurfacePatchProvider
    {
        //TODO: implement removing thigns from cache
        private ESurfacePatchProvider _provider;
        private IAssetsCache<ESurfaceTexturesPackInternalToken, NullableESurfaceTexturesPack> _cache;

        public CachedESurfacePatchProvider(ESurfacePatchProvider provider, IAssetsCache<ESurfaceTexturesPackInternalToken, NullableESurfaceTexturesPack> cache)
        {
            _provider = provider;
            _cache = cache;
        }

        public Task Initialize()
        {
            return _cache.InitializeAsync();
        }

        public async Task<ESurfaceTexturesPack> ProvideSurfaceDetail(MyRectangle inGamePosition, FlatLod flatLod)
        {
            var internalToken = GenerateInternalToken(inGamePosition, flatLod);
            var queryOutput = await _cache.TryRetriveAsync(internalToken);

            if (queryOutput.Asset != null)
            {
                return queryOutput.Asset.Pack;
            }
            else
            {
                var detailElement = _provider.ProvideSurfaceDetail(inGamePosition, flatLod);
                var queryOutputCreationObligationToken = queryOutput.CreationObligationToken.Value;
                var tokenizedElement = await _cache.AddAssetAsync( queryOutputCreationObligationToken, internalToken, new NullableESurfaceTexturesPack(){Pack = detailElement});
                return detailElement;
            }
        }

        private ESurfaceTexturesPackInternalToken GenerateInternalToken(MyRectangle inGamePosition, FlatLod flatLod)
        {
            return new ESurfaceTexturesPackInternalToken(
                new IntRectangle(Mathf.RoundToInt(inGamePosition.X), Mathf.RoundToInt(inGamePosition.Y)
                    , Mathf.RoundToInt(inGamePosition.Width), Mathf.RoundToInt(inGamePosition.Height))
                , flatLod.ScalarValue);
        }

    }


    public class ESurfaceTexturesPackInternalToken : IFromQueryFilenameProvider
    {
        private IntRectangle _queryArea;
        private int _lodScalarValue;

        public ESurfaceTexturesPackInternalToken(IntRectangle queryArea, int lodScalarValue)
        {
            _queryArea = queryArea;
            _lodScalarValue = lodScalarValue;
        }

        public string ProvideFilename()
        {
            return $"Pack_{_lodScalarValue}x{_queryArea.ToString().Replace(':','_')}";
        }
    }


    public class ESurfaceTexturesPackEntityActionsPerformer : MemoryCachableAssetsActionsPerformer<NullableESurfaceTexturesPack>
    {
        private CommonExecutorUTProxy _commonExecutor;

        public ESurfaceTexturesPackEntityActionsPerformer(CommonExecutorUTProxy commonExecutor)
        {
            _commonExecutor = commonExecutor;
        }

        public int CalculateMemoryUsage(NullableESurfaceTexturesPack entity)
        {
            if (entity.Pack == null)
            {
                return 0;
            }
            return (entity.Pack.MainTexture.width + entity.Pack.MainTexture.height) * 4*2;
        }

        public Task DestroyAsset(NullableESurfaceTexturesPack entity)
        {
            return _commonExecutor.AddAction(() =>
            {
                if (entity.Pack != null)
                {
                    GameObject.Destroy(entity.Pack.MainTexture);
                    GameObject.Destroy(entity.Pack.NormalTexture);
                }
            });
        }
    }

    public class NullableESurfaceTexturesPack
    {
        public ESurfaceTexturesPack Pack;
    }

    public class ESurfaceTexturesPackFileManager : IAssetCachingFileManager<ESurfaceTexturesPackInternalToken, NullableESurfaceTexturesPack>
    {
        private CommonExecutorUTProxy _commonExecutor;
        private string _mainDictionaryPath;
        private string _extension = ".png";
        private string _extensionForMainTexture=".main";
        private string _extensionForNormalTexture=".normal";
        private string _extensionForNullFile =".null";

        public ESurfaceTexturesPackFileManager(CommonExecutorUTProxy commonExecutor, string mainDictionaryPath)
        {
            _commonExecutor = commonExecutor;
            _mainDictionaryPath = mainDictionaryPath;
        }

        public async Task<NullableESurfaceTexturesPack> RetriveAssetAsync(string filename, ESurfaceTexturesPackInternalToken query)
        {
            var isNullFilePresent = await _commonExecutor.AddAction(() => File.Exists(_mainDictionaryPath + filename + _extensionForNullFile));
            if (isNullFilePresent)
            {
                return new NullableESurfaceTexturesPack()
                {
                    Pack = null
                };
            }
            return new NullableESurfaceTexturesPack()
            {
                Pack = new ESurfaceTexturesPack() { 
                    MainTexture = await LoadTextureFromFile(_mainDictionaryPath+filename+_extensionForMainTexture+_extension),
                    NormalTexture=  await LoadTextureFromFile(_mainDictionaryPath+filename+_extensionForNormalTexture+_extension),
                }
            };
        }

        private async Task<Texture> LoadTextureFromFile(string path) //todo not remove texture2D but renderTexture
        {
            return await _commonExecutor.AddAction(() => SavingFileManager.LoadPngTextureFromFile(path, true, true));
        }

        public async Task SaveAssetAsync(string filename, ESurfaceTexturesPackInternalToken query, NullableESurfaceTexturesPack asset)
        {
            if (asset.Pack == null)
            {
                await CreateNullFile(_mainDictionaryPath + filename + _extensionForNullFile);
            }
            else
            {
                await SaveTextureToFile(asset.Pack.MainTexture, _mainDictionaryPath + filename + _extensionForMainTexture + _extension);
                await SaveTextureToFile(asset.Pack.NormalTexture, _mainDictionaryPath + filename + _extensionForNormalTexture + _extension);
            }
        }

        private Task CreateNullFile(string path)
        {
            return _commonExecutor.AddAction(() => File.Create(path).Dispose());
        }

        private async Task SaveTextureToFile(Texture texture, string path)
        {
            var tex2d = await ChangeTextureToTexture2DAsync(texture);
            await _commonExecutor.AddAction(() =>
            {
                SavingFileManager.SaveTextureToPngFile(path, tex2d);
            });
        }

        private async Task<Texture2D> ChangeTextureToTexture2DAsync(Texture inputTexture)
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

        public Task<List<string>> RetriveAllAssetFilenamesAsync()
        {
            return _commonExecutor.AddAction(() =>
                Directory.GetFiles(_mainDictionaryPath).Select(
                    c => c.TrimEndString(_extension)
                        .Replace(_mainDictionaryPath, "")
                        .Replace(_extensionForMainTexture,"")
                        .Replace(_extensionForNormalTexture,"")
                        .Replace(_extensionForNullFile, ""))
                    .Distinct().ToList());
        }
    }
}
