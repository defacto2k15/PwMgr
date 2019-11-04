using UnityEngine;

namespace Assets.Grass2.IntensitySampling
{
    public interface IIntensitySamplingProvider
    {
        float Sample(Vector2 uv);
    }
}