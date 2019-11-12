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
            // regiony które !mo¿e! s¹ w queryArea
            //Debug.Log($"R123333333333333333333 qaa:{queryArea} sIntresting ttrs: "+regions.Count(c => c.Space is FatLineString)+"  "+StringUtils.ToString(regions.Select(c => c.Space.GetType())));

            MyProfiler.BeginSample("GRing2PatchesCreation PatchConventer");
            var patchTemplates = _toPatchConventer.Convert(regions, queryArea);
            MyProfiler.EndSample();

            // dzielone s¹ na patche o patch size, ka¿dy patch sk³ada siê ze sliców

            //MyProfiler.BeginSample("GRing2PatchesCreation PatchCombiner"); //TODO remove, not not used as we use only stamped patches and even these only with rendering one slice per pass
            //patchTemplates = _templateCombiner.CombineTemplates(patchTemplates);
            //MyProfiler.EndSample();

            // slice w pojedyñczym patchu sa ³¹czone

            // todo: sprawdzanie czy w danym sayerze slicu sa jakies dane, a jak nie to taki slice wywalic

            Preconditions.Assert(_configuration.IntensityPatternPixelsPerUnit.ContainsKey(lodValue),
                "There is not IntensityPatternPixelsPerUnit for lod " + lodValue);
            float patternPixelsPerUnit = _configuration.IntensityPatternPixelsPerUnit[lodValue];

            MyProfiler.BeginSample("GRing2PatchesCreation IntensityProviding");
            var patches = await TaskUtils.WhenAll(
                patchTemplates
                    .Select(template =>
                        _intensityPatternProvider.ProvidePatchWithIntensityPattern(
                            _patchCreator.CreatePatch(template),
                            template,
                            patternPixelsPerUnit)
                    ).ToList());
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
}