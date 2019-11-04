using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.FinalExecution;
using Assets.Habitat;
using Assets.PreComputation.Configurations;
using Assets.Roads.Pathfinding;
using UnityEngine;

namespace Assets.PreComputation
{
    public class HabitatMapDbPrecomputation
    {
        private GameInitializationFields _gameInitializationFields;
        private PrecomputationConfiguration _rootConfiguration;
        private HabitatMapDbPrecomputationConfiguration _habitatConfiguration;
        private FilePathsConfiguration _filesConfiguration;

        private HabitatMap _habitatMap;

        public HabitatMapDbPrecomputation(GameInitializationFields gameInitializationFields,
            PrecomputationConfiguration rootConfiguration, FilePathsConfiguration filesConfiguration)
        {
            _gameInitializationFields = gameInitializationFields;
            _rootConfiguration = rootConfiguration;
            _filesConfiguration = filesConfiguration;
            _habitatConfiguration = new HabitatMapDbPrecomputationConfiguration(rootConfiguration);
        }

        public void Compute()
        {
            var loader = new HabitatMapOsmLoader();
            var fields = loader.Load(_filesConfiguration.OsmFilePath);

            var translator = new HabitatFieldPositionTranslator(_rootConfiguration.GeoCoordsToUnityTranslator);
            fields = fields.Select(c => translator.Translate(c)).ToList();

            var map = HabitatMap.Create(
                _habitatConfiguration.AreaOnMap,
                _habitatConfiguration.MapGridSize,
                fields,
                _habitatConfiguration.DefaultHabitatType,
                _habitatConfiguration.HabitatTypePriorityResolver);

            _habitatMap = map;

            var fileManager = new HabitatMapFileManager();
            fileManager.SaveHabitatMap(_filesConfiguration.HabitatDbFilePath, map);
        }

        public void Load()
        {
            if (_habitatMap == null)
            {
                var fileManager = new HabitatMapFileManager();
                _habitatMap = fileManager.LoadHabitatMap(_filesConfiguration.HabitatDbFilePath);
            }
            var proxy = new HabitatMapDbProxy(new HabitatMapDb(new HabitatMapDb.HabitatMapDbInitializationInfo()
            {
                Map = _habitatMap
            }));
            _gameInitializationFields.SetField(proxy);
        }
    }
}