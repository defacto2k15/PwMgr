using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.Habitat;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.Ring2.BaseEntities;
using Assets.Ring2.Db;
using Assets.Ring2.Devising;
using Assets.Ring2.Geometries;
using Assets.Ring2.IntensityProvider;
using Assets.Ring2.Painting;
using Assets.Ring2.PatchTemplateToPatch;
using Assets.Ring2.RegionsToPatchTemplate;
using Assets.Ring2.RuntimeManagementOtherThread;
using Assets.Ring2.RuntimeManagementOtherThread.Finalizer;
using Assets.Utils;
using Assets.Utils.MT;
using Assets.Utils.Quadtree;
using UnityEngine;
using UnityEngine.Profiling;

namespace Assets.Ring2.GRuntimeManagementOtherThread
{
    public class GRing2PatchesCreator
    {
        private IRing2RegionsDatabase _database;
        private GRing2RegionsToPatchTemplateConventer _toPatchConventer;
        private Ring2PatchTemplateCombiner _templateCombiner;
        private Ring2PatchCreator _patchCreator;
        private Ring2IntensityPatternProvider _intensityPatternProvider;
        private GRing2Deviser _ring2Deviser;
        private Ring2PatchesOverseerConfiguration _configuration;

        public GRing2PatchesCreator(IRing2RegionsDatabase database,
            GRing2RegionsToPatchTemplateConventer toPatchConventer,
            Ring2PatchTemplateCombiner templateCombiner, Ring2PatchCreator patchCreator,
            Ring2IntensityPatternProvider intensityPatternProvider,
            GRing2Deviser ring2Deviser,
            Ring2PatchesOverseerConfiguration configuration)
        {
            _database = database;
            _toPatchConventer = toPatchConventer;
            _templateCombiner = templateCombiner;
            _patchCreator = patchCreator;
            _intensityPatternProvider = intensityPatternProvider;
            _ring2Deviser = ring2Deviser;
            _configuration = configuration;
        }

        public async Task<List<GRing2PatchDevised>> CreatePatchAsync(MyRectangle queryArea, int lodValue)
        {
            MyProfiler.BeginSample("GRing2PatchesCreation QueryRegions");
            var regions = _database.QueryRegions(lodValue, queryArea);
            MyProfiler.EndSample();
            // regiony które !może! są w queryArea

            MyProfiler.BeginSample("GRing2PatchesCreation PatchConventer");
            var patchTemplates = _toPatchConventer.Convert(regions, queryArea);
            MyProfiler.EndSample();

            MyProfiler.BeginSample("GRing2PatchesCreation Slices combining");
            var slicesCombiner = new GRing2SlicesCombiner(); //TODO to constructor instead of templateCombiner
            patchTemplates = patchTemplates.Select(c => slicesCombiner.CombineSlicesInPatch(c)).ToList();
            MyProfiler.EndSample();

            // dzielone są na patche o patch size, każdy patch składa się ze sliców
            //MyProfiler.BeginSample("GRing2PatchesCreation PatchCombiner"); //TODO remove, not not used as we use only stamped patches and even these only with rendering one slice per pass
            //patchTemplates = _templateCombiner.CombineTemplates(patchTemplates);
            //MyProfiler.EndSample();
            // slice w pojedyñczym patchu sa ³¹czone

            // todo: sprawdzanie czy w danym sayerze slicu sa jakies dane, a jak nie to taki slice wywalic

            Preconditions.Assert(_configuration.IntensityPatternPixelsPerUnit.ContainsKey(lodValue),
                "There is not IntensityPatternPixelsPerUnit for lod " + lodValue);
            float patternPixelsPerUnit = _configuration.IntensityPatternPixelsPerUnit[lodValue];

            MyProfiler.BeginSample("GRing2PatchesCreation IntensityProviding");
            var patches = await TaskUtils.WhenAll(patchTemplates.Select(template =>
                _intensityPatternProvider.ProvidePatchWithIntensityPattern(_patchCreator.CreatePatch(template), template, patternPixelsPerUnit)).ToList());
            MyProfiler.EndSample();

            MyProfiler.BeginSample("GRing2PatchesCreation PatchDevising");
            var devisedPatches = patches.Select(patch => _ring2Deviser.DevisePatch(patch)).ToList();
            MyProfiler.EndSample();

            return devisedPatches;
        }
    }


