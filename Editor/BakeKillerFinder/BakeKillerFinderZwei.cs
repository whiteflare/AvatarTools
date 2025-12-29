/*
 *  The MIT License
 *
 *  Copyright 2020-2026 shajiku_works and whiteflare.
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

using UnityEditor.IMGUI.Controls;

#if UNITY_2021_2_OR_NEWER
using UnityEditor.SceneManagement;
#else
using UnityEditor.Experimental.SceneManagement;
#endif

namespace WF.Tool.World.BakeKillerFinder
{
    public class BakeKillerFinderZweiWindow : EditorWindow
    {
        [MenuItem("Tools/whiteflare/BakeKillerFinder改二", priority = 14)]
        public static void Menu_BakeKillerFinder()
        {
            BakeKillerFinderZweiWindow.ShowWindow();
        }

        public static readonly string WINDOW_TITLE = "BakeKillerFinder改二";

        public static void ShowWindow()
        {
            GetWindow<BakeKillerFinderZweiWindow>(WINDOW_TITLE);
        }

        private GameObject rootObject = null;
        private bool onlyActiveObject = false;

        public TreeViewState treeViewState;
        private BakeKillerListView treeView;
        private bool updated = false;

        private void OnEnable()
        {
            if (treeViewState == null)
            {
                treeViewState = new TreeViewState();
            }
            treeView = new BakeKillerListView(treeViewState);
            UpdateTreeView();
        }

        private void OnHierarchyChange()
        {
            updated = true;
        }

        void OnGUI()
        {
            var oldColor = GUI.color;

            GUILayout.Space(8);

            EditorGUILayout.BeginHorizontal();
            {
                EditorGUI.BeginChangeCheck();
                rootObject = (GameObject)EditorGUILayout.ObjectField("Root GameObject", rootObject, typeof(GameObject), true);
                EditorGUILayout.Space();
                onlyActiveObject = EditorGUILayout.Toggle("Only Active Object", onlyActiveObject);
                EditorGUILayout.Space();
                if (EditorGUI.EndChangeCheck())
                {
                    UpdateTreeView();
                }
                if (updated)
                {
                    GUI.color = Color.yellow;
                }
                if (GUILayout.Button("Refresh", GUILayout.Width(80)))
                {
                    GUI.color = oldColor;
                    UpdateTreeView();
                }
                GUI.color = oldColor;
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(8);

            treeView.OnGUI(GUILayoutUtility.GetRect(0, float.MaxValue, 0, float.MaxValue));
        }

        private void UpdateTreeView()
        {
            treeView.items = CreateTreeViewItem();
            treeView.SortItems();
            treeView.Reload();
            treeView.SetSelection(new List<int>());
            updated = false;
        }

        WarnItem[] CreateTreeViewItem()
        {
            var result = new List<WarnItem>();
            foreach (var seeker in Seekers)
            {
                seeker(rootObject, onlyActiveObject, result);
            }
            return result.ToArray();
        }

        #region WarnItem

        const int COL_Level = 0;
        const int COL_ObjectName = 1;
        const int COL_ComponentType = 2;
        const int COL_ID = 3;
        const int COL_MESSAGE = 4;

        internal enum WarnLevel
        {
            FATAL,
            ERROR,
            WARN,
            INFO,
        }

        internal class WarnItem
        {
            public GameObject gameObject;
            public Component component;
            public WarnLevel level;
            public string wid;
            public string message;

            public object GetValue(int idx)
            {
                if (gameObject == null)
                {
                    return null; // 途中でobjectがdestroyされた場合は何もしない
                }
                switch (idx)
                {
                    case COL_ObjectName:
                        return gameObject.name;
                    case COL_ComponentType:
                        return component == null ? null : component.GetType().Name;
                    case COL_Level:
                        return level;
                    case COL_ID:
                        return wid;
                    case COL_MESSAGE:
                        return message;
                    default:
                        return null;
                }
            }

            public bool IsActive
            {
                get => component == null ? IsActive(gameObject) : IsActive(component);
            }
        }

        #endregion

        #region TreeView

        internal class BakeKillerListView : TreeView
        {
            public WarnItem[] items = new WarnItem[0];

            public BakeKillerListView(TreeViewState state) : base(state, NewHeader())
            {
                multiColumnHeader.sortingChanged += OnSortingChanged;
            }

            private static MultiColumnHeader NewHeader()
            {
                var column = new List<MultiColumnHeaderState.Column>
                {
                    new MultiColumnHeaderState.Column
                    {
                        headerContent = new GUIContent("Level"),
                        width = 100,
                    },
                    new MultiColumnHeaderState.Column
                    {
                        headerContent = new GUIContent("Game Object"),
                        width = 200,
                    },
                    new MultiColumnHeaderState.Column
                    {
                        headerContent = new GUIContent("Component"),
                        width = 100,
                    },
                    new MultiColumnHeaderState.Column
                    {
                        headerContent = new GUIContent("エラーID"),
                        width = 50,
                    },
                    new MultiColumnHeaderState.Column
                    {
                        headerContent = new GUIContent("説明"),
                        width = 360,
                    }
                };

                var state = new MultiColumnHeaderState(column.ToArray());
                var header = new MultiColumnHeader(state);

                return header;
            }

            protected override TreeViewItem BuildRoot()
            {
                var id = 0;
                var root = new TreeViewItem { id = id++, depth = -1, displayName = "Root" };

                var list = new List<TreeViewItem>();
                foreach (var item in items)
                {
                    list.Add(new ExTreeViewItem { id = id++, depth = 0, displayName = item.gameObject == null ? "<null>" : item.gameObject.name, info = item });
                }

                SetupParentsAndChildrenFromDepths(root, list);
                return root;
            }

            protected override void SelectionChanged(IList<int> selectedIds)
            {
                Selection.objects = getSelectedGameObjects();
            }

            private ExTreeViewItem[] getSelectedTreeViewItem(ExTreeViewItem current = null)
            {
                // TreeView から今選択されている ExTreeViewItem を取得
                var items = GetSelection().Select(id => FindItem(id, rootItem) as ExTreeViewItem)
                    .Where(item => item != null)
                    .ToArray();

                // もし current が null ではなく、Selection に入っていないならば、かわりに current のみを対象にする
                if (current != null && !items.Contains(current))
                {
                    return new ExTreeViewItem[] { current };
                }
                return items;
            }

            protected GameObject[] getSelectedGameObjects(ExTreeViewItem current = null)
            {
                return getSelectedTreeViewItem(current)
                    .Where(item => item.info.gameObject != null)
                    .Select(item => item.info.gameObject)
                    .ToArray();
            }

            protected override void ContextClickedItem(int id)
            {
                var ev = Event.current;
                ev.Use();

                var menu = new GenericMenu();
                menu.AddItem(new GUIContent("パスをコピー"), false, () =>
                {
                    var select = getSelectedGameObjects();
                    if (select.Length == 0)
                    {
                        return; // 0件選択のときは Selection を変更しない
                    }
                    string cp = "";
                    foreach (var go in select)
                    {
                        cp += GetHierarchyPath(go) + "\r\n";
                    }
                    EditorGUIUtility.systemCopyBuffer = cp;
                });
                menu.AddItem(new GUIContent("ヘルプを開く"), false, () =>
                {
                    foreach (var sel in getSelectedTreeViewItem())
                    {
                        if (!string.IsNullOrWhiteSpace(sel.info.wid))
                        {
                            Application.OpenURL(HELP_URL + "#" + sel.info.wid);
                            return; // 1件だけ開く
                        }
                    }
                    Application.OpenURL(HELP_URL); // 0件選択のときは HELP_URL だけ開く
                });
                menu.ShowAsContext();
            }

            protected override void RowGUI(RowGUIArgs args)
            {
                var item = (ExTreeViewItem)args.item;
                var info = item.info;

                var levelContent = new GUIContent[]
                {
                    new GUIContent(EditorGUIUtility.IconContent("UnityLogo")),
                    new GUIContent(EditorGUIUtility.IconContent("console.erroricon.sml")),
                    new GUIContent(EditorGUIUtility.IconContent("console.warnicon.sml")),
                    new GUIContent(EditorGUIUtility.IconContent("console.infoicon.sml")),
                };
                levelContent[0].text = "FATAL";
                levelContent[1].text = "ERROR";
                levelContent[2].text = "WARN";
                levelContent[3].text = "INFO";

                var isActive = info.IsActive;

                if (info.gameObject != null) // 途中でobjectがdestroyされた場合は何もしない
                {
                    for (int i = 0; i < args.GetNumVisibleColumns(); i++)
                    {
                        var cellRect = args.GetCellRect(i);
                        CenterRectUsingSingleLineHeight(ref cellRect);

                        int idx = args.GetColumn(i);
                        switch (idx)
                        {
                            case COL_Level:
                                // 非アクティブのときはラベル系を Disabled にして区別できるようにする
                                using (new EditorGUI.DisabledGroupScope(!isActive))
                                {
                                    var level = (int)info.level;
                                    if (0 <= level && level < levelContent.Length)
                                    {
                                        GUI.Label(cellRect, levelContent[level]);
                                    }
                                }
                                break;
                            default:
                                // 非アクティブのときはラベル系を Disabled にして区別できるようにする
                                using (new EditorGUI.DisabledGroupScope(!isActive))
                                {
                                    GUI.Label(cellRect, "" + info.GetValue(idx));
                                }
                                break;
                        }
                    }
                }
            }

            public void SortItems()
            {
                var idx = multiColumnHeader.sortedColumnIndex;
                if (idx < 0)
                {
                    return;
                }
                items = (multiColumnHeader.IsSortedAscending(idx) ?
                    items.OrderBy(mci => mci.GetValue(idx)) :
                    items.OrderByDescending(mci => mci.GetValue(idx))).ToArray();
            }

            void OnSortingChanged(MultiColumnHeader header)
            {
                SortItems();
                Reload();
            }

            internal class ExTreeViewItem : TreeViewItem
            {
                public WarnItem info;
            }
        }

        #endregion


        private static readonly string HELP_URL = "https://whiteflare.github.io/vpm-repos/docs/tools/BakeKillerFinder";

        private readonly System.Action<GameObject, bool, List<WarnItem>>[] Seekers = {
            // =========================
            // 重大
            // =========================

            (rootObject, onlyActiveObject, result) =>
                {
                    result.AddRange(FindObjectInScene(rootObject, onlyActiveObject)
                        .Where(go => go.GetComponents<Component>().Any(cmp => cmp == null)) // null の Component ならば Missing Script
                        .Select(go => new WarnItem(){
                            gameObject = go,
                            level = WarnLevel.FATAL,
                            wid = "A1",
                            message = "Missing Script が含まれています",
                        }));
                },

            (rootObject, onlyActiveObject, result) =>
                {
                    result.AddRange(FindObjectInScene(rootObject, onlyActiveObject)
                        .Where(go => PrefabUtility.GetPrefabInstanceStatus(go) == PrefabInstanceStatus.MissingAsset)
                        .Select(go => new WarnItem(){
                            gameObject = go,
                            level = WarnLevel.FATAL,
                            wid = "A2",
                            message = "Missing Prefab です",
                        }));
                },

            // =========================
            // ベイクエラー
            // =========================

            (rootObject, onlyActiveObject, result) =>
                {
                    result.AddRange(FindInScene<MeshFilter>(rootObject, onlyActiveObject)
                        .Where(IsLightmapped)
                        .Where(IsIllegalUV2)
                        .Select(cmp => new WarnItem(){
                            gameObject = cmp.gameObject,
                            component = cmp,
                            level = WarnLevel.ERROR,
                            wid = "B1",
                            message = "UV2のないメッシュはライトベイクできません",
                        }));
                },

            (rootObject, onlyActiveObject, result) =>
                {
                    result.AddRange(FindInScene<MeshRenderer>(rootObject, onlyActiveObject)
                        .Where(IsContributeGI)
                        .Where(HasTextMeshPro)
                        .Select(cmp => new WarnItem(){
                            gameObject = cmp.gameObject,
                            component = cmp,
                            level = WarnLevel.ERROR,
                            wid = "B2",
                            message = "TextMeshProメッシュはライトベイクできません",
                        }));
                },

            (rootObject, onlyActiveObject, result) =>
                {
                    result.AddRange(FindInScene<MeshRenderer>(rootObject, onlyActiveObject)
                        .Where(IsContributeGI)
                        .Where(HasMissingMaterial)
                        .Select(cmp => new WarnItem(){
                            gameObject = cmp.gameObject,
                            component = cmp,
                            level = WarnLevel.ERROR,
                            wid = "B3",
                            message = "Material なし MeshRenderer はライトベイクできません",
                        }));
                },

            (rootObject, onlyActiveObject, result) =>
                {
                    result.AddRange(FindInScene<MeshFilter>(rootObject, onlyActiveObject)
                        .Where(IsContributeGI)
                        .Where(HasNaNMesh)
                        .Select(cmp => new WarnItem(){
                            gameObject = cmp.gameObject,
                            component = cmp,
                            level = WarnLevel.ERROR,
                            wid = "B4",
                            message = "非数(NaN)を含むメッシュはライトベイクできません",
                        }));
                },

            (rootObject, onlyActiveObject, result) =>
                {
                    var allLights = FindInScene<Light>(rootObject, onlyActiveObject)
                        .Where(light => light.type == LightType.Point)
                        .Where(light => light.lightmapBakeType != LightmapBakeType.Realtime)
                        .Distinct().ToArray();
                    var duplicatedLightSet = new HashSet<Light>(allLights.GroupBy(light => string.Format("{0:F5}/{1:F5}/{2:F5}|{3}|{4:F3}/{5:F3}/{6:F3}|{7:F5}/{8:F3}/{9:F3}",
                            light.transform.position.x, light.transform.position.y, light.transform.position.z, // 0-2
                            light.lightmapBakeType, // 3
                            light.color.r, light.color.g, light.color.b, // 4-6
                            light.range, light.intensity, light.bounceIntensity // 7-9
                        )).Where(g => 2 <= g.Count()).SelectMany(g => g));
                    foreach(var light in allLights)
                    {
                        if (duplicatedLightSet.Contains(light))
                        {
                            result.Add(new WarnItem(){
                                gameObject = light.gameObject,
                                component = light,
                                level = WarnLevel.ERROR,
                                wid = "B5",
                                message = "同一位置に同一設定値のBakedなPointライトが複数あります",
                            });
                        }
                    }
                },

            // =========================
            // 描画不正
            // =========================

            (rootObject, onlyActiveObject, result) =>
                {
                    result.AddRange(FindInScene<SkinnedMeshRenderer>(rootObject, onlyActiveObject).Where(HasMissingMesh)
                        .Select(cmp => new WarnItem(){
                            gameObject = cmp.gameObject,
                            component = cmp,
                            level = WarnLevel.WARN,
                            wid = "C1",
                            message = "Mesh が Missing です",
                        }));
                    result.AddRange(FindInScene<MeshRenderer>(rootObject, onlyActiveObject).Where(HasMissingMesh)
                        .Select(cmp => new WarnItem(){
                            gameObject = cmp.gameObject,
                            component = cmp,
                            level = WarnLevel.WARN,
                            wid = "C1",
                            message = "Mesh が Missing です",
                        }));
                    result.AddRange(FindInScene<MeshFilter>(rootObject, onlyActiveObject).Where(HasMissingMesh)
                        .Select(cmp => new WarnItem(){
                            gameObject = cmp.gameObject,
                            component = cmp,
                            level = WarnLevel.WARN,
                            wid = "C1",
                            message = "Mesh が Missing です",
                        }));
                },

            (rootObject, onlyActiveObject, result) =>
                {
                    result.AddRange(FindInScene<Renderer>(rootObject, onlyActiveObject)
                        .Where(HasMissingMaterial)
                        .Select(cmp => new WarnItem(){
                            gameObject = cmp.gameObject,
                            component = cmp,
                            level = WarnLevel.WARN,
                            wid = "C2",
                            message = "Material が Missing です",
                        }));
                },

            (rootObject, onlyActiveObject, result) =>
                {
                    result.AddRange(FindInScene<Renderer>(rootObject, onlyActiveObject).Where(HasErrorShader)
                        .Select(cmp => new WarnItem(){
                            gameObject = cmp.gameObject,
                            component = cmp,
                            level = WarnLevel.WARN,
                            wid = "C3",
                            message = "Shader が InternalErrorShader です",
                        }));
                },

            (rootObject, onlyActiveObject, result) =>
                {
                    result.AddRange(FindInScene<SkinnedMeshRenderer>(rootObject, onlyActiveObject)
                        .Where(HasLessMaterialCount)
                        .Select(cmp => new WarnItem(){
                            gameObject = cmp.gameObject,
                            component = cmp,
                            level = WarnLevel.WARN,
                            wid = "C4",
                            message = "Material スロット数がメッシュ SubMeshCount よりも少ないです",
                        }));
                    result.AddRange(FindInScene<MeshRenderer>(rootObject, onlyActiveObject)
                        .Where(cmp => !IsBatchingStatic(cmp)) // Batching static な MeshRenderer はC4ではなくC7で確認する
                        .Where(HasLessMaterialCount)
                        .Select(cmp => new WarnItem(){
                            gameObject = cmp.gameObject,
                            component = cmp,
                            level = WarnLevel.WARN,
                            wid = "C4",
                            message = "Material スロット数がメッシュ SubMeshCount よりも少ないです",
                        }));
                },

            (rootObject, onlyActiveObject, result) =>
                {
                    result.AddRange(FindInScene<SkinnedMeshRenderer>(rootObject, onlyActiveObject).Where(HasMissingBone)
                        .Select(cmp => new WarnItem(){
                            gameObject = cmp.gameObject,
                            component = cmp,
                            level = WarnLevel.WARN,
                            wid = "C6",
                            message = "Bone が Missing です",
                        }));
                },

            (rootObject, onlyActiveObject, result) =>
                {
                    result.AddRange(FindInScene<MeshRenderer>(rootObject, onlyActiveObject)
                        .Where(IsBatchingStatic)
                        .Where(HasNotEqualMaterialCount)
                        .Select(cmp => new WarnItem(){
                            gameObject = cmp.gameObject,
                            component = cmp,
                            level = WarnLevel.WARN,
                            wid = "C7",
                            message = "BatchingStatic メッシュの SubMeshCount と Material スロット数が不一致です",
                        }));
                },

            // =========================
            // 好ましくない設定
            // =========================

            (rootObject, onlyActiveObject, result) =>
                {
                    result.AddRange(FindInScene<Renderer>(rootObject, onlyActiveObject).Where(IsAllStatic)
                        .Select(cmp => new WarnItem(){
                            gameObject = cmp.gameObject,
                            level = WarnLevel.INFO,
                            wid = "D1",
                            message = "全ての Static が true になっています",
                        }));
                },

            (rootObject, onlyActiveObject, result) =>
                {
                    var unityDefaultMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Material.mat"); // エディタ上で取得する時はResourcesではなくAssetDatabase
                    result.AddRange(FindInScene<Renderer>(rootObject, onlyActiveObject)
                        .Where(renderer => renderer.sharedMaterials.Contains(unityDefaultMaterial))
                        .Select(cmp => new WarnItem(){
                            gameObject = cmp.gameObject,
                            component = cmp,
                            level = WarnLevel.INFO,
                            wid = "D2",
                            message = "Default-Material.mat が設定された Renderer です",
                        }));
                },

            (rootObject, onlyActiveObject, result) =>
                {
                    result.AddRange(FindInScene<MeshRenderer>(rootObject, onlyActiveObject)
                        .Where(IsLightmapped)
                        .Where(HasUnlitShader)
                        .Select(cmp => new WarnItem(){
                            gameObject = cmp.gameObject,
                            component = cmp,
                            level = WarnLevel.INFO,
                            wid = "D3",
                            message = "Unlitシェーダはライトマップを読み取らないため無駄が生じています",
                        }));
                },

            (rootObject, onlyActiveObject, result) =>
                {
                    result.AddRange(FindInScene<Renderer>(rootObject, onlyActiveObject)
                        .Where(HasModelImportedMaterial)
                        .Select(cmp => new WarnItem(){
                            gameObject = cmp.gameObject,
                            component = cmp,
                            level = WarnLevel.INFO,
                            wid = "D4",
                            message = "モデル組み込みマテリアルが使用されています",
                        }));
                },

            (rootObject, onlyActiveObject, result) =>
                {
                    result.AddRange(FindInScene<MeshFilter>(rootObject, onlyActiveObject)
                        .Where(HasExternalMaterialsMesh)
                        .Select(cmp => new WarnItem(){
                            gameObject = cmp.gameObject,
                            component = cmp,
                            level = WarnLevel.INFO,
                            wid = "D5",
                            message = "External Materials を使用したメッシュが使われています",
                        }));
                    result.AddRange(FindInScene<SkinnedMeshRenderer>(rootObject, onlyActiveObject)
                        .Where(HasExternalMaterialsMesh)
                        .Select(cmp => new WarnItem(){
                            gameObject = cmp.gameObject,
                            component = cmp,
                            level = WarnLevel.INFO,
                            wid = "D5",
                            message = "External Materials を使用したメッシュが使われています",
                        }));
                },
        };

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
        /// GameObjectのHierarchy上のパスを返却する。
        /// </summary>
        static string GetHierarchyPath(GameObject self)
        {
            if (self == null)
            {
                return "";
            }
            string path = self.name;
            Transform parent = self.transform.parent;
            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }
            return path;
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
                result = result.Where(cmp => cmp != null && IsActive(cmp));
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
        /// ContributeGI が付いているならば true
        /// </summary>
        public static bool IsBatchingStatic(GameObject obj)
        {
            if (obj == null)
            {
                return false;
            }
            if (!GameObjectUtility.AreStaticEditorFlagsSet(obj, StaticEditorFlags.BatchingStatic))
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// ContributeGI が付いているならば true
        /// </summary>
        public static bool IsBatchingStatic(Component cmp)
        {
            if (cmp == null)
            {
                return false;
            }
            return IsBatchingStatic(cmp.gameObject);
        }

        /// <summary>
        /// ContributeGI が付いているならば true
        /// </summary>
        public static bool IsContributeGI(GameObject obj)
        {
            if (obj == null)
            {
                return false;
            }
            if (!GameObjectUtility.AreStaticEditorFlagsSet(obj, StaticEditorFlags.ContributeGI))
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// ContributeGI が付いているならば true
        /// </summary>
        public static bool IsContributeGI(Component cmp)
        {
            if (cmp == null)
            {
                return false;
            }
            return IsContributeGI(cmp.gameObject);
        }

        /// <summary>
        /// ライトマップにベイクされるならば true
        /// </summary>
        public static bool IsLightmapped(GameObject obj)
        {
            if (!IsContributeGI(obj))
            {
                return false;
            }
            if (obj == null)
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
        /// ライトマップにベイクされるならば true
        /// </summary>
        public static bool IsLightmapped(Component cmp)
        {
            if (cmp == null)
            {
                return false;
            }
            return IsLightmapped(cmp.gameObject);
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

        public static bool HasExternalMaterialsMesh(MeshFilter mf)
        {
            if (mf == null || mf.sharedMesh == null)
            {
                return false;
            }
            var mesh = mf.sharedMesh;
            var path = AssetDatabase.GetAssetPath(mesh);
            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null)
            {
                return false;
            }
            return importer.materialImportMode != ModelImporterMaterialImportMode.None && importer.materialLocation == ModelImporterMaterialLocation.External;
        }

        public static bool HasExternalMaterialsMesh(SkinnedMeshRenderer smr)
        {
            if (smr == null || smr.sharedMesh == null)
            {
                return false;
            }
            var mesh = smr.sharedMesh;
            var path = AssetDatabase.GetAssetPath(mesh);
            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null)
            {
                return false;
            }
            return importer.materialImportMode != ModelImporterMaterialImportMode.None && importer.materialLocation == ModelImporterMaterialLocation.External;
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
                return false; //  HasMissingMesh(mf); 
                // MeshFilter が存在する場合は MeshFilter 側でメッシュ設定を確認するのでここでは false を返却
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
            if (path == null || (!path.StartsWith("Assets/") && !path.StartsWith("Packages/")))
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

        public static bool HasNotEqualMaterialCount(MeshRenderer renderer)
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
            return renderer.sharedMaterials.Length != mf.sharedMesh.subMeshCount;
        }

        public static bool HasLessMaterialCount(MeshRenderer renderer)
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
            return renderer.sharedMaterials.Length < mf.sharedMesh.subMeshCount;
        }

        public static bool HasLessMaterialCount(SkinnedMeshRenderer renderer)
        {
            if (renderer == null || renderer.sharedMesh == null)
            {
                return false;
            }
            return renderer.sharedMaterials.Length < renderer.sharedMesh.subMeshCount;
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

        public static bool HasNaNMesh(MeshFilter mf)
        {
            if (mf == null || mf.sharedMesh == null)
            {
                return false;
            }
            var mesh = mf.sharedMesh;
            return mesh.vertices.Any(v => float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z))
                || mesh.normals.Any(v => float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z))
                || mesh.uv.Any(v => float.IsNaN(v.x) || float.IsNaN(v.y))
                || mesh.uv2.Any(v => float.IsNaN(v.x) || float.IsNaN(v.y));
        }

#endregion

    }
}

#endif
