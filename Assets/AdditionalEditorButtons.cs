#if UNITY_EDITOR
using Assets.ETerrain.ETerrainIntegration;
using Assets.ETerrain.Pyramid.Shape;
using Assets.ETerrain.Tools.HeightPyramidExplorer;
using Assets.FinalExecution;
using Assets.Heightmaps;
using Assets.Heightmaps.Ring1.Erosion;
using Assets.Roads.Pathfinding;
using Assets.Trees.RuntimeManagement;
using Assets.ShaderUtils;
using Assets.Trees;
using UnityEditor;
using UnityEngine;

namespace Assets.Editor
{
    [CustomEditor(typeof(TerrainLoaderGameObject))]
    class AdditionalEditorButtons : UnityEditor.Editor
    {
        private float _seedValue;
        private float _heightMultiplier;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var terrainLoader = (TerrainLoaderGameObject) target;

            _seedValue = EditorGUILayout.Slider("Seed value",_seedValue, 0.0f, 10.0f,  new GUILayoutOption[]{} );
            _heightMultiplier = EditorGUILayout.Slider("_heightMultiplier",_heightMultiplier, 0.0f, 10.0f,  new GUILayoutOption[]{} );

            var uniformsPack = new UniformsPack();
            uniformsPack.SetUniform("_Seed", _seedValue);
            uniformsPack.SetUniform("_HeightMultiplier", _heightMultiplier);

            if (GUILayout.Button("RegenerateTextureObject"))
            {
                terrainLoader.Ring1Tree.RegenerateTextureShowingObject(uniformsPack);
            }

            if (GUILayout.Button("RegenerateTerrainObject"))
            {
                terrainLoader.Ring1Tree.RegenerateTerrainShowingObject(uniformsPack);
            }
            if (GUILayout.Button("UpdateLod"))
            {
                terrainLoader.UpdateHeightmap();
            }
        }
    }

    [CustomEditor(typeof(TreePlacerDebug))]
    class Editorbuttons_TreePlacerDebug : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var debugObject = (TreePlacerDebug) target;

            if (GUILayout.Button("Recreate"))
            {
                debugObject.Recalculate();
            }
        }
    }

    [CustomEditor(typeof(EroderDebugObject))]
    class Editorbuttons_EroderDebugObject : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var debugObject = (EroderDebugObject) target;

            if (GUILayout.Button("Erode"))
            {
                debugObject.ErodeOneStep();
            }
        }
    }

    [CustomEditor(typeof(ShaderMeiErosionDebugObject))]
    class ShaderMeiErosionDebugObject_EroderDebugObject : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var debugObject = (ShaderMeiErosionDebugObject) target;

            if (GUILayout.Button("Erode"))
            {
                debugObject.Mei_RenderComputeShader();
            }

            if (GUILayout.Button("Step+"))
            {
                debugObject.RecreateWithStepChange(1);
            }

            if (GUILayout.Button("Step-"))
            {
                debugObject.RecreateWithStepChange(-1);
            }
        }
    }

    [CustomEditor(typeof(TerrainRoadPathfidingDebugObject))]
    class EditorButtons_TerrainRoadPathfindingDebugObject : UnityEditor.Editor
    {
        private bool toggleValue = false;
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var debugObject = (TerrainRoadPathfidingDebugObject) target;

            if (GUILayout.Button("Step") ||  GUILayout.RepeatButton("ManyStep") )
            {
                debugObject.Step();
            }
            if (GUILayout.Button("SolveWhole"))
            {
                debugObject.SolvePath();
            }
            toggleValue = GUILayout.Toggle(toggleValue, "Multi");
            if (toggleValue)
            {
                debugObject.Step();
            }
            if (GUILayout.Button("Reset"))
            {
                debugObject.ResetPath();
            }
        }
    }

    [CustomEditor(typeof(GeneralPathCreatorDebugObject))]
    class Editorbuttons_GeneralPathfinderDebugObject : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var debugObject = (GeneralPathCreatorDebugObject) target;

            if (GUILayout.Button("Recreate"))
            {
                debugObject.RecreateTerrain();
            }
        }
    }

    [CustomEditor(typeof(FE1DebugObject))]
    class Editorbuttons_FE1DebugObject : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var debugObject = (FE1DebugObject) target;

            if (GUILayout.Button("MoveCamera"))
            {
                debugObject.DebugMoveCamera();
            }
        }
    }

    [CustomEditor(typeof(HeightPyramidExplorerDEO))]
    class Editorbuttons_HeightPyramidExplorerDEO : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var debugObject = (HeightPyramidExplorerDEO) target;

            if (GUILayout.Button("FillTerrainWithNoise"))
            {
                debugObject.FillTerrainWithNoise();
            }
            if (GUILayout.Button("AddNewSegment"))
            {
                debugObject.AddNewSegment();
            }
        }
    }

    [CustomEditor(typeof(VegetationRuntimeManagementWithSpotDebugObject ))]
    class Editorbuttons_VegetationRuntimeManagementWithSpotDebugObject : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var debugObject = (VegetationRuntimeManagementWithSpotDebugObject ) target;

            if (GUILayout.Button("RecalculateHeight"))
            {
                debugObject.RecalculateHeight();
            }
        }
    }
}
#endif