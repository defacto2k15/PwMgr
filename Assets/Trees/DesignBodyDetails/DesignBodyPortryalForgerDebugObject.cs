using Assets.Trees.DesignBodyDetails.BucketsContainer;
using Assets.Trees.DesignBodyDetails.DesignBodyCharacteristics;
using Assets.Trees.DesignBodyDetails.DetailProvider;
using Assets.Trees.Generation;
using Assets.Trees.RuntimeManagement;
using Assets.Trees.RuntimeManagement.SubjectsInstancesContainer.GpuInstancing.Global;
using Assets.Utils.MT;
using UnityEngine;

namespace Assets.Trees.DesignBodyDetails
{
    public class DesignBodyPortryalForgerDebugObject : MonoBehaviour
    {
        private GlobalGpuInstancingContainer _instancingContainer;

        public void Start()
        {
            TaskUtils.SetGlobalMultithreading(false);
            var quadBillboardMesh = GameObject.CreatePrimitive(PrimitiveType.Quad).GetComponent<MeshFilter>().mesh;
            _instancingContainer = new GlobalGpuInstancingContainer();

            var representationContainer = new DesignBodyRepresentationContainer();
            DesignBodyInstanceBucketsContainer instanceBucketsContainer =
                new DesignBodyInstanceBucketsContainer(_instancingContainer);
            var shifter = new TreeClanToDetailProviderShifter(new DetailProviderRepository(), quadBillboardMesh,
                representationContainer, instanceBucketsContainer);

            TreePrefabManager prefabManager = new TreePrefabManager();
            TreeClan clan = prefabManager.LoadTreeClan("clan1");

            shifter.AddClan(clan, VegetationSpeciesEnum.Tree1A);
            _instancingContainer.StartThread();

            //var forger = new DesignBodyPortrayalForger(representationContainer, instanceBucketsContainer);
            var forger = new SpecificTreeForger(instanceBucketsContainer, representationContainer);

            for (int x = 0; x < 900; x += 10)
            {
                for (int y = 0; y < 900; y += 10)
                {
                    forger.Forge( new DesignBodyLevel1DetailWithSpotModification(){Level1Detail = new DesignBodyLevel1Detail()
                    {
                        DetailLevel = VegetationDetailLevel.BILLBOARD,
                        Pos2D = new Vector2(x, y),
                        Radius = 3,
                        Seed = x * 1000 + y,
                        SpeciesEnum = VegetationSpeciesEnum.Tree1A,
                        Size = 1
                    }, SpotModification = null});

                //    forger.Forge(new DesignBodyLevel1Detail()
                //    {
                //        DetailLevel = VegetationDetailLevel.FULL,
                //        Pos2D = new Vector2(x, y),
                //        Radius = 3,
                //        Seed = x * 1000 + y,
                //        SpeciesEnum = VegetationSpeciesEnum.Tree1A,
                //        Size = 1
                //    });

                //    forger.Forge(new DesignBodyLevel1Detail()
                //    {
                //        DetailLevel = VegetationDetailLevel.REDUCED,
                //        Pos2D = new Vector2(x, y),
                //        Radius = 3,
                //        Seed = x * 1000 + y,
                //        SpeciesEnum = VegetationSpeciesEnum.Tree1A,
                //        Size = 1
                //    });
                }
            }

            //_instancingContainer.FinishUpdateBatch();
        }

        public void Update()
        {
            //_instancingContainer.DrawFrame();
        }
    }
}