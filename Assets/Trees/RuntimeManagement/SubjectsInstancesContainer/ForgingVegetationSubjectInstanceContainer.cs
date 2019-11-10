using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Assets.ShaderUtils;
using Assets.Trees.DesignBodyDetails;
using Assets.Trees.DesignBodyDetails.DesignBodyCharacteristics;
using Assets.Trees.SpotUpdating;
using Assets.Utils;
using Assets.Utils.MT;
using Assets.Utils.Services;
using UnityEngine;
using BaseOtherThreadProxy = Assets.Utils.MT.BaseOtherThreadProxy;

namespace Assets.Trees.RuntimeManagement.SubjectsInstancesContainer
{
    public class ForgingVegetationSubjectInstanceContainerProxy : BaseOtherThreadProxy,
        IVegetationSubjectInstancingContainerChangeListener
    {
        private readonly ForgingVegetationSubjectInstanceContainer _forgingContainer;

        public ForgingVegetationSubjectInstanceContainerProxy(
            ForgingVegetationSubjectInstanceContainer forgingContainer) :
            base("ForgingVegetationSubjectInstanceContainerProxyThread", false)
        {
            _forgingContainer = forgingContainer;
        }

        public void AddInstancingOrder(VegetationDetailLevel level, List<VegetationSubjectEntity> gainedEntities,
            List<VegetationSubjectEntity> lostEntities)
        {
            PostAction(() =>
            {
                var order = new VegetationSubjectsInstancingOrder(
                    new Queue<VegetationSubjectEntity>(gainedEntities),
                    new Queue<VegetationSubjectEntity>(lostEntities),
                    level
                );
                _forgingContainer.ProcessOrder(order);
                return TaskUtils.EmptyCompleted();
            });
        }

        public void AddSpotModifications(Dictionary<SpotId, DesignBodySpotModification> changedSpots)
        {
            PostAction(() =>
            {
                _forgingContainer.ProcessModifications(changedSpots);
                return TaskUtils.EmptyCompleted();
            });
        }
    }

    public class ForgingVegetationSubjectInstanceContainer
    {
        private IDesignBodyPortrayalForger _forger;
        private IDesignBodyChangesListener _spotUpdater;

        private Dictionary<int, ForgedEntityInfo> _currentEntities = new Dictionary<int, ForgedEntityInfo>();
        private DoubleDictionary<SpotId, int> _spotIdToInstanceId = new DoubleDictionary<SpotId, int>();

        private Dictionary<SpotId, EntityWithLevel> _notSpottedEntities = new Dictionary<SpotId, EntityWithLevel>();

        public ForgingVegetationSubjectInstanceContainer(IDesignBodyPortrayalForger forger, IDesignBodyChangesListener spotUpdater = null)
        {
            _forger = forger;
            _spotUpdater = spotUpdater;
        }

        private List<int> _creatingIds = new List<int>();

        public void ProcessOrder(VegetationSubjectsInstancingOrder order)
        {
            if (_spotUpdater == null)
            {
                foreach (var entity in order.CreationList)
                {
                    ForgeEntity(order.Level, entity);
                }
            }
            else
            {
                _creatingIds.AddRange(order.CreationList.Select(c => c.Id));
                if (order.CreationList.Any())
                {
                    var spotIds = _spotUpdater.RegisterDesignBodies(order.CreationList.Select(c => c.Position2D).ToList());
                    var entitiesList = order.CreationList.ToList();
                    for (int i = 0; i < spotIds.Count; i++)
                    {
                        var spotId = spotIds[i];
                        var entity = entitiesList[i];

                        _spotIdToInstanceId.Add(spotId, entity.Id);

                        _notSpottedEntities[spotId] = new EntityWithLevel()
                        {
                            Entity = entity,
                            Level = order.Level
                        };
                    }
                }
            }

            var spotIdsToForget = new List<SpotId>();
            foreach (var id in order.RemovalList)
            {
                if (!_spotIdToInstanceId.Contains(id.Id))
                {
                    Preconditions.Fail("E982 There is not id "+id.Id+" in dict");
                    continue;
                }
                var spotId = _spotIdToInstanceId.Get(id.Id);
                _spotIdToInstanceId.Remove(id.Id);
                if (!_currentEntities.ContainsKey(id.Id)) // this one is deleted before being placed!
                {
                    _notSpottedEntities.Remove(spotId);
                }
                else
                {
                    var entityInfo = _currentEntities[id.Id];
                    _forger.Remove(entityInfo.CombinationInstanceId);
                    if (entityInfo.SpotId.HasValue)
                    {
                        spotIdsToForget.Add(entityInfo.SpotId.Value);
                    }
                    _currentEntities.Remove(id.Id);
                }
            }
            _spotUpdater?.ForgetDesignBodies(spotIdsToForget);
        }


