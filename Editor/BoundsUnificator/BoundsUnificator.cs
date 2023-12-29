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

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using System.Text.RegularExpressions;

namespace WF.Tool.Avatar
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
        public Transform rootBone;
        public Transform anchorTarget;
        public Bounds bounds = new Bounds(Vector3.zero, Vector3.one * 2);
        public BoundsCalcMode calcMode = BoundsCalcMode.PrefabValue;
        public bool showWireFrame = true;

        Vector2 scroll = Vector2.zero;

        public List<SkinnedMeshRenderer> skinMeshRenderers = new List<SkinnedMeshRenderer>();
        public List<MeshRenderer> meshRenderers = new List<MeshRenderer>();

        public static void ShowWindow()
        {
            var window = EditorWindow.GetWindow<BoundsUnificator>(Title);
            window.SetSelection(Selection.GetFiltered(typeof(GameObject), SelectionMode.Editable | SelectionMode.ExcludePrefab));
        }

        internal enum BoundsCalcMode
        {
            PrefabValue,
            CurrentValueOnly,
            BoneTransform,
        }

        private void SetSelection(Object[] objects)
        {
            foreach (var obj in objects)
            {
                if (obj is GameObject)
                {
                    rootObject = (GameObject)obj;
                    DoGetObjectFromRoot();
                    break;
                }
            }
        }

        private SerializedObject serializedObject;

        private void OnEnable()
        {
            SceneView.duringSceneGui += OnSceneViewGUI;
            serializedObject = new SerializedObject(this);
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneViewGUI;
        }

        private void OnSceneViewGUI(SceneView sceneView)
        {
            if (rootBone == null)
            {
                return;
            }
            if (showWireFrame)
            {
                using (new Handles.DrawingScope(rootBone.localToWorldMatrix))
                {
                    Handles.DrawWireCube(bounds.center, bounds.size);
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
                DoGetObjectFromRoot();
            }

            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(skinMeshRenderers)), new GUIContent("SkinnedMeshRenderer (" + skinMeshRenderers.Count + ")"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(meshRenderers)), new GUIContent("MeshRenderer (" + meshRenderers.Count + ")"), true);
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(rootBone)), new GUIContent("RootBone (Hip)", "RootBoneに設定するTransform"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(anchorTarget)), new GUIContent("AnchorOverride", "AnchorOverrideに設定するTransform"));
            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(calcMode)), new GUIContent("Calc Method", "計算方法の指定"));

            using (new EditorGUI.DisabledGroupScope(rootObject == null || rootBone == null || skinMeshRenderers.Count == 0))
            {
                if (GUILayout.Button("Calculate Bounds"))
                {
                    serializedObject.ApplyModifiedProperties();
                    DoCalcBounds();
                }
            }

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(bounds)), new GUIContent("Bounds"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(showWireFrame)), new GUIContent("show wire frame", "Boundsの値をワイヤーフレームで表示する"));

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space();

            using (new EditorGUI.DisabledGroupScope(rootBone == null || anchorTarget == null))
            {
                GUI.color = new Color(0.75f, 0.75f, 1f);
                bool exec = GUILayout.Button("Apply Bounds To Renderer");
                GUI.color = oldColor;
                if (exec && ConfirmContinue())
                {
                    DoApplyBounds();
                }
            }

            // スクロール終了
            EditorGUILayout.EndScrollView();
        }

        private static bool ConfirmContinue()
        {
            return EditorUtility.DisplayDialog(Title, "Continue modify Objects?\nオブジェクトを変更しますか？", "OK", "CANCEL");
        }

        private void DoGetObjectFromRoot()
        {
            rootBone = null;
            anchorTarget = null;
            skinMeshRenderers = new List<SkinnedMeshRenderer>();
            meshRenderers = new List<MeshRenderer>();
            bounds = new Bounds(Vector3.zero, Vector3.one * 2);

            if (rootObject == null)
            {
                return;
            }

            var removeSpace = new Regex(@"\s+", RegexOptions.Compiled);
            foreach (var t in rootObject.GetComponentsInChildren<Transform>())
            {
                string name = removeSpace.Replace(t.name, "");
                if (string.Equals(name, "AnchorOverride", System.StringComparison.OrdinalIgnoreCase) || string.Equals(name, "AnchorTarget", System.StringComparison.OrdinalIgnoreCase))
                {
                    anchorTarget = t;
                    break;
                }
            }

            foreach (var anim in rootObject.GetComponentsInChildren<Animator>(true))
            {
                if (anim != null && anim.isHuman)
                {
                    rootBone = anim.GetBoneTransform(HumanBodyBones.Hips);
                    if (anchorTarget == null)
                    {
                        anchorTarget = anim.GetBoneTransform(HumanBodyBones.Chest);
                    }
                    break;
                }
            }
            if (rootBone == null)
            {
                rootBone = rootObject.transform;
            }
            if (anchorTarget == null)
            {
                anchorTarget = rootObject.transform;
            }

            skinMeshRenderers.AddRange(rootObject.GetComponentsInChildren<SkinnedMeshRenderer>(true));
            meshRenderers.AddRange(rootObject.GetComponentsInChildren<MeshRenderer>(true));
        }

        private IEnumerable<Vector3> IterWorldSpaceCorner(Bounds wb, Transform origBase, Transform newBase)
        {
            if (origBase == null)
            {
                origBase = newBase;
            }
            var scale = SafeDivide(newBase.lossyScale, origBase.lossyScale);
            return IterWorldSpaceCorner(wb)
                .Select(p => p + (newBase.position - origBase.position))
                .Select(p => LerpUnclamped(newBase.position, p, scale));
        }

        private static Vector3 SafeDivide(Vector3 x, Vector3 y)
        {
            return new Vector3(y.x == 0 ? 1 : x.x / y.x, y.y == 0 ? 1 : x.y / y.y, y.z == 0 ? 1 : x.z / y.z);
        }

        private static Vector3 LerpUnclamped(Vector3 b, Vector3 v, Vector3 scale)
        {
            return new Vector3(Mathf.LerpUnclamped(b.x, v.x, scale.x), Mathf.LerpUnclamped(b.y, v.y, scale.y), Mathf.LerpUnclamped(b.z, v.z, scale.z));
        }

        private IEnumerable<Vector3> IterWorldSpaceCorner(Bounds wb)
        {
            yield return new Vector3(wb.min.x, wb.min.y, wb.min.z);
            yield return new Vector3(wb.min.x, wb.min.y, wb.max.z);
            yield return new Vector3(wb.min.x, wb.max.y, wb.min.z);
            yield return new Vector3(wb.min.x, wb.max.y, wb.max.z);
            yield return new Vector3(wb.max.x, wb.min.y, wb.min.z);
            yield return new Vector3(wb.max.x, wb.min.y, wb.max.z);
            yield return new Vector3(wb.max.x, wb.max.y, wb.min.z);
            yield return new Vector3(wb.max.x, wb.max.y, wb.max.z);
        }

        private IEnumerable<Vector3> IterWorldSpaceCorner(Transform t)
        {
            if (t != null)
            {
                // 元座標を追加
                yield return t.position;
                if (t.parent != null)
                {
                    // 周辺座標を追加
                    var d = t.InverseTransformPoint(t.parent.position).magnitude;
                    yield return t.TransformPoint(new Vector3(0, 0, -d));
                    yield return t.TransformPoint(new Vector3(0, 0, +d));
                    yield return t.TransformPoint(new Vector3(0, -d, 0));
                    yield return t.TransformPoint(new Vector3(0, +d, 0));
                    yield return t.TransformPoint(new Vector3(-d, 0, 0));
                    yield return t.TransformPoint(new Vector3(+d, 0, 0));
                }
            }
        }

        private void DoCalcBounds()
        {
            var wsCorner = new List<Vector3>();
            foreach (var r in skinMeshRenderers)
            {
                if (r == null)
                {
                    continue;
                }
                if (IsSkinMeshRendererWithoutBones(r))
                {
                    continue;
                }

                switch (calcMode)
                {
                    case BoundsCalcMode.CurrentValueOnly:
                        // bounds の頂点8箇所のワールド座標を追加
                        if (r.bounds.extents != Vector3.zero)
                        {
                            wsCorner.AddRange(IterWorldSpaceCorner(r.bounds));
                        }
                        break;
                    case BoundsCalcMode.PrefabValue:
                        // Prefab側の bounds の頂点8箇所のワールド座標を追加
                        if (r.bounds.extents != Vector3.zero)
                        {
                            var orig = PrefabUtility.GetCorrespondingObjectFromOriginalSource(r);
                            if (orig != null)
                            {
                                wsCorner.AddRange(IterWorldSpaceCorner(orig.bounds, orig.rootBone, rootBone));
                            }
                            else
                            {
                                wsCorner.AddRange(IterWorldSpaceCorner(r.bounds));
                            }
                        }
                        break;
                    case BoundsCalcMode.BoneTransform:
                        // ボーンのワールド座標をすべて追加
                        foreach (var t in r.bones)
                        {
                            wsCorner.AddRange(IterWorldSpaceCorner(t));
                        }
                        break;
                }
            }
            if (wsCorner.Count == 0)
            {
                return;
            }

            // AABBの作成
            var rawBounds = CreateBounds(wsCorner.Select(rootBone.worldToLocalMatrix.MultiplyPoint));
            var center = rawBounds.center;
            var extents = rawBounds.extents;

            // centerの調整
            center.x = Mathf.RoundToInt(center.x * 100) / 100f;
            center.y = Mathf.RoundToInt(center.y * 100) / 100f;
            center.z = Mathf.RoundToInt(center.z * 100) / 100f;
            // extentsの拡張
            extents.x += Mathf.Abs(center.x - rawBounds.center.x);
            extents.y += Mathf.Abs(center.y - rawBounds.center.y);
            extents.z += Mathf.Abs(center.z - rawBounds.center.z);
            // extentsの調整
            extents.x = Mathf.RoundToInt(extents.x * 100) / 100f;
            extents.y = Mathf.RoundToInt(extents.y * 100) / 100f;
            extents.z = Mathf.RoundToInt(extents.z * 100) / 100f;

            // 作成したAABBをセット
            bounds = new Bounds(center, extents * 2);
        }

        private static Bounds CreateBounds(IEnumerable<Vector3> ps)
        {
            var result = new Bounds();
            if (ps.Count() != 0)
            {
                var min = ps.Aggregate(Vector3.Min);
                var max = ps.Aggregate(Vector3.Max);
                result.center = Vector3.Lerp(min, max, 0.5f);
                result.extents = max - result.center;
            }
            return result;
        }

        private bool IsSkinMeshRendererWithoutBones(SkinnedMeshRenderer r)
        {
            return r.bones == null || r.bones.Length == 0;
        }

        private void DoApplyBounds()
        {
            Undo.RecordObjects(skinMeshRenderers.Union<Component>(meshRenderers).Where(cmp => cmp != null).ToArray(), "set bounds");

            foreach (var r in skinMeshRenderers)
            {
                if (r == null)
                {
                    continue;
                }

                r.probeAnchor = anchorTarget;

                if (IsSkinMeshRendererWithoutBones(r))
                {
                    // ボーンで動いていない場合はリセットできない
                    continue;
                }

                // ボーンで動いている SkinnedMeshRenderer は、トランスフォームをリセット可能
                var transform = r.transform;
                transform.localPosition = Vector3.zero;
                transform.localRotation = Quaternion.identity;
                transform.localScale = Vector3.one;

                r.rootBone = rootBone;
                r.localBounds = bounds;
            }

            foreach (var r in meshRenderers)
            {
                if (r == null)
                {
                    continue;
                }
                r.probeAnchor = anchorTarget;
            }
        }
    }
}

#endif
