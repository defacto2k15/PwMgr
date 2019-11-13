using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Assets.Trees.RuntimeManagement.SubjectsInstancesContainer;
using Assets.Utils;
using UnityEngine;
using UnityEngine.Profiling;
using Debug = UnityEngine.Debug;

namespace Assets.Trees.RuntimeManagement.Management
{
    public class VegetationRuntimeManagement
    {
        private IVegetationSubjectsPositionsProvider _positionsProvider;
        private IVegetationSubjectInstancingContainerChangeListener _vegetationSubjectsChangesListener;
        private VegetationSubjectsVisibleEntitiesContainer _visibleEntitiesContainer;
        private VegetationRuntimeManagementConfiguration _configuration;

        public VegetationRuntimeManagement(IVegetationSubjectsPositionsProvider positionsProvider,
            IVegetationSubjectInstancingContainerChangeListener vegetationSubjectsChangesListener,
            VegetationSubjectsVisibleEntitiesContainer visibleEntitiesContainer,
            VegetationRuntimeManagementConfiguration configuration)
        {
            _positionsProvider = positionsProvider;
            _vegetationSubjectsChangesListener = vegetationSubjectsChangesListener;
            _visibleEntitiesContainer = visibleEntitiesContainer;
            _configuration = configuration;
        }

        private Vector2 _lastUpdatePosition;

        public void Start(Vector3 cameraPosition)
        {
            MyProfiler.BeginSample("VegetationRuntimeManagement Start");
            var twoDPos = new Vector2(cameraPosition.x, cameraPosition.z);
            _lastUpdatePosition = twoDPos;
            var detailFieldsTemplate = _configuration.DetailFieldsTemplate;
            var vegetationManagementAreas = detailFieldsTemplate.CalculateInitialManagementAreas(twoDPos);

            foreach (var managementArea in vegetationManagementAreas)
            {
                var gainedArea = managementArea.GainedArea;
                var level = managementArea.Level;
                var gainedEntities = _positionsProvider.GetEntiesFrom(gainedArea, level);
                if (gainedEntities.Any())
                {
                    _vegetationSubjectsChangesListener.AddInstancingOrder(level, (gainedEntities),
                        new List<VegetationSubjectEntity>());
                    _visibleEntitiesContainer.AddEntitiesFrom(gainedEntities, level);
                }
            }
            MyProfiler.EndSample();
        }

        public void Update(Vector3 cameraPosition)
        {
            MyProfiler.BeginSample("VegetationRuntimeManagement Update");
            var twoDPos = new Vector2(cameraPosition.x, cameraPosition.z);
            Vector2 positionDelta = twoDPos - _lastUpdatePosition;
            var deltaDistance = positionDelta.magnitude;

            if (deltaDistance > _configuration.UpdateMinDistance)
            {
                _lastUpdatePosition = twoDPos;

                var detailFieldsTemplate = _configuration.DetailFieldsTemplate;
                var vegetationManagementAreas = detailFieldsTemplate.CalculateManagementAreas(twoDPos, positionDelta);

                foreach (var managementArea in vegetationManagementAreas)
                {
                    var lostArea = managementArea.LostArea;
                    var gainedArea = managementArea.GainedArea;
                    var level = managementArea.Level;

                    var lostEntities = _visibleEntitiesContainer.GetAndDeleteEntitiesFrom(lostArea, level);
                    var gainedEntities = _positionsProvider.GetEntiesFrom(gainedArea, level);
                    _vegetationSubjectsChangesListener.AddInstancingOrder(level, (gainedEntities), lostEntities);

                    _visibleEntitiesContainer.AddEntitiesFrom(gainedEntities, level);
                }
            }
            MyProfiler.EndSample();
        }
    }
}