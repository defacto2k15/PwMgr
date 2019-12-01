using System.Threading.Tasks;
using Assets.Utils;

namespace Assets.ETerrain.SectorFilling
{
    public interface ISegmentOrdersFillingExecutor
    {
        Task ExecuteSegmentAction(SegmentGenerationProcessToken token, IntVector2 sap);
    }
}