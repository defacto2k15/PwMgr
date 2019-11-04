using System.Collections.Generic;

namespace Assets.NPR.ShaderCustomizer
{
    public enum ShaderFeatureApplyMode
    {
        Off, Fins, SurfaceLine, Filling, LineFilling
    }

    public static class ShaderFeatureApplyModeUtils
    {
        private static Dictionary<ShaderFeatureApplyMode, ShaderFeatureApplyModeDetails> _details = new Dictionary<ShaderFeatureApplyMode, ShaderFeatureApplyModeDetails>()
        {
            {ShaderFeatureApplyMode.Off, new ShaderFeatureApplyModeDetails() {Index = 0,} },
            {ShaderFeatureApplyMode.Fins, new ShaderFeatureApplyModeDetails() {Index = 1,
            } },
            {ShaderFeatureApplyMode.SurfaceLine, new ShaderFeatureApplyModeDetails()
            {
                Index = 2,
            } },
            {ShaderFeatureApplyMode.Filling, new ShaderFeatureApplyModeDetails()
            {
                Index = 3,
            } },
            {ShaderFeatureApplyMode.LineFilling, new ShaderFeatureApplyModeDetails()
            {
                Index = 4,
            } },
        };

        public static ShaderFeatureApplyModeDetails Details(this ShaderFeatureApplyMode detectionMode)
        {
            return _details[detectionMode];
        }
    }

    public class ShaderFeatureApplyModeDetails
    {
        public int Index;
    }
}