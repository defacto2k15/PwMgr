using System.Collections.Generic;

namespace Assets.NPR.ShaderCustomizer
{
    public enum ShaderFeatureDetectionMode
    {
        Off, Vertex, Geometry, Pixel
    }

    public static class ShaderFeatureDetectionModeUtils
    {
        private static Dictionary<ShaderFeatureDetectionMode, ShaderFeatureDetectionModeDetails> _details = new Dictionary<ShaderFeatureDetectionMode, ShaderFeatureDetectionModeDetails>()
        {
            {ShaderFeatureDetectionMode.Off, new ShaderFeatureDetectionModeDetails() {Index = 0} },
            {ShaderFeatureDetectionMode.Vertex, new ShaderFeatureDetectionModeDetails() {Index = 1} },
            {ShaderFeatureDetectionMode.Geometry, new ShaderFeatureDetectionModeDetails() {Index = 2} },
            {ShaderFeatureDetectionMode.Pixel, new ShaderFeatureDetectionModeDetails() {Index = 3} },
        };

        public static ShaderFeatureDetectionModeDetails Details(this ShaderFeatureDetectionMode detectionMode)
        {
            return _details[detectionMode];
        }
    }

    public class ShaderFeatureDetectionModeDetails
    {
        public int Index;
    }
}