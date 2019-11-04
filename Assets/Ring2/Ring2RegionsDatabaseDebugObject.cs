using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.Ring2.BaseEntities;
using Assets.Ring2.Db;
using Assets.Ring2.Devising;
using Assets.Ring2.IntensityProvider;
using Assets.Ring2.PatchTemplateToPatch;
using Assets.TerrainMat;
using Assets.Utils;
using GeoAPI.Geometries;
using NetTopologySuite.Geometries;
using NetTopologySuite.Index.Quadtree;
using NetTopologySuite.Operation.Distance;
using NetTopologySuite.Triangulate;
using UnityEngine;

namespace Assets.Ring2
{
    public class Ring2RegionsDatabaseDebugObject : MonoBehaviour
    {
        public Texture2D Texture0;
        public Texture2D Texture1;
        public Texture2D Texture2;
        public Texture2D Texture3;
        public Texture2D Texture4;

        //public void Start() //todo repair
        //{
        //    Ring2AreaDistanceDatabase distanceDatabase = new Ring2AreaDistanceDatabase();
        //    var groundFabric = new Ring2Fabric(Ring2Fiber.BaseGroundFiber, new Ring2FabricColors(new List<Color>()
        //    {
        //        new Color(0.2f, 0, 0),
        //        new Color(0.5f, 0, 0),
        //        new Color(0.7f, 0, 0),
        //        new Color(1, 0, 0),
        //    }), new FromAreaEdgeDistanceRing2IntensityProvider(1, distanceDatabase), 1);

        //    var dotFabric = new Ring2Fabric(Ring2Fiber.DottedTerrainFiber, new Ring2FabricColors(new List<Color>()
        //    {
        //        new Color(0, 0.2f,  0),
        //        new Color(0, 0.5f,  0),
        //        new Color(0, 0.7f,  0),
        //        new Color(0, 1, 0),
        //    }), new FromAreaEdgeDistanceRing2IntensityProvider(1, distanceDatabase), 1);

        //    var grassFabric = new Ring2Fabric(Ring2Fiber.GrassyFieldFiber, new Ring2FabricColors(new List<Color>()
        //    {
        //        new Color(0,0,0.2f),
        //        new Color(0,0,0.5f),
        //        new Color(0,0, 0.7f),
        //        new Color(0,0,1),
        //    }), new FromAreaEdgeDistanceRing2IntensityProvider(1, distanceDatabase), 1);

        //    var drySandFabric = new Ring2Fabric(Ring2Fiber.DrySandFiber, new Ring2FabricColors(new List<Color>()
        //    {
        //        new Color(1,0,0.2f),
        //        new Color(1,0,0.5f),
        //        new Color(1,0,0.7f),
        //        new Color(1,0,1),
        //    }), new FromAreaEdgeDistanceRing2IntensityProvider(1, distanceDatabase), 1);


        //    var region1Area = RegionSpaceUtils.Create(MyNetTopologySuiteUtils.ToPolygon(new[]
        //    {
        //        new Vector2(0, 0),
        //        new Vector2(10, 0),
        //        new Vector2(10, 10),
        //        new Vector2(0, 10),
        //    }));

        //    var region1Substance = new Ring2Substance(new List<Ring2Fabric>()
        //    {
        //        groundFabric,
        //        dotFabric
        //    });

        //    var region1 = new Ring2Region(region1Area, region1Substance, 2);

        //    var region2Area = RegionSpaceUtils.Create(MyNetTopologySuiteUtils.ToPolygon(new Vector2[]
        //    {
        //        new Vector2(5, 5),
        //        new Vector2(20, 5),
        //        new Vector2(20, 8),
        //        new Vector2(5, 8),
        //    }));

        //    var region2Substance = new Ring2Substance(new List<Ring2Fabric>
        //    {
        //        grassFabric,
        //        drySandFabric
        //    });

        //    var region2 = new Ring2Region(region2Area, region2Substance, 0);


        //    var defaultFabric = new Ring2Fabric(Ring2Fiber.BaseGroundFiber, new Ring2FabricColors(new List<Color>()
        //    {
        //        new Color(1, 0, 1),
        //        new Color(1, 0, 1),
        //        new Color(1, 0, 1),
        //        new Color(1, 0, 1),
        //    }),
        //    new ContantRing2IntensityProvider(),
        //    1);

        //    var defaultSubstance = new Ring2Substance(new List<Ring2Fabric>()
        //    {
        //        defaultFabric
        //    });

        //    Quadtree<Ring2Region> regionsTree = new Quadtree<Ring2Region>();
        //    regionsTree.Insert(region1.RegionEnvelope, region1);
        //    regionsTree.Insert(region2.RegionEnvelope, region2);

        //    var newDatabase = new Ring2RegionsDatabase(regionsTree);

        //    var queryArea = new Ring2Rectangle(0, 0, 10, 10);
        //    var sliceSize = new Vector2(queryArea.Width, queryArea.Height);
        //    var regions = newDatabase.QueryRegions(queryArea);

        //    var toPatchConventer = new Ring2RegionsToPatchTemplateConventer(defaultSubstance);
        //    var patchTemplates = toPatchConventer.Convert(regions, queryArea, sliceSize);

        //    var templateCombiner = new Ring2PatchTemplateCombiner();
        //    patchTemplates = templateCombiner.CombineTemplates(patchTemplates);

        //    var patchCreator = new Ring2PatchCreator();
        //    var patch = patchCreator.CreatePatch(patchTemplates[0], new Vector2(10, 10));


        //    Ring2PlateMaterialRepositiory materialRepositiory = new Ring2PlateMaterialRepositiory();
        //    var ring2Deviser = new Ring2Deviser(materialRepositiory);

        //    var devisedPatch = ring2Deviser.DevisePatch(patch);

        //    var patchesPainter = new Ring2PatchesPainter();
        //    patchesPainter.AddPatch(devisedPatch);

        //    if (patch.Slices.Count > 0)
        //    {
        //        Texture0 = patch.Slices[0].IntensityPattern.Texture;
        //    }
        //    if (patch.Slices.Count > 1)
        //    {
        //        Texture1 = patch.Slices[1].IntensityPattern.Texture;
        //    }
        //    if (patch.Slices.Count > 2)
        //    {
        //        Texture2 = patch.Slices[2].IntensityPattern.Texture;
        //    }
        //    if (patch.Slices.Count > 3)
        //    {
        //        Texture3 = patch.Slices[3].IntensityPattern.Texture;
        //    }
        //    if (patch.Slices.Count > 4)
        //    {
        //        Texture4 = patch.Slices[4].IntensityPattern.Texture;
        //    }
        //}
    }
}