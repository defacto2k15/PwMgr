using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.Repositioning;
using Assets.ShaderUtils;
using Assets.Trees.RuntimeManagement.SubjectsInstancesContainer;
using Assets.Trees.SpotUpdating;
using Assets.Utils;
using Assets.Utils.MT;
using UnityEngine;

namespace Assets.EProps
{
    public class EPropsDesignBodyChangesListener :  IDesignBodySpotUpdater
    {
        private EPropElevationManager _elevationManager;
        private Repositioner _repositioner;
        private ISpotPositionChangesListener _changesListener;
        private Dictionary<SpotId, EPropElevationPointer> _spotIdToElevationIdDict;
        private Dictionary<SpotId, List<EPropElevationPointer>> _spotIdToGroupElevationIdDict;

        public EPropsDesignBodyChangesListener(EPropElevationManager elevationManager, Repositioner repositioner)
        {
            _elevationManager = elevationManager;
            _repositioner = repositioner;
            _spotIdToElevationIdDict = new Dictionary<SpotId, EPropElevationPointer>();
            _spotIdToGroupElevationIdDict = new Dictionary<SpotId, List<EPropElevationPointer>>();
        }

        public void SetChangesListener(ISpotPositionChangesListener listener)
        {
            _changesListener = listener;
        }

        public Task RegisterDesignBodiesAsync(List<FlatPositionWithSpotId> bodiesWithIds)
        {
            Dictionary<SpotId, DesignBodySpotModification> modifications = new Dictionary<SpotId, DesignBodySpotModification>();
            foreach (var pair in bodiesWithIds)
            {
                var elevationId = _elevationManager.RegisterProp(_repositioner.Move(pair.FlatPosition));
                _spotIdToElevationIdDict[pair.SpotId] = elevationId;

                modifications[pair.SpotId] = new DesignBodySpotModification()
                {
                    Uniforms = CreatePackWithPointerUniform(elevationId)
                };
            }

            _changesListener.SpotsWereChanged(modifications);
            return TaskUtils.EmptyCompleted();
        }

        private static UniformsPack CreatePackWithPointerUniform(EPropElevationPointer elevationId)
        {
            var uniformsPack = new UniformsPack();
            uniformsPack.SetUniform("_Pointer", CastUtils.BitwiseCastUIntToFloat(elevationId.Value));
            return uniformsPack;
        }

        public Task RegisterDesignBodiesGroupAsync(SpotId id, List<Vector2> bodiesPositions)
        {
            if (!bodiesPositions.Any())
            {
                _spotIdToGroupElevationIdDict = new Dictionary<SpotId, List<EPropElevationPointer>>();
                return TaskUtils.EmptyCompleted();
            }
            var repositionedBodiesPositions = bodiesPositions.Select(c => _repositioner.Move(c)).ToList();
            var mins = new Vector2(repositionedBodiesPositions.Select(c=>c.x).Min(), repositionedBodiesPositions.Select(c=>c.y).Min());
            var maxs = new Vector2(repositionedBodiesPositions.Select(c=>c.x).Max(), repositionedBodiesPositions.Select(c=>c.y).Max());

            var minimalAlignmentLength = 32f;
            var minimumQuantIndex = new IntVector2(Mathf.FloorToInt(mins.x/minimalAlignmentLength), Mathf.FloorToInt(mins.y/minimalAlignmentLength));
            var maximumQuantIndex = new IntVector2(Mathf.FloorToInt(maxs.x/minimalAlignmentLength), Mathf.FloorToInt(maxs.y/minimalAlignmentLength));

            var perAlignmentBuckets = new Dictionary<IntVector2, List<Vector2>>();
            foreach (var aPosition in repositionedBodiesPositions)
            {
                var alignedBucketPosition = new IntVector2(Mathf.FloorToInt(aPosition.x / minimalAlignmentLength), Mathf.FloorToInt(aPosition.y / minimalAlignmentLength) );
                if (!perAlignmentBuckets.ContainsKey(alignedBucketPosition))
                {
                    perAlignmentBuckets[alignedBucketPosition] = new List<Vector2>();
                }
                perAlignmentBuckets[alignedBucketPosition].Add(aPosition);
            }

            var pointers =perAlignmentBuckets.Values.Where(c => c.Any()).SelectMany(c => _elevationManager.RegisterPropsGroup(c)).ToList();

            _spotIdToGroupElevationIdDict[id] = pointers;
            _changesListener.SpotGroupsWereChanged(new Dictionary<SpotId, List<DesignBodySpotModification>>()
            {
                [id] = pointers.Select(c => new DesignBodySpotModification() { Uniforms = CreatePackWithPointerUniform(c) }).ToList()
            });
            return TaskUtils.EmptyCompleted();
        }

        public void ForgetDesignBodies(List<SpotId> bodiesToRemove)
        {
            //TODO IMPLEMENT
        }

        public Task UpdateBodiesSpotsAsync(UpdatedTerrainTextures newHeightTexture)
        {
            throw new NotImplementedException();
        }

        public void RemoveTerrainTextures(SpotUpdaterTerrainTextureId id)
        {
            throw new NotImplementedException();
        }
    }
}
