/*
 *  The MIT License
 *
 *  Copyright 2022-2024 whiteflare.
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

#if ENV_VRCSDK3_AVATAR

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
#if ENV_VRCSDK3_AVATAR_CST
using VRC.SDK3.Dynamics.Constraint.Components;
#endif

namespace WF.Tool.Avatar
{
    internal class AvatarCopyUtility : EditorWindow
    {
        [MenuItem("Tools/whiteflare/Avatar Copy Utility", priority = 12)]
        public static void Menu_AvatarCopyUtility()
        {
            AvatarCopyUtility.ShowWindow();
        }

        private const string Title = "Avatar Copy Utility";

        public List<GameObject> oldBones = new List<GameObject>();
        public List<GameObject> newBones = new List<GameObject>();

        public List<GameObject> tgtSkinnedMeshR = new List<GameObject>();
        public List<GameObject> tgtMeshR = new List<GameObject>();
        public List<GameObject> tgtPositionC = new List<GameObject>();
        public List<GameObject> tgtRotationC = new List<GameObject>();
        public List<GameObject> tgtScaleC = new List<GameObject>();
        public List<GameObject> tgtParentC = new List<GameObject>();
        public List<GameObject> tgtLookAtC = new List<GameObject>();
        public List<GameObject> tgtAimC = new List<GameObject>();
        public List<GameObject> tgtParticleSystem = new List<GameObject>();

        public List<GameObject> tgtAvatarDesc = new List<GameObject>();
        public List<GameObject> tgtPhysBone = new List<GameObject>();
        public List<GameObject> tgtPhysBoneCollider = new List<GameObject>();
        public List<GameObject> tgtContactSender = new List<GameObject>();
        public List<GameObject> tgtContactReceiver = new List<GameObject>();

#if ENV_VRCSDK3_AVATAR_CST
        public List<GameObject> tgtVRCPositionC = new List<GameObject>();
        public List<GameObject> tgtVRCRotationC = new List<GameObject>();
        public List<GameObject> tgtVRCScaleC = new List<GameObject>();
        public List<GameObject> tgtVRCParentC = new List<GameObject>();
        public List<GameObject> tgtVRCLookAtC = new List<GameObject>();
        public List<GameObject> tgtVRCAimC = new List<GameObject>();
#endif

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
            typeof(ParticleSystem),
#if ENV_VRCSDK3_AVATAR_CST
            typeof(VRCPositionConstraint),
            typeof(VRCRotationConstraint),
            typeof(VRCScaleConstraint),
            typeof(VRCParentConstraint),
            typeof(VRCLookAtConstraint),
            typeof(VRCAimConstraint),
#endif
        };

        private readonly List<string> COPY_FIELD_NAME = new List<string>()
        {
            nameof(tgtAvatarDesc),
            nameof(tgtSkinnedMeshR),
            nameof(tgtMeshR),
            nameof(tgtPhysBone),
            nameof(tgtPhysBoneCollider),
            nameof(tgtContactSender),
            nameof(tgtContactReceiver),
            nameof(tgtPositionC),
            nameof(tgtRotationC),
            nameof(tgtScaleC),
            nameof(tgtParentC),
            nameof(tgtLookAtC),
            nameof(tgtAimC),
            nameof(tgtParticleSystem),
#if ENV_VRCSDK3_AVATAR_CST
            nameof(tgtVRCPositionC),
            nameof(tgtVRCRotationC),
            nameof(tgtVRCScaleC),
            nameof(tgtVRCParentC),
            nameof(tgtVRCLookAtC),
            nameof(tgtVRCAimC),
#endif
        };

        private readonly List<List<GameObject>> targetComponents = new List<List<GameObject>>();
        private readonly List<bool[]> checkComponents = new List<bool[]>();

        private readonly List<SerializedProperty> propList = new List<SerializedProperty>();

        private GameObject srcRoot = null;
        private GameObject dstRoot = null;
        private int tabIndex = 0;
        private Vector2 _scrollPos = Vector2.zero;

        public static void ShowWindow()
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

            var rect2 = new Rect(rect.x + 4f, rect.y + 2f, 13f, 13f);
            prop.isExpanded = EditorGUI.Foldout(rect2, prop.isExpanded, "");

            rect.x += 20f;
            EditorGUI.showMixedValue = check.Distinct().Count() == 2;
            EditorGUI.BeginChangeCheck();
            var allCheck = EditorGUI.ToggleLeft(rect, labelArray, check.Any(v => v));
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

                //int size = Math.Max(0, EditorGUILayout.DelayedIntField("Size", list.Count));
                //var newList = new T[size];
                //if (check.Length != size)
                //{
                //    Array.Resize(ref check, size);
                //}
                int size = list.Count;
                var newList = new T[size];
                for (int i = 0; i < size; i++)
                {
                    var item = i < list.Count ? list[i] : null;

                    rect = EditorGUILayout.GetControlRect();
                    rect.x += 40;
                    rect.width -= 40;

                    if (isHighlight(item))
                    {
                        GUI.color = color_attention;
                    }
                    newList[i] = EditorGUI.ObjectField(rect, "", item, typeof(T), true) as T;
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

        private void ExecuteCopy()
        {
            // Undo
            Undo.RegisterFullObjectHierarchyUndo(dstRoot, "Transfer Armature");

            // Src と Dst の GameObject マッピング辞書
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

            //-------------------------------------------------
            // GameObject の用意
            //-------------------------------------------------
            {
                var prepareSrcGos = new List<GameObject>();
                prepareSrcGos.AddRange(remapGo.Where(ent => ent.Value == null && IsCopyTargetGameObject(ent.Key)).Select(ent => ent.Key));
                prepareSrcGos.AddRange(GetAllSkinnedMeshBones(GetCopyTargetComponents<SkinnedMeshRenderer>()).Select(t => t.gameObject));
                prepareSrcGos.AddRange(GetPhysBoneAffectedTransforms(GetCopyTargetComponents<VRCPhysBone>()).Select(t => t.gameObject));
                prepareSrcGos.AddRange(GetConstraintAffectedTransform(GetCopyTargetComponents<PositionConstraint>()).Select(t => t.gameObject));
                prepareSrcGos.AddRange(GetConstraintAffectedTransform(GetCopyTargetComponents<RotationConstraint>()).Select(t => t.gameObject));
                prepareSrcGos.AddRange(GetConstraintAffectedTransform(GetCopyTargetComponents<ScaleConstraint>()).Select(t => t.gameObject));
                prepareSrcGos.AddRange(GetConstraintAffectedTransform(GetCopyTargetComponents<ParentConstraint>()).Select(t => t.gameObject));
                prepareSrcGos.AddRange(GetConstraintAffectedTransform(GetCopyTargetComponents<LookAtConstraint>()).Select(t => t.gameObject));
                prepareSrcGos.AddRange(GetConstraintAffectedTransform(GetCopyTargetComponents<AimConstraint>()).Select(t => t.gameObject));
#if ENV_VRCSDK3_AVATAR_CST
                prepareSrcGos.AddRange(GetVRCConstraintAffectedTransform(GetCopyTargetComponents<VRCPositionConstraint>()).Select(t => t.gameObject));
                prepareSrcGos.AddRange(GetVRCConstraintAffectedTransform(GetCopyTargetComponents<VRCRotationConstraint>()).Select(t => t.gameObject));
                prepareSrcGos.AddRange(GetVRCConstraintAffectedTransform(GetCopyTargetComponents<VRCScaleConstraint>()).Select(t => t.gameObject));
                prepareSrcGos.AddRange(GetVRCConstraintAffectedTransform(GetCopyTargetComponents<VRCParentConstraint>()).Select(t => t.gameObject));
                prepareSrcGos.AddRange(GetVRCConstraintAffectedTransform(GetCopyTargetComponents<VRCLookAtConstraint>()).Select(t => t.gameObject));
                prepareSrcGos.AddRange(GetVRCConstraintAffectedTransform(GetCopyTargetComponents<VRCAimConstraint>()).Select(t => t.gameObject));
#endif
                foreach (var src in prepareSrcGos.Distinct())
                {
                    PrepareGameObject(remapGo, src, true);
                }
            }

            //-------------------------------------------------
            // コピー対象の列挙
            //-------------------------------------------------

            // Src と Dst の Component マッピング辞書
            var remapCmp = new Dictionary<Component, Component>();
            foreach (var ent in remapGo)
            {
                if (ent.Value == null)
                {
                    continue;
                }
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
                        if (t == typeof(ParticleSystem))
                        {
                            // もしコピーしようとしているのが ParticleSystem であれば、ParticleSystemRenderer も一緒にコピーする
                            RegisterRemapComponent(remapCmp, typeof(ParticleSystemRenderer), ent.Key, ent.Value);
                        }
                    }
                }
            }

            //-------------------------------------------------
            // 値のコピー
            //-------------------------------------------------
            foreach (var ent in remapCmp)
            {
                EditorUtility.CopySerialized(ent.Key, ent.Value);
            }

            //-------------------------------------------------
            // 参照の再接続
            //-------------------------------------------------
            foreach (var dst in remapCmp.Values)
            {
                UpdateReference(remapGo, dst);
            }

            //-------------------------------------------------
            // Transformのコピー
            //-------------------------------------------------
            var copyTransformDsts = new List<Transform>();
            foreach (var dst in remapCmp.Values)
            {
                if (dst is VRCPhysBone dstPB)
                {
                    // VRCLeafTipBoneの座標を合わせる
                    foreach (var t in GetPhysBoneAffectedTransforms(dstPB))
                    {
                        if (!string.IsNullOrEmpty(t.name) && t.name.StartsWith("VRCLeafTipBone"))
                        {
                            copyTransformDsts.Add(t);
                        }
                    }
                }
                if (dst is VRCPhysBoneCollider dstPBC)
                {
                    // コライダーの座標を合わせる
                    copyTransformDsts.Add(dstPBC.rootTransform != null ? dstPBC.rootTransform : dstPBC.transform);
                }
            }
            var dstBoneList = GetHumanoidBoneTransforms(dstRoot); // dst 側の Humanoid に紐づいている bone リスト
            foreach (var t in copyTransformDsts.Distinct())
            {
                // ただしHumanoidボーンに割り当てられているTransformは変更しない
                if (dstBoneList.Contains(t))
                {
                    continue;
                }
                SyncTransform(remapGo, t);
            }

            //-------------------------------------------------
            // 参照チェックと警告ログ
            //-------------------------------------------------
            var dstAllTransforms = dstRoot.GetComponentsInChildren<Transform>(true);
            foreach (var dst in remapCmp.Values)
            {
                CheckReference(dstAllTransforms, dst);
            }

            //-------------------------------------------------
            // dest の書き戻し
            //-------------------------------------------------
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

        #region GameObject の用意

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

        #endregion

        #region コピー対象の列挙

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

        #endregion

        #region 参照の再接続

        private bool UpdateReference(Dictionary<GameObject, GameObject> goRemap, Component cmp)
        {
            // PhysBone
            if (cmp is VRCPhysBone)
            {
                var dst = (VRCPhysBone)cmp;
                UpdateReference(goRemap, dst.rootTransform, t => dst.rootTransform = t);
                UpdateReference(goRemap, dst.ignoreTransforms);
                UpdateReference<VRCPhysBoneColliderBase>(goRemap, dst.colliders);
            }
            if (cmp is VRCPhysBoneCollider)
            {
                var dst = (VRCPhysBoneCollider)cmp;
                UpdateReference(goRemap, dst.rootTransform, t => dst.rootTransform = t);
            }

            // Contact
            if (cmp is VRCContactSender)
            {
                var dst = (VRCContactSender)cmp;
                UpdateReference(goRemap, dst.rootTransform, t => dst.rootTransform = t);
            }
            if (cmp is VRCContactReceiver)
            {
                var dst = (VRCContactReceiver)cmp;
                UpdateReference(goRemap, dst.rootTransform, t => dst.rootTransform = t);
            }

            // Constraint
            if (cmp is IConstraint)
            {
                var dst = (IConstraint)cmp;
                UpdateReference(goRemap, dst);
            }
            if (cmp is LookAtConstraint)
            {
                var dst = (LookAtConstraint)cmp;
                UpdateReference(goRemap, dst.worldUpObject, t => dst.worldUpObject = t);
            }
            if (cmp is AimConstraint)
            {
                var dst = (AimConstraint)cmp;
                UpdateReference(goRemap, dst.worldUpObject, t => dst.worldUpObject = t);
            }

            // Renderer
            if (cmp is SkinnedMeshRenderer)
            {
                var dst = (SkinnedMeshRenderer)cmp;
                UpdateReference(goRemap, dst.probeAnchor, t => dst.probeAnchor = t);
                UpdateReference(goRemap, dst.rootBone, t => dst.rootBone = t);
                UpdateReference(goRemap, dst.bones, ts => dst.bones = ts);  // bones には Transform[] の再設定が必要
            }
            if (cmp is MeshRenderer)
            {
                var dst = (MeshRenderer)cmp;
                UpdateReference(goRemap, dst.probeAnchor, t => dst.probeAnchor = t);
            }

            // VRCAvatarDescriptor
            if (cmp is VRCAvatarDescriptor)
            {
                var dst = (VRCAvatarDescriptor)cmp;
                UpdateReference(goRemap, dst.VisemeSkinnedMesh, t => dst.VisemeSkinnedMesh = t);
                UpdateReference(goRemap, dst.lipSyncJawBone, t => dst.lipSyncJawBone = t);
                if (dst.enableEyeLook)
                {
                    var eyeLook = dst.customEyeLookSettings;
                    UpdateReference(goRemap, eyeLook.leftEye, t => eyeLook.leftEye = t);
                    UpdateReference(goRemap, eyeLook.rightEye, t => eyeLook.rightEye = t);
                    UpdateReference(goRemap, eyeLook.eyelidsSkinnedMesh, t => eyeLook.eyelidsSkinnedMesh = t);
                    dst.customEyeLookSettings = eyeLook;
                }
                UpdateReference(goRemap, dst.collider_fingerIndexL.transform, t => dst.collider_fingerIndexL.transform = t);
                UpdateReference(goRemap, dst.collider_fingerIndexR.transform, t => dst.collider_fingerIndexR.transform = t);
                UpdateReference(goRemap, dst.collider_fingerLittleL.transform, t => dst.collider_fingerLittleL.transform = t);
                UpdateReference(goRemap, dst.collider_fingerLittleR.transform, t => dst.collider_fingerLittleR.transform = t);
                UpdateReference(goRemap, dst.collider_fingerMiddleL.transform, t => dst.collider_fingerMiddleL.transform = t);
                UpdateReference(goRemap, dst.collider_fingerMiddleR.transform, t => dst.collider_fingerMiddleR.transform = t);
                UpdateReference(goRemap, dst.collider_fingerRingL.transform, t => dst.collider_fingerRingL.transform = t);
                UpdateReference(goRemap, dst.collider_fingerRingR.transform, t => dst.collider_fingerRingR.transform = t);
                UpdateReference(goRemap, dst.collider_footL.transform, t => dst.collider_footL.transform = t);
                UpdateReference(goRemap, dst.collider_footR.transform, t => dst.collider_footR.transform = t);
                UpdateReference(goRemap, dst.collider_handL.transform, t => dst.collider_handL.transform = t);
                UpdateReference(goRemap, dst.collider_handR.transform, t => dst.collider_handR.transform = t);
                UpdateReference(goRemap, dst.collider_torso.transform, t => dst.collider_torso.transform = t);
            }

            // ParticleSystem
            if (cmp is ParticleSystem)
            {
                var dst = (ParticleSystem)cmp;
#if UNITY_2020_2_OR_NEWER
                var trigger = dst.trigger;
                for (int i = 0; i < trigger.colliderCount; i++)
                {
                    UpdateReference(goRemap, trigger.GetCollider(i), c => trigger.SetCollider(i, c));
                }
#endif

                var subEmitters = dst.subEmitters;
                for (int i = 0; i < subEmitters.subEmittersCount; i++)
                {
                    UpdateReference(goRemap, subEmitters.GetSubEmitterSystem(i), c => subEmitters.SetSubEmitterSystem(i, c));
                }

                var lights = dst.lights;
                UpdateReference(goRemap, lights.light, c => lights.light = c);
            }

            // VRC Constraint系
#if ENV_VRCSDK3_AVATAR_CST
            if (cmp is VRCConstraintBase)
            {
                var dst = (VRCConstraintBase)cmp;
                UpdateReference(goRemap, dst.TargetTransform, t => dst.TargetTransform = t);
                UpdateReference(goRemap, dst);
            }
            if (cmp is VRCLookAtConstraint)
            {
                var dst = (VRCLookAtConstraint)cmp;
                UpdateReference(goRemap, dst.WorldUpTransform, t => dst.WorldUpTransform = t);
            }
            if (cmp is VRCAimConstraint)
            {
                var dst = (VRCAimConstraint)cmp;
                UpdateReference(goRemap, dst.WorldUpTransform, t => dst.WorldUpTransform = t);
            }
#endif

            return false;
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

#if ENV_VRCSDK3_AVATAR_CST
        private bool UpdateReference(Dictionary<GameObject, GameObject> map, VRCConstraintBase cs)
        {
            return UpdateReference(map, cs.Sources, list => cs.Sources = list);
        }

        private bool UpdateReference(Dictionary<GameObject, GameObject> map, VRCConstraintSourceKeyableList list, Action<VRCConstraintSourceKeyableList> setter)
        {
            bool modified = false;
            for (int i = 0; i < list.Count; i++)
            {
                var cs = list[i];
                if (cs.SourceTransform != null)
                {
                    modified |= UpdateReference(map, list[i].SourceTransform, t =>
                    {
                        cs.SourceTransform = t;
                        list[i] = cs;
                    });
                }
            }
            if (modified)
            {
                setter(list);
            }
            return modified;
        }
#endif

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

        private bool UpdateReference<T>(Dictionary<GameObject, GameObject> map, T cmp, Action<T> setter) where T : Component
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

        #region Transformのコピー

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

        #endregion

        #region 関連するGameObjectを取得する系

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
                    var ret = anim.GetBoneTransform(HumanBodyBones.Hips);
                    if (ret != null)
                    {
                        return ret;
                    }
                }
            }
            return go.transform;
        }

        private List<Transform> GetConstraintAffectedTransform<T>(List<T> list) where T : IConstraint
        {
            var result = new List<Transform>();
            foreach (var c in list)
            {
                var cslist = new List<ConstraintSource>();
                c.GetSources(cslist);
                foreach (var cs in cslist)
                {
                    result.Add(cs.sourceTransform);
                }
                if (c is LookAtConstraint)
                {
                    var c2 = (LookAtConstraint)(object)c;
                    result.Add(c2.worldUpObject);
                }
                if (c is AimConstraint)
                {
                    var c2 = (AimConstraint)(object)c;
                    result.Add(c2.worldUpObject);
                }
            }
            return result.Where(t => t != null).Distinct().ToList();
        }

#if ENV_VRCSDK3_AVATAR_CST
        private List<Transform> GetVRCConstraintAffectedTransform<T>(List<T> list) where T : VRCConstraintBase
        {
            var result = new List<Transform>();
            foreach (var c in list)
            {
                result.Add(c.TargetTransform);
                foreach (var cs in c.Sources)
                {
                    result.Add(cs.SourceTransform);
                }
                if (c is VRCLookAtConstraint)
                {
                    var c2 = (VRCLookAtConstraint)(object)c;
                    result.Add(c2.WorldUpTransform);
                }
                if (c is VRCAimConstraint)
                {
                    var c2 = (VRCAimConstraint)(object)c;
                    result.Add(c2.WorldUpTransform);
                }
            }
            return result.Where(t => t != null).Distinct().ToList();
        }
        #endif

        #endregion

        #region 参照チェックと警告ログ

        private void CheckReference(Transform[] dstAllTransforms, Component cmp)
        {
            // Renderer
            if (cmp is Renderer)
            {
                var dst = (Renderer)cmp;
                CheckReferenceAndWarn(dstAllTransforms, cmp, dst.probeAnchor, "probeAnchor");
            }
            if (cmp is SkinnedMeshRenderer)
            {
                var dst = (SkinnedMeshRenderer)cmp;
                CheckReferenceAndWarn(dstAllTransforms, cmp, dst.rootBone, "rootBone");
                CheckReferenceAndWarn(dstAllTransforms, cmp, dst.bones, "bones");
            }

            // ParticleSystem
            if (cmp is ParticleSystem)
            {
                var dst = (ParticleSystem)cmp;
#if UNITY_2020_2_OR_NEWER
                var trigger = dst.trigger;
                for (int i = 0; i < trigger.colliderCount; i++)
                {
                    CheckReferenceAndWarn(dstAllTransforms, cmp, trigger.GetCollider(i), "trigger");
                }
#endif

                var subEmitters = dst.subEmitters;
                for (int i = 0; i < subEmitters.subEmittersCount; i++)
                {
                    CheckReferenceAndWarn(dstAllTransforms, cmp, subEmitters.GetSubEmitterSystem(i), "subEmitters");
                }

                var lights = dst.lights;
                CheckReferenceAndWarn(dstAllTransforms, cmp, lights.light, "lights");
            }

            // Constraint
            if (cmp is IConstraint)
            {
                var dst = (IConstraint)cmp;
                var list = new List<ConstraintSource>();
                dst.GetSources(list);
                CheckReferenceAndWarn(dstAllTransforms, cmp, list.Select(t => t.sourceTransform), "ConstraintSource");
            }
            if (cmp is LookAtConstraint)
            {
                var dst = (LookAtConstraint)cmp;
                CheckReferenceAndWarn(dstAllTransforms, cmp, dst.worldUpObject, "worldUpObject");
            }
            if (cmp is AimConstraint)
            {
                var dst = (AimConstraint)cmp;
                CheckReferenceAndWarn(dstAllTransforms, cmp, dst.worldUpObject, "worldUpObject");
            }

            // VRCAvatarDescriptor
            if (cmp is VRCAvatarDescriptor)
            {
                var dst = (VRCAvatarDescriptor)cmp;
                CheckReferenceAndWarn(dstAllTransforms, cmp, dst.VisemeSkinnedMesh, "VisemeSkinnedMesh");
                CheckReferenceAndWarn(dstAllTransforms, cmp, dst.lipSyncJawBone, "lipSyncJawBone");

                if (dst.enableEyeLook)
                {
                    var eyeLook = dst.customEyeLookSettings;
                    CheckReferenceAndWarn(dstAllTransforms, cmp, eyeLook.leftEye, "leftEye");
                    CheckReferenceAndWarn(dstAllTransforms, cmp, eyeLook.rightEye, "rightEye");
                    CheckReferenceAndWarn(dstAllTransforms, cmp, eyeLook.eyelidsSkinnedMesh, "eyelidsSkinnedMesh");
                }

                CheckReferenceAndWarn(dstAllTransforms, cmp, dst.collider_fingerIndexL.transform, "collider_fingerIndexL");
                CheckReferenceAndWarn(dstAllTransforms, cmp, dst.collider_fingerIndexR.transform, "collider_fingerIndexR");
                CheckReferenceAndWarn(dstAllTransforms, cmp, dst.collider_fingerLittleL.transform, "collider_fingerLittleL");
                CheckReferenceAndWarn(dstAllTransforms, cmp, dst.collider_fingerLittleR.transform, "collider_fingerLittleR");
                CheckReferenceAndWarn(dstAllTransforms, cmp, dst.collider_fingerMiddleL.transform, "collider_fingerMiddleL");
                CheckReferenceAndWarn(dstAllTransforms, cmp, dst.collider_fingerMiddleR.transform, "collider_fingerMiddleR");
                CheckReferenceAndWarn(dstAllTransforms, cmp, dst.collider_fingerRingL.transform, "collider_fingerRingL");
                CheckReferenceAndWarn(dstAllTransforms, cmp, dst.collider_fingerRingR.transform, "collider_fingerRingR");
                CheckReferenceAndWarn(dstAllTransforms, cmp, dst.collider_footL.transform, "collider_footL");
                CheckReferenceAndWarn(dstAllTransforms, cmp, dst.collider_footR.transform, "collider_footR");
                CheckReferenceAndWarn(dstAllTransforms, cmp, dst.collider_handL.transform, "collider_handL");
                CheckReferenceAndWarn(dstAllTransforms, cmp, dst.collider_handR.transform, "collider_handR");
                CheckReferenceAndWarn(dstAllTransforms, cmp, dst.collider_torso.transform, "collider_torso");
            }

            // PhysBone
            if (cmp is VRCPhysBone)
            {
                var dst = (VRCPhysBone)cmp;
                CheckReferenceAndWarn(dstAllTransforms, cmp, dst.rootTransform, "rootTransform");
                CheckReferenceAndWarn(dstAllTransforms, cmp, dst.ignoreTransforms, "ignoreTransforms");
                CheckReferenceAndWarn(dstAllTransforms, cmp, dst.colliders, "colliders");
            }
            if (cmp is VRCPhysBoneCollider)
            {
                var dst = (VRCPhysBoneCollider)cmp;
                CheckReferenceAndWarn(dstAllTransforms, cmp, dst.rootTransform, "rootTransform");
            }

            // Contact
            if (cmp is VRCContactSender)
            {
                var dst = (VRCContactSender)cmp;
                CheckReferenceAndWarn(dstAllTransforms, cmp, dst.rootTransform, "rootTransform");
            }
            if (cmp is VRCContactReceiver)
            {
                var dst = (VRCContactReceiver)cmp;
                CheckReferenceAndWarn(dstAllTransforms, cmp, dst.rootTransform, "rootTransform");
            }

            // VRC Constraint
#if ENV_VRCSDK3_AVATAR_CST
            if (cmp is VRCConstraintBase)
            {
                var dst = (VRCConstraintBase)cmp;
                CheckReferenceAndWarn(dstAllTransforms, cmp, dst.TargetTransform, "TargetTransform");
                var list = dst.Sources;
                CheckReferenceAndWarn(dstAllTransforms, cmp, list.Select(t => t.SourceTransform), "Sources");
            }
            if (cmp is VRCLookAtConstraint)
            {
                var dst = (VRCLookAtConstraint)cmp;
                CheckReferenceAndWarn(dstAllTransforms, cmp, dst.WorldUpTransform, "WorldUpTransform");
            }
            if (cmp is VRCAimConstraint)
            {
                var dst = (VRCAimConstraint)cmp;
                CheckReferenceAndWarn(dstAllTransforms, cmp, dst.WorldUpTransform, "WorldUpTransform");
            }
#endif
        }

        private void CheckReferenceAndWarn<T>(Transform[] dstAllTransforms, Component parent, T property, string propertyName) where T : Component
        {
            if (IsOutOfAvatar(dstAllTransforms, property))
            {
                Debug.LogWarningFormat(parent, "{0} の {1} がアバター外を参照しています。", parent, propertyName);
            }
        }

        private void CheckReferenceAndWarn<T>(Transform[] dstAllTransforms, Component parent, IEnumerable<T> properties, string propertyName) where T : Component
        {
            if (IsOutOfAvatar(dstAllTransforms, properties))
            {
                Debug.LogWarningFormat(parent, "{0} の {1} がアバター外を参照しています。", parent, propertyName);
            }
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

        #endregion

        #endregion
    }
}

#endif

#endif
