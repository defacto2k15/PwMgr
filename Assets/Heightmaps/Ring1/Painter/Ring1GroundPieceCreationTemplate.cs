using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Ring2.BaseEntities;
using Assets.ShaderUtils;
using Assets.Utils;
using UnityEngine;

namespace Assets.Heightmaps.Ring1.Painter
{
    public class Ring1GroundPieceCreationTemplate
    {
        public Mesh PieceMesh;
        public MyTransformTriplet TransformTriplet;
        public UniformsPack Uniforms;
        public GameObject ParentGameObject;
        public string Name;
        public string ShaderName;
        public ShaderKeywordSet ShaderKeywordSet = new ShaderKeywordSet(new List<string>());
        public Ring1GroundPieceModifier Modifier;
    }

    public class Ring1GroundPieceModifier
    {
        private List<Material> _materials = new List<Material>();
        private Queue<Action<Material>> _dueActions = new Queue<Action<Material>>(); //todo cannot be like this. It grows and grows
        private object _lock = new object();

        public void SetMaterial(Material material)
        {
            lock (_lock)
            {
                _materials.Add(material);
                foreach (var action in _dueActions)
                {
                    action(material);
                }
            }
        }

        public void ModifyMaterial(Action<Material> action)
        {
            lock (_lock)
            {
                _dueActions.Enqueue(action);
                foreach (var material in _materials)
                {
                    action(material);
                }
            }
        }
    }
}