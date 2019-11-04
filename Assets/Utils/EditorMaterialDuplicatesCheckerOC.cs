using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Utils
{
    [ExecuteInEditMode]
    public class EditorMaterialDuplicatesCheckerOC : MonoBehaviour
    {
        private HashSet<Material> _materials = new HashSet<Material>();

        public void Start()
        {
            _materials.Clear();              
        }

        public void Register(Material material)
        {
            Preconditions.Assert(Application.isEditor,"This runs only in editor!");
            Preconditions.Assert(!IsMaterialRegistered(material), "Material is arleady registered");
            _materials.Add(material);
        }

        public void UnRegister(Material material)
        {
            Preconditions.Assert(Application.isEditor,"This runs only in editor!");
            Preconditions.Assert(IsMaterialRegistered(material), "Material is not registered");
            _materials.Remove(material);
        }

        public bool IsMaterialRegistered(Material material)
        {
            Preconditions.Assert(Application.isEditor,"This runs only in editor!");
            return _materials.Contains(material);
        }
    }
}
