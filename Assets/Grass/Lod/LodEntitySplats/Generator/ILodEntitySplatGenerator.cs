using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.Utils;

namespace Assets.Grass.Lod
{
    interface ILodEntitySplatGenerator
    {
        LodEntitySplat Generate(MapAreaPosition position);
    }
}