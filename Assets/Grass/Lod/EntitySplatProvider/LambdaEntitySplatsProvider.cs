using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.Grass.Container;
using UnityEngine;

namespace Assets.Grass.Lod
{
    class LambdaEntitySplatsProvider : IEntitySplatsProvider
    {
        private readonly Func<Vector3, Vector2, int, IEntitySplat> _func;

        public LambdaEntitySplatsProvider(Func<Vector3, Vector2, int, IEntitySplat> func)
        {
            this._func = func;
        }

        public IEntitySplat GenerateGrassSplat(Vector3 position, Vector2 size, int lodLevel)
        {
            return _func(position, size, lodLevel);
        }
    }
}