        private void ForgeEntity(VegetationDetailLevel level, VegetationSubjectEntity entity, SpotId? spotId = null,  DesignBodySpotModification spotModification = null)
        {
            var level1Detail = DesignBodyLevel1Detail.FromLevel0(entity.Detail, level);
            MyProfiler.BeginSample("Super forging1");
            var representationId = _forger.Forge(new DesignBodyLevel1DetailWithSpotModification(){Level1Detail = level1Detail,SpotModification = spotModification});
            _currentEntities[entity.Id] = new ForgedEntityInfo
            {
                CombinationInstanceId = representationId,
                Entity = entity,
                Level = level,
                SpotId = spotId
            };
            MyProfiler.EndSample();
        }

        private void ModifyEntity(ForgedEntityInfo entityInfo, ForgedEntityInfo forgedEntityInfo, DesignBodySpotModification spotModification=null)
        {
            var level1Detail = DesignBodyLevel1Detail.FromLevel0(entityInfo.Entity.Detail, entityInfo.Level);
            MyProfiler.BeginSample("Forge - Modifying entity");
            _forger.Modify(forgedEntityInfo.CombinationInstanceId, new DesignBodyLevel1DetailWithSpotModification(){Level1Detail = level1Detail,SpotModification = spotModification});
            MyProfiler.EndSample();
        }

        //public void RetriveSpotChanges(Dictionary<SpotId, SpotData> changedSpots)
        //{
        //    MyProfiler.BeginSample("Forgin - RetriveSpotChanges1");
        //    var modifiedSpots = new List<KeyValuePair<SpotId, SpotData>>();
        //    foreach (var pair in changedSpots)
        //    {
        //        if (!_spotIdToInstanceId.Contains(pair.Key))
        //        {
        //            // arleady removed
        //        }
        //        else if (_notSpottedEntities.ContainsKey(pair.Key)) // this is first creation
        //        {
        //            var entity = _notSpottedEntities[pair.Key]; //processed by order
        //            ForgeEntity(entity.Level, entity.Entity, pair.Key);
        //            _notSpottedEntities.Remove(pair.Key);
        //        }
        //        else if (_spotIdToInstanceId.Contains(pair.Key) && _currentEntities.ContainsKey(_spotIdToInstanceId.Get(pair.Key)))
        //        {
        //            modifiedSpots.Add(pair); //modification it is
        //        }
        //        else
        //        {
        //            Preconditions.Fail("E4532 Astray id: "+pair.Key);
        //        }
        //    }
        //    MyProfiler.EndSample();

        //    MyProfiler.BeginSample("Forgin - RetriveSpotChanges2");
        //    var modifiedEntities = modifiedSpots.Select(c => new
        //    {
        //        EntityInfo = _currentEntities[_spotIdToInstanceId.Get(c.Key)],
        //        SpotData = c.Value
        //    });
        //    foreach (var entityData in modifiedEntities)
        //    {
        //        _forger.Remove(entityData.EntityInfo.CombinationInstanceId);
        //        _currentEntities.Remove(entityData.EntityInfo.Entity.Id);
        //        ForgeEntity(entityData.EntityInfo.Level, entityData.EntityInfo.Entity,  entityData.EntityInfo.SpotId);
        //    }
        //    MyProfiler.EndSample();
        //}

