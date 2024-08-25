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

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.SceneManagement;
using System.Text.RegularExpressions;

#if UNITY_2021_2_OR_NEWER
using UnityEditor.SceneManagement;
#else
using UnityEditor.Experimental.SceneManagement;
#endif

namespace WF.Tool.Avatar
{
    internal class HierarchyHelper : EditorWindow, IHasCustomMenu
    {
        [MenuItem("Tools/whiteflare/Hierarchy Helper", priority = 17)]
        public static void Menu_HierarchyHelper()
        {
            HierarchyHelper.ShowWindow();
        }

        private const string Title = "HierarchyHelper";
        private const string ConfigKey = "WF.Utillty.HierarchyHelper";

        public static void ShowWindow() {
            GetWindow<HierarchyHelper>(Title);
        }

        public HierarchyHelperConfig config = null;

        private readonly List<HighlightMode> modes = new List<HighlightMode> {
            // Tag関連
            new HighlightMode("EditorOnly",
                go => go.CompareTag("EditorOnly")),

            // 区切り線
            HighlightMode.Dummy,

            // Static関連
            new HighlightMode("Static/Batching Static",
                go => GameObjectUtility.AreStaticEditorFlagsSet(go, StaticEditorFlags.BatchingStatic)),
            new HighlightMode("Static/Lightmap Static",
                go => GameObjectUtility.AreStaticEditorFlagsSet(go, StaticEditorFlags.ContributeGI)),
            new HighlightMode("Static/Reflection Static",
                go => GameObjectUtility.AreStaticEditorFlagsSet(go, StaticEditorFlags.ReflectionProbeStatic)),
            new HighlightMode("Static/Occluder Static",
                go => GameObjectUtility.AreStaticEditorFlagsSet(go, StaticEditorFlags.OccluderStatic)),
            new HighlightMode("Static/Occludee Static",
                go => GameObjectUtility.AreStaticEditorFlagsSet(go, StaticEditorFlags.OccludeeStatic)),

            // Renderer関連
            new HighlightMode("Renderer/SkinnedMesh Renderer",
                result => result.AddRange(FindObjectInScene<SkinnedMeshRenderer>())),
            new HighlightMode("Renderer/SkinnedMesh Renderer Bones",
                result => result.AddRange(FindObjectInScene<SkinnedMeshRenderer, Transform>(smr => smr.bones))),
            new HighlightMode("Renderer/Mesh Renderer",
                result => result.AddRange(FindObjectInScene<MeshRenderer>())),
            new HighlightMode("Renderer/ParticleSystem",
                result => result.AddRange(FindObjectInScene<ParticleSystem>())),
            new HighlightMode("Renderer/Trail Renderer",
                result => result.AddRange(FindObjectInScene<TrailRenderer>())),
            new HighlightMode("Renderer/Line Renderer",
                result => result.AddRange(FindObjectInScene<LineRenderer>())),

            // ライト関連
            new HighlightMode("Light/Realtime Light",
                result => result.AddRange(FindObjectInScene<Light>(cmp => cmp.lightmapBakeType == LightmapBakeType.Realtime))),
            new HighlightMode("Light/Mixed Light",
                result => result.AddRange(FindObjectInScene<Light>(cmp => cmp.lightmapBakeType == LightmapBakeType.Mixed))),
            new HighlightMode("Light/Baked Light",
                result => result.AddRange(FindObjectInScene<Light>(cmp => cmp.lightmapBakeType == LightmapBakeType.Baked))),
            new HighlightMode("Light/LightProbeGroup",
                result => result.AddRange(FindObjectInScene<LightProbeGroup>())),
            new HighlightMode("Light/ReflectionProbe",
                result => result.AddRange(FindObjectInScene<ReflectionProbe>())),

            // Constraint
            new HighlightMode("Constraint",
                result => result.AddRange(FindObjectInScene<Component>(cmp => cmp is IConstraint))),

            // 区切り線
            HighlightMode.Dummy,

#if ENV_VRCSDK3_AVATAR
            // Constraint
            new HighlightMode("VRC/PhysBone",
                result => result.AddRange(FindObjectInScene("VRC.SDK3.Dynamics.PhysBone.Components.VRCPhysBone"))),
            new HighlightMode("VRC/PhysBone Collider",
                result => result.AddRange(FindObjectInScene("VRC.SDK3.Dynamics.PhysBone.Components.VRCPhysBoneCollider"))),
            new HighlightMode("VRC/Contact Receiver",
                result => result.AddRange(FindObjectInScene("VRC.SDK3.Dynamics.Contact.Components.VRCContactReceiver"))),
            new HighlightMode("VRC/Contact Sender",
                result => result.AddRange(FindObjectInScene("VRC.SDK3.Dynamics.Contact.Components.VRCContactSender"))),
            new HighlightMode("VRC/Constraint",
                result => result.AddRange(FindObjectInScene(new Regex(@"VRC\.SDK3\.Dynamics\.Constraint\.Components\..*Constraint", RegexOptions.Compiled)))),
#endif
        };

