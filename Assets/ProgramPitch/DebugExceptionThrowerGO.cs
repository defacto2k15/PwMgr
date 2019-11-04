using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.ProgramPitch
{
    public class DebugExceptionThrowerGO : MonoBehaviour
    {
        public void Start()
        {
            Debug.LogError("E632 ERROR LOG");
            throw new ApplicationException("E633 APP EXCEPTION");
        }
    }
}
