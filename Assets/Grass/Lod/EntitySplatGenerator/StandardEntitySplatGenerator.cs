using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.Grass;
using Assets.Grass.Container;
using Assets.Grass.Generating;
using Assets.Grass.Instancing;
using Assets.Grass.Lod;
using Assets.ShaderUtils;
using Assets.Utils;
using UnityEngine;

abstract class StandardEntitySplatGenerator : IEntitySplatGenerator
{
    private readonly IEntityGenerator _entitiesGenerator;
    private readonly IEntityPositionProvider _positionProvider;
    private readonly AbstractSettingGenerator _settingGenerator;
    private readonly Material _grassMaterial;
    private readonly IEntitiesCountProvider _entitiesCountProvider;
    private readonly IMeshProvider _meshProvider;

    public StandardEntitySplatGenerator(IEntityGenerator entitiesGenerator, IEntityPositionProvider positionProvider,
        AbstractSettingGenerator settingGenerator, Material grassMaterial,
        IEntitiesCountProvider entitiesCountProvider, IMeshProvider meshProvider)
    {
        _entitiesGenerator = entitiesGenerator;
        _positionProvider = positionProvider;
        _settingGenerator = settingGenerator;
        _grassMaterial = grassMaterial;
        _entitiesCountProvider = entitiesCountProvider;
        _meshProvider = meshProvider;
    }

    public IEntitySplat GenerateSplat(MapAreaPosition position, int entityLodLevel)
    {
        List<GrassEntity> outEntities = new List<GrassEntity>();
        for (int i = 0; i < _entitiesCountProvider.GetCount(); i++)
        {
            GrassEntitiesSet grassEntities = _entitiesGenerator.Generate();
            _positionProvider.SetPosition(grassEntities, position);
            _settingGenerator.SetSettings(grassEntities);

            Vector4 color = new Vector4(1, 1, 1, 1);
            if (entityLodLevel == 0)
            {
                color = new Vector4(1, 0, 0, 1);
            }
            else if (entityLodLevel == 1)
            {
                color = new Vector4(1, 1, 0, 1);
            }
            else if (entityLodLevel == 2)
            {
                color = new Vector4(0, 1, 0, 1);
            }
            else if (entityLodLevel == 3)
            {
                color = new Vector4(0, 1, 1, 1);
            }
            else if (entityLodLevel == 4)
            {
                color = new Vector4(0, 0, 1, 1);
            }
            else if (entityLodLevel == 5)
            {
                color = new Vector4(1, 0, 1, 1);
            }
            else if (entityLodLevel == 6)
            {
                color = new Vector4(0.5f, 0.5f, 0, 1);
            }
            else if (entityLodLevel == 7)
            {
                color = new Vector4(0.5f, 0, 0, 1);
            }
            grassEntities.EntitiesBeforeTransform.ForEach(c => c.AddUniform(ShaderUniformName._Color, color));
            // todo repair it

            outEntities.AddRange(grassEntities.EntitiesAfterTransform);
        }
        return CreateSplat(outEntities, _grassMaterial, _meshProvider.GetMesh(entityLodLevel));
    }

    protected abstract IEntitySplat CreateSplat(List<GrassEntity> outEntities, Material grassMaterial, Mesh getMesh);
}

class GpuInstancingEntitySplatGenerator : StandardEntitySplatGenerator
{
    private readonly GpuInstancingGrassInstanceContainer _gpuGrassInstanceContainer;
    private readonly GpuInstancingGrassInstanceGenerator _grassInstanceGenerator;

    public GpuInstancingEntitySplatGenerator(IEntityGenerator entitiesGenerator,
        IEntityPositionProvider positionProvider, AbstractSettingGenerator settingGenerator,
        Material grassMaterial, IEntitiesCountProvider entitiesCountProvider,
        IMeshProvider meshProvider, GpuInstancingGrassInstanceContainer gpuGrassInstanceContainer,
        GpuInstancingGrassInstanceGenerator grassInstanceGenerator)
        : base(entitiesGenerator, positionProvider, settingGenerator, grassMaterial, entitiesCountProvider,
            meshProvider)
    {
        _gpuGrassInstanceContainer = gpuGrassInstanceContainer;
        _grassInstanceGenerator = grassInstanceGenerator;
    }

    protected override IEntitySplat CreateSplat(List<GrassEntity> outEntities, Material grassMaterial, Mesh getMesh)
    {
        return _grassInstanceGenerator.GenerateEntitySplats(
            new GrassEntitiesWithMaterials(outEntities, grassMaterial, getMesh, ContainerType.Instancing),
            _gpuGrassInstanceContainer);
    }
}

class GameObjectEntitySplatGenerator : StandardEntitySplatGenerator
{
    private readonly GameObjectGrassInstanceContainer _gameObjectContainer;

    public GameObjectEntitySplatGenerator(IEntityGenerator entitiesGenerator, IEntityPositionProvider positionProvider,
        AbstractSettingGenerator settingGenerator,
        Material grassMaterial, IEntitiesCountProvider entitiesCountProvider, IMeshProvider meshProvider,
        GameObjectGrassInstanceContainer gameObjectContainer)
        : base(entitiesGenerator, positionProvider, settingGenerator, grassMaterial, entitiesCountProvider,
            meshProvider)
    {
        _gameObjectContainer = gameObjectContainer;
    }

    protected override IEntitySplat CreateSplat(List<GrassEntity> outEntities, Material grassMaterial, Mesh getMesh)
    {
        return _gameObjectContainer.AddGrassEntities(
            new GrassEntitiesWithMaterials(outEntities, grassMaterial, getMesh, ContainerType.GameObject));
    }
}