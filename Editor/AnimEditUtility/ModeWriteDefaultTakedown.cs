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
    internal class ModeWriteDefaultTakedown : AnimEditUtilWindowEditMode
    {
        public AnimationClip emptyAnimClip;
        public AnimationClip resetAnimClip;

        public override void ResetCommonParam(AnimEditUtilWindowCommonParam newParam)
        {
            base.ResetCommonParam(newParam);
            // リセットアニメはアバターごとに異なるので再設定が必要でnullにするが、空アニメはほぼ同じなのでリセットしない
            resetAnimClip = null;
            // EmptyはGUIDの一致するAssetパスがあればロードしてみる
            if (emptyAnimClip == null)
            {
                emptyAnimClip = LoadEmptyAnimClip();
            }
        }

        public override void OnGUI()
        {
            var oldColor = GUI.color;

            GUILayout.Label("WriteDefaults Takedown", StyleHeader);

            EditorGUILayout.HelpBox("WriteDefaults がオンの FX Animator を WriteDefaults オフに再構成します。", MessageType.Info);

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Animator", EditorStyles.boldLabel);
            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUI.BeginChangeCheck();
                param.animator = ObjectFieldRequired(LabelAnimatorController, param.animator, lightRed);
                if (EditorGUI.EndChangeCheck())
                {
                    // リセットアニメはアバターごとに異なるので再設定が必要でnullにするが、空アニメはほぼ同じなのでリセットしない
                    resetAnimClip = null;
                }
            }

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Animation Clip (Optional)", EditorStyles.boldLabel);
            using (new EditorGUI.IndentLevelScope())
            {
                resetAnimClip = (AnimationClip)EditorGUILayout.ObjectField(new GUIContent("Reset AnimationClip"), resetAnimClip, typeof(AnimationClip), false);
                emptyAnimClip = (AnimationClip)EditorGUILayout.ObjectField(new GUIContent("Empty AnimationClip"), emptyAnimClip, typeof(AnimationClip), false);
            }

            EditorGUILayout.Space();

            if (BlueButton("Takedown", !CanExecute()))
            {
                Execute();
            }
        }

        public override string GetShortName()
        {
            return "WriteDefaults";
        }

        private bool CanExecute()
        {
            if (param.avatarRoot == null)
            {
                EditorGUILayout.HelpBox(string.Format("{0} が未設定です。", "Avatar Root"), MessageType.Error);
                return false;
            }
            if (param.animator == null)
            {
                EditorGUILayout.HelpBox(string.Format("{0} が未設定です。", LabelAnimatorController), MessageType.Error);
                return false;
            }
            if (param.animator != null && !AnimatorEditUtility.GetWriteDefault(param.animator, out var ignored))
            {
                EditorGUILayout.HelpBox("WriteDefaults は既にオフになっています。", MessageType.Error);
                return false;
            }
            if (param.animator != null)
            {
                var layer = GetTopLayer(param.animator);
                if (1 <= AnimatorEditUtility.GetAllState(layer).Count())
                {
                    EditorGUILayout.HelpBox(string.Format("一番上のレイヤー {0} に AnimatorState が存在します。Default State を変更するため動作に注意してください。", layer.name), MessageType.Warning);
                    // これは通す
                }
            }
            return true;
        }

        private AnimatorControllerLayer GetTopLayer(AnimatorController animator)
        {
            if (animator != null && 1 <= animator.layers.Length)
            {
                return animator.layers[0];
            }
            return null;
        }

        private void Execute()
        {
            // リセット用アニメーションクリップの作成
            if (resetAnimClip == null)
            {
                resetAnimClip = ModeOtherAnimClipTools.ExecuteGenerateResetClip(AnimatorEditUtility.GetAllAnimationClip(param.animator).ToArray(), param.avatarRoot);
                if (resetAnimClip == null)
                {
                    return; // キャンセル時は何もしない
                }
            }

            // 空のアニメーションクリップの作成
            if (emptyAnimClip == null && AnimatorEditUtility.HasNoneMotionState(param.animator))
            {
                ModeOtherAnimatorTools.ExecuteGenerateEmptyClipIfAbsent(ref emptyAnimClip);
                if (emptyAnimClip == null)
                {
                    return; // キャンセル時は何もしない
                }
            }

            if (!ConfirmContinue())
            {
                return;
            }

            // 空アニメーションを motion に設定する
            ModeOtherAnimatorTools.ExecuteFillEmptyClip(param.animator, emptyAnimClip);

            // リセットアニメーションを BaseLayer に設定する
            var layer = GetTopLayer(param.animator);
            if (layer != null && layer.stateMachine != null)
            {
                // State追加
                var state = layer.stateMachine.AddState(resetAnimClip.name);
                layer.stateMachine.defaultState = state;
                state.motion = resetAnimClip;
                EditorUtility.SetDirty(layer.stateMachine);

                // AvatarMaskを外す
                var layers = param.animator.layers; // マスクを外すときはlayersをコピーしないと反映されない
                layers[0].avatarMask = null;
                layers[0].defaultWeight = 1;
                param.animator.layers = layers;
                EditorUtility.SetDirty(param.animator);
            }

            // WriteDefaults をオフにする
            AnimatorEditUtility.SetWriteDefault(param.animator, false);

            // 保存
            AssetDatabase.SaveAssets();

            // 通知
            OnAfterExecute();
        }
    }
}

#endif
