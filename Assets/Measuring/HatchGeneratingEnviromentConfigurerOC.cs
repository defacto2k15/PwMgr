using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Assets.NPR.Filling;
using Assets.NPR.Filling.Breslav;
using Assets.Utils;
using UnityEngine;

namespace Assets.Measuring
{
    public class HatchGeneratingEnviromentConfigurerOC : MonoBehaviour
    {
        public List<HatchModeConfiguration> PerModeConfigurations;

        public INprRenderingPostProcessingDirector ConfigureEnviroment(HatchGeneratingMode mode, GameObject cameraObject, GameObject hatchedObject)
        {
            Preconditions.Assert(PerModeConfigurations.Any(c => c.Mode == mode), "There is no configuration for mode "+mode);
            var conf = PerModeConfigurations.First(c => c.Mode == mode);

            hatchedObject.GetComponent<MeshRenderer>().material = conf.ObjectMaterial;
            if (conf.SourceGameObject != null)
            {
                foreach (var sourceComponent in conf.SourceGameObject.GetComponents<MonoBehaviour>())
                {
                    var newComponent = hatchedObject.AddComponent(sourceComponent.GetType());
                    ComponentUtils.CopyClassValues(sourceComponent, newComponent);
                }
            }


            INprRenderingPostProcessingDirector ppDirector = null;
            if (conf.SourceCameraObject!= null)
            {
                foreach (var sourceComponent in conf.SourceCameraObject.GetComponents<MonoBehaviour>())
                {
                    var newComponent = cameraObject.AddComponent(sourceComponent.GetType());
                    ComponentUtils.CopyClassValues(sourceComponent, newComponent);
                    if (newComponent is INprRenderingPostProcessingDirector)
                    {
                        ppDirector = (INprRenderingPostProcessingDirector) newComponent;
                    }
                }
            }

            return ppDirector;
        }

    }

    public static class ComponentUtils
    {
        public static void CopyClassValues<T>(T sourceComp, T targetComp)
        {
            FieldInfo[] sourceFields = sourceComp.GetType().GetFields(BindingFlags.Public |
                                                                      BindingFlags.NonPublic |
                                                                      BindingFlags.Instance);
            int i = 0;
            for (i = 0; i < sourceFields.Length; i++)
            {
                var value = sourceFields[i].GetValue(sourceComp);
                sourceFields[i].SetValue(targetComp, value);
            }
        }

    }

    [Serializable]
    public class HatchModeConfiguration
    {
        public HatchGeneratingMode Mode;
        public Material ObjectMaterial;
        public GameObject SourceGameObject;
        public GameObject SourceCameraObject;
    }
}