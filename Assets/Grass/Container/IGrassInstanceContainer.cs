using Assets.ShaderUtils;
using UnityEngine;

namespace Assets.Grass.Container
{
    interface IGrassInstanceContainer
    {
        void Draw();
        void SetGlobalColor(string name, Color value);
        void SetGlobalUniform(ShaderUniformName name, float strength);
        void SetGlobalUniform(ShaderUniformName name, Vector4 value);
    }
}