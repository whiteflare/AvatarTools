﻿/*
 *  The MIT License
 *
 *  Copyright 2020-2022 shajiku_works and whiteflare.
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

using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;

#if UNITY_2021_2_OR_NEWER
using UnityEditor.SceneManagement;
#else
using UnityEditor.Experimental.SceneManagement;
#endif

namespace VKetEditorTools.BakeKillerFinder
{
    public class BakeKillerFinderZweiWindow : EditorWindow
    {
        [MenuItem("Tools/whiteflare/BakeKillerFinder改", priority = 14)]
        public static void Menu_BakeKillerFinder()
        {
            VKetEditorTools.BakeKillerFinder.BakeKillerFinderZweiWindow.ShowWindow();
        }

        public static readonly string WINDOW_TITLE = "BakeKillerFinder改";

        public static void ShowWindow()
        {
            GetWindow<BakeKillerFinderZweiWindow>(WINDOW_TITLE);
        }

        private GameObject rootObject = null;
        private bool onlyActiveObject = false;

        private Vector2 _scrollPos = Vector2.zero;

        private static readonly string HELP_URL = "https://whiteflare.github.io/vpm-repos/docs/tools/BakeKillerFinder";

        private readonly CheckingTask[] Tasks = {
            new CheckingTask("Unityがクラッシュするレベル", "Missing スクリプト",
                HELP_URL + "#A1",
                (rootObject, onlyActiveObject) => 
                    FindObjectInScene(rootObject, onlyActiveObject)
                    .Where(go => go.GetComponents<Component>().Any(cmp => cmp == null)) // null の Component ならば Missing Script
                    .Select(go => go.transform)),

            new CheckingTask("ライトベイクがクラッシュするレベル", "UV2 なし Lightmap static な MeshFilter",
                HELP_URL + "#B1",
                (rootObject, onlyActiveObject) =>
                    FindInScene<MeshFilter>(rootObject, onlyActiveObject).Where(IsLightmapStatic).Where(IsIllegalUV2)),

            new CheckingTask("ライトベイクがクラッシュするレベル", "Lightmap static な TextMeshPro",
                HELP_URL + "#B2",
                (rootObject, onlyActiveObject) =>
                    FindInScene<MeshRenderer>(rootObject, onlyActiveObject).Where(IsLightmapStatic).Where(HasTextMeshPro)),

            new CheckingTask("ライトベイクがクラッシュするレベル", "Material なし Lightmap static な MeshRenderer",
                HELP_URL + "#B3",
                (rootObject, onlyActiveObject) =>
                    FindInScene<MeshRenderer>(rootObject, onlyActiveObject).Where(IsLightmapStatic).Where(HasMissingMaterial)),

            new CheckingTask("エラーメッシュ", "Mesh なし Renderer",
                HELP_URL + "#C1",
                (rootObject, onlyActiveObject) =>
                    FindInScene<SkinnedMeshRenderer>(rootObject, onlyActiveObject).Where(HasMissingMesh).Select(cmp => cmp.gameObject.transform)
                    .Union(FindInScene<MeshRenderer>(rootObject, onlyActiveObject).Where(HasMissingMesh).Select(cmp => cmp.gameObject.transform))
                    .Union(FindInScene<MeshFilter>(rootObject, onlyActiveObject).Where(HasMissingMesh).Select(cmp => cmp.gameObject.transform))
                    .Distinct()),

            new CheckingTask("エラーメッシュ", "Material なし Renderer",
                HELP_URL + "#C2",
                (rootObject, onlyActiveObject) =>
                    FindInScene<Renderer>(rootObject, onlyActiveObject).Where(HasMissingMaterial)),

            new CheckingTask("エラーメッシュ", "InternalErrorShader な Material のある Renderer",
                HELP_URL + "#C3",
                (rootObject, onlyActiveObject) =>
                    FindInScene<Renderer>(rootObject, onlyActiveObject).Where(HasErrorShader)),

            new CheckingTask("エラーメッシュ", "SubMeshCount と Material スロット数が不一致",
                HELP_URL + "#C4",
                (rootObject, onlyActiveObject) =>
                    FindInScene<SkinnedMeshRenderer>(rootObject, onlyActiveObject).Where(HasUnmatchMaterialCount).Select(cmp => cmp.gameObject.transform)
                    .Union(FindInScene<MeshRenderer>(rootObject, onlyActiveObject).Where(HasUnmatchMaterialCount).Select(cmp => cmp.gameObject.transform))),

            new CheckingTask("エラーメッシュ", "Missing Prefab",
                HELP_URL + "#C5",
                (rootObject, onlyActiveObject) =>
                    FindObjectInScene(rootObject, onlyActiveObject)
                    .Where(go => PrefabUtility.GetPrefabInstanceStatus(go) == PrefabInstanceStatus.MissingAsset)
                    .Select(go => go.transform)),

            new CheckingTask("エラーメッシュ", "Missing な Bone を含む SkinnedMeshRenderer",
                HELP_URL + "#C6",
                (rootObject, onlyActiveObject) =>
                    FindInScene<SkinnedMeshRenderer>(rootObject, onlyActiveObject).Where(HasMissingBone)),

            new CheckingTask("好ましくない設定", "全ての Static が true になっている Renderer",
                HELP_URL + "#D1",
                (rootObject, onlyActiveObject) =>
                    FindInScene<Renderer>(rootObject, onlyActiveObject).Where(IsAllStatic)),

            new CheckingTask("好ましくない設定", "Unity Default-Material.mat を含む Renderer",
                HELP_URL + "#D2",
                (rootObject, onlyActiveObject) =>
                {
                    var unityDefaultMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Material.mat"); // エディタ上で取得する時はResourcesではなくAssetDatabase
                    return FindInScene<Renderer>(rootObject, onlyActiveObject).Where(renderer => renderer.sharedMaterials.Contains(unityDefaultMaterial));
                }),
            new CheckingTask("好ましくない設定", "Unlit シェーダだが Lightmap static になっている Mesh Renderer",
                HELP_URL + "#D3",
                (rootObject, onlyActiveObject) => 
                    FindInScene<MeshRenderer>(rootObject, onlyActiveObject).Where(IsLightmapStatic).Where(HasUnlitShader)),

            new CheckingTask("好ましくない設定", "モデル組み込みマテリアルを含む Renderer",
                HELP_URL + "#D4",
                (rootObject, onlyActiveObject) => 
                    FindInScene<Renderer>(rootObject, onlyActiveObject).Where(HasModelImportedMaterial)),
        };

        void OnEnable()
        {
            Search();
        }

        void OnHierarchyChange()
        {
            Search();
        }

        void OnInspectorUpdate()
        {
            Search();
        }

        void Search()
        {
            foreach(var task in Tasks)
            {
                task.Update(rootObject, onlyActiveObject);
            }
        }

        void OnGUI()
        {
            GUILayout.Space(8);

            EditorGUI.BeginChangeCheck();
            rootObject = (GameObject)EditorGUILayout.ObjectField("Root GameObject", rootObject, typeof(GameObject), true);
            onlyActiveObject = EditorGUILayout.Toggle("Only Active Object", onlyActiveObject);
            if (EditorGUI.EndChangeCheck())
            {
                Search();
            }
            GUILayout.Space(8);

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            GUILayout.BeginVertical();
            {
                var styleHeader = new GUIStyle(EditorStyles.largeLabel)
                {
                    fontSize = 20,
                    fontStyle = FontStyle.Bold,
                    fixedHeight = 32,
                    margin = new RectOffset(4, 4, 4, 10),
                };

                string currentCategory = "";
                foreach(var task in Tasks)
                {
                    if (task.category != currentCategory)
                    {
                        currentCategory = task.category;
                        GUILayout.Label(currentCategory, styleHeader);
                    }
                    ShowList(task);
                }
            }
            GUILayout.EndVertical();
            EditorGUILayout.EndScrollView();
        }

        void ShowList(CheckingTask task)
        {
            var list = task.GetResult();

            int cnt = list == null ? 0 : list.Count();
            task.foldOpen = FoldoutHeader(task.title + " (" + cnt + " 個)", task.foldOpen, cnt == 0 ? Color.black : new Color(0.75f, 0f, 0f),
                string.IsNullOrWhiteSpace(task.helpUrl) ? (System.Action)null : () => { Application.OpenURL(task.helpUrl); }
            );

            if (task.foldOpen && list != null && list.Count() != 0)
            {
                GUIStyle styleLabel = new GUIStyle(EditorStyles.textField)
                {
                    margin = new RectOffset(12, 12, 2, 2),
                };

                var color = GUI.color;
                foreach (var cmp in list)
                {
                    GUI.color = IsActive(cmp) ? Color.yellow : new Color(0.8f, 0.8f, 0.8f);
                    if (GUILayout.Button(GetHierarchyPath(cmp), styleLabel))
                    {
                        EditorGUIUtility.PingObject(cmp);
                    }
                }
                GUILayout.Space(8);
                GUI.color = color;

                if (GUILayout.Button("クリップボードにコピー"))
                {
                    string cp = "";
                    foreach (var cmp in list)
                    {
                        cp += GetHierarchyPath(cmp) + "\r\n";
                    }
                    EditorGUIUtility.systemCopyBuffer = cp;
                }
            }

            GUILayout.Space(8);
        }

        bool FoldoutHeader(string title, bool display, Color textColor, System.Action onHelpClick = null)
        {
            GUIStyle style = new GUIStyle("ShurikenModuleTitle")
            {
                font = EditorStyles.boldLabel.font,
                fontStyle = FontStyle.Bold,
                fixedHeight = 20,
                margin = new RectOffset(4, 4, 10, 10),
                contentOffset = new Vector2(20, -2),
            };
            style.normal.textColor = textColor;

            var rect = GUILayoutUtility.GetRect(16f, 22f, style);
            GUI.Box(rect, title, style);

            if (onHelpClick != null)
            {
                var helpRect = new Rect(rect.x + rect.width - 24, rect.y + 1, 18f, 18f);
                if (GUI.Button(helpRect, EditorGUIUtility.IconContent("_Help"), new GUIStyle("IconButton")))
                {
                    onHelpClick();
                }
            }

            var e = Event.current;

            var toggleRect = new Rect(rect.x + 4f, rect.y + 2f, 13f, 13f);
            if (e.type == EventType.Repaint)
            {
                EditorStyles.foldout.Draw(toggleRect, false, false, display, false);
            }

            if (e.type == EventType.MouseDown && rect.Contains(e.mousePosition))
            {
                display = !display;
                e.Use();
            }

            return display;
        }

        /// <summary>
        /// GameObjectがHierarchy上で active ならば true
        /// </summary>
        static bool IsActive(GameObject obj)
        {
            return obj != null && obj.activeInHierarchy;
        }

        /// <summary>
        /// ComponentがHierarchy上で active ならば true
        /// </summary>
        static bool IsActive(Component cmp)
        {
            if (cmp == null)
            {
                return false;
            }
            if (!IsActive(cmp.gameObject))
            { // GameObject自体が非activeならばfalse
                return false;
            }
            if (cmp is Renderer)
            {
                return ((Renderer)cmp).enabled; // Rendererのactiveはenabledプロパティ
            }
            if (cmp is MonoBehaviour)
            {
                return ((MonoBehaviour)cmp).enabled; // MonoBehaviourのactiveはenabledプロパティ
            }
            return true;
        }

        /// <summary>
        /// ComponentのHierarchy上のパスを返却する。
        /// </summary>
        static string GetHierarchyPath(Transform self)
        {
            if (self == null)
            {
                return "";
            }
            string path = self.gameObject.name;
            Transform parent = self.parent;
            while (parent != null)
            {
                path = parent.name + " / " + path;
                parent = parent.parent;
            }
            return path;
        }

        /// <summary>
        /// ComponentのHierarchy上のパスを返却する。
        /// </summary>
        static string GetHierarchyPath(Component self)
        {
            if (self == null)
            {
                return "";
            }
            return GetHierarchyPath(self.gameObject.transform);
        }

        /// <summary>
        /// Hierarchyに表示されているシーンを全て取得する。
        /// </summary>
        /// <returns>Sceneのコレクション</returns>
        static IEnumerable<Scene> GetAllLoadedScenes()
        {
            var result = new List<Scene>();
            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage != null)
            {
                result.Add(prefabStage.scene);
            }
            else
            {
                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    var scene = SceneManager.GetSceneAt(i);
                    if (scene.isLoaded)
                    {
                        result.Add(scene);
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Hierarchyに表示されているシーンの、全てのルートオブジェクトを取得する。
        /// </summary>
        /// <returns>GameObjectのコレクション</returns>
        static IEnumerable<GameObject> GetAllRootGameObjects()
        {
            return GetAllLoadedScenes().SelectMany(scene => scene.GetRootGameObjects());
        }

        /// <summary>
        /// シーン内に存在する全ての Component を列挙する。
        /// </summary>
        /// <typeparam name="T">Componentの型</typeparam>
        /// <param name="rootObject">基点</param>
        /// <returns>Componentのコレクション</returns>
        static IEnumerable<T> FindInScene<T>(GameObject rootObject, bool onlyActiveObject) where T : Component
        {
            var roots = rootObject != null ? new GameObject[] { rootObject } : GetAllRootGameObjects();
            // 起点から全ての Component を非アクティブ含めて列挙
            var result = roots.SelectMany(root => root.GetComponentsInChildren<T>(true));
            if (onlyActiveObject)
            {
                // ただしonlyActiveObjectが指定されている場合はアクティブのみ検出する
                result = result.Where(cmp => cmp != null && cmp.gameObject.activeInHierarchy);
            }
            return result;
        }

        /// <summary>
        /// シーン内に存在する全ての GameObject を列挙する。
        /// </summary>
        /// <param name="rootObject">基点</param>
        /// <returns>GameObjectのコレクション</returns>
        static IEnumerable<GameObject> FindObjectInScene(GameObject rootObject, bool onlyActiveObject)
        {
            return FindInScene<Transform>(rootObject, onlyActiveObject).Select(t => t.gameObject);    // Transformを検索すると全GameObjectが引っかかる
        }

#region Component/GameObject判定用static関数

        /// <summary>
        /// 全ての StaticEditorFlags がオンになっているならば true
        /// </summary>
        public static bool IsAllStatic(GameObject obj)
        {
            if (obj == null)
            {
                return false;
            }
            foreach (StaticEditorFlags flag in System.Enum.GetValues(typeof(StaticEditorFlags)))
            {
                if (!GameObjectUtility.AreStaticEditorFlagsSet(obj, flag))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 全ての StaticEditorFlags がオンになっているならば true
        /// </summary>
        public static bool IsAllStatic(Component cmp)
        {
            if (cmp == null)
            {
                return false;
            }
            return IsAllStatic(cmp.gameObject);
        }

        /// <summary>
        /// Lightmap static が付いているならば true
        /// </summary>
        public static bool IsLightmapStatic(GameObject obj)
        {
            if (obj == null)
            {
                return false;
            }
            if (!GameObjectUtility.AreStaticEditorFlagsSet(obj, StaticEditorFlags.ContributeGI))
            {
                return false;
            }
            var mr = obj.GetComponent<MeshRenderer>();
            if (mr != null && mr.receiveGI == ReceiveGI.LightProbes)
            {
                return false;
            }
            if (mr != null && mr.scaleInLightmap == 0)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Lightmap static が付いているならば true
        /// </summary>
        public static bool IsLightmapStatic(Component cmp)
        {
            if (cmp == null)
            {
                return false;
            }
            return IsLightmapStatic(cmp.gameObject);
        }

        /// <summary>
        /// マテリアルの設定されていない Renderer ならば true
        /// </summary>
        public static bool HasMissingMaterial(Renderer renderer)
        {
            if (renderer == null)
            {
                return false;
            }
            if (renderer is ParticleSystemRenderer)
            {
                return false; // Renderer の中でも ParticleSystemRenderer はマテリアル構造が特殊なので扱わない
            }
            return renderer.sharedMaterials.Count() == 0 || renderer.sharedMaterials.Any(mat => mat == null);
        }

        /// <summary>
        /// ModelImporterがインポートしたMaterialを含むRendererならばtrue
        /// </summary>
        public static bool HasModelImportedMaterial(Renderer renderer)
        {
            if (renderer == null)
            {
                return false;
            }
            foreach(var mat in renderer.sharedMaterials.Where(mat => mat != null))
            {
                var path = AssetDatabase.GetAssetPath(mat);
                if (string.IsNullOrWhiteSpace(path))
                {
                    continue;
                }
                if (!path.StartsWith("Assets/", System.StringComparison.InvariantCulture) || path.StartsWith("Packages/", System.StringComparison.InvariantCulture))
                {
                    if (path.EndsWith(".mat", System.StringComparison.InvariantCulture))
                    {
                        continue;
                    }
                    if (path.EndsWith(".fbx", System.StringComparison.InvariantCulture))
                    {
                        return true;
                    }
                    if (path.EndsWith(".blend", System.StringComparison.InvariantCulture))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Missing な mesh があるならば true
        /// </summary>
        public static bool HasMissingMesh(MeshFilter mf)
        {
            return mf != null && mf.sharedMesh == null;
        }

        /// <summary>
        /// MeshRenderer の隣に MeshFilter が無いか、または Missing な mesh ならば true
        /// </summary>
        public static bool HasMissingMesh(MeshRenderer renderer)
        {
            if (renderer == null)
            {
                return false;
            }
            // MeshFilter
            var mf = renderer.gameObject.GetComponent<MeshFilter>();
            if (mf != null)
            {
                return HasMissingMesh(mf);
            }
            // TextMesh
            var tm = renderer.gameObject.GetComponent<TextMesh>();
            if (tm != null)
            {
                return false;
            }
#if ENV_TEXTMESHPRO
            // TextMeshPro
            var tmp = renderer.gameObject.GetComponent<TMPro.TextMeshPro>();
            if (tmp != null)
            {
                return false;
            }
#endif
            // どちらも無い時は true
            return true;
        }

        /// <summary>
        /// SkinnedMeshRenderer が mesh を持たない、または Missing な mesh ならば true
        /// </summary>
        public static bool HasMissingMesh(SkinnedMeshRenderer renderer)
        {
            return renderer != null && renderer.sharedMesh == null;
        }

        /// <summary>
        /// UV2 を持たない mesh ならば true
        /// </summary>
        public static bool IsIllegalUV2(MeshFilter mf)
        {
            if (mf == null)
            {
                return false; // MeshFilter が無いなら false
            }
            if (mf.sharedMesh == null)
            {
                return true; // Mesh が無いなら UV2 も無いので true
            }

            // UnityプリミティブはUV2を持っているようにみえないがエラーの原因にはならない様子なので除外する
            var path = AssetDatabase.GetAssetPath(mf.sharedMesh);
            if (path == null || !path.StartsWith("Assets/"))
            {
                return false;
            }

            return mf.sharedMesh.uv2 == null || mf.sharedMesh.uv2.Length == 0;
        }

        /// <summary>
        /// InternalErrorShader なマテリアルを持つ Renderer ならば true
        /// </summary>
        public static bool HasErrorShader(Renderer renderer)
        {
            if (renderer == null)
            {
                return false;
            }
            return renderer.sharedMaterials.Any(mat => mat != null && mat.shader.name == "Hidden/InternalErrorShader");
        }

        /// <summary>
        /// Missing な bone を含む SkinnedMeshRenderer ならば true
        /// </summary>
        public static bool HasMissingBone(SkinnedMeshRenderer renderer)
        {
            if (renderer == null)
            {
                return false;
            }
            return renderer.bones.Any(t => t == null);
        }

        public static bool HasUnmatchMaterialCount(MeshRenderer renderer)
        {
            if (renderer == null)
            {
                return false;
            }
            // MeshFilter
            var mf = renderer.gameObject.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null)
            {
                return false;
            }
            if (mf.sharedMesh.subMeshCount == 1)
            {
                return false; // サブメッシュカウントが 1 のときは、マテリアルは何個でもOK
            }
            return mf.sharedMesh.subMeshCount != renderer.sharedMaterials.Length;
        }

        public static bool HasUnmatchMaterialCount(SkinnedMeshRenderer renderer)
        {
            if (renderer == null)
            {
                return false;
            }
            if (renderer.sharedMesh != null)
            {
                if (renderer.sharedMesh.subMeshCount == 1)
                {
                    return false; // サブメッシュカウントが 1 のときは、マテリアルは何個でもOK
                }
                return renderer.sharedMesh.subMeshCount != renderer.sharedMaterials.Length;
            }
            else
            {
                var mf = renderer.gameObject.GetComponent<MeshFilter>();
                if (mf == null)
                {
                    return false;
                }
                if (mf.sharedMesh.subMeshCount == 1)
                {
                    return false; // サブメッシュカウントが 1 のときは、マテリアルは何個でもOK
                }
                return mf.sharedMesh.subMeshCount != renderer.sharedMaterials.Length;
            }
        }

        public static bool HasUnlitShader(MeshRenderer renderer)
        {
            if (renderer == null)
            {
                return false;
            }
            return renderer.sharedMaterials.Where(mat => mat != null).Any(mat => 
                mat.shader.name == "Unlit/Texture"
                || mat.shader.name == "Unlit/Color"
                || mat.shader.name == "Unlit/Transparent"
                || mat.shader.name == "Unlit/Transparent Cutout");
        }

        /// <summary>
        /// TextMeshProを使用するMeshRendererならばtrue
        /// </summary>
        public static bool HasTextMeshPro(MeshRenderer renderer)
        {
            if (renderer == null)
            {
                return false;
            }
            return renderer.gameObject.GetComponents<Component>().Any(cmp => cmp != null && cmp.GetType().FullName == "TMPro.TextMeshPro");
        }

#endregion

        internal class CheckingTask
        {
            public readonly string category;
            public readonly string title;
            public readonly string helpUrl;
            public bool foldOpen = false;

            private readonly System.Func<GameObject, bool, IEnumerable<Component>> updater;
            private List<Component> current = null;

            public CheckingTask(string category, string title, string helpUrl, System.Func<GameObject, bool, IEnumerable<Component>> updater)
            {
                this.category = category;
                this.title = title;
                this.helpUrl = helpUrl;
                this.updater = updater;
            }

            public List<Component> GetResult()
            {
                if (current == null)
                {
                    return new List<Component>();
                }
                return current;
            }

            public void Update(GameObject rootObject, bool onlyActiveObject)
            {
                current = new List<Component>();
                current.AddRange(updater(rootObject, onlyActiveObject).OrderBy(GetHierarchyPath));
                // FindInSceneしたIEnumerableはそのままだとシーンと直結していて重いので要素だけをListにコピーする
            }
        }
    }
}

#endif
