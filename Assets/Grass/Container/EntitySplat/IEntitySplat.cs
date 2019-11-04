using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.Grass
{
    interface IEntitySplat
    {
        void Remove();
        IEntitySplat Copy();
        void SetMesh(Mesh newMesh);
        void Enable();
    }
}