using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.Heightmaps.Ring1.MeshGeneration;

namespace Assets.Utils.UTUpdating
{
    public interface IUpdatable
    {
        float Update();
    }
}