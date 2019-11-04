using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.Utils;
using UnityEngine;

namespace Assets.PerspectiveScaling
{
    public class PerspectiveScallingPropsGeneratorGO : MonoBehaviour
    {
        public float OuterGenerationCubeLength;
        public float InnerGenerationCubeLength;
        public int PropsToGenerateCount;

        public void Start()
        {
            UnityEngine.Random.InitState(123);
            var rootGO = new GameObject("RootPropGO");

            var maxTries = 100;
            for (int i = 0; i < PropsToGenerateCount; i++)
            {
                var propPos = Vector3.zero;
                bool goodPositionFound = false;
                int tryIndex = 0;
                while (!goodPositionFound && tryIndex < maxTries)
                {
                    tryIndex++;
                    propPos = new Vector3(
                        UnityEngine.Random.Range(-OuterGenerationCubeLength, OuterGenerationCubeLength),
                        UnityEngine.Random.Range(-OuterGenerationCubeLength, OuterGenerationCubeLength),
                        UnityEngine.Random.Range(-OuterGenerationCubeLength, OuterGenerationCubeLength)
                    );

                    if (!propPos.ToArray().All(c => c < InnerGenerationCubeLength && c > -InnerGenerationCubeLength))
                    {
                        goodPositionFound = true;
                    }
                }

                if (!goodPositionFound)
                {
                    continue;
                }


                var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                go.GetComponent<MeshRenderer>().material.color = UnityEngine.Random.ColorHSV();
                go.transform.position = propPos;
                go.transform.SetParent(rootGO.transform);
                go.name = "Prop " + i;

                var distanceToCenter = propPos.magnitude;
                var scale = distanceToCenter / 30;
                go.transform.localScale = new Vector3(scale,scale,scale);
            }
        }
    }
}