        public void ProcessModifications(Dictionary<SpotId, DesignBodySpotModification> modifications)
        {
            MyProfiler.BeginSample("Forgin - RetriveSpotChanges1");

            var modifiedSpots = new List<KeyValuePair<SpotId, DesignBodySpotModification>>();
            foreach (var pair in modifications)
            {
                if (!_spotIdToInstanceId.Contains(pair.Key))
                {
                    // arleady removed
                }
                else if (_notSpottedEntities.ContainsKey(pair.Key)) // this is first creation
                {
                    var entity = _notSpottedEntities[pair.Key]; //processed by order
                    ForgeEntity(entity.Level, entity.Entity, pair.Key, pair.Value);
                    _notSpottedEntities.Remove(pair.Key);
                }
                else if (_spotIdToInstanceId.Contains(pair.Key) && _currentEntities.ContainsKey(_spotIdToInstanceId.Get(pair.Key)))
                {
                    modifiedSpots.Add(pair); //modification it is
                }
                else
                {
                    Preconditions.Fail("E4532 Astray id: "+pair.Key);
                }
            }
            MyProfiler.EndSample();

            MyProfiler.BeginSample("Forgin - RetriveSpotChanges2");
            var modifiedEntities = modifiedSpots.Select(c => new
            {
                EntityInfo = _currentEntities[_spotIdToInstanceId.Get(c.Key)],
                Modification = c.Value
            });
            foreach (var entityData in modifiedEntities)
            {
                ModifyEntity(entityData.EntityInfo, entityData.EntityInfo, entityData.Modification);
            }
            MyProfiler.EndSample();
        }


        private class EntityWithLevel
        {
            public VegetationSubjectEntity Entity;
            public VegetationDetailLevel Level;
        }

        private class ForgedEntityInfo
        {
            public VegetationSubjectEntity Entity;
            public VegetationDetailLevel Level;
            public RepresentationCombinationInstanceId CombinationInstanceId;
            public SpotId? SpotId;
        }
    }

    public class DesignBodySpotModification
    {
        public UniformsPack Uniforms;
        public SpotData SpotData;

        public DesignBodyLevel2Detail GenerateLevel2Details()
        {
            float height = 0;
            if (SpotData != null)
            {
                height = SpotData.Height;
            }
            return new DesignBodyLevel2Detail(position: new Vector3(0, height, 0), rotation: Quaternion.identity, scale: new Vector3(1, 1, 1),
                uniformsPack: Uniforms);
        }
    }

    public class DoubleDictionary<T1, T2>
    {
        private Dictionary<T1, T2> _forward = new Dictionary<T1, T2>();
        private Dictionary<T2, T1> _reverese = new Dictionary<T2, T1>();

        public void Add(T1 t1, T2 t2)
        {
            _forward[t1] = t2;
            _reverese[t2] = t1;
        }

        public T2 Get(T1 t1)
        {
            return _forward[t1];
        }

        public T1 Get(T2 t2)
        {
            return _reverese[t2];
        }

        public bool Contains(T1 t1)
        {
            return _forward.ContainsKey(t1);
        }

        public bool Contains(T2 t2)
        {
            return _reverese.ContainsKey(t2);
        }

        public void Remove(T1 t1)
        {
            Preconditions.Assert(_forward.ContainsKey(t1), $"There is not value {t1} in dictionary");
            var t2 = _forward[t1];
            _forward.Remove(t1);
            _reverese.Remove(t2);
        }

        public void Remove(T2 t2)
        {
            Preconditions.Assert( _reverese.ContainsKey(t2), $"There is not value {t2} in dictionary");
            var t1 = _reverese[t2];
            _reverese.Remove(t2);
            _forward.Remove(t1);
        }
    }
}