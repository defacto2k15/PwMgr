using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.AI;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Assets.AITesting
{
    public class AITestScenarioLoaderGO : MonoBehaviour
    {
        public String TestName;
        public Object TestPrefab;

        public void Start()
        {
            //var testPrefab = Resources.Load("Assets/Prefabs/AITesting/"+TestName);
            //var testPrefab = Resources.Load("Assets/Prefabs/AITesting/NavigateToOnePoint");
            GameObject.Instantiate(TestPrefab, Vector3.zero, Quaternion.identity);
        }

    }

}
