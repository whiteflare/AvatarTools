/*
 *  The MIT License
 *
 *  Copyright 2021-2025 whiteflare.
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
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace WF.Tool.Avatar.AnimEdit
{
    internal class ModeOtherAnimClipTools : AnimEditUtilWindowEditMode
    {
        public AnimatorController t01Controller;
        public AnimationClip[] t01Clips = { };

        public AnimationClip t02SrcClips = null;
        public AnimationClip[] t02DstClips = { };

        public AnimatorController t03Controller;
        public AnimationClip[] t03Clips = { };

        public bool loopTime = false;
        public bool loopTimeMixed = false;

        public override void ResetCommonParam(AnimEditUtilWindowCommonParam newParam)
        {
            base.ResetCommonParam(newParam);
            ResetLoopTime();
        }

        private void ResetLoopTime()
        {
            // リセット
            loopTime = false;
            loopTimeMixed = false;
            // 回収
            loopTime = AnimatorEditUtility.GetAnimClipLoopTime(t01Clips, out loopTimeMixed);
        }

        private void PropertyDstClips(string name, string title)
        {
            EditorGUILayout.PropertyField(serializedThis.FindProperty(name), new GUIContent(title), true);
        }

        public override void OnGUI()
        {
            serializedThis.Update();
            var oldColor = GUI.color;

            GUILayout.Label("Set LoopTime", StyleHeader);
            {
                EditorGUILayout.HelpBox("AnimationClip の LoopTime を一括設定します。", MessageType.Info);

                EditorGUILayout.Space();

                EditorGUI.BeginChangeCheck();
                {
                    t01Controller = (AnimatorController)EditorGUILayout.ObjectField(new GUIContent(LabelAnimatorController), t01Controller, typeof(AnimatorController), true);
                }
                if (EditorGUI.EndChangeCheck())
                {
                    t01Clips = AnimatorEditUtility.GetAllAnimationClip(t01Controller).ToArray();
                }

                EditorGUI.BeginChangeCheck();
                {
                    using(new ChangeColorScope(t01Clips.Length == 0 ? lightRed : GUI.color))
                        PropertyDstClips(nameof(t01Clips), "AnimationClips");
                }
                if (EditorGUI.EndChangeCheck())
                {
                    ResetLoopTime();
                }

                EditorGUI.showMixedValue = loopTimeMixed;
                EditorGUI.BeginChangeCheck();
                loopTime = EditorGUILayout.Toggle("Loop Time", loopTime);
                if (EditorGUI.EndChangeCheck())
                {
                    loopTimeMixed = false;
                }
                EditorGUI.showMixedValue = false;

                EditorGUILayout.Space();

                if (BlueButton("Set Value", t01Clips.Length == 0 || loopTimeMixed))
                {
                    ExecuteSetLoopTime();
                }
            }

            EditorGUILayout.Space();
            GUILayout.Label("Copy Property If Absent", StyleHeader);
            {
                EditorGUILayout.HelpBox("Source の AnimationClip のプロパティが、Destination の AnimationClip にない場合にコピーします。", MessageType.Info);

                EditorGUILayout.Space();

                t02SrcClips = ObjectFieldRequired("Source", t02SrcClips, lightRed);
                using (new ChangeColorScope(t02DstClips.Length == 0 ? lightRed : GUI.color))
                    PropertyDstClips(nameof(t02DstClips), "Destination");

                EditorGUILayout.Space();

                if (BlueButton("Copy Properties", t02SrcClips == null || t02DstClips.Length == 0))
                {
                    ExecutePropertyCopyIfAbsent();
                }
            }

            EditorGUILayout.Space();
            GUILayout.Label("Generate Reset AnimationClip", StyleHeader);
            {
                EditorGUILayout.HelpBox("AnimatorController 内の全ての Property を含む AnimationClip を生成します。", MessageType.Info);

                EditorGUILayout.Space();

                param.avatarRoot = ObjectFieldRequired("Avatar Root", param.avatarRoot, param.avatarRoot == null ? lightRed : GUI.color);
                EditorGUI.BeginChangeCheck();
                {
                    t03Controller = (AnimatorController)EditorGUILayout.ObjectField(new GUIContent(LabelAnimatorController), t03Controller, typeof(AnimatorController), true);
                }
                if (EditorGUI.EndChangeCheck())
                {
                    t03Clips = AnimatorEditUtility.GetAllAnimationClip(t03Controller).ToArray();
                }

                using (new ChangeColorScope(t03Clips.Length == 0 ? lightRed : GUI.color))
                    PropertyDstClips(nameof(t03Clips), "AnimationClips");

                EditorGUILayout.Space();

                if (BlueButton("Generate", param.avatarRoot == null || t03Clips.Length == 0))
                {
                    ExecuteGenerateResetAnim();
                }
            }

            serializedThis.ApplyModifiedPropertiesWithoutUndo();
        }

        private void ExecutePropertyCopyIfAbsent()
        {
            if (!ConfirmContinue())
            {
                return;
            }
            var dstClips = t02DstClips.Distinct().ToArray();
            Undo.RecordObjects(dstClips, "Copy Property If Absent");
            foreach (var dstClip in dstClips)
            {
                if (dstClip == null)
                {
                    continue;
                }
                if (AnimatorEditUtility.CopyAnimPropertiesIfAbsent(t02SrcClips, dstClip))
                {
                    EditorUtility.SetDirty(dstClip);
                }
            }

            // 通知
            OnAfterExecute();
        }

        private void ExecuteGenerateResetAnim()
        {
            ExecuteGenerateResetClip(t03Clips, param.avatarRoot);
            // 通知
            OnAfterExecute();
        }

        public static AnimationClip ExecuteGenerateResetClip(AnimationClip[] clips, GameObject avatarRoot)
        {
            var path = EditorUtility.SaveFilePanelInProject(AnimEditUtilWindow.Title + ": Save Clip", "Reset", "anim", "");
            if (string.IsNullOrWhiteSpace(path))
            {
                return null;
            }
            var newClip = new AnimationClip();
            // newClipに全てのプロパティをリセット状態でコピーする
            foreach (var srcClip in clips)
            {
                if (srcClip == null)
                {
                    continue;
                }
                if (AnimatorEditUtility.CopyResetAnimPropertiesIfAbsent(srcClip, newClip, avatarRoot))
                {
                    EditorUtility.SetDirty(newClip);
                }
            }
            CreateOrOverrideAsset(newClip, path);

            return AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        }

        private void ExecuteSetLoopTime()
        {
            // clipのloopTimeを一括設定する
            if (AnimatorEditUtility.SetAnimClipLoopTime(t01Clips, loopTime))
            {
                AssetDatabase.SaveAssets();
            }

            // 通知
            OnAfterExecute();
        }

        public override string GetShortName()
        {
            return "AnimClip Tools";
        }

    }
}

#endif
