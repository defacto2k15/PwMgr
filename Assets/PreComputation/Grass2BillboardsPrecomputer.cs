using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.FinalExecution;
using Assets.Grass2.Billboards;
using Assets.Heightmaps.Ring1.RenderingTex;
using Assets.Heightmaps.Ring1.TerrainDescription.FeatureGenerating;
using Assets.PreComputation.Configurations;
using Assets.Utils.TextureRendering;
using UnityEngine;

namespace Assets.PreComputation
{
    public class Grass2BillboardsPrecomputer
    {
        private Grass2BillboardsPrecomputerConfiguration _billboardsConfuguration;
        private FilePathsConfiguration _pathsConfiguration;
        private GameInitializationFields _gameInitializationFields;

        public Grass2BillboardsPrecomputer(GameInitializationFields gameInitializationFields,
            FilePathsConfiguration pathsConfiguration)
        {
            _gameInitializationFields = gameInitializationFields;
            _pathsConfiguration = pathsConfiguration;
            _billboardsConfuguration = new Grass2BillboardsPrecomputerConfiguration();
        }

        public void Compute()
        {
            var generator = new Grass2BillboardGenerator(
                new UTTextureRendererProxy(new TextureRendererService(
                    new MultistepTextureRenderer(_gameInitializationFields.Retrive<ComputeShaderContainerGameObject>()),
                    new TextureRendererServiceConfiguration()
                    {
                        StepSize = new Vector2(10, 10)
                    })), _billboardsConfuguration.BillboardGeneratorConfiguration);

            var clansGenerator =
                new Grass2BillboardClanGenerator(generator, _billboardsConfuguration.ClanGeneratorConfiguration);
            var clan = clansGenerator.Generate();

            var fileManager = new Grass2BillboardClanFilesManager();
            fileManager.Save(_pathsConfiguration.Grass2BillboardsPath, clan);
        }
    }
}