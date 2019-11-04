using Assets.Grass2.Billboards;
using Assets.Utils;

namespace Assets.PreComputation.Configurations
{
    public class Grass2BillboardsPrecomputerConfiguration
    {
        public Grass2BillboardGenerator.Grass2BillboardGeneratorConfiguration BillboardGeneratorConfiguration =
            new Grass2BillboardGenerator.Grass2BillboardGeneratorConfiguration()
            {
                BillboardSize = new IntVector2(256, 256)
            };

        public Grass2BillboardClanGeneratorConfiguration ClanGeneratorConfiguration =
            new Grass2BillboardClanGeneratorConfiguration()
            {
                BillboardsToGenerateCount = 5
            };
    }
}