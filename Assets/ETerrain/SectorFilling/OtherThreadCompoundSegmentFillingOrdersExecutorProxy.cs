using Assets.Utils;
using Assets.Utils.MT;

namespace Assets.ETerrain.SectorFilling
{
    public class OtherThreadCompoundSegmentFillingOrdersExecutorProxy :  BaseOtherThreadProxy
    {
        private ISegmentOrdersFillingExecutor _executor;

        public OtherThreadCompoundSegmentFillingOrdersExecutorProxy(string namePrefix, ISegmentOrdersFillingExecutor executor)
            : base($"{namePrefix} - OtherThreadCompoundSegmentFillingListenerProxy", false)
        {
            _executor = executor;
        }

        public void ExecuteSegmentAction(SegmentGenerationProcessToken token, IntVector2 sap)
        {
            PostPureAsyncAction(() => _executor.ExecuteSegmentAction(token, sap));
        }
    }
}