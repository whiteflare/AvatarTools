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

// VRCSDK有無の判定ここから //////
#if VRC_SDK_VRCSDK3
#define ENV_VRCSDK3
#if UDON
#define ENV_VRCSDK3_WORLD
#else
#define ENV_VRCSDK3_AVATAR
#endif
#endif
// VRCSDK有無の判定ここまで //////

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

#if ENV_VRCSDK3_AVATAR
using VRC.SDK3.Avatars.Components;
#endif

namespace WF.Tool.Avatar.AnimEdit
{
    internal class AnimEditUtilWindow : EditorWindow
    {
        public const string Title = "Anim Edit Utility";

        public int modeEdit = 0;
        private Vector2 scrollPosition;

        public AnimEditUtilWindowCommonParam commonParam;

        private System.Type[] modeTypes = {
            typeof(ModeNewLayer),
            typeof(ModeCopyLayer),
            typeof(ModeWriteDefaultTakedown),
#if ENV_VRCSDK3_AVATAR
            typeof(ModeSetupAvatarMask),
#endif
            typeof(ModeOtherAnimatorTools),
            typeof(ModeOtherAnimClipTools),
            typeof(ModeOtherAvatarMaskTools),
        };
        private List<AnimEditUtilWindowEditMode> modes = new List<AnimEditUtilWindowEditMode>();

        private void OnEnable()
        {
            if (commonParam == null)
            {
                commonParam = CreateInstance<AnimEditUtilWindowCommonParam>();
                commonParam.hideFlags = HideFlags.DontSave;
            }

            modes.Clear();
            foreach (var t in modeTypes)
            {
                var m = CreateInstance(t) as AnimEditUtilWindowEditMode;
                m.hideFlags = HideFlags.DontSave;
                m.ResetCommonParam(commonParam);
                m.AfterExecute += OnAfterExecute;
                modes.Add(m);
            }
        }

        private void OnAfterExecute(AnimEditUtilWindowEditMode mode)
        {
            // CommonParam を再設定して各種情報をリセットする
            foreach (var m in modes)
            {
                m.ResetCommonParam(commonParam);
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.Space();

            EditorGUI.BeginChangeCheck();
            {
                commonParam.avatarRoot = (GameObject)EditorGUILayout.ObjectField(new GUIContent("Avatar Root"), commonParam.avatarRoot, typeof(GameObject), true);
            }
            if (EditorGUI.EndChangeCheck())
            {
                SetValueFromAvatarDescriptor();
            }

            EditorGUILayout.Space();

            modeEdit = GUILayout.Toolbar(modeEdit, modes.Select(m => m.GetShortName()).ToArray(), new GUIStyle("LargeButton"), GUI.ToolbarButtonSize.FitToContents);

            EditorGUILayout.Space();

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            if (0 <= modeEdit && modeEdit < modes.Count)
            {
                if (modes[modeEdit] != null)
                {
                    modes[modeEdit].OnGUI();
                }
            }

            EditorGUILayout.Space();

            EditorGUILayout.EndScrollView();
        }

        private void SetValueFromAvatarDescriptor()
        {
            commonParam.animator = null;
#if ENV_VRCSDK3_AVATAR
            commonParam.expMenu = null;
            commonParam.expParams = null;
#endif

            if (commonParam.avatarRoot != null)
            {
                // avatarRoot があれば Animator を検索する
                var animator = commonParam.avatarRoot.GetComponent<Animator>();
                if (animator != null && animator.runtimeAnimatorController is AnimatorController)
                {
                    commonParam.animator = (AnimatorController)animator.runtimeAnimatorController;
                }

#if ENV_VRCSDK3_AVATAR
                var desc = commonParam.avatarRoot.GetComponent<VRCAvatarDescriptor>();
                if (desc != null)
                {
                    if (commonParam.animator == null)
                    {
                        commonParam.animator = desc?.baseAnimationLayers
                            .Where(ly => ly.type == VRCAvatarDescriptor.AnimLayerType.FX && ly.animatorController is AnimatorController)
                            .Select(ly => (AnimatorController)ly.animatorController)
                            .FirstOrDefault();
                    }
                    commonParam.expMenu = desc.expressionsMenu;
                    commonParam.expParams = desc.expressionParameters;
                }
#endif
            }

            foreach (var m in modes)
            {
                m.ResetCommonParam(commonParam);
            }
        }

        private void SetSelection(Object[] objects)
        {
            foreach (var obj in objects)
            {
                if (obj is GameObject)
                {
                    var go = (GameObject)obj;
                    if (
#if ENV_VRCSDK3_AVATAR
                        go.GetComponent<VRCAvatarDescriptor>() != null ||
#endif
                        go.GetComponent<Animator>() != null)
                    {
                        commonParam.avatarRoot = go;
                        SetValueFromAvatarDescriptor();
                        break;
                    }
                }
            }
        }

        public static void ShowWindow()
        {
            var window = GetWindow<AnimEditUtilWindow>(Title);
            window.SetSelection(Selection.GetFiltered(typeof(GameObject), SelectionMode.Editable | SelectionMode.ExcludePrefab));
        }

        public static void ShowWindowWriteDefault()
        {
            var window = GetWindow<AnimEditUtilWindow>(Title);
            window.SetSelection(Selection.GetFiltered(typeof(GameObject), SelectionMode.Editable | SelectionMode.ExcludePrefab));
            window.modeEdit = Mathf.Max(0, ArrayUtility.IndexOf(window.modeTypes, typeof(ModeWriteDefaultTakedown)));
        }

#if ENV_VRCSDK3_AVATAR
        public static void ShowWindowAvatarMask()
        {
            var window = GetWindow<AnimEditUtilWindow>(Title);
            window.SetSelection(Selection.GetFiltered(typeof(GameObject), SelectionMode.Editable | SelectionMode.ExcludePrefab));
            window.modeEdit = Mathf.Max(0, ArrayUtility.IndexOf(window.modeTypes, typeof(ModeSetupAvatarMask)));
        }
#endif
    }
}

#endif
