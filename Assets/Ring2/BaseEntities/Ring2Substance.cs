using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Ring2.BaseEntities
{
    public class Ring2Substance
    {
        public Ring2Substance(List<Ring2Fabric> layerFabrics)
        {
            LayerFabrics = layerFabrics;
        }

        public List<Ring2Fabric> LayerFabrics { get; private set; }

        public List<Ring2Fabric> GetProperLayerFabrics
        {
            get { return LayerFabrics.Take(Mathf.Min(4, LayerFabrics.Count)).OrderBy(c => c.Fiber.Index).ToList(); }
        }

        public Vector4 GetProperLayerFabricsPriorities
        {
            get
            {
                Vector4 outValue = new Vector4(0, 0, 0, 0);
                for (int i = 0; i < GetProperLayerFabrics.Count; i++)
                {
                    outValue[i] = GetProperLayerFabrics[i].LayerPriority;
                }
                return outValue;
            }
        }

        public Vector4 GetLayerFabricsPatternScales
        {
            get
            {
                Vector4 outValue = new Vector4(0, 0, 0, 0);
                for (int i = 0; i < GetProperLayerFabrics.Count; i++)
                {
                    outValue[i] = GetProperLayerFabrics[i].PatternScale;
                }
                return outValue;
            }
        }

        public ShaderKeywordSet RetriveShaderKeywordSet()
        {
            List<string> keywords = new List<string>();
            for (int i = 0; i < Math.Min(LayerFabrics.Count, 4); i++)
            {
                keywords.Add(LayerFabrics[i].Fiber.FiberKeyword);
            }
            return new ShaderKeywordSet(keywords);
        }
    }
}