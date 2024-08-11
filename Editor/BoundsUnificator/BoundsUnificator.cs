/*
 *  The MIT License
 *
 *  Copyright 2020-2024 whiteflare.
 *
 *  Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"),
 *  to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense,
 *  and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 *
 *  The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 *
 *  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 *  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
 *  IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
 *  TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace WF.Tool.Avatar.BU
{
    internal class BoundsUnificator : EditorWindow
    {
        [MenuItem("Tools/whiteflare/Bounds Unificator", priority = 16)]
        public static void Menu_BoundsUniticator()
        {
            BoundsUnificator.ShowWindow();
        }

        private const string Title = "Bounds Unificator";

        public GameObject rootObject;
        public BoundsCalcMode calcMode = BoundsCalcMode.SkinnedVertex;
        public bool showWireFrame = true;

        private Vector2 scroll = Vector2.zero;
        private BoundsRecalculator boundsRecalculator;
        private bool repaintHandle = false;

        public static void ShowWindow()
        {
            var window = EditorWindow.GetWindow<BoundsUnificator>(Title);
            window.SetSelection(Selection.GetFiltered(typeof(GameObject), SelectionMode.Editable | SelectionMode.ExcludePrefab));
        }

        private void SetSelection(Object[] objects)
        {
            foreach (var obj in objects)
            {
                if (obj is GameObject)
                {
                    rootObject = (GameObject)obj;
                    boundsRecalculator.SetAvatarRoot(rootObject);
                    repaintHandle = true;
                    break;
                }
            }
        }

        private SerializedObject serializedObject;

        private void OnEnable()
        {
            repaintHandle = false;
            rootObject = null;
            boundsRecalculator = CreateInstance<BoundsRecalculator>();
            serializedObject = new SerializedObject(boundsRecalculator);
            SceneView.duringSceneGui += OnSceneViewGUI;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneViewGUI;
        }

        private void OnSceneViewGUI(SceneView sceneView)
        {
            if (boundsRecalculator.rootBone == null)
            {
                return;
            }
            if (showWireFrame)
            {
                using (new Handles.DrawingScope(boundsRecalculator.rootBone.localToWorldMatrix))
                {
                    Handles.DrawWireCube(boundsRecalculator.bounds.center, boundsRecalculator.bounds.size);
                }
            }
        }

        private void OnGUI()
        {
            serializedObject.Update();
            var oldColor = GUI.color;

            // スクロール開始
            scroll = EditorGUILayout.BeginScrollView(scroll);

            var oldRootObject = rootObject;
            EditorGUI.BeginChangeCheck();
            {
                rootObject = (GameObject)EditorGUILayout.ObjectField(new GUIContent("Root Object"), oldRootObject, typeof(GameObject), true);
                EditorGUILayout.Space();
            }
            if (EditorGUI.EndChangeCheck() && rootObject != oldRootObject)
            {
                boundsRecalculator.SetAvatarRoot(rootObject);
                repaintHandle = true;
            }

            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(boundsRecalculator.skinMeshRenderers)), new GUIContent("SkinnedMeshRenderer (" + boundsRecalculator.skinMeshRenderers.Count + ")"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(boundsRecalculator.meshRenderers)), new GUIContent("MeshRenderer (" + boundsRecalculator.meshRenderers.Count + ")"), true);
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(boundsRecalculator.rootBone)), new GUIContent("RootBone (Hip)", "RootBoneに設定するTransform"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(boundsRecalculator.anchorTarget)), new GUIContent("AnchorOverride", "AnchorOverrideに設定するTransform"));
            EditorGUILayout.Space();

            calcMode = (BoundsCalcMode) EditorGUILayout.EnumPopup(new GUIContent("Calc Method", "計算方法の指定"), calcMode);

            using (new EditorGUI.DisabledGroupScope(rootObject == null || boundsRecalculator.rootBone == null || boundsRecalculator.skinMeshRenderers.Count == 0))
            {
                if (GUILayout.Button("Calculate Bounds"))
                {
                    serializedObject.ApplyModifiedProperties();
                    boundsRecalculator.CalcBounds(calcMode);
                    repaintHandle = true;
                }
            }

            EditorGUILayout.Space();

            EditorGUI.BeginChangeCheck();
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(boundsRecalculator.bounds)), new GUIContent("Bounds"));
                showWireFrame = EditorGUILayout.ToggleLeft(new GUIContent("show wire frame", "Boundsの値をワイヤーフレームで表示する"), showWireFrame);
            }
            if (EditorGUI.EndChangeCheck())
            {
                repaintHandle = true;
            }

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space();

            using (new EditorGUI.DisabledGroupScope(boundsRecalculator.rootBone == null || boundsRecalculator.anchorTarget == null))
            {
                GUI.color = new Color(0.75f, 0.75f, 1f);
                bool exec = GUILayout.Button("Apply Bounds To Renderer");
                GUI.color = oldColor;
                if (exec && ConfirmContinue())
                {
                    boundsRecalculator.ApplyBounds();
                }
            }

            // スクロール終了
            EditorGUILayout.EndScrollView();

            if (repaintHandle)
            {
                SceneView.RepaintAll();
                repaintHandle = false;
            }
        }

        private static bool ConfirmContinue()
        {
            return EditorUtility.DisplayDialog(Title, "Continue modify Objects?\nオブジェクトを変更しますか？", "OK", "CANCEL");
        }
    }
}

#endif