        HighlightMode CurrentMode
        {
            get {
                int index = config.highlightMode;
                if (0 <= index && index < modes.Count) {
                    return modes[index];
                }
                return HighlightMode.Dummy;
            }
        }

        #region メッセージハンドラ

        public void AddItemsToMenu(GenericMenu menu) {
            menu.AddItem(new GUIContent("Reset Settings"), false, () => {
                this.config = CreateInstance<HierarchyHelperConfig>();
                OnHierarchyChange();
                EditorApplication.RepaintHierarchyWindow();
            });
        }

        public void OnEnable() {
            if (this.config == null) {
                this.config = CreateInstance<HierarchyHelperConfig>();
            }
            this.minSize = new Vector2(100, 32);
            EditorApplication.hierarchyWindowItemOnGUI += hierarchyWindowItemOnGUI;

            var savedText = EditorUserSettings.GetConfigValue(ConfigKey);
            if (!string.IsNullOrWhiteSpace(savedText)) {
                EditorJsonUtility.FromJsonOverwrite(savedText, config);
            }
        }

        public void OnDisable() {
            EditorApplication.hierarchyWindowItemOnGUI -= hierarchyWindowItemOnGUI;
            if (config != null) {
                EditorUserSettings.SetConfigValue(ConfigKey, EditorJsonUtility.ToJson(config));
            }
        }

