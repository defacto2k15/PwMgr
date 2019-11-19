using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Grass2.Groups;
using Assets.Grass2.IntensitySampling;
using Assets.Grass2.PositionResolving;
using Assets.Grass2.Types;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.Repositioning;
using Assets.ShaderUtils;
using Assets.Trees.RuntimeManagement.SubjectsInstancesContainer;
using Assets.Trees.SpotUpdating;
using Assets.Utils;
using UnityEngine;

namespace Assets.Grass2.Planting
{
    public class GrassGroupsPlanter
    {
        private GrassDetailInstancer _grassDetailInstancer;
        private IGrassPositionResolver _positionResolver;
        private GrassGroupsContainer _groupsContainer;
        private IDesignBodyChangesListener _designBodiesChangeListener;
        private IGrass2AspectsGenerator _aspectsGenerator;
        private Dictionary<GrassType, GrassTypeTemplate> _templatesDictionary;

        private DoubleDictionary<SpotId, GrassGroupId> _spotIdToGrassGroupId =
            new DoubleDictionary<SpotId, GrassGroupId>();

        private Dictionary<SpotId, GroupWaitingToBePlanted> _groupsWaitingToBePlanted =
            new Dictionary<SpotId, GroupWaitingToBePlanted>();

        private Dictionary<GrassGroupId, List<Grass2Aspect>> _groupsAspectInfos =
            new Dictionary<GrassGroupId, List<Grass2Aspect>>();

        private Repositioner _repositioner;

        public GrassGroupsPlanter(
            GrassDetailInstancer grassDetailInstancer,
            IGrassPositionResolver positionResolver,
            GrassGroupsContainer groupsContainer,
            IDesignBodyChangesListener designBodiesChangeListener,
            IGrass2AspectsGenerator aspectsGenerator,
            Dictionary<GrassType, GrassTypeTemplate> templatesDictionary, Repositioner repositioner)
        {
            _grassDetailInstancer = grassDetailInstancer;
            _positionResolver = positionResolver;
            _groupsContainer = groupsContainer;
            _designBodiesChangeListener = designBodiesChangeListener;
            _templatesDictionary = templatesDictionary;
            _repositioner = repositioner;
            _aspectsGenerator = aspectsGenerator;
        }

        public GrassGroupId AddGrassGroup(MyRectangle generationArea, GrassType type,
            IIntensitySamplingProvider intensityProvider)
        {
            var instancesPerUnitSquare = _templatesDictionary[type].InstancesPerUnitSquare;
            var flatPositions =
                _positionResolver.ResolvePositions(generationArea, intensityProvider, instancesPerUnitSquare);

            var spotId = _designBodiesChangeListener.RegisterBodiesGroup(flatPositions);

            var grassGroupId = GrassGroupId.GenerateNext;
            _spotIdToGrassGroupId.Add(spotId, grassGroupId);

            var groupWaitingToBePlantedInfo = new GroupWaitingToBePlanted()
            {
                FlatPositions = flatPositions,
                Type = type
            };

            _groupsWaitingToBePlanted[spotId] = groupWaitingToBePlantedInfo;

            return grassGroupId;
        }

        public void RemoveGroup(GrassGroupId id)
        {
            _designBodiesChangeListener.ForgetDesignBodies(new List<SpotId>() {_spotIdToGrassGroupId.Get(id)});
            _spotIdToGrassGroupId.Remove(id);
            if (_groupsAspectInfos.ContainsKey(id)) //it was planteed
            {
                _groupsAspectInfos.Remove(id);
                _groupsContainer.RemoveGroup(id);
            }
        }

