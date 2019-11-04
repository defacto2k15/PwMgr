using System.Collections.Generic;

namespace Assets.NPR.ShaderCustomizer
{
    public enum ShaderToggle
    {
        GeometryShader
    }

    public static class ShaderTogglesUtils
    {
        private static Dictionary<ShaderToggle, ShaderToggleDetails> _details = new Dictionary<ShaderToggle, ShaderToggleDetails>()
        {
            {ShaderToggle.GeometryShader, new ShaderToggleDetails() { Token = "GEOMETRY_SHADER", LinesToDisablePrefix = new List<string>() {"#pragma geometry"} }},
        };

        public static ShaderToggleDetails Details(this ShaderToggle feature)
        {
            return _details[feature];
        }
    }

    public class ShaderToggleDetails
    {
        public string Token;
        public List<string> LinesToDisablePrefix = new List<string>(); 
    }
}