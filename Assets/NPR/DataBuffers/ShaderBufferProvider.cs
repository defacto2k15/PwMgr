using UnityEngine;

namespace Assets.NPR.Lines
{
    public class ShaderBufferProvider  //: MonoBehaviour
    {
        public virtual ComputeBuffer ProvideBuffer(bool forceReload)
        {
            return null;
        }
    }
}