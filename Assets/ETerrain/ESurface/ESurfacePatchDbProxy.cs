using System.Threading.Tasks;
using Assets.ETerrain.Pyramid.Map;
using Assets.Heightmaps.GRing;
using Assets.Heightmaps.Ring1.TerrainDescription;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.Utils.MT;

namespace Assets.ESurface
{
    public class ESurfacePatchDbProxy : BaseOtherThreadProxy
    {
        private CachedESurfacePatchProvider _patchProvider;

        public ESurfacePatchDbProxy(CachedESurfacePatchProvider patchProvider) : base("ESurfacePatchDbProxy", false)
        {
            _patchProvider = patchProvider;
        }

        public Task<TokenizedESurfaceTexturesPackToken> ProvideSurfaceDetail(MyRectangle inGamePosition, FlatLod flatLod)
        {
            return GenericPostAction(() => _patchProvider.ProvideSurfaceDetail(inGamePosition, flatLod));
        }

        public void RemoveSurfaceDetailAsync(ESurfaceTexturesPack pack, ESurfaceTexturesPackToken token)
        {
            PostPureAsyncAction(() => _patchProvider.RemoveSurfaceDetailAsync(pack, token));
        }

    }
}