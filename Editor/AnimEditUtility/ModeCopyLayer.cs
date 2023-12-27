/*
 *  The MIT License
 *
 *  Copyright 2021-2024 whiteflare.
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

namespace WF.Tool.Avatar.AnimEdit
{
    internal class ModeCopyLayer : AnimEditUtilWindowEditMode
    {
        public AnimatorController srcAnimator;
        public int srcLayerIndex;
        public string dstLayerName = "";

        public int srcCopyParamIndex;
        public string srcCopyParamName = "";
        public string dstCopyParamName = "";

        public override void OnGUI()
        {
            var oldColor = GUI.color;

            GUILayout.Label("Copy AnimatorControllerLayer", StyleHeader);

            EditorGUILayout.HelpBox("指定の AnimationLayer を複製し、新しく追加します。Parameter が必要な場合は Parameter も追加されます。", MessageType.Info);

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Source", EditorStyles.boldLabel);
            using (new EditorGUI.IndentLevelScope())
            {
                bool isSameName = IsSameString(GetSourceLayerName(), dstLayerName);

                EditorGUI.BeginChangeCheck();

                srcAnimator = ObjectFieldRequired(LabelAnimatorController, srcAnimator, lightRed);
                srcLayerIndex = IntPopupAnimatorLayer(LabelLayerName, srcAnimator, srcLayerIndex);

                if (EditorGUI.EndChangeCheck())
                {
                    var srcLayer = GetSourceLayer();
                    if (isSameName && srcLayer != null)
                    {
                        dstLayerName = srcLayer.name;
                    }
                }
            }

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Destination", EditorStyles.boldLabel);
            using (new EditorGUI.IndentLevelScope())
            {
                param.animator = ObjectFieldRequired(LabelAnimatorController, param.animator, lightRed);
                dstLayerName = TextFieldColored(LabelLayerName, dstLayerName,
                    string.IsNullOrEmpty(dstLayerName) ? lightRed :
                    AnimatorEditUtility.HasLayer(param.animator, dstLayerName) ? lightYellow : GUI.color);
            }

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Option", EditorStyles.boldLabel);
            using (new EditorGUI.IndentLevelScope())
            {

                EditorGUILayout.LabelField("Replace Parameter", EditorStyles.boldLabel);
                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUI.BeginChangeCheck();
                    srcCopyParamIndex = IntPopupAnimatorParameter("Before", srcAnimator, srcLayerIndex, srcCopyParamIndex, out srcCopyParamName, "<none>");
                    if (EditorGUI.EndChangeCheck())
                    {
                        dstCopyParamName = srcCopyParamName;
                    }
                    if (GetSourceLayer() == null)
                    {
                        dstCopyParamName = "";
                    }
                    dstCopyParamName = TextFieldColored("After", dstCopyParamName, srcCopyParamIndex != 0 && string.IsNullOrWhiteSpace(dstCopyParamName)
                        || srcCopyParamName == dstCopyParamName ? lightYellow : GUI.color);
                }

#if ENV_VRCSDK3_AVATAR
                EditorGUILayout.LabelField("Copy Parameter", EditorStyles.boldLabel);
                using (new EditorGUI.IndentLevelScope())
                {
                    param.expParams = ObjectFieldRequired(LabelVRCExpressionParameters, param.expParams, GUI.color);
                }
#endif
            }

            EditorGUILayout.Space();

            if (BlueButton("Copy Layer", !CanExecute()))
            {
                Execute();
            }
        }

        public override string GetShortName()
        {
            return "Copy Layer";
        }

        private bool CanExecute()
        {
            if (param.animator == null || srcAnimator == null)
            {
                EditorGUILayout.HelpBox(string.Format("{0} が未設定です。", LabelAnimatorController), MessageType.Error);
                return false;
            }
            if (string.IsNullOrWhiteSpace(dstLayerName))
            {
                EditorGUILayout.HelpBox(string.Format("{0} が未設定です。", LabelLayerName), MessageType.Error);
                return false;
            }
            return true;
        }

        private void Execute()
        {
            if (!ConfirmContinue())
            {
                return;
            }

            var srcLayer = GetSourceLayer();
            if (srcLayer != null)
            {
                var usedParams = new List<AnimatorControllerParameter>(AnimatorEditUtility.GetUsedParameters(srcAnimator, srcLayer));

                // レイヤーコピー
                var dstLayer = AnimatorEditUtility.DuplicateLayer(srcAnimator, srcLayer, param.animator, dstLayerName);

                // Parameterを置換
                if (dstLayer != null && !string.IsNullOrWhiteSpace(srcCopyParamName) && !string.IsNullOrWhiteSpace(dstCopyParamName) && srcCopyParamName != dstCopyParamName)
                {
                    AnimatorEditUtility.ReplaceParameter(dstLayer, srcCopyParamName, dstCopyParamName);
                    for(int i = 0; i < usedParams.Count; i++)
                    {
                        if (usedParams[i].name == srcCopyParamName)
                        {
                            var prm = new AnimatorControllerParameter()
                            {
                                name = dstCopyParamName,
                                type = usedParams[i].type,
                                defaultBool = usedParams[i].defaultBool,
                                defaultFloat = usedParams[i].defaultFloat,
                                defaultInt = usedParams[i].defaultInt,
                            };
                            usedParams[i] = prm;
                        }
                    }
                }

                // Parameterが無いならそれもコピー
                foreach (var prm in usedParams)
                {
                    if (!AnimatorEditUtility.HasParameter(param.animator, prm.name))
                    {
                        AnimatorEditUtility.AddParameterIfAbsent(param.animator, prm.name, prm.type);
#if ENV_VRCSDK3_AVATAR
                        // HasParameter が false の場合にのみ ExParams にパラメータを追加する
                        if (!WELLKNOWN_PARAMS.Contains(prm.name)) // ただしVRCが動的に変更するパラメータ名の場合はExpressionParametersには追加しない
                        {
                            AvatarAssetEditUtility.AddExParameterIfAbsent(param.expParams, prm.name, prm.type, true); // saved は暫定 true
                        }
#endif
                    }
                }

                EditorUtility.SetDirty(param.animator);
            }
            // 通知
            OnAfterExecute();
        }

        private AnimatorControllerLayer GetSourceLayer()
        {
            return GetAnimatorLayer(srcAnimator, srcLayerIndex);
        }

        private string GetSourceLayerName()
        {
            return GetAnimatorLayerName(srcAnimator, srcLayerIndex);
        }

#if ENV_VRCSDK3_AVATAR
        private static readonly HashSet<string> WELLKNOWN_PARAMS = new HashSet<string>{
            "IsLocal",
            "Viseme",
            "GestureLeft",
            "GestureRight",
            "GestureLeftWeight",
            "GestureRightWeight",
            "AngularY",
            "VelocityX",
            "VelocityY",
            "VelocityZ",
            "Upright",
            "Grounded",
            "Seated",
            "AFK",
            "Expression1",
            "Expression2",
            "Expression3",
            "Expression4",
            "Expression5",
            "Expression6",
            "Expression7",
            "Expression8",
            "Expression9",
            "Expression10",
            "Expression11",
            "Expression12",
            "Expression13",
            "Expression14",
            "Expression15",
            "Expression16",
            "TrackingType",
            "VRMode",
            "MuteSelf",
            "InStation",
        };
#endif
    }
}

#endif
