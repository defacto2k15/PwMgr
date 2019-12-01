using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Assets.Utils;

namespace Assets.ETerrain.SectorFilling
{
    public class CompoundSegmentOrdersFillingExecutor<T> : ISegmentOrdersFillingExecutor
    {
        private Func<IntVector2, Task<T>> _segmentGeneratingFunc;
        private Func<IntVector2,T, Task> _segmentFillingFunc;
        private Func<T, Task> _segmentRemovalFunc;
        private Dictionary<IntVector2,T > _currentlyCreatedSegments;

        public CompoundSegmentOrdersFillingExecutor(Func<IntVector2, Task<T>> segmentGeneratingFunc, Func<IntVector2, T, Task> segmentFillingFunc, Func<T, Task> segmentRemovalFunc)
        {
            _segmentGeneratingFunc = segmentGeneratingFunc;
            _segmentFillingFunc = segmentFillingFunc;
            _segmentRemovalFunc = segmentRemovalFunc;
            _currentlyCreatedSegments = new Dictionary<IntVector2, T>();
        }

        private async Task<bool> SegmentProcessOneLoop(SegmentGenerationProcessToken token, IntVector2 sap)
        {
            switch (token.CurrentSituation)
            {
                case SegmentGenerationProcessSituation.BeforeStartOfCreation:
                    switch (token.RequiredSituation)
                    {
                        case RequiredSegmentSituation.Created:
                            await GenerateSegment(token, sap);
                            return true;
                        case RequiredSegmentSituation.Filled:
                            if (!_currentlyCreatedSegments.ContainsKey(sap))
                            {
                                await GenerateSegment(token, sap);
                            }
                            else
                            {
                                await FillSegment(token, sap);
                            }
                            return true;
                        case RequiredSegmentSituation.Removed:
                            await RemoveSegment(token, sap);
                            return false;
                        default:
                            Preconditions.Fail("Unexpected required situation " + token.RequiredSituation);
                            return false;
                    }
                case SegmentGenerationProcessSituation.Created:
                    switch (token.RequiredSituation)
                    {
                        case RequiredSegmentSituation.Created:
                            return false;
                        case RequiredSegmentSituation.Filled:
                            await FillSegment(token, sap);
                            return true;
                        case RequiredSegmentSituation.Removed:
                            await RemoveSegment(token, sap);
                            return false;
                        default:
                            Preconditions.Fail("Unexpected required situation " + token.RequiredSituation);
                            return false;
                    }
                case SegmentGenerationProcessSituation.Filled:
                    switch (token.RequiredSituation)
                    {
                        case RequiredSegmentSituation.Created:
                            return false;
                        case RequiredSegmentSituation.Filled:
                            return false;
                        case RequiredSegmentSituation.Removed:
                            await RemoveSegment(token, sap);
                            return false;
                        default:
                            Preconditions.Fail("Unexpected required situation " + token.RequiredSituation);
                            return false;
                    }
                default:
                    Preconditions.Fail("Unexpected situation " + token.CurrentSituation);
                    return false;
            }
        }

        public async Task ExecuteSegmentAction(SegmentGenerationProcessToken token, IntVector2 sap)
        {
            if (!token.ProcessIsOngoing) { 
                bool shouldContinue = true;
                while (shouldContinue)
                {
                    shouldContinue = await SegmentProcessOneLoop(token, sap);
                }
            }
        }

        private async Task RemoveSegment(SegmentGenerationProcessToken token, IntVector2 sap)
        {
            Preconditions.Assert(!token.ProcessIsOngoing, "Cannot remove while process in ongoing");
            Preconditions.Assert(token.RequiredSituation == RequiredSegmentSituation.Removed, "Required situation in not removed but "+token.RequiredSituation);
            if (_currentlyCreatedSegments.ContainsKey(sap))
            {
                var segment = _currentlyCreatedSegments[sap];
                _currentlyCreatedSegments.Remove(sap);
                await _segmentRemovalFunc(segment);
            }
        }

        private async Task FillSegment(SegmentGenerationProcessToken token, IntVector2 sap)
        {
            Preconditions.Assert(token.CurrentSituation == SegmentGenerationProcessSituation.Created, "Unexpected situaton "+token.CurrentSituation);
            token.CurrentSituation = SegmentGenerationProcessSituation.DuringFilling;
            await _segmentFillingFunc(sap, _currentlyCreatedSegments[sap]);
            token.CurrentSituation = SegmentGenerationProcessSituation.Filled;
        }

        private async Task GenerateSegment(SegmentGenerationProcessToken token, IntVector2 sap)
        {
            Preconditions.Assert(token.CurrentSituation == SegmentGenerationProcessSituation.BeforeStartOfCreation, "Unexpected situaton "+token.CurrentSituation);
            if (!_currentlyCreatedSegments.ContainsKey(sap))
            {
                token.CurrentSituation = SegmentGenerationProcessSituation.DuringCreation;
                var newSegment = await _segmentGeneratingFunc(sap);
                _currentlyCreatedSegments[sap] = newSegment;
            }
            token.CurrentSituation = SegmentGenerationProcessSituation.Created;
        }
    }
}