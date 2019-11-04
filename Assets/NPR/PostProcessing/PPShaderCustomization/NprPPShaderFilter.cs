using System.Collections.Generic;

namespace Assets.NPR.PostProcessing.PPShaderCustomization
{
    public enum NprPPShaderFilter
    {
        Sobel, Roberts, Fwidth, FreiChen
    }

    public static class NprPPShaderFilterUtils
    {
        private static Dictionary<NprPPShaderFilter, NprPPShaderFilterDetails> _details = new Dictionary<NprPPShaderFilter, NprPPShaderFilterDetails>()
        {
            {NprPPShaderFilter.Sobel, new NprPPShaderFilterDetails() {Token = "sobel"} },
            {NprPPShaderFilter.Roberts, new NprPPShaderFilterDetails() {Token = "roberts"} },
            {NprPPShaderFilter.Fwidth, new NprPPShaderFilterDetails() {Token = "fwidth"} },
            {NprPPShaderFilter.FreiChen, new NprPPShaderFilterDetails() {Token = "freiChen"} },
        };

        public static NprPPShaderFilterDetails Details(this NprPPShaderFilter factor)
        {
            return _details[factor];
        }
    }

    public class NprPPShaderFilterDetails
    {
        public string Token;
    }
}