    public class GRing2PatchesCreatorProxy : BaseOtherThreadProxy
    {
        private GRing2PatchesCreator _creator;

        public GRing2PatchesCreatorProxy(GRing2PatchesCreator creator) : base("GRing2PatchesCreatorProxy", false)
        {
            _creator = creator;
        }

        public Task<List<GRing2PatchDevised>> CreatePatchAsync(MyRectangle queryArea, int lodValue)
        {
            var tcs = new TaskCompletionSource<List<GRing2PatchDevised>>();
            PostAction(async () => tcs.SetResult(await _creator.CreatePatchAsync(queryArea, lodValue)));
            return tcs.Task;
        }
    }

    public class GRing2SlicesCombiner
    {
        public Ring2PatchTemplate CombineSlicesInPatch(Ring2PatchTemplate ring2PatchTemplate)
        {
            var workList = ring2PatchTemplate.SliceTemplates.ToList();

            var outList = new List<Ring2SliceTemplate>();
            while (workList.Any())
            {
                var indexesOfSimilarSlices = new List<int>(){0};
                var patternElement = workList[0];

                for (int i = 1; i < workList.Count; i++)
                {
                    var elementToCheck = workList[i];
                    if (SlicesAreSimilar(patternElement, elementToCheck))
                    {
                        indexesOfSimilarSlices.Add(i);
                    }
                }

                var similarSlices = indexesOfSimilarSlices.Select(c => workList[c]).ToList();
                indexesOfSimilarSlices.Reverse();
                foreach (var idx in indexesOfSimilarSlices)
                {
                    workList.RemoveAt(idx);
                }

                if (similarSlices.Count == 1)
                {
                    outList.Add(patternElement);
                }
                else
                {

                    List<Ring2Fabric> fabrics = new List<Ring2Fabric>();
                    for (var fabricIndex = 0; fabricIndex < patternElement.Substance.LayerFabrics.Count; fabricIndex++)
                    {
                        var subProviders = similarSlices.Select(c => c.Substance.LayerFabrics[fabricIndex].IntensityProvider).ToList();
                        var summedProvider = new MaxValueCollectionIntensityProvider(subProviders);
                        var patternFabric = patternElement.Substance.LayerFabrics[fabricIndex];
                        fabrics.Add(new Ring2Fabric(patternFabric.Fiber, patternFabric.PaletteColors, summedProvider, patternFabric.LayerPriority, patternFabric.PatternScale));
                    }
                    outList.Add(new Ring2SliceTemplate(new Ring2Substance(fabrics)));
                }
            }

            return new Ring2PatchTemplate(outList, ring2PatchTemplate.SliceArea);
        }

        private bool SlicesAreSimilar(Ring2SliceTemplate t1, Ring2SliceTemplate t2)
        {
            if (!(t1.Substance.LayerFabrics.Count == t2.Substance.LayerFabrics.Count))
            {
                return false;
            }

            for (int i = 0; i < t1.Substance.LayerFabrics.Count; i++)
            {
                if (!FabricsAreSimilar(t1.Substance.LayerFabrics[i], t2.Substance.LayerFabrics[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private bool FabricsAreSimilar(Ring2Fabric f1, Ring2Fabric f2)
        {
            double EPSILON = 0.0000001f;
            return f1.IsFirm == f2.IsFirm
                   && Equals(f1.Fiber, f2.Fiber)
                   && Mathf.Abs(f1.LayerPriority - f2.LayerPriority) < EPSILON
                   && Mathf.Abs(f1.PatternScale - f2.PatternScale) < EPSILON
                   && PalettesAreSimilar(f1.PaletteColors, f2.PaletteColors);
        }

        private bool PalettesAreSimilar(Ring2FabricColors p1, Ring2FabricColors p2)
        {
            if (p1.Colors.Count != p2.Colors.Count)
            {
                return false;
            }

            double EPSILON = 0.0000001f;
            return Enumerable.Range(0, p1.Colors.Count).Select(c => p1.Colors[c].ToVector3() - p2.Colors[c].ToVector3()).Select(c => c.magnitude).Min() < EPSILON;
        }
    }
}