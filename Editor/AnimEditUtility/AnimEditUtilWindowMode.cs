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

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace WF.Utillty.AnimEdit
{
    internal abstract class AnimEditUtilWindowEditMode : ScriptableObject
    {
        [System.NonSerialized]
        protected AnimEditUtilWindowCommonParam param;

        protected const string LabelVRCExpressionParameters = "ExParam";
        protected const string LabelVariableName = "変数名";
        protected const string LabelVariableType = "型";
        protected const string LabelLayerName = "レイヤー名";
        protected const string LabelDuration = "Duration";
        protected const string LabelWriteDefaults = "Write Defaults";
        protected const string LabelAnimationClip = "Animation Clip";
        protected const string LabelAnimatorController = "Animator Controller";
        protected const string LabelVRCExpressionMenu = "ExMenu";
        protected const string LabelAnimCount = "アニメ生成数";
        protected const string LabelAnimFilePrefix = "ファイル名のプレフィックス";
        protected const string LabelSwitchingObjects = "Switching Objects";

        public event System.Action<AnimEditUtilWindowEditMode> AfterExecute;

        public virtual void ResetCommonParam(AnimEditUtilWindowCommonParam newParam)
        {
            this.param = newParam;
        }

        public virtual void OnGUI()
        {
        }

        public abstract string GetShortName();

        protected void OnAfterExecute()
        {
            AfterExecute?.Invoke(this);
        }

        protected static bool BlueButton(string text, bool disabled = false)
        {
            var oldColor = GUI.color;

            using (new EditorGUI.DisabledGroupScope(disabled))
            {
                GUI.color = new Color(0.75f, 0.75f, 1f);
                bool exec = GUI.Button(EditorGUI.IndentedRect(EditorGUILayout.GetControlRect()), text);
                GUI.color = oldColor;
                return exec;
            }
        }

        protected static bool ConfirmContinue()
        {
            return EditorUtility.DisplayDialog(AnimEditUtilWindow.Title, "Continue modify Objects?\nオブジェクトを変更しますか？", "OK", "CANCEL");
        }


        protected static int IntPopupAnimatorLayer(string label, AnimatorController animator, int indexLayer)
        {
            var layers = animator == null ? new string[0] : animator.layers.Select((ly, i) => i + ": " + ly.name).ToArray();
            indexLayer = layers.Length == 0 ? 0 : Mathf.Clamp(indexLayer, 0, layers.Length - 1);
            return EditorGUILayout.Popup(label, indexLayer, layers);
        }

        protected static int IntPopupAnimatorParameter(string label, AnimatorController animator, int indexLayer, int indexParam, out string resultParamName, string defaultName = null)
        {
            var hasDefaultName = !string.IsNullOrWhiteSpace(defaultName);
            var srcLayer = GetAnimatorLayer(animator, indexLayer);

            var usedParams = new List<string>();
            if (srcLayer != null)
            {
                usedParams.AddRange(AnimatorEditUtility.GetUsedParameters(animator, srcLayer).Select(p => p.name));
            }
            else
            {
                indexParam = 0;
            }

            var labels = new List<string>(usedParams);
            if (hasDefaultName)
            {
                labels.Insert(0, defaultName);
            }
            indexParam = EditorGUILayout.Popup(label, indexParam, labels.ToArray());

            var idx = indexParam;
            if (hasDefaultName)
            {
                idx--;
            }
            if (0 <= idx && idx < usedParams.Count)
            {
                resultParamName = usedParams[idx];
            }
            else
            {
                resultParamName = "";
            }

            return indexParam;
        }

        protected static AnimatorControllerLayer GetAnimatorLayer(AnimatorController animator, int indexLayer)
        {
            if (animator != null && 0 <= indexLayer && indexLayer < animator.layers.Length)
            {
                return animator.layers[indexLayer];
            }
            return null;
        }

        protected static string GetAnimatorLayerName(AnimatorController animator, int indexLayer)
        {
            var srcLayer = GetAnimatorLayer(animator, indexLayer);
            if (srcLayer != null)
            {
                return srcLayer.name;
            }
            return "";
        }


        protected static readonly Color lightYellow = new Color(1, 1, 0.6f);
        protected static readonly Color lightRed = new Color(1, 0.75f, 0.75f);

        protected static string TextFieldColored(string label, string text, Color color)
        {
            using (new ChangeColorScope(color))
                return EditorGUILayout.TextField(label, text);
        }

        protected static T ObjectFieldRequired<T>(string label, T value, Color attensionColor) where T : Object
        {
            using (new ChangeColorScope(value == null ? attensionColor : GUI.color))
                return (T)EditorGUILayout.ObjectField(new GUIContent(label), value, typeof(T), true);
        }

        public class ChangeColorScope : GUI.Scope
        {
            private readonly Color oldColor;

            public ChangeColorScope(Color color)
            {
                oldColor = GUI.color;
                GUI.color = color;
            }

            protected override void CloseScope()
            {
                GUI.color = oldColor;
            }
        }

        protected static void CreateOrOverrideAsset<T>(T asset, string path) where T : Object
        {
            var exist = AssetDatabase.LoadAssetAtPath<T>(path);
            if (exist == null)
            {
                AssetDatabase.CreateAsset(asset, path);
            }
            else
            {
                EditorUtility.CopySerialized(asset, exist);
                EditorUtility.SetDirty(exist);
                AssetDatabase.SaveAssets();
            }
        }

        protected static bool IsSameString(string x, string y)
        {
            return x == y || (string.IsNullOrEmpty(x) && string.IsNullOrEmpty(y));
        }

        protected static AnimationClip LoadEmptyAnimClip()
        {
            var emptyPath = AssetDatabase.GUIDToAssetPath("25469b584e6b19041bd086070b069270"); // EmptyAnimationClipのGUID
            if (emptyPath != null)
            {
                return AssetDatabase.LoadAssetAtPath<AnimationClip>(emptyPath);
            }
            return null;
        }

        protected GUIStyle StyleHeader
        {
            get => new GUIStyle(EditorStyles.largeLabel)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                fixedHeight = 24,
                margin = new RectOffset(4, 4, 4, 10),
            };
        }
    }
}

#endif
