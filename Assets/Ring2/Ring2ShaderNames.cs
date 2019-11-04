using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.Ring2
{
    public static class Ring2ShaderNames
    {
        public static List<string> ShaderNames
        {
            get
            {
                return new List<string>()
                {
                    RuntimeTerrainTexture,
                    FromImageTerrainTexture
                };
            }
        }

        public static string RuntimeTerrainTexture = "Custom/Ring2TerrainTextureTest1Feature";
        public static string FromImageTerrainTexture = "Custom/Ring2FromImageTerrainTexture";
    }
}