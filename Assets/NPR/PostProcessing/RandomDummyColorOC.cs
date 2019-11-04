using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.Random;
using UnityEngine;

namespace Assets.NPR.PostProcessing
{
    public class RandomDummyColorOC : MonoBehaviour
    {
        public void Start()
        {
            var random = new System.Random(GetInstanceID());
            GetComponent<Renderer>().material.SetVector("_DummyColor", random.NextVector3());
            GetComponent<Renderer>().material.SetInt("_ObjectID", Math.Abs(GetInstanceID()));
        }
    }
}