        public void OnGUI() {
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.BeginHorizontal();
            config.highlightEnable = EditorGUILayout.Toggle("Highlight", config.highlightEnable);
            config.highlightColor = EditorGUILayout.ColorField(GUIContent.none, config.highlightColor, false, true, false, GUILayout.Width(60));
            EditorGUILayout.EndHorizontal();

            config.highlightMode = EditorGUILayout.Popup("Mode", config.highlightMode, modes.Select(m => m.Name).ToArray());

            if (EditorGUI.EndChangeCheck()) {
                if (config != null) {
                    EditorUtility.SetDirty(config);
                }
                OnHierarchyChange();
                EditorApplication.RepaintHierarchyWindow();
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Selection");
            var doSelect = GUILayout.Button("select objects", EditorStyles.miniButton);
            EditorGUILayout.EndHorizontal();

            if (doSelect) {
                Selection.objects = FindObjectInScene<Transform>().Where(CurrentMode.IsActive).ToArray();
            }
        }

        public void OnHierarchyChange() {
            modes.ForEach(m => m.Clear());
            CurrentMode.OnHierarchyChange();
        }

        private void hierarchyWindowItemOnGUI(int instanceID, Rect selectionRect) {
            var oldColor = GUI.backgroundColor;

            if (config.highlightEnable) {
                var go = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
                if (go != null && CurrentMode.IsActive(go)) {
                    GUI.backgroundColor = config.highlightColor;
#if false
                    var style = new GUIStyle(GUI.skin.box);
                    style.alignment = TextAnchor.MiddleLeft;
                    style.padding.left = 18;
                    GUI.Box(selectionRect, go.name, style);
#else
                    GUI.Box(selectionRect, "");
#endif
                }
            }

            GUI.backgroundColor = oldColor;
        }

        #endregion

        #region サブクラス

        internal delegate void PreIsActiveHandler(List<GameObject> result);
        internal delegate bool IsActiveHandler(GameObject obj);

        internal class HighlightMode
        {
            public static readonly HighlightMode Dummy = new HighlightMode("", go => false);

            public readonly string Name;
            private readonly PreIsActiveHandler preIsActive;
            private readonly IsActiveHandler isActive;

            private readonly List<GameObject> targets = new List<GameObject>();

            internal HighlightMode(string name, PreIsActiveHandler preIsActive) : this(name, preIsActive, null) {
            }

            internal HighlightMode(string name, IsActiveHandler isActive) : this(name, null, isActive) {
            }

            private HighlightMode(string name, PreIsActiveHandler preIsActive, IsActiveHandler isActive) {
                Name = name;
                this.preIsActive = preIsActive;
                this.isActive = isActive;
            }

            public void Clear() {
                targets.Clear();
            }

            public void OnHierarchyChange() {
                targets.Clear();
                preIsActive?.Invoke(targets);
            }

            public bool IsActive(GameObject go) {
                if (preIsActive != null) {
                    return targets.Contains(go);
                }
                return isActive(go);
            }
        }

        internal class HierarchyHelperConfig : ScriptableObject
        {
            public bool highlightEnable = false;
            public int highlightMode = 0;
            public Color highlightColor = new Color(0, 0, 1, 0.2f);
        }

        #endregion

        public static IEnumerable<Scene> GetAllLoadedScenes() {
            var result = new List<Scene>();
            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage != null) {
                result.Add(prefabStage.scene);
            }
            else {
                for (int i = 0; i < SceneManager.sceneCount; i++) {
                    var scene = SceneManager.GetSceneAt(i);
                    if (scene.isLoaded)
                    {
                        result.Add(scene);
                    }
                }
            }
            return result;
        }

        public static IEnumerable<GameObject> GetAllRootGameObjects() {
            return GetAllLoadedScenes().SelectMany(scene => scene.GetRootGameObjects());
        }

        public static IEnumerable<GameObject> FindObjectInScene<T>(Func<T, bool> pred = null) where T : Component {
            if (pred == null) {
                pred = _ => true;
            }
            return GetAllRootGameObjects().SelectMany(go => go.GetComponentsInChildren<T>(true))
                .Where(pred)
                .Distinct()
                .Select(cmp => cmp.gameObject);
        }

        public static IEnumerable<GameObject> FindObjectInScene<T, U>(Func<T, IEnumerable<U>> split) where T : Component where U : Component {
            return GetAllRootGameObjects().SelectMany(go => go.GetComponentsInChildren<T>(true))
                .SelectMany(split)
                .Where(cmp => cmp != null)
                .Distinct()
                .Select(cmp => cmp.gameObject);
        }

        public static IEnumerable<GameObject> FindObjectInScene(string fullName, Func<Component, bool> pred = null)
        {
            if (pred == null)
            {
                pred = _ => true;
            }
            return GetAllRootGameObjects().SelectMany(go => go.GetComponentsInChildren<Component>(true))
                .Where(cmp => cmp != null && cmp.GetType().FullName == fullName)
                .Where(pred)
                .Distinct()
                .Select(cmp => cmp.gameObject);
        }

        public static IEnumerable<GameObject> FindObjectInScene(Regex fullName, Func<Component, bool> pred = null)
        {
            if (pred == null)
            {
                pred = _ => true;
            }
            return GetAllRootGameObjects().SelectMany(go => go.GetComponentsInChildren<Component>(true))
                .Where(cmp => cmp != null && fullName.IsMatch(cmp.GetType().FullName))
                .Where(pred)
                .Distinct()
                .Select(cmp => cmp.gameObject);
        }
    }
}

#endif
