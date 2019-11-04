using System.Collections.Generic;
using Assets.NPR.ShaderCustomizer;

namespace Assets.NPR.PostProcessing.PPShaderCustomization
{
    public enum NprPPShaderFactor
    {
        Normals, Depth, ObjectID, SuggestiveHighlights, SuggestiveContours, IlluminationRidgeValley, HybridApparentRidges,
    }

    public static class NprPPShaderFactorUtils
    {
        private static Dictionary<NprPPShaderFactor, NprPPShaderFactorDetails> _details = new Dictionary<NprPPShaderFactor, NprPPShaderFactorDetails>()
        {
            {NprPPShaderFactor.Depth, new NprPPShaderFactorDetails() { Token = "depth", IsFilter = true} },
            {NprPPShaderFactor.Normals, new NprPPShaderFactorDetails() { Token = "normals", IsFilter = true} },
            {NprPPShaderFactor.ObjectID, new NprPPShaderFactorDetails() { Token = "objectid", IsFilter = true} },
            {NprPPShaderFactor.SuggestiveHighlights, new NprPPShaderFactorDetails() { Token = "sh"} },
            {NprPPShaderFactor.SuggestiveContours, new NprPPShaderFactorDetails() { Token = "sc"} },
            {NprPPShaderFactor.IlluminationRidgeValley, new NprPPShaderFactorDetails() { Token = "irv"} },
            {NprPPShaderFactor.HybridApparentRidges, new NprPPShaderFactorDetails() { Token = "ha"} },
        };

        public static NprPPShaderFactorDetails Details(this NprPPShaderFactor factor)
        {
            return _details[factor];
        }
    }

    public class NprPPShaderFactorDetails
    {
        public string Token;
        public bool IsFilter = false;
    }
}