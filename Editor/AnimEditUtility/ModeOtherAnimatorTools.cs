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
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace WF.Tool.Avatar.AnimEdit
{
    internal class ModeOtherAnimatorTools : AnimEditUtilWindowEditMode
    {
        public bool writeDefault = false;
        public bool writeDefaultMixed = false;

        public bool canTransitionToSelf = false;
        public bool canTransitionToSelfMixed = false;

        public AnimationClip emptyAnimClip;

        public int replaceLayerIndex;
        public int replaceIndexFrom = 0;
        public string replaceNameFrom = "";
        public string replaceNameTo = "";

        public int namingLayerIndex;

        public override void ResetCommonParam(AnimEditUtilWindowCommonParam newParam)
        {
            base.ResetCommonParam(newParam);
            ResetWriteDefaultValue();
            ResetCanTransitionToSelfValue();
            if (emptyAnimClip == null)
            {
                emptyAnimClip = LoadEmptyAnimClip();
            }
        }

        private void ResetWriteDefaultValue()
        {
            // リセット
            writeDefault = false;
            writeDefaultMixed = false;
            // 回収
            if (param != null && param.animator != null)
            {
                writeDefault = AnimatorEditUtility.GetWriteDefault(param.animator, out writeDefaultMixed);
            }
        }

        private void ResetCanTransitionToSelfValue()
        {
            // リセット
            canTransitionToSelf = false;
            canTransitionToSelfMixed = false;
            // 回収
            if (param != null && param.animator != null)
            {
                canTransitionToSelf = AnimatorEditUtility.GetCanTransitionToSelf(param.animator, out canTransitionToSelfMixed);
            }
        }

        public override void OnGUI()
        {
            var oldColor = GUI.color;

            GUILayout.Label("Change Write Defaults", StyleHeader);
            {
                EditorGUILayout.HelpBox("WriteDefault のオンオフを切り替えます。", MessageType.Info);

                EditorGUILayout.Space();

                EditorGUI.BeginChangeCheck();
                param.animator = ObjectFieldRequired(LabelAnimatorController, param.animator, lightRed);
                if (EditorGUI.EndChangeCheck())
                {
                    ResetWriteDefaultValue();
                }

                EditorGUI.showMixedValue = writeDefaultMixed;
                EditorGUI.BeginChangeCheck();
                writeDefault = EditorGUILayout.Toggle("Write Defaults", writeDefault);
                if (EditorGUI.EndChangeCheck())
                {
                    writeDefaultMixed = false;
                }
                EditorGUI.showMixedValue = false;

                EditorGUILayout.Space();

                if (writeDefaultMixed)
                {
                    EditorGUILayout.HelpBox("WriteDefault の true/false が混在しています。どちらかに統一してください。", MessageType.Warning);
                }

                if (BlueButton("Set WriteDefaults", param.animator == null || writeDefaultMixed))
                {
                    if (ConfirmContinue())
                    {
                        AnimatorEditUtility.SetWriteDefault(param.animator, writeDefault);
                        writeDefaultMixed = false;
                        // 通知
                        OnAfterExecute();
                    }
                }
            }

            EditorGUILayout.Space();
            GUILayout.Label("Change Can Transition To Self", StyleHeader);
            {
                EditorGUILayout.HelpBox("CanTransitionToSelf のオンオフを切り替えます。", MessageType.Info);

                EditorGUILayout.Space();

                EditorGUI.BeginChangeCheck();
                param.animator = ObjectFieldRequired(LabelAnimatorController, param.animator, lightRed);
                if (EditorGUI.EndChangeCheck())
                {
                    ResetCanTransitionToSelfValue();
                }

                EditorGUI.showMixedValue = canTransitionToSelfMixed;
                EditorGUI.BeginChangeCheck();
                canTransitionToSelf = EditorGUILayout.Toggle("Can Transition To Self", canTransitionToSelf);
                if (EditorGUI.EndChangeCheck())
                {
                    canTransitionToSelfMixed = false;
                }
                EditorGUI.showMixedValue = false;

                EditorGUILayout.Space();

                if (canTransitionToSelfMixed)
                {
                    EditorGUILayout.HelpBox("CanTransitionToSelf の true/false が混在しています。", MessageType.Warning);
                }

                if (BlueButton("Set CanTransitionToSelf", param.animator == null || canTransitionToSelfMixed))
                {
                    if (ConfirmContinue())
                    {
                        AnimatorEditUtility.SetCanTransitionToSelf(param.animator, canTransitionToSelf);
                        canTransitionToSelfMixed = false;
                        // 通知
                        OnAfterExecute();
                    }
                }
            }


            EditorGUILayout.Space();
            GUILayout.Label("Fill Empty AnimationClip", StyleHeader);
            {
                EditorGUILayout.HelpBox("motion 未指定の State に空のアニメーションクリップを設定します。", MessageType.Info);

                EditorGUILayout.Space();

                param.animator = ObjectFieldRequired(LabelAnimatorController, param.animator, lightRed);
                emptyAnimClip = ObjectFieldRequired("Empty AnimationClip", emptyAnimClip, lightRed);

                EditorGUILayout.Space();

                if (param.animator != null && AnimatorEditUtility.GetAllState(param.animator).Any(st => st.motion == null))
                {
                    EditorGUILayout.HelpBox("motion 未指定の State が存在します。", MessageType.Warning);
                }

                if (BlueButton("Fill Empty AnimationClip", param.animator == null || emptyAnimClip == null))
                {
                    if (ConfirmContinue())
                    {
                        ExecuteGenerateEmptyClipIfAbsent(ref emptyAnimClip);
                        ExecuteFillEmptyClip(param.animator, emptyAnimClip);
                        // 通知
                        OnAfterExecute();
                    }
                }
            }


            EditorGUILayout.Space();
            GUILayout.Label("Replace Parameter", StyleHeader);
            {
                EditorGUILayout.HelpBox("Parameter を置換します。", MessageType.Info);

                EditorGUILayout.Space();

                EditorGUI.BeginChangeCheck();

                param.animator = ObjectFieldRequired(LabelAnimatorController, param.animator, lightRed);
                replaceLayerIndex = IntPopupAnimatorLayer(LabelLayerName, param.animator, replaceLayerIndex);
                replaceIndexFrom = IntPopupAnimatorParameter("Before", param.animator, replaceLayerIndex, replaceIndexFrom, out replaceNameFrom);

                if (EditorGUI.EndChangeCheck())
                {
                    replaceNameTo = replaceNameFrom;
                }
                if (GetAnimatorLayer(param.animator, replaceLayerIndex) == null)
                {
                    replaceNameTo = "";
                }
                replaceNameTo = TextFieldColored("After", replaceNameTo, string.IsNullOrWhiteSpace(replaceNameTo) || replaceNameFrom == replaceNameTo ? lightRed : GUI.color);

                EditorGUILayout.Space();

                if (BlueButton("Replace Parameter", param.animator == null || string.IsNullOrWhiteSpace(replaceNameFrom) || string.IsNullOrWhiteSpace(replaceNameTo) || replaceNameFrom == replaceNameTo))
                {
                    if (ConfirmContinue())
                    {
                        ExecuteReplaceParameter(param.animator, replaceLayerIndex, replaceNameFrom, replaceNameTo);
                        // 通知
                        OnAfterExecute();
                    }
                }
            }

            EditorGUILayout.Space();
            GUILayout.Label("Format State Names", StyleHeader);
            {
                EditorGUILayout.HelpBox("AnimatorControllerLayer 内 State の名称を振り直します。", MessageType.Info);

                EditorGUILayout.Space();

                param.animator = ObjectFieldRequired(LabelAnimatorController, param.animator, lightRed);
                namingLayerIndex = IntPopupAnimatorLayer(LabelLayerName, param.animator, namingLayerIndex);

                EditorGUILayout.Space();

                if (BlueButton("Format State Names", param.animator == null))
                {
                    if (ConfirmContinue())
                    {
                        ExecuteFormatStateNames(param.animator, namingLayerIndex);
                        // 通知
                        OnAfterExecute();
                    }
                }
            }

            EditorGUILayout.Space();
            GUILayout.Label("Cleanup AnimatorController", StyleHeader);
            {
                EditorGUILayout.HelpBox("AnimatorController 内の未使用データを削除します。同一フォルダにバックアップを作成します。", MessageType.Info);

                EditorGUILayout.Space();

                param.animator = ObjectFieldRequired(LabelAnimatorController, param.animator, lightRed);

                EditorGUILayout.Space();

                if (BlueButton("Cleanup AnimatorController", param.animator == null))
                {
                    if (ConfirmContinue())
                    {
                        ExecuteCleanupAnimator(param.animator);
                        // 通知
                        OnAfterExecute();
                    }
                }
            }
        }

        public static void ExecuteFormatStateNames(AnimatorController animator, int namingLayerIndex)
        {
            var layer = GetAnimatorLayer(animator, namingLayerIndex);
            if (layer != null)
            {
                var names = new System.Collections.Generic.Dictionary<string, int>();
                foreach (var state in AnimatorEditUtility.GetAllState(layer))
                {
                    string suggestedName;
                    if (state.motion != null)
                    {
                        if (!string.IsNullOrWhiteSpace(state.motion.name))
                        {
                            suggestedName = state.motion.name;
                        }
                        else
                        {
                            suggestedName = state.motion.GetType().Name;
                        }
                    }
                    else
                    {
                        suggestedName = "None";
                    }

                    if (names.ContainsKey(suggestedName))
                    {
                        var index = names[suggestedName] + 1;
                        state.name = suggestedName + " " + index;
                        names[suggestedName] = index;

                    }
                    else
                    {
                        names[suggestedName] = 0;
                        state.name = suggestedName;
                    }
                    EditorUtility.SetDirty(state);
                }
            }

            EditorUtility.SetDirty(animator);
        }

        public static void ExecuteCleanupAnimator(AnimatorController animator)
        {
            EditorUtility.SetDirty(animator);
            AssetDatabase.SaveAssets();

            // バックアップファイルにコピー
            var path = AssetDatabase.GetAssetPath(animator);
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            var pathTemp = new Regex(@"\.controller$", RegexOptions.IgnoreCase).Replace(path, "_bak.controller");
            pathTemp = AssetDatabase.GenerateUniqueAssetPath(pathTemp);
            if (!AssetDatabase.CopyAsset(path, pathTemp))
            {
                return;
            }

            // 元のAnimatorの全データを削除
            animator.layers = new AnimatorControllerLayer[0];
            animator.parameters = new AnimatorControllerParameter[0];
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (asset == null || asset is AnimatorController)
                {
                    continue;
                }
                AssetDatabase.RemoveObjectFromAsset(asset);
            }

            EditorUtility.SetDirty(animator);
            AssetDatabase.SaveAssets();

            var animatorTemp = AssetDatabase.LoadAssetAtPath<AnimatorController>(pathTemp);

            foreach (var layer in animatorTemp.layers)
            {
                AnimatorEditUtility.DuplicateLayer(animatorTemp, layer, animator, layer.name);
                // Parameterが無いならそれもコピー
                foreach (var prm in AnimatorEditUtility.GetUsedParameters(animatorTemp, layer))
                {
                    AnimatorEditUtility.AddParameterIfAbsent(animator, prm.name, prm.type);
                }
            }

            EditorUtility.SetDirty(animator);
            AssetDatabase.SaveAssets();
        }

        public static void ExecuteFillEmptyClip(AnimatorController animator, AnimationClip emptyAnimClip)
        {
            if (emptyAnimClip != null)
            {
                foreach (var state in AnimatorEditUtility.GetAllState(animator))
                {
                    if (state.motion == null)
                    {
                        state.motion = emptyAnimClip;
                        EditorUtility.SetDirty(state);
                    }
                }
            }
        }

        private void ExecuteReplaceParameter(AnimatorController animator, int replaceLayerIndex, string replaceNameFrom, string replaceNameTo)
        {
            var layer = GetAnimatorLayer(animator, replaceLayerIndex);
            if (layer != null)
            {
                var prm = animator.parameters.Where(p => p.name == replaceNameFrom).FirstOrDefault();
                if (prm != null)
                {
                    // パラメータを準備
                    AnimatorEditUtility.AddParameterIfAbsent(animator, replaceNameTo, prm.type);
                    // 置換
                    AnimatorEditUtility.ReplaceParameter(layer, replaceNameFrom, replaceNameTo);
                    EditorUtility.SetDirty(animator);
                }
            }
        }

        public static void ExecuteGenerateEmptyClipIfAbsent(ref AnimationClip emptyAnimClip)
        {
            if (emptyAnimClip == null)
            {
                var path = SaveFilePanelInProject(AnimEditUtilWindow.Title + ": Save Clip", "Empty", "anim", "");
                if (string.IsNullOrWhiteSpace(path))
                {
                    return;
                }
                var newClip = new AnimationClip();
                CreateOrOverrideAsset(newClip, path);

                emptyAnimClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            }
        }

        public override string GetShortName()
        {
            return "Animator Tools";
        }

    }
}

#endif
