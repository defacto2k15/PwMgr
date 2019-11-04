using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Assets.Heightmaps.Ring1.Creator;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.Ring2.Db;
using Assets.Ring2.Devising;
using Assets.Ring2.IntensityProvider;
using Assets.Ring2.Painting;
using Assets.Ring2.PatchTemplateToPatch;
using Assets.Ring2.RegionsToPatchTemplate;
using Assets.Ring2.RuntimeManagementOtherThread.Finalizer;
using Assets.Utils;
using Assets.Utils.MT;
using UnityEngine;

namespace Assets.Ring2.RuntimeManagementOtherThread
{
    public class Ring2PatchesOverseer
    {
        private Ring2RegionsDatabase _database;
        private Ring2RegionsToPatchTemplateConventer _toPatchConventer;
        private Ring2PatchTemplateCombiner _templateCombiner;
        private Ring2PatchCreator _patchCreator;
        private Ring2IntensityPatternProvider _intensityPatternProvider;
        private Ring2Deviser _ring2Deviser;
        private Ring2PatchesPainterUTProxy _patchesPainter;
        private Ring2PatchesOverseerConfiguration _configuration;
        private IRing2PatchesOverseerFinalizer _finalizer;


        public Ring2PatchesOverseer(Ring2RegionsDatabase database,
            Ring2RegionsToPatchTemplateConventer toPatchConventer,
            Ring2PatchTemplateCombiner templateCombiner, Ring2PatchCreator patchCreator,
            Ring2IntensityPatternProvider intensityPatternProvider,
            Ring2Deviser ring2Deviser,
            Ring2PatchesPainterUTProxy patchesPainter, Ring2PatchesOverseerConfiguration configuration,
            IRing2PatchesOverseerFinalizer finalizer = null)
        {
            _database = database;
            _toPatchConventer = toPatchConventer;
            _templateCombiner = templateCombiner;
            _patchCreator = patchCreator;
            _intensityPatternProvider = intensityPatternProvider;
            _ring2Deviser = ring2Deviser;
            _patchesPainter = patchesPainter;
            _configuration = configuration;
            _finalizer = finalizer;
        }

        private async Task<OverseedPatchId> CreatePatchAsync(Ring2PatchesCreationOrderElement order)
        {
            AssertPatchSquareness(order.Rectangle);
            var queryArea = order.Rectangle;
            var regions = _database.QueryRegions(queryArea);
            // regiony które !mo¿e! s¹ w queryArea

            var patchTemplates = _toPatchConventer.Convert(regions, queryArea, _configuration.PatchSize);
            // dzielone s¹ na patche o patch size, ka¿dy patch sk³ada siê ze sliców

            patchTemplates = _templateCombiner.CombineTemplates(patchTemplates);
            // slice w pojedyñczym patchu sa ³¹czone

            // todo: sprawdzanie czy w danym sayerze slicu sa jakies dane, a jak nie to taki slice wywalic

            var patches = await TaskUtils.WhenAll(
                patchTemplates
                    .Select(template =>
                        _intensityPatternProvider.ProvidePatchWithIntensityPattern(
                            _patchCreator.CreatePatch(template),
                            template,
                            1 / 3f)
                    ).ToList());

            var devisedPatches = patches.Select(patch => _ring2Deviser.DevisePatch(patch)).ToList();

            if (_finalizer != null)
            {
                devisedPatches = await _finalizer.FinalizePatchesCreation(devisedPatches);
            }

            return await _patchesPainter.AddOrder(new Ring2PatchesPainterOrder()
            {
                CreationOrder = new Ring2PatchesPainterCreationOrder()
                {
                    Patches = devisedPatches
                }
            });
        }

        private void AssertPatchSquareness(MyRectangle orderRectangle)
        {
            var slicesSize = VectorUtils.MemberwiseDivide(orderRectangle.Size, _configuration.PatchSize);
            Preconditions.Assert(
                Mathf.Abs(slicesSize.x - Mathf.Round(slicesSize.x)) < 0.001 &&
                Mathf.Abs(slicesSize.y - Mathf.Round(slicesSize.y)) < 0.001,
                $"After splicing into patches all patches must be square, but are not!. orderRect: {orderRectangle} patchSize: {_configuration.PatchSize}"
            );
        }

        private void RemovePatch(List<uint> ids)
        {
            _patchesPainter.AddOrder(new Ring2PatchesPainterOrder()
            {
                RemovalOrder = new OverseedPatchId()
                {
                    Ids = ids
                }
            });
        }

        public async Task<List<OverseedPatchId>> ProcessOrderAsync(Ring2PatchesOverseerOrder order)
        {
            foreach (var removalOrder in order.RemovalOrders)
            {
                RemovePatch(removalOrder.Ids);
            }
            return await TaskUtils.WhenAll(
                order.CreationOrder.Select(creationOrder => CreatePatchAsync(creationOrder))
            );
        }
    }
}