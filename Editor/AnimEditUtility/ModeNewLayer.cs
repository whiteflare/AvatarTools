/*
 *  The MIT License
 *
 *  Copyright 2021-2022 whiteflare.
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
using UnityEngine;

#if ENV_VRCSDK3_AVATAR
using VRC.SDK3.Avatars.ScriptableObjects;
#endif

namespace WF.Tool.Avatar.AnimEdit
{
    internal class ModeNewLayer : AnimEditUtilWindowEditMode
    {
        protected const string LabelSaved = "Saved";

        public bool writeDefault;
        public bool writeDefaultMixed;
        public string layerName;
        public float duration;
        public bool isSaved = true;
        public bool addFirst = false;

        public override void ResetCommonParam(AnimEditUtilWindowCommonParam newParam)
        {
            base.ResetCommonParam(newParam);
            // リセット
            writeDefault = false;
            writeDefaultMixed = false;
            // 回収
            if (param != null && param.animator != null)
            {
                writeDefault = AnimatorEditUtility.GetWriteDefault(param.animator, out writeDefaultMixed);
            }
        }

        public override void OnGUI()
        {
            var oldColor = GUI.color;

            ////////////////
            // New Layer
            ////////////////

            GUILayout.Label("Add New AnimatorControllerLayer", StyleHeader);

            EditorGUILayout.HelpBox("AnimatorController に新しい Layer と Parameter を追加します。", MessageType.Info);
            EditorGUILayout.Space();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("AnimatorController Settings", EditorStyles.boldLabel);

            using (new EditorGUI.IndentLevelScope())
            {
                param.animator = ObjectFieldRequired(LabelAnimatorController, param.animator, lightRed);

                // LayerName と VarName が同じなら、LayerName 変更時に VarName も同期させる
                bool isSameName = IsSameString(layerName, param.varName);

                EditorGUI.BeginChangeCheck();
                layerName = TextFieldColored(LabelLayerName, layerName,
                    string.IsNullOrWhiteSpace(layerName) ? lightRed :
                    AnimatorEditUtility.HasLayer(param.animator, layerName) ? lightYellow : GUI.color);
                if (EditorGUI.EndChangeCheck())
                {
                    if (isSameName)
                    {
                        param.varName = layerName;
                    }
                }
                param.varName = TextFieldColored(LabelVariableName, param.varName,
                    string.IsNullOrWhiteSpace(param.varName) ? lightRed :
                    AnimatorEditUtility.HasParameter(param.animator, param.varName) ? lightYellow : GUI.color);

                param.varType = EditorGUILayout.Popup(LabelVariableType, param.varType, new string[] { "bool", "int" });

                duration = EditorGUILayout.FloatField(LabelDuration, duration);

                EditorGUI.showMixedValue = writeDefaultMixed;
                EditorGUI.BeginChangeCheck();
                writeDefault = EditorGUILayout.Toggle(LabelWriteDefaults, writeDefault);
                if (EditorGUI.EndChangeCheck())
                {
                    writeDefaultMixed = false;
                }
                EditorGUI.showMixedValue = false;
            }

            OnGuiSub_ClipEditButtons();

            var so = new SerializedObject(param);
            so.Update();
            EditorGUI.BeginChangeCheck();
            using (new ChangeColorScope(param.clips.Length == 0 ? lightRed : GUI.color))
                EditorGUILayout.PropertyField(so.FindProperty("clips"), new GUIContent(LabelAnimationClip), true);
            if (EditorGUI.EndChangeCheck())
            {
                so.ApplyModifiedProperties();
            }

#if ENV_VRCSDK3_AVATAR
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("ExpressionParameter Settings", EditorStyles.boldLabel);
            using (new EditorGUI.IndentLevelScope())
            {
                param.expParams = ObjectFieldRequired(LabelVRCExpressionParameters, param.expParams, lightYellow);
                isSaved = EditorGUILayout.Toggle(LabelSaved, isSaved);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("ExpressionMenu Settings", EditorStyles.boldLabel);
            using (new EditorGUI.IndentLevelScope())
            {
                param.expMenu = ObjectFieldRequired(LabelVRCExpressionMenu, param.expMenu, lightYellow);
                addFirst = EditorGUILayout.Toggle("最初のクリップも追加する", addFirst);
                OnGuiSub_SubMenuCreateButton();
            }
#endif

            EditorGUILayout.Space();
            if (BlueButton("Add New Layer", !CanExecuteNewLayer()))
            {
                ExecuteAll();
            }
        }

        private void OnGuiSub_ClipEditButtons()
        {
            GUILayout.BeginHorizontal();
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Add", GUILayout.Width(64)))
                {
                    ArrayUtility.Add(ref param.clips, null);
                    // Add で3以上にした場合はboolからintに変える
                    if (3 <= param.clips.Length && param.varType == 0)
                    {
                        param.varType = 1;
                    }
                }
                using (new EditorGUI.DisabledGroupScope(param.clips.Length == 0))
                {
                    if (GUILayout.Button("Del", GUILayout.Width(64)))
                    {
                        if (1 <= param.clips.Length)
                        {
                            ArrayUtility.RemoveAt(ref param.clips, param.clips.Length - 1);
                        }
                    }
                    if (GUILayout.Button("Sort", GUILayout.Width(64)))
                    {
                        param.clips = param.clips.OrderBy(clip => clip == null).ThenBy(clip => clip == null ? null : clip.name).ToArray();
                    }
                    if (GUILayout.Button("Gen", GUILayout.Width(64)))
                    {
                        FillAnimation();
                    }
                }
            }
            GUILayout.EndHorizontal();
        }

        private void ExecuteAll()
        {
            if (!ConfirmContinue())
            {
                return;
            }

            ExecuteNewLayer();
#if ENV_VRCSDK3_AVATAR
            ExecuteAddParam();
            ExecuteAddMenu();
#endif
            // 通知
            OnAfterExecute();
        }

        public override string GetShortName()
        {
            return "New Layer";
        }

        private bool CanExecuteNewLayer()
        {
            if (param.animator == null)
            {
                EditorGUILayout.HelpBox(string.Format("{0} が未設定です。", LabelAnimatorController), MessageType.Error);
                return false;
            }
            if (string.IsNullOrWhiteSpace(layerName))
            {
                EditorGUILayout.HelpBox(string.Format("{0} が未設定です。", LabelLayerName), MessageType.Error);
                return false;
            }
            if (string.IsNullOrWhiteSpace(param.varName))
            {
                EditorGUILayout.HelpBox(string.Format("{0} が未設定です。", LabelVariableName), MessageType.Error);
                return false;
            }
            if (writeDefaultMixed)
            {
                EditorGUILayout.HelpBox("WriteDefault の true/false が混在しています。どちらかに統一してください。", MessageType.Error);
                return false;
            }
            if (param.clips.Length == 0)
            {
                EditorGUILayout.HelpBox(string.Format("{0} が未設定です。", LabelAnimationClip), MessageType.Error);
                return false;
            }
            if (param.varType == 0 && 2 < param.clips.Length)
            {
                EditorGUILayout.HelpBox(string.Format("{0} が bool のとき {1} は2個以下である必要があります。", LabelVariableType, LabelAnimationClip), MessageType.Error);
                return false;
            }
            if (!writeDefault && param.clips.Any(clip => clip == null))
            {
                EditorGUILayout.HelpBox(string.Format("WriteDefault が False の場合、AnimationClip が None の State は誤動作します。AnimationClip を設定してください。", LabelVariableType, LabelAnimationClip), MessageType.Warning);
                // これは通す
            }

            return true;
        }

        private void ExecuteNewLayer()
        {
            // Param 追加
            var prm = AnimatorEditUtility.AddParameterIfAbsent(param.animator, param.varName,
                param.varType == 0 ? AnimatorControllerParameterType.Bool : AnimatorControllerParameterType.Int);

            // レイヤー追加
            AnimatorEditUtility.AddLayerAndSetupState(param.animator, layerName, param.clips, prm, writeDefault, duration);

            EditorUtility.SetDirty(param.animator);

        }

        private void FillAnimation()
        {
            // clips 内に null があるかどうか確認
            if (!param.clips.Any(c => c == null))
            {
                return; // 無ければ何もしない
            }

            // nullではないアニメが設定されているならコピーして使用する

            // コピー元の AnimationClip
            var srcClips = param.clips.Select(c => c != null && !string.IsNullOrWhiteSpace(AssetDatabase.GetAssetPath(c)) ? c : null).ToArray();

            // 最初のコピー元がないなら 
            var first = srcClips.FirstOrDefault(c => c != null);
            if (first == null)
            {
                var path = EditorUtility.SaveFilePanelInProject(AnimEditUtilWindow.Title + "Generate Clip", "", "anim", "Please enter new AnimationClip filename");
                if (string.IsNullOrWhiteSpace(path))
                {
                    return;
                }
                CreateOrOverrideAsset(new AnimationClip(), path);
                first = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            }
            srcClips[0] = first;

            // コピー元 Fill
            for (int i = 1; i < srcClips.Length; i++)
            {
                if (srcClips[i] == null)
                {
                    srcClips[i] = srcClips[i - 1];
                }
            }

            // clips をコピー
            for (int i = 0; i < param.clips.Length; i++)
            {
                if (param.clips[i] == null)
                {
                    var src = srcClips[i];
                    var path = AssetDatabase.GetAssetPath(src);
                    var newPath = AssetDatabase.GenerateUniqueAssetPath(path);
                    AssetDatabase.CopyAsset(path, newPath);
                    param.clips[i] = AssetDatabase.LoadAssetAtPath<AnimationClip>(newPath);
                }
            }
        }

#if ENV_VRCSDK3_AVATAR

        private void OnGuiSub_SubMenuCreateButton()
        {
            GUILayout.BeginHorizontal();
            {
                GUILayout.FlexibleSpace();
                using (new EditorGUI.DisabledGroupScope(param.expMenu == null))
                {
                    if (GUILayout.Button("New SubMenu", GUILayout.Width(128)))
                    {
                        AvatarAssetEditUtility.CreateNewSubMenu(ref param.expMenu);
                    }
                }
            }
            GUILayout.EndHorizontal();
        }

        private void ExecuteAddParam()
        {
            AnimatorControllerParameterType type = param.varType == 0 ? AnimatorControllerParameterType.Bool : AnimatorControllerParameterType.Int;
            AvatarAssetEditUtility.AddExParameterIfAbsent(param.expParams, param.varName, type, isSaved);
        }

        private void ExecuteAddMenu()
        {
            if (addFirst ? param.clips.Length == 0 : param.clips.Length <= 1)
            {
                // 設定すべき AnimationClip が未登録ならば何もしない
                return;
            }

            // menu を追加
            for (int i = 0; i < param.clips.Length; i++)
            {
                AnimationClip clip = param.clips[i];
                // 未設定の Clip は追加しない
                if (clip == null || string.IsNullOrEmpty(clip.name))
                {
                    continue;
                }
                // 最初の1個は追加しない
                if (!addFirst && i == 0)
                {
                    continue;
                }

                AvatarAssetEditUtility.AddExMenuToggle(param.expMenu, param.varName, clip.name, i);
            }
        }

#endif
    }
}

#endif
