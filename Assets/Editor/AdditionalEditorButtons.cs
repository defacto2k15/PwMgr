
using Assets.ActorSystem.Registry;
using Assets.ESurface;
using Assets.ETerrain.ETerrainIntegration;
using Assets.ETerrain.Pyramid.Shape;
using Assets.ETerrain.Tools.HeightPyramidExplorer;
using Assets.FinalExecution;
using Assets.Heightmaps;
using Assets.Heightmaps.Ring1.Erosion;
using Assets.NPR;
using Assets.NPR.DataBuffers;
using Assets.NPR.Lines;
using Assets.NPR.PostProcessing;
using Assets.NPR.PostProcessing.PPShaderCustomization;
using Assets.NPR.ShaderCustomizer;
using Assets.Roads.Pathfinding;
using Assets.ShaderUtils;
using Assets.Trees;
using Assets.Utils;
using Assets.Utils.Editor;
using Assets.Utils.ShaderBuffers;
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

    //[CustomEditor(typeof(ETerrainIntegrationFakeHeightInputDEO))]
    //class Editorbuttons_ETerrainIntegrationFakeHeightInputDEO : UnityEditor.Editor
    //{
    //    public override void OnInspectorGUI()
    //    {
    //        DrawDefaultInspector();
    //        var debugObject = (ETerrainIntegrationFakeHeightInputDEO) target;

    //        if (GUILayout.Button("AddQueuedSegment"))
    //        {
    //            debugObject.AddQueuedSegment();
    //        }
    //    }
    //}

    [CustomEditor(typeof(NPRFilterRenderer))]
    class Editorbuttons_NPRFilterRenderer : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var debugObject = (NPRFilterRenderer) target;

            if (GUILayout.Button("RecreateKernelTexture"))
            {
                debugObject.ResetKernelTexture();
            }
        }
    }

    [CustomEditor(typeof(NPRAdjacencyBufferDebugGO))]
    class Editorbuttons_NPRAdjacencyBufferGeneratorComponent : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var debugObject = (NPRAdjacencyBufferDebugGO) target;

            if (GUILayout.Button("Reset"))
            {
                debugObject.Reset();
            }
        }
    }

    [CustomEditor(typeof(ShaderBufferInjectOC))]
    class Editorbuttons_NPRAdjacencyBufferInjectOC : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var debugObject = (ShaderBufferInjectOC) target;

            if (GUILayout.Button("RecreateBuffer"))
            {
                debugObject.RecreateBuffer();
            }
        }
    }

    [CustomEditor(typeof(CachedShaderBufferInjectOC))]
    class Editorbuttons_CachedShaderBufferInjectOC: UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var debugObject = (CachedShaderBufferInjectOC) target;

            if (GUILayout.Button("RecreateBuffer"))
            {
                debugObject.RecreateBuffer();
            }
        }
    }

    [CustomEditor(typeof(MasterCachedShaderBufferInjectOC))]
    class Editorbuttons_MasterCachedShaderBufferInject: UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var debugObject = (MasterCachedShaderBufferInjectOC) target;

            if (GUILayout.Button("RecreateBuffer"))
            {
                debugObject.RecreateBuffer();
            }
        }
    }

    [CustomEditor(typeof(NPRObjectShaderCustomizerOC ))]
    class Editorbuttons_NPRShaderCustomizerOC:  UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var debugObject = (NPRObjectShaderCustomizerOC ) target;

            if (GUILayout.Button("RecreateShader"))
            {
                debugObject.RecreateShader();
            }
            if (GUILayout.Button("ReturnToPattern"))
            {
                debugObject.ReturnToPatternShader();
            }
        }
    }

    [CustomEditor(typeof(NprPPShaderCustomizationInjectorOC ))]
    class Editorbuttons_NprPPShaderCustomizationInjectorOC:  UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var debugObject = (NprPPShaderCustomizationInjectorOC ) target;

            if (GUILayout.Button("RecreateShader"))
            {
                debugObject.RecreateShader();
            }
            if (GUILayout.Button("ReturnToPattern"))
            {
                debugObject.ReturnToPatternShader();
            }
        }
    }

    [CustomEditor(typeof(NPRMasterShaderBufferAssetGeneratorOC))]
    class Editorbuttons_NPRMasterShaderBufferAssetGeneratorOC:  UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var debugObject = (NPRMasterShaderBufferAssetGeneratorOC) target;

            if (GUILayout.Button("Regenerate"))
            {
                debugObject.Regenerate();
            }
        }
    }

    [CustomEditor(typeof(EditorUpdate2GO))]
    class Editorbuttons_EditorUpdate2GO:  UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var debugObject = (EditorUpdate2GO) target;

            if (GUILayout.Button("ClearAllOrders"))
            {
                debugObject.ClearAllOrders();
            }
        }
    }

    [CustomEditor(typeof(DebugIlluminationRidgeValleyMatrixCalculatorGO))]
    class Editorbuttons_DebugIlluminationRidgeValleyMatrixCalculatorGO:  UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var debugObject = (DebugIlluminationRidgeValleyMatrixCalculatorGO) target;

            if (GUILayout.Button("Calculate"))
            {
                debugObject.Calculate();
            }
        }
    }

    [CustomEditor(typeof(AsTelegramRegistryGO))]
    class Editorbuttons_AsTelegramRegistryGO:  UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var debugObject = (AsTelegramRegistryGO) target;

            if (GUILayout.Button("DebugFillRegistry"))
            {
                debugObject.DebugFillRegistry();
            }
        }
    }

    [CustomEditor(typeof(BufferReloaderRootGO))]
    class Editorbuttons_BufferReloaderRootGO:  UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var debugObject = (BufferReloaderRootGO) target;

            if (GUILayout.Button("UpdateBuffers"))
            {
                debugObject.UpdateBuffers();
            }
        }
    }

    [CustomEditor(typeof(ESurfaceSinglePatchDebugGO))]
    class Editorbuttons_ESurfaceSinglePatchDebugGO:  UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var debugObject = (ESurfaceSinglePatchDebugGO) target;

            if (GUILayout.Button("RegeneratePatch"))
            {
                debugObject.RegeneratePatch();
            }
        }
    }
}