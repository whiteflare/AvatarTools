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

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace WF.Utillty.AnimEdit
{
    internal class ModeOtherAvatarMaskTools : AnimEditUtilWindowEditMode
    {
        private bool t01AllowHumanoid = true;
        private bool t01AllowTransform = true;

        private AvatarMask t02AvatarMask = null;

        private AvatarMask t03AvatarMaskSrc = null;
        private AvatarMask t03AvatarMaskDst = null;
        private bool t03AddIfAbsent = false;


        public override void OnGUI()
        {
            var oldColor = GUI.color;

            GUILayout.Label("Generate AvatarMask", StyleHeader);
            {
                EditorGUILayout.HelpBox("Avatar から AvatarMask を生成します。", MessageType.Info);

                EditorGUILayout.Space();

                param.avatarRoot = ObjectFieldRequired("Avatar Root", param.avatarRoot, param.avatarRoot == null ? lightRed : GUI.color);
                t01AllowHumanoid = EditorGUILayout.Toggle("Allow Humanoid", t01AllowHumanoid);
                t01AllowTransform = EditorGUILayout.Toggle("Allow Transform", t01AllowTransform);

                EditorGUILayout.Space();

                if (BlueButton("Generate", param.avatarRoot == null))
                {
                    ExecuteGenAvatarMask();
                }
            }

            EditorGUILayout.Space();

            GUILayout.Label("Edit AvatarMask Transform", StyleHeader);
            {
                EditorGUILayout.HelpBox("AvatarMask の Transform を追加・削除します。", MessageType.Info);

                EditorGUILayout.Space();

                t02AvatarMask = ObjectFieldRequired("AvatarMask", t02AvatarMask, t02AvatarMask == null ? lightRed : GUI.color);
                param.avatarRoot = ObjectFieldRequired("Avatar Root", param.avatarRoot, param.avatarRoot == null ? lightRed : GUI.color);

                EditorGUILayout.Space();

                using (new EditorGUI.DisabledGroupScope(param.avatarRoot == null || t02AvatarMask == null))
                {
                    EditorGUILayout.BeginHorizontal();
                    if (BlueButton("Add Transform"))
                    {
                        ExecuteEditAvatarMaskAdd();
                    }
                    if (BlueButton("Remove Unmatch Transform"))
                    {
                        ExecuteEditAvatarMaskRemove();
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }

            EditorGUILayout.Space();

            GUILayout.Label("Copy AvatarMask", StyleHeader);
            {
                EditorGUILayout.HelpBox("AvatarMask 間で設定値をコピーします。", MessageType.Info);

                EditorGUILayout.Space();

                t03AvatarMaskSrc = ObjectFieldRequired("コピー元 (from)", t03AvatarMaskSrc, t03AvatarMaskSrc == null ? lightRed : GUI.color);
                t03AvatarMaskDst = ObjectFieldRequired("コピー先 (to)", t03AvatarMaskDst, t03AvatarMaskDst == null ? lightRed : GUI.color);
                t03AddIfAbsent = EditorGUILayout.Toggle("存在しない項目は追加", t03AddIfAbsent);

                EditorGUILayout.Space();

                if (BlueButton("Copy", t03AvatarMaskSrc == null || t03AvatarMaskDst == null))
                {
                    ExecuteCopyAvatarMask();
                }
            }

            EditorGUILayout.Space();
        }

        private void ExecuteGenAvatarMask()
        {
            var path = EditorUtility.SaveFilePanelInProject("AvatarMask の保存", "", "mask", "");
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }
            // 保存
            CreateOrOverrideAsset(AnimatorEditUtility.GenerateAvatarMask(param.avatarRoot), path);

            // 通知
            OnAfterExecute();
        }

        private void ExecuteEditAvatarMaskAdd()
        {
            Undo.RecordObject(t02AvatarMask, "Edit AvatarMask");

            // AvatarRootからTransformを列挙し、avatarMaskに含まれていないものを追加する
            AnimatorEditUtility.AddAvatarMaskPathIfAbsent(t02AvatarMask, param.avatarRoot);

            EditorUtility.SetDirty(t02AvatarMask);

            // 通知
            OnAfterExecute();
        }

        private void ExecuteEditAvatarMaskRemove()
        {
            Undo.RecordObject(t02AvatarMask, "Edit AvatarMask");

            // AvatarRootからTransformを列挙し、avatarMaskには含まれているがAvatarRootには含まれていないものを削除する
            AnimatorEditUtility.RemoveAvatarMaskPathIfUnmatched(t02AvatarMask, param.avatarRoot);

            EditorUtility.SetDirty(t02AvatarMask);

            // 通知
            OnAfterExecute();
        }

        private void ExecuteCopyAvatarMask()
        {
            Undo.RecordObject(t03AvatarMaskDst, "Edit AvatarMask");

            // 足りない項目を追加
            if (t03AddIfAbsent)
            {
                AnimatorEditUtility.AddAvatarMaskPathIfAbsent(t03AvatarMaskDst, AnimatorEditUtility.GetAllTransformPath(t03AvatarMaskSrc));
            }

            // 改めてコピー
            AnimatorEditUtility.CopyAvatarMask(t03AvatarMaskSrc, t03AvatarMaskDst);
            EditorUtility.SetDirty(t03AvatarMaskDst);

            // 通知
            OnAfterExecute();
        }

        public override string GetShortName()
        {
            return "AvatarMask Tools";
        }

    }
}

#endif
