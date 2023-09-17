/*
 *  The MIT License
 *
 *  Copyright 2023 whiteflare.
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
using VRC.SDK3.Dynamics.PhysBone.Components;

namespace WF.Tool.Avatar
{
    internal class PhysBoneSetupHelper : EditorWindow
    {
        [MenuItem("Tools/whiteflare/PhysBone Setup Helper", priority = 21)]
        public static void Menu_PBSetupHelper()
        {
            PhysBoneSetupHelper.ShowWindow();
        }

        private const string Title = "PhysBone Setup Helper";

        private GameObject _avatar;
        private GameObject _armature;
        private Vector2 _scrollPos = Vector2.zero;
        private bool resetField = false;
        private int attachTypePB = 0;
        private int attachTypePBC = 0;

        [MenuItem("Tools/whiteflare/PhysBone Setup Helper")]
        public static void ShowWindow()
        {
            var window = GetWindow<PhysBoneSetupHelper>(Title);
            var go = Selection.activeGameObject;
            if (IsVRCAvatarObject(go))
            {
                window._avatar = go;
                window._armature = null;
                window.resetField = true;
            }
        }

        private static bool IsVRCAvatarObject(GameObject go)
        {
            if (go == null)
            {
                return false;
            }
            return go.GetComponents<Component>()
                .Where(cmp => cmp != null)
                .Any(cmp => cmp.GetType().Name == "VRC_AvatarDescriptor" || cmp.GetType().Name == "VRCAvatarDescriptor");
        }

        #region GUI

        private void OnGUI()
        {
            var styleHeader = new GUIStyle(EditorStyles.largeLabel)
            {
                fontSize = 18,
                fixedHeight = 30,
                margin = new RectOffset(4, 4, 8, 0),
            };

            ////////////////////
            // 対象の設定
            ////////////////////

            GUILayout.Label("■ 編集対象アバター", styleHeader);

            EditorGUI.BeginChangeCheck();

            // アバタールート
            _avatar = EditorGUILayout.ObjectField("Avatar Root", _avatar, typeof(GameObject), true) as GameObject;

            if (EditorGUI.EndChangeCheck() || resetField)
            {
                // _avatar がセットされたら _armature の再検索
                ResetArmatureReference();
                resetField = false;
            }

            // Armatureルート
            _armature = EditorGUILayout.ObjectField("Armature", _armature, typeof(GameObject), true) as GameObject;

            if (_avatar == null || _armature == null)
            {
                return;
            }

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            GUILayout.BeginVertical();
            {

                ////////////////////
                // ベリファイ
                ////////////////////

                EditorGUILayout.Space();
                GUILayout.Label("■ Verify", styleHeader);
                EditorGUILayout.HelpBox(new GUIContent("PhysBoneの設定をチェックしてConsoleログに出力します"));

                if (GUILayout.Button("verify"))
                {
                    VerifyVRCPhysBone();
                }

                ////////////////////
                // 参照の再接続
                ////////////////////

                EditorGUILayout.Space();
                GUILayout.Label("■ 参照の再接続", styleHeader);
                EditorGUILayout.HelpBox(new GUIContent("Armature 外への参照を Armature 内へと付け替えます"));

                if (GUILayout.Button("Re-Connect References") && ConfirmContinue())
                {
                    Undo.RegisterFullObjectHierarchyUndo(_avatar, "PhysBone参照の再接続");
                    DoConnectReferences();
                }

                ////////////////////
                // アタッチ先の変更
                ////////////////////

                EditorGUILayout.Space();
                GUILayout.Label("■ アタッチ先オブジェクトの変更", styleHeader);

                string[] label = {
                    "揺らすボーン自体",
                    "独立したオブジェクト",
                };

                var stylePopup = new GUIStyle(EditorStyles.popup)
                {
                    fixedHeight = 18,
                };
                stylePopup.margin.top += 1;

                EditorGUILayout.HelpBox(new GUIContent("VRCPhysBoneをアタッチする対象オブジェクトを変更します"));

                EditorGUILayout.BeginHorizontal();
                attachTypePB = EditorGUILayout.Popup(attachTypePB, label, stylePopup);
                if (GUILayout.Button("Move") && ConfirmContinue())
                {
                    Undo.RegisterFullObjectHierarchyUndo(_avatar, "PhysBoneアタッチ先の変更");
                    switch (attachTypePB)
                    {
                        case 0:
                            MovePhysBoneToBone();
                            break;
                        case 1:
                            MovePhysBoneToSingle();
                            break;
                        default:
                            break;
                    }
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space();
                EditorGUILayout.HelpBox(new GUIContent("VRCPhysBoneColliderをアタッチする対象オブジェクトを変更します"));

                EditorGUILayout.BeginHorizontal();
                attachTypePBC = EditorGUILayout.Popup(attachTypePBC, label, stylePopup);
                if (GUILayout.Button("Move") && ConfirmContinue())
                {
                    Undo.RegisterFullObjectHierarchyUndo(_avatar, "PhysBoneColliderアタッチ先の変更");
                    switch (attachTypePBC)
                    {
                        case 0:
                            MovePhysBoneColliderToBone();
                            break;
                        case 1:
                            MovePhysBoneColliderToSingle();
                            break;
                        default:
                            break;
                    }
                }
                EditorGUILayout.EndHorizontal();

            }
            GUILayout.EndVertical();
            EditorGUILayout.EndScrollView();
        }

        private void ResetArmatureReference()
        {
            if (_avatar != null)
            {
                // avatar が設定されているとき、armature が avatar の子ではないならば armature クリア
                if (_armature != null && !IsChildrenOf(_armature, _avatar))
                {
                    _armature = null;
                }
                // armature 未指定ならば、avatar 配下から "Armature" という名前のオブジェクトを探してきて設定する
                if (_armature == null)
                {
                    _armature = _avatar.GetComponentsInChildren<Transform>(true)
                        .Select(cmp => cmp.gameObject)
                        .Where(go => go != null && "Armature" == go.name)
                        .FirstOrDefault();
                }
                // それでも見つからないならば avatar 配下から "Armature" で始まる名前のオブジェクトを探してきて設定する
                if (_armature == null)
                {
                    _armature = _avatar.GetComponentsInChildren<Transform>(true)
                        .Select(cmp => cmp.gameObject)
                        .Where(go => go != null && go.name.StartsWith("Armature"))
                        .FirstOrDefault();
                }
            }
            else
            {
                // avatar 未設定なら armature もクリアする
                _armature = null;
            }
        }

        private static bool ConfirmContinue()
        {
            return EditorUtility.DisplayDialog(Title, "Continue modify Objects?\nオブジェクトを変更しますか？", "OK", "CANCEL");
        }

        #endregion

        #region ベリファイ

        /// <summary>
        /// ベリファイ
        /// </summary>
        private void VerifyVRCPhysBone()
        {
            // アバタールートから全てのVRCPhysBoneについて検査
            foreach (var bone in _avatar.GetComponentsInChildren<VRCPhysBone>(true))
            {
                VerifyVRCPhysBone(bone);
            }

            // アバタールートから全てのVRCPhysBoneColliderについて検査
            foreach (var collider in _avatar.GetComponentsInChildren<VRCPhysBoneCollider>(true))
            {
                VerifyVRCPhysBoneCollider(collider);
            }
        }

        private void VerifyVRCPhysBone(VRCPhysBone bone)
        {
            // 参照先が未設定
            if (bone.colliders.Any(col => col == null))
            {
                Debug.LogWarningFormat(bone, "[{0}] {1} の Colliders が null です", Title, bone);
            }
            if (bone.ignoreTransforms.Any(col => col == null))
            {
                Debug.LogWarningFormat(bone, "[{0}] {1} の IgnoreTransforms が null です", Title, bone);
            }

            // 参照先が armature の外
            if (bone.rootTransform != null && !IsChildrenOf(bone.rootTransform.gameObject, _armature))
            {
                Debug.LogWarningFormat(bone, "[{0}] {1} の Root が Armature の外を指しています", Title, bone);
            }
            if (bone.colliders.Any(col => col != null && !IsChildrenOf(col.rootTransform != null ? col.rootTransform.gameObject : col.gameObject, _avatar)))
            {
                Debug.LogWarningFormat(bone, "[{0}] {1} の Colliders が Armature の外を指しています", Title, bone);
            }
            if (bone.ignoreTransforms.Any(col => col != null && !IsChildrenOf(col.gameObject, _avatar)))
            {
                Debug.LogWarningFormat(bone, "[{0}] {1} の IgnoreTransforms が Armature の外を指しています", Title, bone);
            }

            // PBが揺らすボーンが存在しているかどうか
            if (!HasAffectedBone(bone))
            {
                Debug.LogWarningFormat(bone, "[{0}] {1} の長さが 0 です", Title, bone);
            }

            // 同じボーンを揺らしているVRCPhysBoneが複数ないかどうか
            if (HasOtherAffectedBone(bone, _avatar))
            {
                Debug.LogWarningFormat(bone, "[{0}] {1} のボーンを動かす VRCPhysBone が他にも存在します", Title, bone);
            }
        }

        private bool HasAffectedBone(VRCPhysBone bone)
        {
            if (bone.endpointPosition != Vector3.zero)
            {
                return true; // endpointが指定されているならOK
            }
            // 全てのTransformを取得
            var allTransforms = GetPhysBoneAffectedTransforms(bone);
            // Leafをリストから削除する
            var notLeafTransforms = allTransforms.Where(t =>
            {
                // 子を取得
                var childs = t.GetComponentsInChildren<Transform>(true).Where(c => t != c);
                // 子がallTransformsに存在するならLeafではない
                return childs.Any(allTransforms.Contains);
            }).ToList();
            if (0 < notLeafTransforms.Count)
            {
                // Transform が存在して First または Average なら問題なし
                if (bone.multiChildType != VRC.Dynamics.VRCPhysBoneBase.MultiChildType.Ignore)
                {
                    return true;
                }
                // Ignore であれば、1つの子をもつ枝が存在している場合にのみ揺れる
                return notLeafTransforms.Any(t =>
                {
                    var count = 0;
                    for (int i = 0; i < t.childCount; i++)
                    {
                        if (allTransforms.Contains(t.GetChild(i)))
                        {
                            count++;
                        }
                    }
                    return count == 1;
                });
            }
            return false;
        }

        private bool HasOtherAffectedBone(VRCPhysBone bone, GameObject go)
        {
            var otherBones = go.GetComponentsInChildren<VRCPhysBone>(true).Where(b => b != bone).SelectMany(GetPhysBoneAffectedTransforms).Distinct().ToList();
            foreach (var current in GetPhysBoneAffectedTransforms(bone))
            {
                if (otherBones.Contains(current))
                {
                    return true;
                }
            }
            return false;
        }

        private List<Transform> GetPhysBoneAffectedTransforms(VRCPhysBone bone)
        {
            var result = new List<Transform>();
            var root = bone.rootTransform != null ? bone.rootTransform : bone.gameObject.transform; // ?? 演算子は使えない
            // root 配下を全て追加
            result.AddRange(root.GetComponentsInChildren<Transform>(true));
            // ignores 配下を全て削除
            foreach (var ignores in bone.ignoreTransforms)
            {
                if (ignores != null)
                {
                    foreach (var ig in ignores.GetComponentsInChildren<Transform>(true))
                    {
                        result.Remove(ig);
                    }
                }
            }
            // Ignoreの場合、子を複数持つTransformは除外する
            if (bone.multiChildType == VRC.Dynamics.VRCPhysBoneBase.MultiChildType.Ignore)
            {
                var mc = new List<Transform>();
                foreach (var t in result)
                {
                    if (2 <= result.Where(t2 => t == t2.parent).Count())
                    {
                        mc.Add(t);
                    }
                }
                result.RemoveAll(mc.Contains);
            }
            return result;
        }

        private void VerifyVRCPhysBoneCollider(VRCPhysBoneCollider collider)
        {
            // 参照先が avatar の外
            if (collider.rootTransform != null && !IsChildrenOf(collider.rootTransform.gameObject, _avatar))
            {
                Debug.LogWarningFormat(collider, "[{0}] {1} の Root が Avatar の外を指しています", Title, collider);
            }

            // VRCPhysBone から参照されていない VRCPhysBoneColliderBase があるかどうか
            if (!_avatar.GetComponentsInChildren<VRCPhysBone>(true).Any(bone => bone.colliders.Contains(collider)))
            {
                Debug.LogWarningFormat(collider, "[{0}] {1} は、どの VRCPhysBone からも参照されていません", Title, collider);
            }
        }

        private Transform GetRootTransform(VRCPhysBone bone)
        {
            return bone.rootTransform != null ? bone.rootTransform : bone.gameObject.transform; // ?? 演算子は使えない
        }

        private Transform GetRootTransform(VRCPhysBoneCollider collider)
        {
            return collider.rootTransform != null ? collider.rootTransform : collider.gameObject.transform; // ?? 演算子は使えない
        }

        #endregion

        #region 参照の再設定

        /// <summary>
        /// 参照の再設定
        /// </summary>
        private void DoConnectReferences()
        {
            // アバタールートから全てのVRCPhysBoneについて
            foreach (var bone in _avatar.GetComponentsInChildren<VRCPhysBone>(true))
            {
                // m_Root の再接続
                bone.rootTransform = FindTargetFromArmature(bone, GetRootTransform(bone));
                // m_Colliders の再接続
                for (int i = 0; i < bone.colliders.Count; i++)
                {
                    bone.colliders[i] = FindTargetFromArmature(bone, bone.colliders[i]);
                }
                // m_Exclusions の再接続
                for (int i = 0; i < bone.ignoreTransforms.Count; i++)
                {
                    bone.ignoreTransforms[i] = FindTargetFromArmature(bone, bone.ignoreTransforms[i]);
                }
            }
            // アバタールートから全てのVRCPhysBoneColliderについて
            foreach (var collider in _avatar.GetComponentsInChildren<VRCPhysBoneCollider>(true))
            {
                // m_Root の再接続
                collider.rootTransform = FindTargetFromArmature(collider, GetRootTransform(collider));
            }
        }

        private T FindTargetFromArmature<T>(UnityEngine.Object source, T obj) where T : Component
        {
            if (obj == null)
            {
                return null;
            }

            // 既に Armature の子ならばそのまま返却
            if (_armature.GetComponentsInChildren<T>(true).Any(cmp => cmp == obj))
            {
                return obj;
            }

            // Armature から名称の一致する子を検索して返却
            string target_name = obj.gameObject.name;
            var targets = _armature.GetComponentsInChildren<T>(true).Where(cmp => cmp.gameObject.name == target_name).ToArray();
            if (1 <= targets.Length)
            {
                if (targets.Length != 1)
                {
                    Debug.LogWarning("名称 " + target_name + " と一致するGameObjectが Armature から複数見つかりました", source);
                }
                // 先頭要素を返却
                return targets[0];
            }

            // Armatureから見つからなかったならばAvatarからも探す
            targets = _avatar.GetComponentsInChildren<T>(true).Where(cmp => cmp.gameObject.name == target_name).ToArray();
            if (1 <= targets.Length)
            {
                if (targets.Length != 1)
                {
                    Debug.LogWarning("名称 " + target_name + " と一致するGameObjectが Avatar から複数見つかりました", source);
                }
                else
                {
                    Debug.LogWarning("名称 " + target_name + " と一致するGameObjectを Armature 外から見つけて設定しました", source);
                }
                // 先頭要素を返却
                return targets[0];
            }

            // 見つからなかったときはnullを返すのではなく引数をそのまま返却する
            Debug.LogWarning("名称 " + target_name + " と一致するGameObjectが見つかりませんでした", source);
            return obj;
        }

        #endregion

        #region アタッチ先オブジェクトの変更

        private void MovePhysBoneToBone()
        {
            // アバタールートから全てのVRCPhysBoneについて
            foreach (var pb in _avatar.GetComponentsInChildren<VRCPhysBone>(true))
            {
                MoveComponentTo(pb, GetRootTransform(pb).gameObject);
            }
        }

        private void MovePhysBoneToSingle()
        {
            var rootObject = CreateEmptyGameObject(_avatar, _avatar.transform, "PhysBone");
            // アバタールートから全てのVRCPhysBoneについて
            foreach (var pb in _avatar.GetComponentsInChildren<VRCPhysBone>(true))
            {
                // ルートが設定されていないならば設定しておく
                pb.rootTransform = GetRootTransform(pb);
                // ボーンと同名のオブジェクトを新しく作って
                var target = CreateEmptyGameObject(rootObject, pb.rootTransform, pb.gameObject.name);
                // 移動
                MoveComponentTo(pb, target);
            }
        }

        private void MovePhysBoneColliderToBone()
        {
            // アバタールートから全てのVRCPhysBoneについて
            foreach (var pbc in _avatar.GetComponentsInChildren<VRCPhysBoneCollider>(true))
            {
                MoveComponentTo(pbc, GetRootTransform(pbc).gameObject);
            }
        }

        private void MovePhysBoneColliderToSingle()
        {
            var rootObject = CreateEmptyGameObject(_avatar, _avatar.transform, "PhysBoneCollider");
            // アバタールートから全てのVRCPhysBoneについて
            foreach (var pbc in _avatar.GetComponentsInChildren<VRCPhysBoneCollider>(true))
            {
                // ルートが設定されていないならば設定しておく
                pbc.rootTransform = GetRootTransform(pbc);
                // ボーンと同名のオブジェクトを新しく作って
                var target = CreateEmptyGameObject(rootObject, pbc.rootTransform, pbc.gameObject.name);
                // 移動
                MoveComponentTo(pbc, target);
            }
        }

        private void MoveComponentTo(VRCPhysBone bone, GameObject target)
        {
            if (bone == null || target == null)
            {
                return;
            }
            if (bone.gameObject == target)
            { // 同じオブジェクトに移動しようとしている
                return;
            }
            if (ExistsSameVRCPhysBone(target, bone))
            { // 同じボーンを揺らす VRCPhysBone が target に付いているなら移動しない
                return;
            }

            // コピー＆ペースト
            bone.rootTransform = GetRootTransform(bone); // ルートが設定されていないならば設定しておく
            UnityEditorInternal.ComponentUtility.CopyComponent(bone);
            UnityEditorInternal.ComponentUtility.PasteComponentAsNew(target);

            // 元のコンポーネントを削除
            DestroyImmediate(bone);
        }

        private void MoveComponentTo(VRCPhysBoneCollider collider, GameObject target)
        {
            if (collider == null || target == null)
            {
                return;
            }
            if (collider.gameObject == target)
            { // 同じオブジェクトに移動しようとしている
                return;
            }
            // コピー＆ペースト
            collider.rootTransform = GetRootTransform(collider); // ルートが設定されていないならば設定しておく
            UnityEditorInternal.ComponentUtility.CopyComponent(collider);
            UnityEditorInternal.ComponentUtility.PasteComponentAsNew(target);

            // アバター配下のPhysBone全ての参照を変更する
            var dst = target.GetComponents<VRCPhysBoneCollider>().LastOrDefault();
            foreach (var pb in _avatar.GetComponentsInChildren<VRCPhysBone>(true))
            {
                for (int i = 0; i < pb.colliders.Count; i++)
                {
                    if (pb.colliders[i] == collider)
                    {
                        pb.colliders[i] = dst;
                    }
                }
            }

            // 元のコンポーネントを削除
            DestroyImmediate(collider);
        }

        #endregion

        private bool IsChildrenOf(GameObject it, GameObject parent)
        {
            return parent.GetComponentsInChildren<Transform>(true).Any(cmp => cmp.gameObject == it);
        }

        private static GameObject CreateEmptyGameObject(GameObject parent, Transform transform, String name)
        {
            var obj = new GameObject(name);
            // world space でコピーすることで parent.transform と transform の不一致を吸収する
            obj.transform.position = transform.position;
            obj.transform.rotation = transform.rotation;
            obj.transform.localScale = transform.lossyScale;
            // 移動してから parent を変更すると、このタイミングで local space に計算してくれる
            obj.transform.parent = parent.transform;
            return obj;
        }

        /// <summary>
        /// 指定された VRCPhysBone と同じボーンを揺らす VRCPhysBone が GameObject に存在するかどうかを返す。
        /// </summary>
        /// <param name="bone"></param>
        /// <returns></returns>
        private bool ExistsSameVRCPhysBone(GameObject go, VRCPhysBone bone)
        {
            return GetRootTransform(bone).gameObject.GetComponents<VRCPhysBone>().Any(cmp => GetRootTransform(cmp) == go);
        }

    }
}

#endif
