using Assets.NPR.Lines;
using UnityEngine;

namespace Assets.Measuring
{
    public class ShaderBufferNameUpdaterOC : MonoBehaviour, IOneTestConfigurationConsumer
    {
        public void ConsumeConfiguration(MOneTestConfiguration configuration)
        {
            GetComponent<MasterCachedShaderBufferInjectOC>().BaseName = configuration.MeshToUse.MeshBufferName;
        }
    }
}