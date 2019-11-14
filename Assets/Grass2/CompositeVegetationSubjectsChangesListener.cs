using System.Collections.Generic;
using System.Linq;
using Assets.Trees.RuntimeManagement;
using Assets.Trees.RuntimeManagement.SubjectsInstancesContainer;
using UnityEngine;

namespace Assets.Grass2
{
    public class CompositeVegetationSubjectsChangesListener : IVegetationSubjectInstancingContainerChangeListener
    {
        private List<VegetationSubjectsInstancingChangeListenerWithFilter> _listenersWithFilters;

        public CompositeVegetationSubjectsChangesListener(
            List<VegetationSubjectsInstancingChangeListenerWithFilter> listenersWithFilters)
        {
            _listenersWithFilters = listenersWithFilters;
        }

        public void AddInstancingOrder(
            VegetationDetailLevel level,
            List<VegetationSubjectEntity> gainedEntities,
            List<VegetationSubjectEntity> lostEntities)
        {
            var gainedLists = new List<List<VegetationSubjectEntity>>();
            var lostLists = new List<List<VegetationSubjectEntity>>();
            for (int i = 0; i < _listenersWithFilters.Count; i++)
            {
                gainedLists.Add(new List<VegetationSubjectEntity>());
                lostLists.Add(new List<VegetationSubjectEntity>());
            }

            foreach (var entity in gainedEntities)
            {
                int i = 0;
                bool foundListener = false;
                foreach (var listenerWithFilter in _listenersWithFilters)
                {
                    if (listenerWithFilter.Filter(entity))
                    {
                        gainedLists[i].Add(entity);
                        foundListener = true;
                        break;
                    }
                    i++;
                }
                if (!foundListener)
                {
                    Debug.LogError("E41. No listener accepted entity " + entity);
                }
            }

            foreach (var entity in lostEntities)
            {
                int i = 0;
                bool foundListener = false;
                foreach (var listenerWithFilter in _listenersWithFilters)
                {
                    if (listenerWithFilter.Filter(entity))
                    {
                        lostLists[i].Add(entity);
                        foundListener = true;
                        break;
                    }
                    i++;
                }
                if (!foundListener)
                {
                    Debug.LogError("E42. No listener accepted entity " + entity);
                }
            }

            int k = 0;
            foreach (var listener in _listenersWithFilters.Select(c => c.ChangeListener))
            {
                var gained = gainedLists[k];
                var lost = lostLists[k];
                if (gained.Any() || lost.Any())
                {
                    listener.AddInstancingOrder(level, gained, lost);
                }
                k++;
            }
        }
    }
}