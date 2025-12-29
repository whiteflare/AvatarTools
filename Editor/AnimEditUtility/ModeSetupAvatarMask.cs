/*
 *  The MIT License
 *
 *  Copyright 2021-2026 whiteflare.
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

#if ENV_VRCSDK3_AVATAR
using VRC.SDK3.Avatars.Components;
#endif

namespace WF.Tool.Avatar.AnimEdit
{
#if ENV_VRCSDK3_AVATAR

    internal class ModeSetupAvatarMask : AnimEditUtilWindowEditMode
    {
        public override void OnGUI()
        {
            var oldColor = GUI.color;

            GUILayout.Label("Setup AvatarMask For Humanoid Avatars", StyleHeader);
            {
                EditorGUILayout.HelpBox("Humanoid 向けに AvatarMask をセットアップします。", MessageType.Info);

                EditorGUILayout.Space();

                param.avatarRoot = ObjectFieldRequired("Avatar Root", param.avatarRoot, param.avatarRoot == null ? lightRed : GUI.color);

                EditorGUILayout.Space();

                if (BlueButton("Setup AvatarMask", !CanExecute()))
                {
                    Execute();
                }
            }
        }

        private bool CanExecute()
        {
            if (param.avatarRoot == null)
            {
                EditorGUILayout.HelpBox(string.Format("{0} が未設定です。", "Avatar Root"), MessageType.Error);
                return false;
            }

            var avatarDesc = param.avatarRoot.GetComponent<VRCAvatarDescriptor>();
            if (avatarDesc == null)
            {
                EditorGUILayout.HelpBox(string.Format("{0} に VRCAvatarDescriptor が未設定です。", "Avatar Root"), MessageType.Error);
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

            var avatarDesc = param.avatarRoot.GetComponent<VRCAvatarDescriptor>();

            Undo.RecordObject(avatarDesc, "Setup AvatarMask");

            var allTransforms = new List<string>();
            var generatedMasks = new List<AvatarMask>();
            for (int i = 0; i < avatarDesc.baseAnimationLayers.Length; i++)
            {
                var layer = avatarDesc.baseAnimationLayers[i];
                if (layer.isDefault)
                {
                    continue; // Default ならば何もしない
                }
                var animator = layer.animatorController as AnimatorController;
                if (animator == null)
                {
                    continue;
                }

                // AvatarMask作成
                if (!SetupAvatarMask(layer.type, animator, allTransforms, out var mask)) // maskがnullの場合も想定すること
                {
                    continue; // 作成対象外のレイヤーは何もしない
                }

                // AvatarMask アセットの保存
                if (mask != null && AssetDatabase.GetAssetPath(mask) == "")
                {
                    mask.name = "mask_" + layer.animatorController.name;
                    generatedMasks.Add(mask);
                }

                // Animator の最初のレイヤーに AvatarMask を設定
                if (animator.layers.Length != 0)
                {
                    Undo.RecordObject(animator, "Setup AvatarMask");

                    var layers = animator.layers;
                    layers[0].avatarMask = mask;
                    animator.layers = layers;

                    EditorUtility.SetDirty(animator);
                }

                // AvatarDescriptorにAvatarMaskを設定
                avatarDesc.baseAnimationLayers[i].mask = mask;
                EditorUtility.SetDirty(avatarDesc);
            }

            // 未保存のAvatarMaskを最後に保存
            if (0 < generatedMasks.Count)
            {
                var folderPath = EditorUtility.SaveFolderPanel("AvatarMask保存先フォルダの指定", "Assets", "");
                if (string.IsNullOrWhiteSpace(folderPath))
                {
                    Undo.RevertAllInCurrentGroup();
                    return;
                }
                if (!folderPath.StartsWith(Application.dataPath, System.StringComparison.InvariantCultureIgnoreCase))
                {
                    Debug.LogError("AvatarMask は Assets フォルダ配下に保存してください: " + folderPath);
                    Undo.RevertAllInCurrentGroup();
                    return;
                }
                folderPath = "Assets" + folderPath.Substring(Application.dataPath.Length);
                foreach (var mask in generatedMasks)
                {
                    var path = AssetDatabase.GenerateUniqueAssetPath(folderPath + "/" + mask.name + ".mask");
                    AssetDatabase.CreateAsset(mask, path);
                }
            }

            // 通知
            OnAfterExecute();
        }

        public override string GetShortName()
        {
            return "Setup AvatarMask";
        }

        private bool SetupAvatarMask(VRCAvatarDescriptor.AnimLayerType type, AnimatorController animator, List<string> upperLayerTransforms, out AvatarMask mask)
        {
            mask = null;
            switch (type)
            {
                case VRCAvatarDescriptor.AnimLayerType.Gesture:
                    // Gestureレイヤでは『Humanoid 使っているもののみ許可、Transform 使っているもののみ許可』の AvatarMask を作成する

                    // Gesture の全ての AnimationClip を列挙
                    var gestureClips = AnimatorEditUtility.GetAllAnimationClip(animator);
                    // Gesture で弄っている全ての Transform のパスを列挙
                    var gestureTransforms = AnimatorEditUtility.GetAllTransformAnimationPropertyPaths(gestureClips);
                    // Gesture で弄っている全ての Humanoid のプロパティ名を列挙
                    var gestureHumanoids = ToAvatarMaskBodyPart(AnimatorEditUtility.GetAllHumanoidPropertyAttributes(gestureClips));

                    if (gestureHumanoids.Count() == 2 && gestureHumanoids.Contains(AvatarMaskBodyPart.LeftFingers) && gestureHumanoids.Contains(AvatarMaskBodyPart.RightFingers)
                        && gestureTransforms.Count() == 0)
                    {
                        // もし右指・左指のみ有効かつ Transform を含まない場合 vrc_HandsOnly.mask を設定する。
                        mask = LoadVRCHandOnlyMask();
                    }
                    if (mask == null)
                    {
                        // AvatarMask作成(初期値は拒否)
                        mask = AnimatorEditUtility.GenerateAvatarMask(param.avatarRoot, false, false, false);
                        // 使っている Humanoid を許可にする
                        foreach (var hum in gestureHumanoids)
                        {
                            mask.SetHumanoidBodyPartActive(hum, true);
                        }
                        // 使っている Transform を許可にする
                        AnimatorEditUtility.SetAvatarMaskTransformActive(mask, gestureTransforms, true);
                    }

                    upperLayerTransforms.AddRange(gestureTransforms);

                    // 作成OK
                    return true;

                case VRCAvatarDescriptor.AnimLayerType.FX:
                    // FXレイヤでは『Humanoid 拒否、Transform 許可(ただし上位レイヤのTransformは拒否)』の AvatarMask を作成する
                    if (upperLayerTransforms.Count == 0)
                    {
                        // 上位レイヤでTransformアニメがない場合、マスクを null とすることで
                        // VRC 側が「humanoid 拒否、Transform 許可」のAvatarMaskを設定してくれることを期待する。
                        mask = null;
                    }
                    else
                    {
                        // AvatarMask作成(初期値はHumanoid拒否、Transform許可)
                        mask = AnimatorEditUtility.GenerateAvatarMask(param.avatarRoot, false, false, true);
                        // ただし Gesture レイヤで使っている Transform は拒否に設定する
                        AnimatorEditUtility.SetAvatarMaskTransformActive(mask, upperLayerTransforms.ToArray(), false);
                    }
                    // 作成OK
                    return true;

                case VRCAvatarDescriptor.AnimLayerType.Base:
                case VRCAvatarDescriptor.AnimLayerType.Additive:
                case VRCAvatarDescriptor.AnimLayerType.Action:
                    // これらのレイヤーではnullとすることでVRC側で指定したAvatarMaskが使われるようにする
                    mask = null;
                    return true;

                default:
                    // 作成NG
                    return false; // 他のレイヤは何もしない
            }
        }

        private static AvatarMask LoadVRCHandOnlyMask()
        {
            foreach (var path in new string[] {
                AssetDatabase.GUIDToAssetPath("b2b8bad9583e56a46a3e21795e96ad92"),
                "Packages/com.vrchat.avatars/Samples/AV3 Demo Assets/Animation/Masks/vrc_HandsOnly.mask",
                "Assets/VRCSDK/Examples3/Animation/Masks/vrc_HandsOnly.mask",
            })
            {
                if (!string.IsNullOrEmpty(path))
                {
                    var mask = AssetDatabase.LoadAssetAtPath<AvatarMask>(path);
                    if (mask != null)
                    {
                        return mask;
                    }
                }
            }
            return null;
        }

        private static IEnumerable<AvatarMaskBodyPart> ToAvatarMaskBodyPart(string[] humanoidPropertyNames)
        {
            var result = new List<AvatarMaskBodyPart>();
            foreach (var hum in humanoidPropertyNames)
            {
                foreach (var ent in HumanBonePrefixToBodyPart)
                {
                    if (hum.StartsWith(ent.Key))
                    {
                        result.Add(ent.Value);
                    }
                }
            }
            return result.Distinct();
        }

        private static readonly Dictionary<string, AvatarMaskBodyPart> HumanBonePrefixToBodyPart = new Dictionary<string, AvatarMaskBodyPart>{
            // ルート
            //{ "RootQ.", AvatarMaskBodyPart.Root },
            //{ "RootT.", AvatarMaskBodyPart.Root },

            // ボディ
            { "Chest", AvatarMaskBodyPart.Body },
            { "Spine", AvatarMaskBodyPart.Body },
            { "UpperChest", AvatarMaskBodyPart.Body },

            // 頭
            { "Neck", AvatarMaskBodyPart.Head },
            { "Head", AvatarMaskBodyPart.Head },
            { "Jaw", AvatarMaskBodyPart.Head },
            { "Left Eye", AvatarMaskBodyPart.Head },
            { "Right Eye", AvatarMaskBodyPart.Head },

            // 左脚
            { "Left Foot", AvatarMaskBodyPart.LeftLeg },
            { "Left Lower Leg", AvatarMaskBodyPart.LeftLeg },
            { "Left Toes", AvatarMaskBodyPart.LeftLeg },
            { "Left Upper Leg", AvatarMaskBodyPart.LeftLeg },

            // 右脚
            { "Right Foot", AvatarMaskBodyPart.RightLeg },
            { "Right Lower Leg", AvatarMaskBodyPart.RightLeg },
            { "Right Toes", AvatarMaskBodyPart.RightLeg },
            { "Right Upper Leg", AvatarMaskBodyPart.RightLeg },

            // 左腕
            { "Left Arm", AvatarMaskBodyPart.LeftArm },
            { "Left Forearm", AvatarMaskBodyPart.LeftArm },
            { "Left Hand", AvatarMaskBodyPart.LeftArm },
            { "Left Shoulder", AvatarMaskBodyPart.LeftArm },

            // 右腕
            { "Right Arm", AvatarMaskBodyPart.RightArm },
            { "Right Forearm", AvatarMaskBodyPart.RightArm },
            { "Right Hand", AvatarMaskBodyPart.RightArm },
            { "Right Shoulder", AvatarMaskBodyPart.RightArm },

            // 左手の指
            { "LeftHand.", AvatarMaskBodyPart.LeftFingers },

            // 右手の指
            { "RightHand.", AvatarMaskBodyPart.RightFingers },

            // 左足IK
            //{ "LeftFootQ.", AvatarMaskBodyPart.LeftFootIK },
            //{ "LeftFootT.", AvatarMaskBodyPart.LeftFootIK },

            // 右足IK
            //{ "RightFootQ.", AvatarMaskBodyPart.RightFootIK },
            //{ "RightFootT.", AvatarMaskBodyPart.RightFootIK },

            // 左腕IK
            //{ "LeftHandQ.", AvatarMaskBodyPart.LeftHandIK },
            //{ "LeftHandT.", AvatarMaskBodyPart.LeftHandIK },

            // 右腕IK
            //{ "RightHandQ.", AvatarMaskBodyPart.RightHandIK },
            //{ "RightHandT.", AvatarMaskBodyPart.RightHandIK },
        };
    }

#endif
}

#endif
