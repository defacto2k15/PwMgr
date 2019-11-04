using System.Collections.Generic;

namespace Assets.Grass2.Billboards
{
    public class Grass2BillboardClanGenerator
    {
        private Grass2BillboardGenerator _billboardGenerator;
        private Grass2BillboardClanGeneratorConfiguration _configuration;

        public Grass2BillboardClanGenerator(Grass2BillboardGenerator billboardGenerator,
            Grass2BillboardClanGeneratorConfiguration configuration = null)
        {
            _billboardGenerator = billboardGenerator;
            if (configuration == null)
            {
                configuration = new Grass2BillboardClanGeneratorConfiguration();
            }
            _configuration = configuration;
        }

        public Grass2SingledBillboardClan Generate()
        {
            var billboardsList = new List<DetailedGrass2SingleBillboard>();
            for (int i = 0; i < _configuration.BillboardsToGenerateCount; i++)
            {
                var bladesCount = (i + 3) * 10;
                for (int k = 0; k < 5; k++)
                {
                    var seed = i + ((float) k / 10);
                    var texture = _billboardGenerator.GenerateBillboardImageAsync(bladesCount, seed).Result;
                    billboardsList.Add(new DetailedGrass2SingleBillboard()
                    {
                        BladesCount = bladesCount,
                        Texture = texture
                    });
                }
            }
            return new Grass2SingledBillboardClan()
            {
                BillboardsList = billboardsList
            };
        }
    }

    public class Grass2BillboardClanGeneratorConfiguration
    {
        public int BillboardsToGenerateCount = 5;
    }
}