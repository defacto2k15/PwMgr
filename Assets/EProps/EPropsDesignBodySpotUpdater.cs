using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.Trees.SpotUpdating;
using UnityEngine;

namespace Assets.EProps
{
    public class EPropsDesignBodySpotUpdater : IDesignBodySpotUpdater
    {
        public Task RegisterDesignBodiesAsync(List<FlatPositionWithSpotId> bodiesWithIds)
        {
        }

        public Task RegisterDesignBodiesGroupAsync(SpotId id, List<Vector2> bodiesPositions)
        {
        }

        public void ForgetDesignBodies(List<SpotId> bodiesToRemove)
        {
        }

        public Task UpdateBodiesSpotsAsync(UpdatedTerrainTextures newHeightTexture)
        {
        }

        public void RemoveTerrainTextures(SpotUpdaterTerrainTextureId id)
        {
        }
    }
}
