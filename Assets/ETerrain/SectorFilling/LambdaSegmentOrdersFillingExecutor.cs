using System;
using System.Threading.Tasks;
using Assets.Utils;
using Assets.Utils.MT;

namespace Assets.ETerrain.SectorFilling
{
    public class LambdaSegmentOrdersFillingExecutor : ISegmentOrdersFillingExecutor
    {
        private Func<IntVector2, Task> _segmentFillingFunc;

        public Task ExecuteSegmentAction(SegmentGenerationProcessToken token, IntVector2 sap)
        {
            if (token.RequiredSituation == RequiredSegmentSituation.Filled || token.RequiredSituation == RequiredSegmentSituation.Created)
            {
                return _segmentFillingFunc(sap);
            }
            else
            {
                return TaskUtils.EmptyCompleted();
            }
        }
    }
}