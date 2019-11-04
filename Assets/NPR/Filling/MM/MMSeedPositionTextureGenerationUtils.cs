using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.NPR.Filling.Szecsi;
using Assets.Utils;
using UnityEngine;

namespace Assets.NPR.Filling.MM
{
    public static class MMSeedPositionTextureGenerationUtils
    {
        public static Color[,,] GenerateSeedPositionsArray3D (PointsWithLastBits positionsWithBits)
        {
            var outArray = new Color[4, 4, 4];
            for (int i = 0; i < 64; i++)
            {
                var pos = positionsWithBits.Positions[i];
                //pos[2] = 0.1f;

                var seedBlockCoord = new IntVector3(
                    Mathf.FloorToInt(pos[0] * 4),
                    Mathf.FloorToInt(pos[1] * 4),
                    Mathf.FloorToInt(pos[2] * 4)
                );

                Vector3 seedOffset = new Vector3(
                    pos[0] * 4 - seedBlockCoord.X,
                    pos[1] * 4 - seedBlockCoord.Y,
                    pos[2] * 4 - seedBlockCoord.Z
                );

                var bits = positionsWithBits.LastCycleBits[i];

                int bitsFactor = 0;
                if (bits[0] == 1)
                {
                    bitsFactor += 1;
                }

                if (bits[1] == 1)
                {
                    bitsFactor += 2;
                }

                if (bits[2] == 1)
                {
                    bitsFactor += 4;
                }

                float bitsFloat = bitsFactor / 7.0f;

                outArray[seedBlockCoord.X, seedBlockCoord.Y, seedBlockCoord.Z] = 
                    new Color(seedOffset.x, seedOffset.y, seedOffset.z, bitsFloat);
            }

            return outArray;
        }


        public static Texture3D GenerateSeedPositionTexture3DFromArray (Color[,,] array)
        {
            var tex = new Texture3D(array.GetLength(0), array.GetLength(1), array.GetLength(2), TextureFormat.ARGB32, false)
            {
                wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Point
            };

            for (int x = 0; x < array.GetLength(0); x++)
            {
                for (int y = 0; y < array.GetLength(1); y++)
                {
                    for (int z = 0; z < array.GetLength(2); z++)
                    {
                        tex.SetPixel(x,y,z, array[x,y,z]);
                    }
                }
            }
            tex.Apply();
            return tex;
        }

    }
}
