/*
 *  The MIT License
 *
 *  Copyright 2020 whiteflare.
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
using UnityEditor;
using UnityEditor.IMGUI.Controls;

namespace WF.Utillty
{
    internal class MeshPolyCounter : EditorWindow
    {
        [MenuItem("Tools/whiteflare/Mesh/Mesh Poly Counter")]
        public static void ShowWindow() {
            var window = GetWindow<MeshPolyCounter>("Mesh Poly Counter");
            var root = Selection.activeGameObject;
            if (root != null) {
                window.rootObject = root;
                window.UpdateTreeView();
            }
            window.Show();
        }

        public TreeViewState treeViewState;
        private MeshCountTreeView treeView;
        private GameObject rootObject;

        private void OnEnable() {
            if (treeViewState == null) {
                treeViewState = new TreeViewState();
            }
            treeView = new MeshCountTreeView(treeViewState);
            UpdateTreeView();
        }

        private void OnHierarchyChange() {
            UpdateTreeView();
        }

        private MeshCountInfo[] CountMeshes(GameObject root) {
            if (root == null) {
                return new MeshCountInfo[0];
            }
            return root.GetComponentsInChildren<MeshFilter>(true)
                    .Where(mf => mf.sharedMesh != null).Select(mf => new MeshCountInfo(mf, mf.sharedMesh))
                .Union(root.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                    .Where(smr => smr.sharedMesh != null).Select(smr => new MeshCountInfo(smr, smr.sharedMesh)))
                .OrderByDescending(mci => mci.poly)
                .ToArray();
        }

        private void UpdateTreeView() {
            treeView.items = CountMeshes(rootObject);
            treeView.SortItems();
            treeView.Reload();
        }

        private void OnGUI() {
            EditorGUILayout.Space();

            EditorGUI.BeginChangeCheck();
            rootObject = EditorGUILayout.ObjectField("Root Object", rootObject, typeof(GameObject), true) as GameObject;
            if (EditorGUI.EndChangeCheck()) {
                UpdateTreeView();
            }

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Total", "" + treeView.items.Select(mci => (int)mci.poly).Sum());
            EditorGUILayout.LabelField("Checked Sum", "" + treeView.items.Where(mci => mci.check).Select(mci => (int)mci.poly).Sum());
            EditorGUILayout.LabelField("Unchecked Sum", "" + treeView.items.Where(mci => !mci.check).Select(mci => (int)mci.poly).Sum());

            EditorGUILayout.Space();

            treeView.OnGUI(GUILayoutUtility.GetRect(0, float.MaxValue, 0, float.MaxValue));
        }

        internal class MeshCountInfo
        {
            public Component owner;
            public Mesh mesh;
            public uint poly;
            public bool check;

            public MeshCountInfo(Component owner, Mesh mesh) {
                this.owner = owner;
                this.mesh = mesh;

                if (mesh != null) {
                    if (mesh.isReadable) {
                        for (int i = 0; i < mesh.subMeshCount; i++) {
                            poly += mesh.GetIndexCount(i) / 3u;
                        }
                    }
                    else {
                        poly = uint.MaxValue;
                    }
                }
            }

            public object GetValue(int idx) {
                switch (idx) {
                    case 0:
                        return owner.name;
                    case 1:
                        return mesh.name;
                    case 2:
                        return poly;
                    default:
                        return null;
                }
            }
        }

        internal class MeshCountTreeView : TreeView
        {
            public MeshCountInfo[] items = new MeshCountInfo[0];

            public MeshCountTreeView(TreeViewState state) : base(state, NewHeader()) {
                multiColumnHeader.sortingChanged += OnSortingChanged;
            }

            private static MultiColumnHeader NewHeader() {
                var column = new List<MultiColumnHeaderState.Column>();
                column.Add(new MultiColumnHeaderState.Column {
                    headerContent = new GUIContent("GameObject"),
                    width = 120,
                });
                column.Add(new MultiColumnHeaderState.Column {
                    headerContent = new GUIContent("Mesh"),
                    width = 120,
                });
                column.Add(new MultiColumnHeaderState.Column {
                    headerContent = new GUIContent("Polygon"),
                    width = 120,
                });

                var state = new MultiColumnHeaderState(column.ToArray());
                var header = new MultiColumnHeader(state);

                return header;
            }

            protected override void DoubleClickedItem(int id) {
                var item = FindItem(id, rootItem) as ExTreeViewItem;
                if (item != null) {
                    Selection.activeGameObject = item.info.owner.gameObject;
                }
            }

            protected override TreeViewItem BuildRoot() {
                var id = 0;
                var root = new TreeViewItem { id = id++, depth = -1, displayName = "Root" };

                var list = new List<TreeViewItem>();
                foreach (var item in items) {
                    list.Add(new ExTreeViewItem { id = id++, depth = 0, displayName = item.owner.name, info = item });
                }

                SetupParentsAndChildrenFromDepths(root, list);
                return root;
            }

            protected override void RowGUI(RowGUIArgs args) {
                var item = (ExTreeViewItem)args.item;

                for (int i = 0; i < args.GetNumVisibleColumns(); i++) {
                    var cellRect = args.GetCellRect(i);
                    CenterRectUsingSingleLineHeight(ref cellRect);

                    int idx = args.GetColumn(i);
                    if (idx == 2) {
                        item.info.check = EditorGUI.ToggleLeft(cellRect, "" + item.info.poly, item.info.check);
                    }
                    else {
                        GUI.Label(cellRect, "" + item.info.GetValue(idx));
                    }
                }
            }

            public void SortItems() {
                var idx = multiColumnHeader.sortedColumnIndex;
                if (idx < 0) {
                    return;
                }
                items = (multiColumnHeader.IsSortedAscending(idx) ?
                    items.OrderBy(mci => mci.GetValue(idx)) :
                    items.OrderByDescending(mci => mci.GetValue(idx))).ToArray();
            }

            void OnSortingChanged(MultiColumnHeader header) {
                SortItems();
                Reload();
            }

            class ExTreeViewItem : TreeViewItem
            {
                public MeshCountInfo info;
            }
        }
    }
}

#endif
