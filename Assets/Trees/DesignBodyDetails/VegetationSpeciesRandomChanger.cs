using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.Random;
using Assets.Trees.RuntimeManagement;

namespace Assets.Trees.DesignBodyDetails
{
    public class VegetationSpeciesRandomChanger
    {
        private Dictionary<VegetationSpeciesEnum, List<VegetationSpeciesEnum>> _changingLists;
        private int _randomSeed;

        public VegetationSpeciesRandomChanger(Dictionary<VegetationSpeciesEnum,
            List<VegetationSpeciesEnum>> changingLists, int randomSeed)
        {
            _changingLists = changingLists;
            _randomSeed = randomSeed;
        }

        public VegetationSubjectEntity ChangeSpecies(VegetationSubjectEntity inputEntity)
        {
            var type = inputEntity.Detail.SpeciesEnum;
            if (_changingLists.ContainsKey(type))
            {
                var posibilitiesList = _changingLists[type];

                var randomProvider = new RandomProvider(inputEntity.Id + _randomSeed);
                var speciesIndex = randomProvider.NextWithMax(0, posibilitiesList.Count - 1);
                var newType = posibilitiesList[speciesIndex];
                inputEntity.Detail.SpeciesEnum = newType;
            }
            return inputEntity;
        }
    }
}