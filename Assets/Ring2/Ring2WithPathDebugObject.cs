using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.Random.Fields;
using Assets.Repositioning;
using Assets.Ring2.BaseEntities;
using Assets.Ring2.Db;
using Assets.Ring2.Devising;
using Assets.Ring2.IntensityProvider;
using Assets.Ring2.Painting;
using Assets.Ring2.PatchTemplateToPatch;
using Assets.Ring2.RegionsToPatchTemplate;
using Assets.Ring2.RuntimeManagementOtherThread;
using Assets.Roads.Files;
using Assets.TerrainMat;
using Assets.Utils;
using Assets.Utils.MT;
using Assets.Utils.TextureRendering;
using Assets.Utils.Textures;
using GeoAPI.Geometries;
using NetTopologySuite.Index.Quadtree;
using UnityEngine;

namespace Assets.Ring2
{
    public class Ring2WithPathDebugObject : MonoBehaviour
    {
        private Ring2PatchesPainterUTProxy _ring2PatchesPainterUtProxy;

        public void Start()
        {
            TaskUtils.SetGlobalMultithreading(false);

            var ring2MeshRepository = Ring2PlateMeshRepository.Create();
            var ring2ShaderRepository = Ring2PlateShaderRepository.Create();

            TextureConcieverUTProxy conciever =
                (new GameObject("TextureConciever", typeof(TextureConcieverUTProxy)))
                .GetComponent<TextureConcieverUTProxy>();

            _ring2PatchesPainterUtProxy = new Ring2PatchesPainterUTProxy(
                new Ring2PatchesPainter(
                    new Ring2MultishaderMaterialRepository(ring2ShaderRepository, Ring2ShaderNames.ShaderNames)));

            Ring2RandomFieldFigureGenerator figureGenerator = new Ring2RandomFieldFigureGenerator(new TextureRenderer(),
                new Ring2RandomFieldFigureGeneratorConfiguration()
                {
                    PixelsPerUnit = new Vector2(4, 4)
                });
            var utFigureGenerator = new RandomFieldFigureGeneratorUTProxy(figureGenerator);

            var randomFieldFigureRepository = new Ring2RandomFieldFigureRepository(utFigureGenerator,
                new Ring2RandomFieldFigureRepositoryConfiguration(2, new Vector2(20, 20)));

            var fileManager = new PathFileManager();
            var paths =
                (fileManager.LoadPaths(@"C:\inz\wrt\").First().Line as ILineString).Coordinates
                .Select(c => MyNetTopologySuiteUtils.ToVector2(c))
                .Select(c => (c - new Vector2(48150, 51800)) / 4f);


            Quadtree<Ring2Region> regionsTree =
                Ring2TestUtils.CreateRegionsTreeWithLinePath(randomFieldFigureRepository, paths);


            Ring2PatchesOverseer patchesOverseer = new Ring2PatchesOverseer(
                new MonoliticRing2RegionsDatabase(regionsTree),
                new Ring2RegionsToPatchTemplateConventer(),
                new Ring2PatchTemplateCombiner(),
                new Ring2PatchCreator(),
                new Ring2IntensityPatternProvider(conciever),
                new Ring2Deviser(ring2MeshRepository, Repositioner.Default),
                _ring2PatchesPainterUtProxy,
                new Ring2PatchesOverseerConfiguration()
                {
                    IntensityPatternPixelsPerUnit = new Dictionary<int, float>()
                    {
                        {1, 8}
                    },
                    PatchSize = new Vector2(5, 5)
                }
            );
            var msw = new MyStopWatch();
            msw.StartSegment("Main");
            patchesOverseer
                .ProcessOrderAsync(Ring2TestUtils.CreateCreationOrderOn(new MyRectangle(-25, -25, 50, 50),
                    new Vector2(20, 20))).Wait();
            UnityEngine.Debug.Log(msw.CollectResults());
        }
    }
}