using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Trees.SpotUpdating
{
    public interface IDesignBodySpotUpdater
    {
        Task RegisterDesignBodiesAsync(List<FlatPositionWithSpotId> bodiesWithIds);
        Task RegisterDesignBodiesGroupAsync(SpotId id, List<Vector2> bodiesPositions);
        void ForgetDesignBodies(List<SpotId> bodiesToRemove);
        Task UpdateBodiesSpotsAsync(UpdatedTerrainTextures newHeightTexture);
        void RemoveTerrainTextures(SpotUpdaterTerrainTextureId id);
    }
}
