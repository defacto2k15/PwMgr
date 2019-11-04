using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.Heightmaps.Ring1.TerrainDescription;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.Utils.MT;
using Assets.Utils.Textures;

namespace Assets.Heightmaps.Welding
{
    public class HeightArrayWelderProxy : BaseOtherThreadProxy
    {
        private readonly HeightArrayWeldingDispatcher _dispatcher;
        private readonly MTCounter _lastTerrainId = new MTCounter();

        public HeightArrayWelderProxy(HeightArrayWeldingDispatcher dispatcher) : base("HeightArrayWelderProxy", false)
        {
            _dispatcher = dispatcher;
        }

        public int RegisterTerrain(WeldingInputTerrain inputTerrain)
        {
            var newId = _lastTerrainId.GetNext();
            inputTerrain.WeldingInputTerrainId = newId;
            PostPureAsyncAction(() => _dispatcher.RegisterTerrain(inputTerrain));
            return newId;
        }

        public void RemoveTerrain(int weldingTerrainId)
        {
            PostPureAsyncAction(() => _dispatcher.RemoveTerrain(weldingTerrainId));
        }
    }
}