using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Assets.ProgramPitch
{
    public class MainMenuScriptOC : MonoBehaviour
    {
        public void ConsistencyTest()
        {
            SceneManager.LoadScene("m-consistency");

        }

        public void PerformanceTest()
        {
            SceneManager.LoadScene("m-performance");
        }

        public void Breslav()
        {
            SceneManager.LoadScene("mf-Breslav");
        }

        public void Jordane()
        {
            SceneManager.LoadScene("mf-Jordane");
        }

        public void Szecsi()
        {
            SceneManager.LoadScene("mf-Szecsi");
        }

        public void ShowerDoor()
        {
            SceneManager.LoadScene("mf-ShowerDoor");
        }

        public void Wolowski()
        {
            SceneManager.LoadScene("mf-Wolowski");
        }

        public void Tam()
        {
            SceneManager.LoadScene("mf-tam");
        }

        public void TamIss()
        {
            SceneManager.LoadScene("mf-tamiss");
        }

        public void MMStandard()
        {
            SceneManager.LoadScene("mf-mmStandard");
        }

        public void MMGeometric()
        {
            SceneManager.LoadScene("mf-mmGeometric");
        }
    }
}
