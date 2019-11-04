using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Utils
{
    public static class CastUtils
    {
        public static float BitwiseCastUIntToFloat(uint i)
        {
            return BitConverter.ToSingle(BitConverter.GetBytes(i), 0);
        }

        public static byte[] ConvertFloatArrayToByte(float[] floatArray1)
        {
            var byteArray = new byte[floatArray1.Length * 4];
            Buffer.BlockCopy(floatArray1, 0, byteArray, 0, byteArray.Length);
            return byteArray;
        }
    }
}
