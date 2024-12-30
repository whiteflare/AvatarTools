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

using UnityEditor;
using UnityEngine;

#if ENV_VRCSDK3_AVATAR
using VRC.SDK3.Avatars.ScriptableObjects;
#endif

namespace WF.Tool.Avatar.AnimEdit
{
#if ENV_VRCSDK3_AVATAR

    /// <summary>
    /// UnityEditor.Animations.AnimatorController とその周辺を編集するユーティリティ
    /// </summary>
    internal static class AvatarAssetEditUtility
    {
        public static bool HasExParameter(VRCExpressionParameters exParam, string name)
        {
            if (exParam != null)
            {
                foreach (var p in exParam.parameters)
                {
                    if (p.name == name)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public static void AddExParameterIfAbsent(VRCExpressionParameters exParam, string name, AnimatorControllerParameterType type, bool saved)
        {
            if (exParam == null || string.IsNullOrWhiteSpace(name))
            {
                return; // 未設定なら何もせず正常終了
            }
            if (HasExParameter(exParam, name))
            {
                return; // 同名の変数がすでにあるならば何もしない
            }

            // 追加
            var newParam = new VRCExpressionParameters.Parameter();
            SetExParameterValue(newParam, name, type, saved);

            if (exParam.CalcTotalCost() + VRCExpressionParameters.TypeCost(newParam.valueType) <= VRCExpressionParameters.MAX_PARAMETER_COST)
            {
                ArrayUtility.Add(ref exParam.parameters, newParam);
                EditorUtility.SetDirty(exParam);
            }
        }

        private static void SetExParameterValue(VRCExpressionParameters.Parameter newParam, string name, AnimatorControllerParameterType type, bool saved)
        {
            newParam.name = name;
            switch (type)
            {
                case AnimatorControllerParameterType.Float:
                default: // Trigger とかどうしようか迷ったけど float で
                    newParam.valueType = VRCExpressionParameters.ValueType.Float;
                    break;
                case AnimatorControllerParameterType.Int:
                    newParam.valueType = VRCExpressionParameters.ValueType.Int;
                    break;
                case AnimatorControllerParameterType.Bool:
                    newParam.valueType = VRCExpressionParameters.ValueType.Bool;
                    break;
            }
            newParam.saved = saved;
        }

        public static void AddExMenuToggle(VRCExpressionsMenu exMenu, string var, string title, bool value)
        {
            AddExMenuToggle(exMenu, var, title, value ? 1f : 0f);
        }

        public static void AddExMenuToggle(VRCExpressionsMenu exMenu, string var, string title, int value)
        {
            AddExMenuToggle(exMenu, var, title, (float) value);
        }

        public static void AddExMenuToggle(VRCExpressionsMenu exMenu, string var, string title, float value)
        {
            if (exMenu == null || string.IsNullOrWhiteSpace(var))
            {
                return; // 未設定なら何もせず正常終了
            }
            if (VRCExpressionsMenu.MAX_CONTROLS <= exMenu.controls.Count)
            {
                return; // 最大になっているならば追加できないので何もしない
            }

            // 追加
            var newControl = new VRCExpressionsMenu.Control
            {
                name = title,
                type = VRCExpressionsMenu.Control.ControlType.Toggle,
                parameter = new VRCExpressionsMenu.Control.Parameter
                {
                    name = var,
                },
                value = value,
            };
            exMenu.controls.Add(newControl);

            EditorUtility.SetDirty(exMenu);
        }

        public static bool CreateNewSubMenu(ref VRCExpressionsMenu exMenu)
        {
            var path = AssetDatabase.GetAssetPath(exMenu);
            if (string.IsNullOrEmpty(path))
            {
                return false;
            }

            // 元の ExMenu と同じフォルダに新しい ExMenu アセットを作成
            path = AssetDatabase.GenerateUniqueAssetPath(path);
            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<VRCExpressionsMenu>(), path);
            var subMenu = AssetDatabase.LoadAssetAtPath<VRCExpressionsMenu>(path);

            // 余裕があるなら元の ExMenu に SubMenu として登録
            if (exMenu.controls.Count < VRCExpressionsMenu.MAX_CONTROLS)
            {
                // 追加
                var newControl = new VRCExpressionsMenu.Control
                {
                    name = subMenu.name,
                    type = VRCExpressionsMenu.Control.ControlType.SubMenu,
                    subMenu = subMenu,
                };
                exMenu.controls.Add(newControl);
            }

            // SubMenu に差し替える
            exMenu = subMenu;
            return true;
        }
    }

#endif
}

#endif
