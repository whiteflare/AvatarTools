/*
 *  The MIT License
 *
 *  Copyright 2022-2023 whiteflare.
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
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using VRC.Dynamics;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Dynamics.PhysBone.Components;
using VRC.SDK3.Dynamics.Contact.Components;

namespace WF.Tool.Avatar
{
    internal class AvatarCopyUtility : EditorWindow
    {
        private const string Title = "Avatar Copy Utility";

        public List<GameObject> oldBones = new List<GameObject>();
        public List<GameObject> newBones = new List<GameObject>();

        public List<GameObject> tAVD = new List<GameObject>();
        public List<GameObject> tSMR = new List<GameObject>();
        public List<GameObject> tMR = new List<GameObject>();
        public List<GameObject> tPB = new List<GameObject>();
        public List<GameObject> tPBC = new List<GameObject>();
        public List<GameObject> tCS = new List<GameObject>();
        public List<GameObject> tCR = new List<GameObject>();
        public List<GameObject> tPosC = new List<GameObject>();
        public List<GameObject> tRotC = new List<GameObject>();
        public List<GameObject> tScaC = new List<GameObject>();
        public List<GameObject> tParC = new List<GameObject>();
        public List<GameObject> tLkaC = new List<GameObject>();
        public List<GameObject> tAimC = new List<GameObject>();

        private readonly Type[] COPY_TARGET_TYPE = {
            typeof(VRCAvatarDescriptor),
            typeof(SkinnedMeshRenderer),
            typeof(MeshRenderer),
            typeof(VRCPhysBone),
            typeof(VRCPhysBoneCollider),
            typeof(VRCContactSender),
            typeof(VRCContactReceiver),
            typeof(PositionConstraint),
            typeof(RotationConstraint),
            typeof(ScaleConstraint),
            typeof(ParentConstraint),
            typeof(LookAtConstraint),
            typeof(AimConstraint),
        };

        private readonly List<string> COPY_FIELD_NAME = new List<string>()
        {
            "tAVD",
            "tSMR",
            "tMR",
            "tPB",
            "tPBC",
            "tCS",
            "tCR",
            "tPosC",
            "tRotC",
            "tScaC",
            "tParC",
            "tLkaC",
            "tAimC",
        };

        private readonly List<List<GameObject>> targetComponents = new List<List<GameObject>>();
        private readonly List<bool[]> checkComponents = new List<bool[]>();

        private readonly List<SerializedProperty> propList = new List<SerializedProperty>();

        private GameObject srcRoot = null;
        private GameObject dstRoot = null;
        private int tabIndex = 0;
        private Vector2 _scrollPos = Vector2.zero;

        [MenuItem("Tools/whiteflare/Avatar Copy Utility")]
        public static void Create()
        {
            GetWindow<AvatarCopyUtility>(Title);
        }

        public void OnEnable()
        {
            targetComponents.Clear();
            checkComponents.Clear();
            foreach (var t in COPY_TARGET_TYPE)
            {
                targetComponents.Add(new List<GameObject>());
                checkComponents.Add(new bool[0]);
            }

            propList.Clear();
            var so = new SerializedObject(this);
            foreach (var fn in COPY_FIELD_NAME)
            {
                propList.Add(so.FindProperty(fn));
            }
            foreach (var p in propList)
            {
                p.isExpanded = false;
            }

            DoFindCopyTarget();
        }

        #region GUI

        private void OnGUI()
        {
            var oldColor = GUI.color;

            ////////////////////
            // 対象の設定
            ////////////////////

            EditorGUILayout.Space();

            var so = new SerializedObject(this);
            so.Update();

            EditorGUI.BeginChangeCheck();

            EditorGUI.BeginChangeCheck();
            srcRoot = EditorGUILayout.ObjectField(new GUIContent("コピー元 Avatar (From)"), srcRoot, typeof(GameObject), true) as GameObject;
            if (EditorGUI.EndChangeCheck())
            {
                DoFindCopyTarget();
            }

            dstRoot = EditorGUILayout.ObjectField(new GUIContent("コピー先 Avatar (To)"), dstRoot, typeof(GameObject), true) as GameObject;
            if (EditorGUI.EndChangeCheck())
            {
                DoFillOldBones();
                DoFillNewBonesByName();
            }

            EditorGUILayout.Space();

            tabIndex = GUILayout.Toolbar(tabIndex, new string[] { "Copy Target", "Bone Mapping" }, new GUIStyle("LargeButton"), GUI.ToolbarButtonSize.FitToContents);

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            if (tabIndex == 0)
            {
                if (targetComponents.Any(lst => 0 < lst.Count))
                {
                    EditorGUILayout.HelpBox("コピーする対象コンポーネントを選択してください。", MessageType.Info);
                    EditorGUILayout.Space();

                    for (int i = 0; i < COPY_TARGET_TYPE.Length; i++)
                    {
                        var tgt = targetComponents[i];
                        if (tgt.Count != 0)
                        {
                            checkComponents[i] = CheckedPropertyField(checkComponents[i], tgt, propList[i], new GUIContent(COPY_TARGET_TYPE[i].Name), item =>
                            {
                                for (int j = 0; j < oldBones.Count; j++)
                                {
                                    if (oldBones[j] == item)
                                    {
                                        return newBones[j] == null;
                                    }
                                }
                                return false;
                            });
                            EditorGUILayout.Space();
                        }
                    }
                }
            }
            else if (tabIndex == 1)
            {
                if (0 < oldBones.Count)
                {
                    EditorGUILayout.HelpBox("GameObject のマッピングをカスタマイズすることができます。", MessageType.Info);
                    EditorGUILayout.Space();
                    PairListPropertyField(oldBones, newBones, so.FindProperty(nameof(oldBones)), new GUIContent("bone remapping"), "Old Bone ", "New Bone");
                }
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();

            if (GUILayout.Button("全てチェック"))
            {
                foreach (var chk in checkComponents)
                {
                    for (int i = 0; i < chk.Length; i++)
                    {
                        chk[i] = true;
                    }
                }
            }

            EditorGUI.BeginDisabledGroup(IsNotReadyCopy());

            GUI.color = new Color(0.75f, 0.75f, 1f);
            bool apply = GUILayout.Button("コピー");
            GUI.color = oldColor;

            EditorGUI.EndDisabledGroup();

            if (apply && ConfirmContinue())
            {
                ExecuteCopy();
            }

            EditorGUILayout.Space();
        }

        private bool IsNotReadyCopy()
        {
            if (dstRoot == null)
            {
                return true;
            }
            if (oldBones.Count == 0 || newBones.Count == 0)
            {
                return true;
            }
            return !checkComponents.SelectMany(lst => lst).Any(chk => chk);
        }

        public static bool[] CheckedPropertyField<T>(bool[] check, List<T> list, SerializedProperty prop, GUIContent labelArray, Func<T, bool> isHighlight) where T : UnityEngine.Object
        {
            if (check.Length != list.Count)
            {
                Array.Resize(ref check, list.Count);
            }

            var oldColor = GUI.color;
            var color_attention = new Color(1, 1, 0.75f);

            var rect = EditorGUILayout.GetControlRect();
            rect.x += 16;
            EditorGUI.PropertyField(rect, prop, labelArray, false);

            rect.x -= 16;
            rect.width = 16;
            EditorGUI.showMixedValue = check.Distinct().Count() == 2;
            EditorGUI.BeginChangeCheck();
            var allCheck = EditorGUI.ToggleLeft(rect, "", check.Any(v => v));
            if (EditorGUI.EndChangeCheck())
            {
                for (int i = 0; i < check.Length; i++)
                {
                    check[i] = allCheck;
                }
            }
            EditorGUI.showMixedValue = false;

            if (prop.isExpanded)
            {
                EditorGUI.indentLevel++;

                EditorGUI.BeginChangeCheck();

                int size = Math.Max(0, EditorGUILayout.DelayedIntField("Size", list.Count));
                var newList = new T[size];
                if (check.Length != size)
                {
                    Array.Resize(ref check, size);
                }
                for (int i = 0; i < size; i++)
                {
                    var item = i < list.Count ? list[i] : null;

                    rect = EditorGUILayout.GetControlRect();
                    rect.x += 16;

                    if (isHighlight(item))
                    {
                        GUI.color = color_attention;
                    }
                    newList[i] = EditorGUI.ObjectField(rect, new GUIContent("array " + i), item, typeof(T), true) as T;
                    GUI.color = oldColor;

                    rect.x -= 16;
                    rect.width = 120;
                    check[i] = EditorGUI.ToggleLeft(rect, "", check[i]);
                }

                if (EditorGUI.EndChangeCheck())
                {
                    list.Clear();
                    list.AddRange(newList);
                }

                EditorGUI.indentLevel--;
            }

            return check;
        }

        public static void PairListPropertyField<T>(List<T> firstList, List<T> secondList, SerializedProperty firstProp, GUIContent labelArray, string labelFirst, string labelSecond) where T : UnityEngine.Object
        {
            var oldColor = GUI.color;

            EditorGUILayout.PropertyField(firstProp, labelArray, false);
            if (firstProp.isExpanded)
            {
                EditorGUI.indentLevel++;

                EditorGUI.BeginChangeCheck();

                var color_attention = new Color(1, 1, 0.75f);

                int size = Math.Max(0, EditorGUILayout.DelayedIntField("Size of Pair", Math.Max(firstList.Count, secondList.Count)));
                var firstList2 = new T[size];
                var secondList2 = new T[size];

                for (int i = 0; i < size; i++)
                {
                    var src = i < firstList.Count ? firstList[i] : null;
                    var dst = i < secondList.Count ? secondList[i] : null;

                    GUILayoutUtility.GetRect(6, 1);

                    if (src == null)
                    {
                        GUI.color = color_attention;
                    }
                    firstList2[i] = EditorGUILayout.ObjectField(new GUIContent(labelFirst + i), src, typeof(T), true) as T;
                    GUI.color = oldColor;

                    if (dst == null)
                    {
                        GUI.color = color_attention;
                    }
                    secondList2[i] = EditorGUILayout.ObjectField(new GUIContent(labelSecond + i), dst, typeof(T), true) as T;
                    GUI.color = oldColor;
                }

                if (EditorGUI.EndChangeCheck())
                {
                    firstList.Clear();
                    firstList.AddRange(firstList2);
                    secondList.Clear();
                    secondList.AddRange(secondList2);
                }

                EditorGUI.indentLevel--;
            }
        }

        #endregion

        private void DoFindCopyTarget()
        {
            for (int i = 0; i < targetComponents.Count; i++)
            {
                targetComponents[i].Clear();
            }

            if (srcRoot == null)
            {
                return;
            }

            for (int i = 0; i < targetComponents.Count; i++)
            {
                targetComponents[i].AddRange(srcRoot.GetComponentsInChildren(COPY_TARGET_TYPE[i], true).Select(cmp => cmp.gameObject));
            }

            // 入れ替えた後はすべてチェックを外す
            foreach (var chk in checkComponents)
            {
                for (int i = 0; i < chk.Length; i++)
                {
                    chk[i] = false;
                }
            }
        }

        #region Remapping編集

        private void DoFillOldBones()
        {
            oldBones.Clear();

            if (srcRoot == null)
            {
                return;
            }

            // Source から Transform を全て列挙
            oldBones.AddRange(srcRoot.GetComponentsInChildren<Transform>(true).Select(t => t.gameObject));
        }

        class ResolveGoWork
        {
            public readonly GameObject gameObject;
            public readonly string[] pathString;
            public readonly string absolutePathString;

            public readonly bool isRoot;
            public readonly bool isArmature;

            public GameObject resolvedDstGo = null;
            public GameObject resolvedSrcGo = null;

            public ResolveGoWork(GameObject gameObject, bool isRoot, bool isArmature, string[] pathString)
            {
                this.gameObject = gameObject;
                this.isRoot = isRoot;
                this.isArmature = isArmature;
                this.pathString = pathString;
                this.absolutePathString = pathString[pathString.Length - 1];
            }
        }

        private List<ResolveGoWork> CreateGoStatusList(List<GameObject> list, GameObject root)
        {
            var animT = GetAnimationRootTransforms(root).GetComponentsInChildren<Transform>(true);

            var result = new List<ResolveGoWork>();
            foreach(var go in list)
            {
                var isRoot = root == go;
                var isArmature = animT.Contains(go.transform);
                var pathString = IterAllPathString(root.transform, go.transform).ToArray();
                var wk = new ResolveGoWork(go, isRoot, isArmature, pathString);
                result.Add(wk);
            }
            return result;
        }

        private Dictionary<string, List<ResolveGoWork>> CreatePathStringMap(List<ResolveGoWork> list, Func<ResolveGoWork, bool> cond)
        {
            var dic = new Dictionary<string, List<ResolveGoWork>>();
            foreach (var wk in list)
            {
                if (!cond(wk))
                {
                    continue;
                }
                foreach (var name in wk.pathString)
                {
                    if (!dic.ContainsKey(name))
                    {
                        dic.Add(name, new List<ResolveGoWork>());
                    }
                    dic[name].Add(wk);
                }
            }
            return dic;
        }

        private Dictionary<string, List<ResolveGoWork>> CreateAbsolutePathMap(List<ResolveGoWork> list, Func<ResolveGoWork, bool> cond)
        {
            var dic = new Dictionary<string, List<ResolveGoWork>>();
            foreach (var wk in list)
            {
                if (!cond(wk))
                {
                    continue;
                }
                var name = wk.absolutePathString;
                if (!dic.ContainsKey(name))
                {
                    dic.Add(name, new List<ResolveGoWork>());
                }
                dic[name].Add(wk);
            }
            return dic;
        }

        private void ResolveMappingStrict(List<ResolveGoWork> srcWkList, List<ResolveGoWork> dstWkList, Func<ResolveGoWork, bool> cond)
        {
            var mapDst = CreateAbsolutePathMap(dstWkList, cond);

            // パスの部分一致でチェック
            foreach (var srcWk in srcWkList)
            {
                // 解決済みなら何もしない
                if (srcWk.resolvedDstGo != null)
                {
                    continue;
                }
                // 条件が違うならば何もしない
                if (!cond(srcWk))
                {
                    continue;
                }
                // それ以外は名前でマッチ
                var name = srcWk.absolutePathString;
                if (mapDst.TryGetValue(name, out var list))
                {
                    // それ以外は名前でマッチ
                    var list2 = list.Where(wk => wk.resolvedSrcGo == null).ToList();
                    if (list2.Count == 1)
                    {
                        var dstWk = list2[0];
                        srcWk.resolvedDstGo = dstWk.gameObject;
                        dstWk.resolvedSrcGo = srcWk.gameObject;
                    }
                }
            }
        }

        private void ResolveMapping(List<ResolveGoWork> srcWkList, List<ResolveGoWork> dstWkList, Func<ResolveGoWork, bool> cond)
        {
            var mapDst = CreatePathStringMap(dstWkList, cond);

            // パスの部分一致でチェック
            foreach (var srcWk in srcWkList)
            {
                // 解決済みなら何もしない
                if (srcWk.resolvedDstGo != null)
                {
                    continue;
                }
                // 条件が違うならば何もしない
                if (!cond(srcWk))
                {
                    continue;
                }
                // それ以外は名前でマッチ
                foreach (var name in srcWk.pathString)
                {
                    if (mapDst.TryGetValue(name, out var list))
                    {
                        // それ以外は名前でマッチ
                        var list2 = list.Where(cond).Where(wk => wk.resolvedSrcGo == null).ToList();
                        if (list2.Count() == 1)
                        {
                            var dstWk = list2[0];
                            srcWk.resolvedDstGo = dstWk.gameObject;
                            dstWk.resolvedSrcGo = srcWk.gameObject;
                            break;
                        }
                    }
                }
            }
        }

        private void DoFillNewBonesByName()
        {
            newBones.Clear();
            // Source と同じ長さになるまで Destination 拡張
            while (newBones.Count < oldBones.Count)
            {
                newBones.Add(null);
            }

            if (srcRoot == null || dstRoot == null)
            {
                return;
            }

            var srcWkList = CreateGoStatusList(oldBones, srcRoot);
            var dstWkList = CreateGoStatusList(dstRoot.GetComponentsInChildren<Transform>(true).Select(cmp => cmp.gameObject).ToList(), dstRoot);

            // マッピング作成
            ResolveMappingStrict(srcWkList, dstWkList, sts => !sts.isRoot && sts.isArmature); // アーマチュア内をマッチング
            ResolveMappingStrict(srcWkList, dstWkList, sts => !sts.isRoot && !sts.isArmature); // アーマチュア外をマッチング
            ResolveMappingStrict(srcWkList, dstWkList, sts => !sts.isRoot); // 見つからなかったものは全域でマッチング
            ResolveMapping(srcWkList, dstWkList, sts => !sts.isRoot && sts.isArmature); // アーマチュア内をマッチング
            ResolveMapping(srcWkList, dstWkList, sts => !sts.isRoot && !sts.isArmature); // アーマチュア外をマッチング
            ResolveMapping(srcWkList, dstWkList, sts => !sts.isRoot); // 見つからなかったものは全域でマッチング

            // Source と一致する名前を検索して割り当て
            for (int i = 0; i < oldBones.Count(); i++)
            {
                var src = oldBones[i];
                var dst = newBones[i];
                if (src == null || dst != null)
                {
                    continue;
                }
                // srcRoot だけは dstRoot とマッチさせる
                // それ以外はマッピング結果から拾ってくる
                newBones[i] = srcRoot == src ? dstRoot : srcWkList.Where(sts => sts.gameObject == src).Select(sts => sts.resolvedDstGo).FirstOrDefault();
            }
        }

        private IEnumerable<string> IterAllPathString(Transform root, Transform obj)
        {
            var path = "";
            while (obj != null && obj.name != null)
            {
                if (obj == root)
                    path += "/AvatarRoot";
                else
                    path += obj.name;
                yield return path;

                path += "\n";
                obj = obj.parent;
            }
            yield break;
        }

        #endregion

        #region コンポーネント変更

        private static bool ConfirmContinue()
        {
            return EditorUtility.DisplayDialog(Title, "Continue modify Objects?\nオブジェクトを変更しますか？", "OK", "CANCEL");
        }

        private void RegisterRemapComponent(Dictionary<Component, Component> cmpRemap, Type t, GameObject src, GameObject dst)
        {
            if (src != null && dst != null)
            {
                var srcArray = src.GetComponents(t);
                var dstArray = dst.GetComponents(t);
                if (dstArray.Length < srcArray.Length)
                {
                    for (int i = dstArray.Length; i < srcArray.Length; i++)
                    {
                        Undo.AddComponent(dst.gameObject, t);
                    }
                    dstArray = dst.GetComponents(t);
                }
                int size = Math.Min(srcArray.Length, dstArray.Length);
                for (int i = 0; i < size; i++)
                {
                    cmpRemap[srcArray[i]] = dstArray[i];
                }
            }
        }

        private GameObject PrepareGameObject(Dictionary<GameObject, GameObject> mapRemap, GameObject srcGo, bool deepCreate)
        {
            if (srcGo == null)
            {
                return null;
            }

            // remap に入っているのでそれを返却
            if (mapRemap.TryGetValue(srcGo, out var dstGo) && dstGo != null)
            {
                return dstGo;
            }

            // src の親を準備する
            if (srcGo.transform.parent == null)
            {
                return null;
            }
            var srcParentGo = srcGo.transform.parent.gameObject;
            if (deepCreate && PrepareGameObject(mapRemap, srcParentGo, deepCreate) == null)
            {
                return null;
            }

            if (mapRemap.TryGetValue(srcParentGo, out var dstParentGo) && dstParentGo != null)
            {
                // src の親が remap に入っているので、dst の親を取得して Create して remap に入れて返却
                var newGo = new GameObject();
                Undo.RegisterCreatedObjectUndo(newGo, "Transfer Armature");
                newGo.transform.parent = dstParentGo.transform;

                CopyGameObject(srcGo, newGo);
                mapRemap[srcGo] = newGo;

                return newGo;
            }
            return null;
        }

        private void CopyGameObject(GameObject srcGo, GameObject newGo)
        {
            newGo.name = srcGo.name;
            newGo.tag = srcGo.tag;
            newGo.layer = srcGo.layer;
            GameObjectUtility.SetStaticEditorFlags(newGo, GameObjectUtility.GetStaticEditorFlags(srcGo));

            newGo.transform.localPosition = srcGo.transform.localPosition;
            newGo.transform.localRotation = srcGo.transform.localRotation;
            newGo.transform.localScale = srcGo.transform.localScale;

            newGo.SetActive(srcGo.activeSelf);
        }

        private bool IsCopyTargetGameObject(GameObject go, List<GameObject> list, bool[] check)
        {
            int length = Math.Min(list.Count, check.Length);
            for (int i = 0; i < length; i++)
            {
                if (check[i] && list[i] == go)
                {
                    return true;
                }
            }
            return false;
        }

        private bool IsCopyTargetGameObject(GameObject go, Type t)
        {
            for (int i = 0; i < targetComponents.Count; i++)
            {
                if (COPY_TARGET_TYPE[i] == t && IsCopyTargetGameObject(go, targetComponents[i], checkComponents[i]))
                {
                    return true;
                }
            }
            return false;
        }

        private bool IsCopyTargetGameObject(GameObject go)
        {
            for (int i = 0; i < targetComponents.Count; i++)
            {
                if (IsCopyTargetGameObject(go, targetComponents[i], checkComponents[i]))
                {
                    return true;
                }
            }
            return false;
        }

        private List<T> GetCopyTargetComponents<T>() where T : Component
        {
            var result = new List<T>();
            for (int i = 0; i < targetComponents.Count; i++)
            {
                if (COPY_TARGET_TYPE[i] == typeof(T))
                {
                    var tgt = targetComponents[i];
                    var chk = checkComponents[i];
                    for (int j = 0; j < tgt.Count; j++)
                    {
                        if (chk[j])
                        {
                            result.AddRange(tgt[j].GetComponents<T>());
                        }
                    }
                }
            }
            return result.Distinct().ToList();
        }

        private List<Transform> GetPhysBoneAffectedTransforms(List<VRCPhysBone> bones)
        {
            var result = new List<Transform>();
            foreach (var bone in bones)
            {
                result.AddRange(GetPhysBoneAffectedTransforms(bone));
            }
            return result.Distinct().ToList();
        }

        private List<Transform> GetPhysBoneAffectedTransforms(VRCPhysBone bone)
        {
            var result = new List<Transform>();
            var root = bone.rootTransform != null ? bone.rootTransform : bone.gameObject.transform; // ?? 演算子は使えない
            // root 配下を全て追加
            result.AddRange(root.GetComponentsInChildren<Transform>(true));
            foreach (var ignores in bone.ignoreTransforms)
            {
                if (ignores != null)
                {
                    // ignores 配下を全て削除
                    foreach (var ig in ignores.GetComponentsInChildren<Transform>(true))
                    {
                        result.Remove(ig);
                    }
                }
            }
            return result;
        }

        private void ExecuteCopy()
        {
            // Undo
            Undo.RegisterFullObjectHierarchyUndo(dstRoot, "Transfer Armature");

            var remapGo = new Dictionary<GameObject, GameObject>();
            for (int i = 0; i < oldBones.Count(); i++)
            {
                var src = i < oldBones.Count ? oldBones[i] : null;
                if (src != null)
                {
                    var dst = i < newBones.Count ? newBones[i] : null;
                    remapGo[src] = dst != null ? dst : null;
                }
            }

            // Transformのコピー
            foreach (var ent in remapGo)
            {
                if (!string.IsNullOrEmpty(ent.Key.name) && ent.Key.name.StartsWith("VRCLeafTipBone"))
                {
                    if (ent.Value != null)
                    {
                        CopyTransformIfDifference(ent.Key.transform, ent.Value.transform);
                    }
                }
            }

            // GameObject の用意
            foreach (var ent in remapGo.ToArray())
            {
                if (ent.Value == null)
                {
                    var src = ent.Key;
                    if (IsCopyTargetGameObject(src))
                    {
                        PrepareGameObject(remapGo, src, true);
                    }
                }
            }
            // SkinnedMeshRenderer の bones に指定されている GameObject の用意
            var smrBones = GetAllSkinnedMeshBones(GetCopyTargetComponents<SkinnedMeshRenderer>());
            foreach (var bone in smrBones)
            {
                PrepareGameObject(remapGo, bone.gameObject, true);
            }
            // VRCLeafTipBone の用意
            var pbBones = GetPhysBoneAffectedTransforms(GetCopyTargetComponents<VRCPhysBone>());
            foreach (var ent in remapGo.ToArray())
            {
                if (ent.Value == null)
                {
                    var src = ent.Key;
                    if (!string.IsNullOrEmpty(src.name) && src.name.StartsWith("VRCLeafTipBone") && pbBones.Contains(src.transform))
                    {
                        PrepareGameObject(remapGo, src, false); // 親Transformも無い場合はスキップ
                    }
                }
            }

            var remapCmp = new Dictionary<Component, Component>();

            // コンポーネントの用意
            foreach (var ent in remapGo)
            {
                if (ent.Value != null)
                {
                    foreach (var t in COPY_TARGET_TYPE)
                    {
                        if (IsCopyTargetGameObject(ent.Key, t))
                        {
                            RegisterRemapComponent(remapCmp, t, ent.Key, ent.Value);
                            if (t == typeof(MeshRenderer))
                            {
                                // もしコピーしようとしているのが MeshRenderer であれば、MeshFilter も一緒にコピーする
                                RegisterRemapComponent(remapCmp, typeof(MeshFilter), ent.Key, ent.Value);
                            }
                        }
                    }
                }
            }

            // 値のコピー
            foreach (var ent in remapCmp)
            {
                EditorUtility.CopySerialized(ent.Key, ent.Value);
            }

            // 参照の再接続
            foreach (var dst in remapCmp.Values)
            {
                UpdateReference(remapGo, dst);
            }

            // dst 側の Humanoid に紐づいている bone リスト
            var dstBoneList = GetHumanoidBoneTransforms(dstRoot);

            // Transformのコピー
            foreach (var dst in remapCmp.Values)
            {
                if (dst is VRCPhysBoneCollider dstPBC)
                {
                    // コライダーの座標を合わせる
                    var t = dstPBC.rootTransform != null ? dstPBC.rootTransform : dstPBC.transform;
                    // ただしHumanoidボーンに割り当てられているTransformは変更しない
                    if (!dstBoneList.Contains(t))
                    {
                        SyncTransform(remapGo, t);
                    }
                }
            }

            // 参照チェックと警告ログ
            var dstAllTransforms = dstRoot.GetComponentsInChildren<Transform>(true);
            foreach (var dst in remapCmp.Values)
            {
                CheckReference(dstAllTransforms, dst);
            }

            // dest の書き戻し
            for (int i = 0; i < newBones.Count; i++)
            {
                var dst = newBones[i];
                var src = i < oldBones.Count ? oldBones[i] : null;
                if (src != null && dst == null && remapGo.TryGetValue(src, out var newDst))
                {
                    newBones[i] = newDst;
                }
            }
        }

        private void SyncTransform(Dictionary<GameObject, GameObject> map, Transform dstT)
        {
            if (dstT != null)
            {
                // dst から src を逆引き
                foreach (var ent in map)
                {
                    if (ent.Value == dstT)
                    {
                        CopyTransformIfDifference(ent.Key.transform, dstT);
                    }
                }
            }
        }

        private List<Transform> GetHumanoidBoneTransforms(GameObject go)
        {
            var result = new List<Transform>();
            foreach (var anim in go.GetComponentsInChildren<Animator>(true))
            {
                if (anim.isHuman)
                {
                    foreach (var bone in (HumanBodyBones[])Enum.GetValues(typeof(HumanBodyBones)))
                    {
                        if (bone == HumanBodyBones.LastBone)
                        {
                            continue;
                        }
                        var t = anim.GetBoneTransform(bone);
                        if (t != null)
                        {
                            result.Add(t);
                        }
                    }
                }
            }
            return result;
        }

        private List<Transform> GetAllSkinnedMeshBones(List<SkinnedMeshRenderer> list)
        {
            return list.SelectMany(smr => smr.bones).Where(b => b != null).Distinct().ToList();
        }

        private Transform GetAnimationRootTransforms(GameObject go)
        {
            foreach (var anim in go.GetComponentsInChildren<Animator>(true))
            {
                if (anim.isHuman)
                {
                    return anim.GetBoneTransform(HumanBodyBones.Hips);
                }
            }
            return go.transform;
        }

        private static bool CopyTransformIfDifference(Transform src, Transform dst)
        {
            bool modified = false;
            if (0.001 < (dst.position - src.position).magnitude)
            {
                modified = true;
                dst.position = src.position;    // ワールド座標系で一致するようにコピー
                var lp = dst.localPosition; // もしローカル座標系で0に近い値だったら0にしてしまう
                lp.x = Mathf.Abs(lp.x) < 1e-5f ? 0 : lp.x;
                lp.y = Mathf.Abs(lp.y) < 1e-5f ? 0 : lp.y;
                lp.z = Mathf.Abs(lp.z) < 1e-5f ? 0 : lp.z;
                dst.localPosition = lp;
            }
            if (0.01 < (dst.transform.rotation.eulerAngles - src.transform.rotation.eulerAngles).magnitude)
            {
                modified = true;
                dst.transform.rotation = src.transform.rotation;
            }
            return modified;
        }

        private bool UpdateReference(Dictionary<GameObject, GameObject> goRemap, Component cmp)
        {
            // PhysBone
            if (cmp is VRCPhysBone pbDst)
            {
                UpdateReference(goRemap, pbDst.rootTransform, t => pbDst.rootTransform = t);
                UpdateReference(goRemap, pbDst.ignoreTransforms);
                UpdateReference<VRCPhysBoneColliderBase>(goRemap, pbDst.colliders);
            }
            if (cmp is VRCPhysBoneCollider pcDst)
            {
                UpdateReference(goRemap, pcDst.rootTransform, t => pcDst.rootTransform = t);
            }

            // Contact
            if (cmp is VRCContactSender cnsDst)
            {
                UpdateReference(goRemap, cnsDst.rootTransform, t => cnsDst.rootTransform = t);
            }
            if (cmp is VRCContactReceiver cnrDst)
            {
                UpdateReference(goRemap, cnrDst.rootTransform, t => cnrDst.rootTransform = t);
            }

            // Constraint
            if (cmp is IConstraint csDst)
            {
                UpdateReference(goRemap, csDst);
            }
            if (cmp is LookAtConstraint lkaDst)
            {
                UpdateReference(goRemap, lkaDst.worldUpObject, t => lkaDst.worldUpObject = t);
            }
            if (cmp is AimConstraint aimDst)
            {
                UpdateReference(goRemap, aimDst.worldUpObject, t => aimDst.worldUpObject = t);
            }

            // Renderer
            if (cmp is SkinnedMeshRenderer smr)
            {
                UpdateReference(goRemap, smr.probeAnchor, t => smr.probeAnchor = t);
                UpdateReference(goRemap, smr.rootBone, t => smr.rootBone = t);
                UpdateReference(goRemap, smr.bones, ts => smr.bones = ts);  // bones には Transform[] の再設定が必要
            }
            if (cmp is MeshRenderer mr)
            {
                UpdateReference(goRemap, mr.probeAnchor, t => mr.probeAnchor = t);
            }

            // VRCAvatarDescriptor
            if (cmp is VRCAvatarDescriptor avd)
            {
                UpdateReference(goRemap, avd.VisemeSkinnedMesh, t => avd.VisemeSkinnedMesh = t);
                UpdateReference(goRemap, avd.lipSyncJawBone, t => avd.lipSyncJawBone = t);
                if (avd.enableEyeLook)
                {
                    var eyeLook = avd.customEyeLookSettings;
                    UpdateReference(goRemap, eyeLook.leftEye, t => eyeLook.leftEye = t);
                    UpdateReference(goRemap, eyeLook.rightEye, t => eyeLook.rightEye = t);
                    UpdateReference(goRemap, eyeLook.eyelidsSkinnedMesh, t => eyeLook.eyelidsSkinnedMesh = t);
                    avd.customEyeLookSettings = eyeLook;
                }
                UpdateReference(goRemap, avd.collider_fingerIndexL.transform, t => avd.collider_fingerIndexL.transform = t);
                UpdateReference(goRemap, avd.collider_fingerIndexR.transform, t => avd.collider_fingerIndexR.transform = t);
                UpdateReference(goRemap, avd.collider_fingerLittleL.transform, t => avd.collider_fingerLittleL.transform = t);
                UpdateReference(goRemap, avd.collider_fingerLittleR.transform, t => avd.collider_fingerLittleR.transform = t);
                UpdateReference(goRemap, avd.collider_fingerMiddleL.transform, t => avd.collider_fingerMiddleL.transform = t);
                UpdateReference(goRemap, avd.collider_fingerMiddleR.transform, t => avd.collider_fingerMiddleR.transform = t);
                UpdateReference(goRemap, avd.collider_fingerRingL.transform, t => avd.collider_fingerRingL.transform = t);
                UpdateReference(goRemap, avd.collider_fingerRingR.transform, t => avd.collider_fingerRingR.transform = t);
                UpdateReference(goRemap, avd.collider_footL.transform, t => avd.collider_footL.transform = t);
                UpdateReference(goRemap, avd.collider_footR.transform, t => avd.collider_footR.transform = t);
                UpdateReference(goRemap, avd.collider_handL.transform, t => avd.collider_handL.transform = t);
                UpdateReference(goRemap, avd.collider_handR.transform, t => avd.collider_handR.transform = t);
                UpdateReference(goRemap, avd.collider_torso.transform, t => avd.collider_torso.transform = t);
            }

            return false;
        }

        private bool IsOutOfAvatar<T>(Transform[] dstAllTransforms, T cmp) where T : Component
        {
            if (cmp == null)
            {
                return false;
            }
            return !dstAllTransforms.Contains(cmp.transform);
        }

        private bool IsOutOfAvatar<T>(Transform[] dstAllTransforms, IEnumerable<T> ts) where T : Component
        {
            if (ts == null)
            {
                return false;
            }
            return ts.Where(t => t != null).Any(t => !dstAllTransforms.Contains(t.transform));
        }

        private void CheckReference(Transform[] dstAllTransforms, Component cmp)
        {
            // PhysBone
            if (cmp is VRCPhysBone pbDst)
            {
                if (IsOutOfAvatar(dstAllTransforms, pbDst.rootTransform))
                {
                    Debug.LogWarningFormat(cmp, "{0} の rootTransform がアバター外を参照しています。", cmp);
                }
                if (IsOutOfAvatar(dstAllTransforms, pbDst.ignoreTransforms))
                {
                    Debug.LogWarningFormat(cmp, "{0} の ignoreTransforms がアバター外を参照しています。", cmp);
                }
                if (IsOutOfAvatar(dstAllTransforms, pbDst.colliders))
                {
                    Debug.LogWarningFormat(cmp, "{0} の colliders がアバター外を参照しています。", cmp);
                }
            }
            if (cmp is VRCPhysBoneCollider pcDst)
            {
                if (IsOutOfAvatar(dstAllTransforms, pcDst.rootTransform))
                {
                    Debug.LogWarningFormat(cmp, "{0} の rootTransform がアバター外を参照しています。", cmp);
                }
            }

            // Contact
            if (cmp is VRCContactSender cnsDst)
            {
                if (IsOutOfAvatar(dstAllTransforms, cnsDst.rootTransform))
                {
                    Debug.LogWarningFormat(cmp, "{0} の rootTransform がアバター外を参照しています。", cmp);
                }
            }
            if (cmp is VRCContactReceiver cnrDst)
            {
                if (IsOutOfAvatar(dstAllTransforms, cnrDst.rootTransform))
                {
                    Debug.LogWarningFormat(cmp, "{0} の rootTransform がアバター外を参照しています。", cmp);
                }
            }

            // Constraint
            if (cmp is IConstraint csDst)
            {
                var list = new List<ConstraintSource>();
                csDst.GetSources(list);
                if (IsOutOfAvatar(dstAllTransforms, list.Select(t => t.sourceTransform)))
                {
                    Debug.LogWarningFormat(cmp, "{0} の ConstraintSource がアバター外を参照しています。", cmp);
                }
            }
            if (cmp is LookAtConstraint lkaDst)
            {
                if (IsOutOfAvatar(dstAllTransforms, lkaDst.worldUpObject))
                {
                    Debug.LogWarningFormat(cmp, "{0} の worldUpObject がアバター外を参照しています。", cmp);
                }
            }
            if (cmp is AimConstraint aimDst)
            {
                if (IsOutOfAvatar(dstAllTransforms, aimDst.worldUpObject))
                {
                    Debug.LogWarningFormat(cmp, "{0} の worldUpObject がアバター外を参照しています。", cmp);
                }
            }

            // Renderer
            if (cmp is SkinnedMeshRenderer smr)
            {
                if (IsOutOfAvatar(dstAllTransforms, smr.probeAnchor))
                {
                    Debug.LogWarningFormat(cmp, "{0} の probeAnchor がアバター外を参照しています。", cmp);
                }
                if (IsOutOfAvatar(dstAllTransforms, smr.rootBone))
                {
                    Debug.LogWarningFormat(cmp, "{0} の rootBone がアバター外を参照しています。", cmp);
                }
                if (IsOutOfAvatar(dstAllTransforms, smr.bones))
                {
                    Debug.LogWarningFormat(cmp, "{0} の bones がアバター外を参照しています。", cmp);
                }
            }
            if (cmp is MeshRenderer mr)
            {
                if (IsOutOfAvatar(dstAllTransforms, mr.probeAnchor))
                {
                    Debug.LogWarningFormat(cmp, "{0} の probeAnchor がアバター外を参照しています。", cmp);
                }
            }

            if (cmp is VRCAvatarDescriptor avd)
            {
                if (IsOutOfAvatar(dstAllTransforms, avd.VisemeSkinnedMesh))
                {
                    Debug.LogWarningFormat(cmp, "{0} の VisemeSkinnedMesh がアバター外を参照しています。", cmp);
                }
                if (IsOutOfAvatar(dstAllTransforms, avd.lipSyncJawBone))
                {
                    Debug.LogWarningFormat(cmp, "{0} の lipSyncJawBone がアバター外を参照しています。", cmp);
                }
                if (avd.enableEyeLook)
                {
                    var eyeLook = avd.customEyeLookSettings;
                    if (IsOutOfAvatar(dstAllTransforms, eyeLook.leftEye))
                    {
                        Debug.LogWarningFormat(cmp, "{0} の leftEye がアバター外を参照しています。", cmp);
                    }
                    if (IsOutOfAvatar(dstAllTransforms, eyeLook.rightEye))
                    {
                        Debug.LogWarningFormat(cmp, "{0} の rightEye がアバター外を参照しています。", cmp);
                    }
                    if (IsOutOfAvatar(dstAllTransforms, eyeLook.eyelidsSkinnedMesh))
                    {
                        Debug.LogWarningFormat(cmp, "{0} の eyelidsSkinnedMesh がアバター外を参照しています。", cmp);
                    }
                }
                if (IsOutOfAvatar(dstAllTransforms, avd.collider_fingerIndexL.transform))
                {
                    Debug.LogWarningFormat(cmp, "{0} の collider_fingerIndexL がアバター外を参照しています。", cmp);
                }
                if (IsOutOfAvatar(dstAllTransforms, avd.collider_fingerIndexR.transform))
                {
                    Debug.LogWarningFormat(cmp, "{0} の collider_fingerIndexR がアバター外を参照しています。", cmp);
                }
                if (IsOutOfAvatar(dstAllTransforms, avd.collider_fingerLittleL.transform))
                {
                    Debug.LogWarningFormat(cmp, "{0} の collider_fingerLittleL がアバター外を参照しています。", cmp);
                }
               if (IsOutOfAvatar(dstAllTransforms, avd.collider_fingerLittleR.transform))
                {
                    Debug.LogWarningFormat(cmp, "{0} の collider_fingerLittleR がアバター外を参照しています。", cmp);
                }
                if (IsOutOfAvatar(dstAllTransforms, avd.collider_fingerMiddleL.transform))
                {
                    Debug.LogWarningFormat(cmp, "{0} の collider_fingerMiddleL がアバター外を参照しています。", cmp);
                }
                if (IsOutOfAvatar(dstAllTransforms, avd.collider_fingerMiddleR.transform))
                {
                    Debug.LogWarningFormat(cmp, "{0} の collider_fingerMiddleR がアバター外を参照しています。", cmp);
                }
                if (IsOutOfAvatar(dstAllTransforms, avd.collider_fingerRingL.transform))
                {
                    Debug.LogWarningFormat(cmp, "{0} の collider_fingerRingL がアバター外を参照しています。", cmp);
                }
                if (IsOutOfAvatar(dstAllTransforms, avd.collider_fingerRingR.transform))
                {
                    Debug.LogWarningFormat(cmp, "{0} の collider_fingerRingR がアバター外を参照しています。", cmp);
                }
                if (IsOutOfAvatar(dstAllTransforms, avd.collider_footL.transform))
                {
                    Debug.LogWarningFormat(cmp, "{0} の collider_footL がアバター外を参照しています。", cmp);
                }
                if (IsOutOfAvatar(dstAllTransforms, avd.collider_footR.transform))
                {
                    Debug.LogWarningFormat(cmp, "{0} の collider_footR がアバター外を参照しています。", cmp);
                }
                if (IsOutOfAvatar(dstAllTransforms, avd.collider_handL.transform))
                {
                    Debug.LogWarningFormat(cmp, "{0} の collider_handL がアバター外を参照しています。", cmp);
                }
                if (IsOutOfAvatar(dstAllTransforms, avd.collider_handR.transform))
                {
                    Debug.LogWarningFormat(cmp, "{0} の collider_handR がアバター外を参照しています。", cmp);
                }
                if (IsOutOfAvatar(dstAllTransforms, avd.collider_torso.transform))
                {
                    Debug.LogWarningFormat(cmp, "{0} の collider_torso がアバター外を参照しています。", cmp);
                }

            }
        }

        private bool UpdateReference(Dictionary<GameObject, GameObject> map, IConstraint cs)
        {
            var list = new List<ConstraintSource>();
            cs.GetSources(list);
            if (UpdateReference(map, list))
            {
                cs.SetSources(list);
                return true;
            }
            return false;
        }

        private bool UpdateReference(Dictionary<GameObject, GameObject> map, List<ConstraintSource> list)
        {
            bool modified = false;
            if (list != null)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    var cs = list[i];
                    if (cs.sourceTransform != null)
                    {
                        modified |= UpdateReference(map, list[i].sourceTransform, t =>
                        {
                            cs.sourceTransform = t;
                            list[i] = cs;
                        });
                    }
                }
            }
            return modified;
        }

        private bool UpdateReference<T>(Dictionary<GameObject, GameObject> map, List<T> list) where T : Component
        {
            bool modified = false;
            if (list != null)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i] != null)
                    {
                        modified |= UpdateReference(map, list[i].transform, t =>
                            list[i] = t.GetComponent<T>()
                        );
                    }
                }
            }
            return modified;
        }

        private bool UpdateReference(Dictionary<GameObject, GameObject> map, List<Transform> list)
        {
            bool modified = false;
            if (list != null)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    modified |= UpdateReference(map, list[i], t => list[i] = t);
                }
            }
            return modified;
        }

        private bool UpdateReference(Dictionary<GameObject, GameObject> map, Transform[] list, Action<Transform[]> setter)
        {
            bool modified = false;
            if (list != null)
            {
                for (int i = 0; i < list.Length; i++)
                {
                    modified |= UpdateReference(map, list[i], t => list[i] = t);
                }
            }
            if (modified)
            {
                setter(list);
            }
            return modified;
        }

        private bool UpdateReference<T>(Dictionary<GameObject, GameObject> map, T cmp, Action<T> setter) where T: Component
        {
            if (cmp == null)
            {
                return false;
            }
            var nt = RemapTransform(map, cmp.transform);
            if (nt != null)
            {
                if (cmp is Transform)
                {
                    setter(nt as T);
                    return true;
                }
                else
                {
                    var newCmp = nt.GetComponent<T>();
                    if (newCmp != null)
                    {
                        setter(newCmp);
                        return true;
                    }
                }
            }
            return false;
        }

        private Transform RemapTransform(Dictionary<GameObject, GameObject> map, Transform t)
        {
            if (t != null && map.ContainsKey(t.gameObject) && map[t.gameObject] != null)
            {
                return map[t.gameObject].transform;
            }
            return null;
        }

        #endregion
    }
}

#endif