        public void GrassGroupSpotChanged(SpotId spotId, List< DesignBodySpotModification> spotDatas)
        {
            Debug.Log(" GrassGroupSpotChanged "+spotDatas.Count);
            MyProfiler.BeginSample("GrassGroupsPlanter. GrassGroupSpotChanged");
            var groupId = _spotIdToGrassGroupId.Get(spotId);
            if (!_groupsAspectInfos.ContainsKey(groupId))
            {
                var info = _groupsWaitingToBePlanted[spotId];
                _groupsWaitingToBePlanted.Remove(spotId);
                NewGroupResolvingEnded(spotId, spotDatas, info, groupId);
            }
            else
            {
                GroupMovingEnded(spotDatas, groupId);
            }
            MyProfiler.EndSample();
        }

        private void GroupMovingEnded(List< DesignBodySpotModification> spotDatas, GrassGroupId groupId)
        {
            MyProfiler.BeginSample("GrassGroupsPlanter. GroupMovingEnded");
            var aspectInfos = _groupsAspectInfos[groupId];

            _groupsContainer.RemoveGroup(groupId);
            _groupsContainer.AddGroup(
                Enumerable.Range(0, aspectInfos.Count)
                    .SelectMany(i => PositionEntitiesFromAspect(aspectInfos[i], spotDatas[i])).ToList(),
                groupId);
            MyProfiler.EndSample();
        }

        private void NewGroupResolvingEnded(SpotId spotId, List< DesignBodySpotModification> spotDatas, GroupWaitingToBePlanted info,
            GrassGroupId groupId)
        {
            MyProfiler.BeginSample("GrassGroupsPlanter. NewGroupResolvingEnded");
            var template = _templatesDictionary[info.Type];

            var unplantedInstances = _grassDetailInstancer.Initialize(spotDatas.Count, template);

            var outAspectsList = new List<Grass2Aspect>(spotDatas.Count);
            for (int i = 0; i < spotDatas.Count; i++)
            {
                var unplantedInstance = unplantedInstances[i];
                var flatPosition = _repositioner.Move(info.FlatPositions[i]);
                var aspects = _aspectsGenerator.GenerateAspect(unplantedInstance, flatPosition);
                outAspectsList.Add(aspects);
            }

            _groupsAspectInfos[groupId] = outAspectsList;
            _groupsContainer.AddGroup(
                Enumerable.Range(0, outAspectsList.Count)
                    .SelectMany(i => PositionEntitiesFromAspect(outAspectsList[i], spotDatas[i])).ToList(),
                groupId);
            MyProfiler.EndSample();
        }

        private static bool WarningWasSaid = false;

        private List<Grass2PositionedEntity> PositionEntitiesFromAspect(Grass2Aspect aspect,  DesignBodySpotModification modification)
        {
            var flatPos = aspect.FlatPos;
            //var rotation = Quaternion.LookRotation(modification.Normal.normalized).eulerAngles * Mathf.Deg2Rad; //TODO recreate
            if (!WarningWasSaid)
            {
                Debug.Log("W4 WARNING. GRASS ROTATION IS NOT SET. DO STH!!!");
                WarningWasSaid = true;
            }
            var centerTriplet = new MyTransformTriplet(
                new Vector3(flatPos.x, 0, flatPos.y),
                Vector3.zero, //rotation,
                Vector3.one
            );
            if (modification.SpotData != null)
            {
                centerTriplet.Position = new Vector3(centerTriplet.Position.x, modification.SpotData.Height, centerTriplet.Position.z);
            }
            var centerMatrix = centerTriplet.ToLocalToWorldMatrix();

            var additionalUniforms = new UniformsPack();
            if (modification.Uniforms != null)
            {
                additionalUniforms = modification.Uniforms;
            }

            return aspect.Entities.Select(c => new Grass2PositionedEntity()
            {
                LocalToWorldMatrix = centerMatrix * c.DeltaTransformTriplet.ToLocalToWorldMatrix(),
                Uniforms = UniformsPack.MergeTwo( c.Uniforms, additionalUniforms)
            }).ToList();
        }


        private class GroupWaitingToBePlanted
        {
            public List<Vector2> FlatPositions;
            public GrassType Type;
        }
    }
}