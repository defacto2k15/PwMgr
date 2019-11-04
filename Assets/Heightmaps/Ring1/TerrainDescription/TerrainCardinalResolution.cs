using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.Utils;
using UnityEngine;

namespace Assets.Heightmaps.Ring1.TerrainDescription
{
    [Serializable]
    public class TerrainCardinalResolution
    {
        [SerializeField] private TerrainDetailResolution _detailResolution;
        [SerializeField] private string _name;

        public TerrainCardinalResolution(TerrainDetailResolution detailResolution, string name)
        {
            _detailResolution = detailResolution;
            _name = name;
        }

        public static TerrainCardinalResolution MAX_RESOLUTION =
            new TerrainCardinalResolution(TerrainDetailResolution.FromMetersPerPixel(0.375f), "MAX_RESOLUTION");

        public static TerrainCardinalResolution MID_RESOLUTION =
            new TerrainCardinalResolution(TerrainDetailResolution.FromMetersPerPixel(3), "MID_RESOLUTION");

        public static TerrainCardinalResolution MIN_RESOLUTION =
            new TerrainCardinalResolution(TerrainDetailResolution.FromMetersPerPixel(24), "MIN_RESOLUTION");

        public static List<TerrainCardinalResolution> AllResolutions = new List<TerrainCardinalResolution>()
        {
            MIN_RESOLUTION,
            MID_RESOLUTION,
            MAX_RESOLUTION
        };

        public static TerrainCardinalResolution ToSingletonResolution(TerrainCardinalResolution oldResolution)
        {
            return AllResolutions.First(c => Equals(c._detailResolution, oldResolution._detailResolution));
        }

        public TerrainDetailResolution DetailResolution => _detailResolution;

        public TerrainCardinalResolution LowerResolution
        {
            get
            {
                TerrainCardinalResolution previousResolution = null;
                foreach (var aResolution in AllResolutions)
                {
                    if (aResolution == this)
                    {
                        if (previousResolution == null)
                        {
                            Preconditions.Fail("there is no lower resolution. This is lowest");
                        }
                        else
                        {
                            return previousResolution;
                        }
                    }
                    previousResolution = aResolution;
                }
                Preconditions.Fail("unexpected. there is no current resolution in ranked resolutions list");
                return null;
            }
        }

        public static TerrainCardinalResolution FromRing1NodeLodLevel(int lodLevel)
        {
            return MIN_RESOLUTION; //todo!!
            if (lodLevel == 0)
            {
                return MIN_RESOLUTION;
            }
            else if (lodLevel == 1)
            {
                return MID_RESOLUTION;
            }
            else
            {
                return MAX_RESOLUTION;
            }
        }

        public override string ToString()
        {
            return $"{nameof(_name)}: {_name}";
        }
    }
}