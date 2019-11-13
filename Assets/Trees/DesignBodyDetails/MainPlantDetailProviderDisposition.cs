using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.Trees.RuntimeManagement;
using UnityEngine;

namespace Assets.Trees.DesignBodyDetails
{
    public class MainPlantDetailProviderDisposition
    {
        public Dictionary<VegetationDetailLevel, SingleDetailDisposition> PerDetailDispositions;
    }

    public class SingleDetailDisposition
    {
        public Vector3 SizeMultiplier = new Vector3(1, 1, 1);
        public Vector4 Color = new Vector4(1, 1, 1, 1);
        public List<Color> ColorGroups = new List<Color>();
        public float MeshHeightOffset = 0;
    }
}