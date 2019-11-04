using System.Collections.Generic;

namespace Assets.NPR.ShaderCustomizer
{
    public enum ShaderAspect
    {
        Barycentric
    }

    public static class ShaderAspectsUtils
    {
        private static Dictionary<ShaderAspect, ShaderAspectDetails> _details = new Dictionary<ShaderAspect, ShaderAspectDetails>()
        {
            {ShaderAspect.Barycentric, new ShaderAspectDetails() { Token = "barycentric", SupportedModes = new List<ShaderFeatureDetectionMode>()
                { ShaderFeatureDetectionMode.Off, ShaderFeatureDetectionMode.Geometry, ShaderFeatureDetectionMode.Vertex}  } },
        };

        public static ShaderAspectDetails Details(this ShaderAspect feature)
        {
            return _details[feature];
        }
    }

    public class ShaderAspectDetails
    {
        public string Token;
        public List<ShaderFeatureDetectionMode> SupportedModes;
    }
}