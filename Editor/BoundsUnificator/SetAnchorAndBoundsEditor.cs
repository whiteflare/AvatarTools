/*
 *  The MIT License
 *
 *  Copyright 2020-2025 whiteflare.
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

using UnityEditor;
using UnityEngine;

namespace WF.Tool.Avatar.BU
{
    [CustomEditor(typeof(SetAnchorAndBounds))]
    internal class SetAnchorAndBoundsEditor : Editor
    {
        private static bool fdCustomize = false;
        private static bool fdUtility = false;

        public override void OnInspectorGUI ()
        {
            var component = target as SetAnchorAndBounds;
            if (component == null)
            {
                return;
            }

#if !ENV_VRCSDK3_AVATAR
            EditorGUILayout.HelpBox("VRCSDK3 Avatars 環境ではないため、このコンポーネントは無視されます。", MessageType.Warning);
#elif !ENV_NDMF
            EditorGUILayout.HelpBox("Non-Destructive Modular Framework が導入されていないため、このコンポーネントは無視されます。", MessageType.Warning);
#else
            if (component.GetComponent<VRC.SDK3.Avatars.Components.VRCAvatarDescriptor>() == null)
            {
                EditorGUILayout.HelpBox("このコンポーネントは VRC AvatarDescriptor と同じ GameObject に追加してください。", MessageType.Error);
                return;
            }
            if (component.enabled)
            {
                EditorGUILayout.HelpBox("ビルド時に AnchorOverride と Bounds が自動で設定されます。", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("コンポーネントを有効にするとビルド時に AnchorOverride と Bounds が自動で設定されます。", MessageType.Warning);
            }
#endif

            serializedObject.Update();

            EditorGUILayout.Space();
            fdCustomize = EditorGUILayout.Foldout(fdCustomize, "高度な設定");
            if (fdCustomize)
            {
                using(new EditorGUI.IndentLevelScope())
                {
                    EditorGUILayout.HelpBox("このセクションでは動作をカスタマイズすることができます。\n未設定の場合は動作は変化しません。", MessageType.None);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(SetAnchorAndBounds.customAnchorOverride)), new GUIContent("Anchor Override"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(SetAnchorAndBounds.customRootBone)), new GUIContent("Root Bone"));
                }
            }

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space();
            fdUtility = EditorGUILayout.Foldout(fdUtility, "ユーティリティ");
            if (fdUtility)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    if (GUI.Button(EditorGUILayout.GetControlRect(), "Open (Manual) Tool Window"))
                    {
                        BoundsUnificator.ShowWindow(component.gameObject, component.customAnchorOverride, component.customRootBone);
                    }
                }
            }
        }
    }
}

#endif
