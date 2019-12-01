using System.Collections.Generic;
using System.Linq;
using Assets.Utils;

namespace Assets.ETerrain.SectorFilling
{
    public class UnityThreadCompoundSegmentFillingListener : ISegmentFillingListener
    {
        private OtherThreadCompoundSegmentFillingOrdersExecutorProxy _executor;
        private Dictionary<IntVector2,  SegmentGenerationProcessTokenWithFillingNecessity> _tokensDict;

        public UnityThreadCompoundSegmentFillingListener(OtherThreadCompoundSegmentFillingOrdersExecutorProxy executor)
        {
            _executor = executor;
            _tokensDict = new Dictionary<IntVector2, SegmentGenerationProcessTokenWithFillingNecessity>();
        }

        public Dictionary<IntVector2, SegmentGenerationProcessTokenWithFillingNecessity> TokensDict => _tokensDict;

        public void AddSegment(SegmentInformation segmentInfo)
        {
            var sap = segmentInfo.SegmentAlignedPosition;
            Preconditions.Assert(!_tokensDict.ContainsKey(sap), $"There arleady is segment of sap {sap}");

            RequiredSegmentSituation requiredSituation;
            bool fillingIsNecessary;
            if (segmentInfo.SegmentState == SegmentState.Active)
            {
                fillingIsNecessary = true;
                requiredSituation = RequiredSegmentSituation.Filled;
            }
            else if (segmentInfo.SegmentState == SegmentState.Standby)
            {
                fillingIsNecessary = false;
                requiredSituation = RequiredSegmentSituation.Filled;
            }
            else
            {
                fillingIsNecessary = false;
                requiredSituation = RequiredSegmentSituation.Created;
            }
            var newToken = new SegmentGenerationProcessToken(SegmentGenerationProcessSituation.BeforeStartOfCreation,requiredSituation);
            _tokensDict[sap] = new SegmentGenerationProcessTokenWithFillingNecessity()
            {
                Token = newToken,
                FillingIsNecessary =  fillingIsNecessary
            };
            _executor.ExecuteSegmentAction(newToken, sap);
        }

        public void RemoveSegment(SegmentInformation segmentInfo)
        {
            var sap = segmentInfo.SegmentAlignedPosition;
            Preconditions.Assert(_tokensDict.ContainsKey(sap),"Cannot remove segment, as it was never present in dict "+segmentInfo.SegmentAlignedPosition);
            var token = _tokensDict[sap].Token;
            token.RequiredSituation = RequiredSegmentSituation.Removed;
            _executor.ExecuteSegmentAction(token, sap);
            _tokensDict.Remove(sap);
        }

        public void SegmentStateChange(SegmentInformation segmentInfo)
        {
            var sap = segmentInfo.SegmentAlignedPosition;
            Preconditions.Assert(_tokensDict.ContainsKey(sap), "During segmentStateChange to Active there is no tokens in dict");
            var token = _tokensDict[sap];
            if (segmentInfo.SegmentState == SegmentState.Active)
            {
                token.Token.RequiredSituation = RequiredSegmentSituation.Filled;
                token.FillingIsNecessary = true;
                _executor.ExecuteSegmentAction(token.Token, sap);
            }
            else if (segmentInfo.SegmentState == SegmentState.Standby)
            {
                token.Token.RequiredSituation = RequiredSegmentSituation.Filled;
                token.FillingIsNecessary = false;
            }
            else
            {
                token.Token.RequiredSituation = RequiredSegmentSituation.Created;
                token.FillingIsNecessary = false;

            }
        }

        public int BlockingProcessesCount()
        {
            if (!_tokensDict.Any())
            {
                return 0;
            }

            return _tokensDict.Select(c => c.Value).Sum(c =>
            {
                if (c.Token.RequiredSituation == RequiredSegmentSituation.Filled && c.FillingIsNecessary)
                {
                    if (c.Token.CurrentSituation == SegmentGenerationProcessSituation.Filled)
                    {
                        return 0;
                    }
                    else
                    {
                        return 1;
                    }
                }

                return 0;
            });
        }
    }
}