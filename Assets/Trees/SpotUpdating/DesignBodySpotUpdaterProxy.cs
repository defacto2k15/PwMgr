using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Assets.Heightmaps.Ring1;
using Assets.Utils;
using Assets.Utils.MT;
using Assets.Utils.Services;
using UnityEngine;
using Wintellect.PowerCollections;
using BaseOtherThreadProxy = Assets.Utils.MT.BaseOtherThreadProxy;

namespace Assets.Trees.SpotUpdating
{
    public class DesignBodySpotUpdaterProxy : BaseOtherThreadProxy, IDesignBodyChangesListener
    {
        private readonly IDesignBodySpotUpdater _updater;

        private int _lastSpotId = 0;
        private int _lastTerrainId = 0;

        public DesignBodySpotUpdaterProxy(IDesignBodySpotUpdater updater) : base("DesignBodySpotUpdaterProxyThread",
            true)
        {
            _updater = updater;
        }

        public List<SpotId> RegisterDesignBodies(List<Vector2> bodiesFlatPositions)
        {
            var bodiesWithIds = bodiesFlatPositions.Select(c => new FlatPositionWithSpotId()
            {
                FlatPosition = c,
                SpotId = new SpotId(_lastSpotId++)
            }).ToList();

            PostPureAsyncAction(() => _updater.RegisterDesignBodiesAsync(bodiesWithIds));
            return bodiesWithIds.Select(c => c.SpotId).ToList();
        }

        public SpotId RegisterBodiesGroup(List<Vector2> bodiesPositions)
        {
            var id = new SpotId(_lastSpotId++, true);

            PostPureAsyncAction(() => _updater.RegisterDesignBodiesGroupAsync(id, bodiesPositions));
            return id;
        }

        public void ForgetDesignBodies(List<SpotId> bodiesToRemove)
        {
            PostAction(() =>
            {
                _updater.ForgetDesignBodies(bodiesToRemove);
                return TaskUtils.EmptyCompleted();
            });
        }

        public SpotUpdaterTerrainTextureId UpdateBodiesSpots(UpdatedTerrainTextures newHeightTexture)
        {
            var outId = new SpotUpdaterTerrainTextureId(_lastTerrainId++);
            PostPureAsyncAction(() =>
            {
                newHeightTexture.TerrainTextureId = outId;
                return _updater.UpdateBodiesSpotsAsync(newHeightTexture);
            });
            return outId;
        }

        public void RemoveTerrainTextures(SpotUpdaterTerrainTextureId id)
        {
            PostAction(() =>
            {
                _updater.RemoveTerrainTextures(id);
                return TaskUtils.EmptyCompleted();
            });
        }
    }

    public interface IDesignBodyChangesListener
    {
        List<SpotId> RegisterDesignBodies(List<Vector2> bodiesFlatPositions);
        SpotId RegisterBodiesGroup(List<Vector2> bodiesPositions);
        void ForgetDesignBodies(List<SpotId> bodiesToRemove);
    }


    public class RootMediatorSpotPositionsUpdater : ISpotPositionChangesListener
    {
        private List<ISpotPositionChangesListener> _innerListeners = new List<ISpotPositionChangesListener>();

        public void AddListener(ISpotPositionChangesListener listener)
        {
            _innerListeners.Add(listener);
        }

        public void SpotsWereChanged(Dictionary<SpotId, SpotData> changedSpots)
        {
            _innerListeners.ForEach(c => c.SpotsWereChanged(changedSpots));
        }

        public void SpotGroupsWereChanged(Dictionary<SpotId, List<SpotData>> changedSpots)
        {
            _innerListeners.ForEach(c => c.SpotGroupsWereChanged(changedSpots));
        }
    }

    public class ListenerCenteredMediatorDesignBodyChangesUpdater : ISpotPositionChangesListener, IDesignBodyChangesListener
    {
        private IDesignBodyChangesListener _innerDesignBodyChangesListener;
        private ISpotPositionChangesListener _targetChangesListener;

        private Set<SpotId> _ourTargetSpotIds = new Set<SpotId>();

        public ListenerCenteredMediatorDesignBodyChangesUpdater(IDesignBodyChangesListener innerDesignBodyChangesListener)
        {
            _innerDesignBodyChangesListener = innerDesignBodyChangesListener;
        }

        public void SetTargetChangesListener(ISpotPositionChangesListener listener)
        {
            _targetChangesListener = listener;
        }


        public void SpotsWereChanged(Dictionary<SpotId, SpotData> changedSpots)
        {
            var filtered = changedSpots.Where(c => _ourTargetSpotIds.Contains(c.Key)).ToDictionary(c => c.Key, c => c.Value);
            if (filtered.Any())
            {
                _targetChangesListener.SpotsWereChanged(filtered);
            }
        }

        public void SpotGroupsWereChanged(Dictionary<SpotId, List<SpotData>> changedSpots)
        {
            var filtered = changedSpots.Where(c => _ourTargetSpotIds.Contains(c.Key)).ToDictionary(c => c.Key, c => c.Value);
            if (filtered.Any())
            {
                _targetChangesListener.SpotGroupsWereChanged(filtered);
            }
        }

        public List<SpotId> RegisterDesignBodies(List<Vector2> bodiesFlatPositions)
        {
            var ids = _innerDesignBodyChangesListener.RegisterDesignBodies(bodiesFlatPositions);
            _ourTargetSpotIds.AddMany(ids);
            return ids;
        }

        public SpotId RegisterBodiesGroup(List<Vector2> bodiesPositions)
        {
            var id = _innerDesignBodyChangesListener.RegisterBodiesGroup(bodiesPositions);
            _ourTargetSpotIds.Add(id);
            return id;
        }

        public void ForgetDesignBodies(List<SpotId> bodiesToRemove)
        {
            _ourTargetSpotIds.RemoveMany(bodiesToRemove);
            _innerDesignBodyChangesListener.ForgetDesignBodies(bodiesToRemove);
        }
    }
}