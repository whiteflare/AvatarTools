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
    /// <summary>
    /// UnityEditor.Animations.AnimatorController とその周辺を編集するユーティリティ
    /// </summary>
    internal static class AnimatorEditUtility
    {
        #region Animator編集系

        /// <summary>
        /// AnimatorController に新しい AnimatorControllerLayer を追加し、指定の AnimationClip を再生する State を追加する。
        /// </summary>
        /// <param name="animator"></param>
        /// <param name="layerName"></param>
        /// <param name="clips"></param>
        /// <param name="param">切り替え制御用のParameter</param>
        /// <param name="writeDefault"></param>
        /// <param name="duration"></param>
        /// <returns>追加された AnimatorControllerLayer</returns>
        public static AnimatorControllerLayer AddLayerAndSetupState(AnimatorController animator, string layerName, AnimationClip[] clips, AnimatorControllerParameter param, bool writeDefault, float duration)
        {
            return InternalAnimatorSetupUtil.AddLayerAndSetupState(animator, layerName, clips, param, writeDefault, duration);
        }

        /// <summary>
        /// パラメータが存在していないならば追加する。同名のパラメータが既に存在しているならば何もしない。
        /// </summary>
        /// <param name="animator"></param>
        /// <param name="varName"></param>
        /// <param name="type"></param>
        /// <returns>追加された AnimatorControllerParameter、既に存在しているならばその AnimatorControllerParameter</returns>
        public static AnimatorControllerParameter AddParameterIfAbsent(AnimatorController animator, string varName, AnimatorControllerParameterType type)
        {
            return InternalAnimatorValueUtil.AddParameterIfAbsent(animator, varName, type);
        }

        /// <summary>
        /// 指定の AnimatorControllerLayer を複製して新しい AnimatorControllerLayer を作成する。
        /// </summary>
        /// <param name="srcAnimator"></param>
        /// <param name="srcLayer"></param>
        /// <param name="dstAnimator"></param>
        /// <param name="newName"></param>
        /// <returns>追加された AnimatorControllerLayer</returns>
        public static AnimatorControllerLayer DuplicateLayer(AnimatorController srcAnimator, AnimatorControllerLayer srcLayer, AnimatorController dstAnimator, string newName)
        {
            return new AnimatorCopyUtility(srcAnimator, dstAnimator ?? srcAnimator).CopyLayer(srcLayer, newName);
        }

        /// <summary>
        /// Animator 内に WriteDefaultValue が true の State があるかどうか返す。
        /// </summary>
        /// <param name="animator"></param>
        /// <param name="hasMixedValue">true/falseが混在しているならばtrue</param>
        /// <returns></returns>
        public static bool GetWriteDefault(AnimatorController animator, out bool hasMixedValue)
        {
            return InternalAnimatorValueUtil.GetWriteDefault(animator, out hasMixedValue);
        }

        /// <summary>
        /// Animator 内の全ての AnimatorState の writeDefault を設定する。
        /// </summary>
        /// <param name="animator"></param>
        /// <param name="writeDefault"></param>
        public static void SetWriteDefault(AnimatorController animator, bool writeDefault)
        {
            InternalAnimatorValueUtil.SetWriteDefault(animator, writeDefault);
        }

        /// <summary>
        /// Animator 内に CanTransitionToSelf が true の State があるかどうか返す。
        /// </summary>
        /// <param name="animator"></param>
        /// <param name="hasMixedValue">true/falseが混在しているならばtrue</param>
        /// <returns></returns>
        public static bool GetCanTransitionToSelf(AnimatorController animator, out bool hasMixedValue)
        {
            return InternalAnimatorValueUtil.GetCanTransitionToSelf(animator, out hasMixedValue);
        }

        /// <summary>
        /// Animator 内の全ての AnimatorState の CanTransitionToSelf を設定する。
        /// </summary>
        /// <param name="animator"></param>
        /// <param name="canTransitionToSelf"></param>
        public static void SetCanTransitionToSelf(AnimatorController animator, bool canTransitionToSelf)
        {
            InternalAnimatorValueUtil.SetCanTransitionToSelf(animator, canTransitionToSelf);
        }

        /// <summary>
        /// AnimatorControllerLayer から使用されている全ての AnimatorControllerParameter を取得する。
        /// </summary>
        /// <param name="animator"></param>
        /// <param name="layer"></param>
        /// <returns></returns>
        public static IEnumerable<AnimatorControllerParameter> GetUsedParameters(AnimatorController animator, AnimatorControllerLayer layer)
        {
            return InternalAnimatorValueUtil.GetUsedParameters(animator, layer);
        }

        public static void ReplaceParameter(AnimatorControllerLayer layer, string before, string after)
        {
            InternalAnimatorValueUtil.ReplaceParameter(layer, before, after);
        }

        /// <summary>
        /// AnimatorController 内の全ての AnimatorState を列挙する。
        /// </summary>
        /// <param name="animator"></param>
        /// <returns></returns>
        public static IEnumerable<AnimatorState> GetAllState(AnimatorController animator)
        {
            return InternalAnimatorValueUtil.GetAllState(animator);
        }

        /// <summary>
        /// AnimatorControllerLayer 内の全ての AnimatorState を列挙する。
        /// </summary>
        /// <param name="layer"></param>
        /// <returns></returns>
        public static IEnumerable<AnimatorState> GetAllState(AnimatorControllerLayer layer)
        {
            return InternalAnimatorValueUtil.GetAllState(layer);
        }

        /// <summary>
        /// AnimatorStateMachine 内の全ての AnimatorState を列挙する。
        /// </summary>
        /// <param name="stateMachine"></param>
        /// <returns></returns>
        public static IEnumerable<AnimatorState> GetAllState(AnimatorStateMachine stateMachine)
        {
            return InternalAnimatorValueUtil.GetAllState(stateMachine);
        }

        /// <summary>
        /// AnimatorController 内の全ての AnimationClip を列挙する。
        /// </summary>
        /// <param name="animator"></param>
        /// <returns></returns>
        public static IEnumerable<AnimationClip> GetAllAnimationClip(AnimatorController animator)
        {
            return InternalAnimatorValueUtil.GetAllAnimationClip(animator);
        }

        /// <summary>
        /// AnimatorControllerLayer 内の全ての AnimationClip を列挙する。
        /// </summary>
        /// <param name="layer"></param>
        /// <returns></returns>
        public static IEnumerable<AnimationClip> GetAllAnimationClip(AnimatorControllerLayer layer)
        {
            return InternalAnimatorValueUtil.GetAllAnimationClip(layer);
        }

        /// <summary>
        /// AnimatorController 内に motion 未指定の State があるかどうか返す。
        /// </summary>
        /// <param name="animator"></param>
        /// <returns></returns>
        public static bool HasNoneMotionState(AnimatorController animator)
        {
            return InternalAnimatorValueUtil.GetAllState(animator).Any(state => state.motion == null);
        }

        /// <summary>
        /// AnimatorController 内に指定の名前の AnimatorControllerLayer があるかどうか返す。
        /// </summary>
        /// <param name="animator"></param>
        /// <param name="layerName"></param>
        /// <returns></returns>
        public static bool HasLayer(AnimatorController animator, string layerName)
        {
            if (animator != null && !string.IsNullOrWhiteSpace(layerName))
            {
                return animator.layers.Where(ly => ly != null).Any(ly => ly.name == layerName);
            }
            return false;
        }

        /// <summary>
        /// AnimatorController 内に指定の名前の AnimatorControllerParameter があるかどうか返す。
        /// </summary>
        /// <param name="animator"></param>
        /// <param name="paramName"></param>
        /// <returns></returns>
        public static bool HasParameter(AnimatorController animator, string paramName)
        {
            if (animator != null && !string.IsNullOrWhiteSpace(paramName))
            {
                return animator.parameters.Where(ly => ly != null).Any(ly => ly.name == paramName);
            }
            return false;
        }

        #region Internal

        /// <summary>
        /// AnimatorController のセットアップを行う関係のサブユーティリティ
        /// </summary>
        private static class InternalAnimatorSetupUtil
        {
            /// <summary>
            /// AnimatorController に新しい AnimatorControllerLayer を追加する。
            /// </summary>
            /// <param name="animator"></param>
            /// <param name="layerName"></param>
            /// <returns>追加された AnimatorControllerLayer</returns>
            public static AnimatorControllerLayer AddLayer(AnimatorController animator, string layerName)
            {
                // レイヤー追加
                var layer = new AnimatorControllerLayer()
                {
                    name = layerName,
                    defaultWeight = 1.0f,
                    stateMachine = new AnimatorStateMachine(),
                };
                layer.stateMachine.name = layerName;
                layer.stateMachine.hideFlags = HideFlags.HideInHierarchy;
                animator.AddLayer(layer);

                if (!string.IsNullOrWhiteSpace(AssetDatabase.GetAssetPath(animator)))
                {
                    AssetDatabase.AddObjectToAsset(layer.stateMachine, animator);
                }

                return layer;
            }

            /// <summary>
            /// AnimatorController に新しい AnimatorControllerLayer を追加し、指定の AnimationClip を再生する State を追加する。
            /// </summary>
            /// <param name="animator"></param>
            /// <param name="layerName"></param>
            /// <param name="clips"></param>
            /// <param name="param">切り替え制御用のParameter</param>
            /// <param name="writeDefault"></param>
            /// <param name="duration"></param>
            /// <returns>追加された AnimatorControllerLayer</returns>
            public static AnimatorControllerLayer AddLayerAndSetupState(AnimatorController animator, string layerName, AnimationClip[] clips, AnimatorControllerParameter param, bool writeDefault, float duration)
            {
                var layer = AddLayer(animator, layerName);
                SetupAnimationState(layer, clips, param, writeDefault, duration);
                return layer;
            }

            /// <summary>
            /// AnimatorControllerLayer に指定の AnimationClip を再生する State を用意する。
            /// </summary>
            /// <param name="layer"></param>
            /// <param name="clips"></param>
            /// <param name="param">切り替え制御用のParameter</param>
            /// <param name="writeDefault"></param>
            /// <param name="duration"></param>
            public static void SetupAnimationState(AnimatorControllerLayer layer, AnimationClip[] clips, AnimatorControllerParameter param, bool writeDefault, float duration)
            {
                // State 追加
                for (int i = 0; i < clips.Length; i++)
                {
                    var clip = clips[i];

                    // State
                    var state = layer.stateMachine.AddState(clip != null ? clip.name : "State " + i, new Vector3(300, 50 * i, i));
                    state.writeDefaultValues = writeDefault;
                    state.motion = clip;

                    if (param.type == AnimatorControllerParameterType.Bool)
                    {
                        // Any -> state
                        var tranAny = layer.stateMachine.AddAnyStateTransition(state);
                        SetupTransitionParameter(tranAny, duration);
                        SetupTransitionCondition(tranAny, param, i);
                    }
                    else
                    {
                        // Entry -> state
                        var trnEntry = layer.stateMachine.AddEntryTransition(state);
                        SetupTransitionCondition(trnEntry, param, i);

                        // state -> Exit
                        var trnExit = state.AddExitTransition();
                        SetupTransitionParameter(trnExit, duration);
                        SetupTransitionCondition(trnExit, param, i, true);
                    }
                }

                EditorUtility.SetDirty(layer.stateMachine);
            }

            private static void SetupTransitionParameter(AnimatorStateTransition tran, float duration)
            {
                tran.exitTime = 1.0f;
                tran.hasExitTime = false;
                tran.hasFixedDuration = true;
                tran.duration = duration;
                tran.canTransitionToSelf = false;
            }

            private static void SetupTransitionCondition(AnimatorStateTransition tran, AnimatorControllerParameter param, int index, bool negate = false)
            {
                switch (param.type)
                {
                    case AnimatorControllerParameterType.Bool:
                        tran.AddCondition(index == 1 ^ negate ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0, param.name);
                        break;

                    case AnimatorControllerParameterType.Int:
                        tran.AddCondition(!negate ? AnimatorConditionMode.Equals : AnimatorConditionMode.NotEqual, index, param.name);
                        break;

                    default:
                        break;
                }
            }

            private static void SetupTransitionCondition(AnimatorTransition tran, AnimatorControllerParameter param, int index, bool negate = false)
            {
                switch (param.type)
                {
                    case AnimatorControllerParameterType.Bool:
                        tran.AddCondition(index == 1 ^ negate ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0, param.name);
                        break;

                    case AnimatorControllerParameterType.Int:
                        tran.AddCondition(!negate ? AnimatorConditionMode.Equals : AnimatorConditionMode.NotEqual, index, param.name);
                        break;

                    default:
                        break;
                }
            }
        }

        /// <summary>
        /// AnimatorControllerの値を取得・設定する関係のサブユーティリティ
        /// </summary>
        private static class InternalAnimatorValueUtil
        {
            #region WriteDefault

            public static bool GetWriteDefault(AnimatorController animator, out bool hasMixedValue)
            {
                return HasValue(GetAllState(animator).Select(state => state.writeDefaultValues), true, out hasMixedValue);
            }

            public static void SetWriteDefault(AnimatorController animator, bool writeDefault)
            {
                foreach (var state in GetAllState(animator))
                {
                    if (state.writeDefaultValues != writeDefault)
                    {
                        state.writeDefaultValues = writeDefault;
                        EditorUtility.SetDirty(state);
                    }
                }
            }

            #endregion

            #region CanTransitionToSelf

            public static bool GetCanTransitionToSelf(AnimatorController animator, out bool hasMixedValue)
            {
                return HasValue(GetAllAnyStateTransition(animator).Select(tran => tran.canTransitionToSelf), true, out hasMixedValue);
            }

            public static void SetCanTransitionToSelf(AnimatorController animator, bool canTransitionToSelf)
            {
                foreach (var tran in GetAllAnyStateTransition(animator))
                {
                    tran.canTransitionToSelf = canTransitionToSelf;
                    EditorUtility.SetDirty(tran);
                }
            }

            #endregion

            #region UsedParameters

            public static AnimatorControllerParameter AddParameterIfAbsent(AnimatorController animator, string varName, AnimatorControllerParameterType type)
            {
                var param = animator.parameters.Where(p => p.name == varName).FirstOrDefault();
                if (param == null)
                {
                    // Param が存在しない場合にだけ追加する
                    param = new AnimatorControllerParameter()
                    {
                        name = varName,
                        type = type,
                    };
                    animator.AddParameter(param);
                }

                return param;
            }

            public static IEnumerable<AnimatorControllerParameter> GetUsedParameters(AnimatorController animator, AnimatorControllerLayer layer)
            {
                var result = new List<AnimatorControllerParameter>();

                // 通常Stateからの遷移
                foreach (var state in GetAllState(layer))
                {
                    foreach (var tran in state.transitions)
                    {
                        GetTransitionParameter(animator, result, tran);
                    }
                }

                // AnyStateからの遷移
                foreach (var tran in layer.stateMachine.anyStateTransitions)
                {
                    GetTransitionParameter(animator, result, tran);
                }

                // Entryからの遷移
                foreach (var tran in layer.stateMachine.entryTransitions)
                {
                    GetTransitionParameter(animator, result, tran);
                }

                // BlendTreeで使用されているパラメータ
                foreach (var state in GetAllState(layer))
                {
                    if (state.motion is BlendTree tree)
                    {
                        GetBlendTreeParameter(animator, result, tree);
                    }
                }

                // AnimatorState に設定されているパラメータ
                foreach (var state in GetAllState(layer))
                {
                    GetAnimatorStateParameter(animator, result, state);
                }

                return result;
            }

            private static void GetTransitionParameter(AnimatorController animator, List<AnimatorControllerParameter> result, AnimatorStateTransition tran)
            {
                foreach (var cond in tran.conditions)
                {
                    GetParameterAndAdd(animator, result, cond.parameter);
                }
            }

            private static void GetTransitionParameter(AnimatorController animator, List<AnimatorControllerParameter> result, AnimatorTransition tran)
            {
                foreach (var cond in tran.conditions)
                {
                    GetParameterAndAdd(animator, result, cond.parameter);
                }
            }

            private static void GetBlendTreeParameter(AnimatorController animator, List<AnimatorControllerParameter> result, BlendTree tree)
            {
                GetParameterAndAdd(animator, result, tree.blendParameter);
                GetParameterAndAdd(animator, result, tree.blendParameterY);
                foreach (var child in tree.children)
                {
                    if (child.motion is BlendTree subTree)
                    {
                        GetBlendTreeParameter(animator, result, subTree);
                    }
                }
            }

            private static void GetAnimatorStateParameter(AnimatorController animator, List<AnimatorControllerParameter> result, AnimatorState state)
            {
                if (state.mirrorParameterActive)
                {
                    GetParameterAndAdd(animator, result, state.mirrorParameter);
                }
                if (state.cycleOffsetParameterActive)
                {
                    GetParameterAndAdd(animator, result, state.cycleOffsetParameter);
                }
                if (state.speedParameterActive)
                {
                    GetParameterAndAdd(animator, result, state.speedParameter);
                }
                if (state.timeParameterActive)
                {
                    GetParameterAndAdd(animator, result, state.timeParameter);
                }
            }

            private static void GetParameterAndAdd(AnimatorController animator, List<AnimatorControllerParameter> result, string name)
            {
                if (!string.IsNullOrWhiteSpace(name))
                {
                    var param = animator.parameters.FirstOrDefault(p => p.name == name);
                    if (param != null && !result.Contains(param))
                    {
                        result.Add(param);
                    }
                }
            }

            public static void ReplaceParameter(AnimatorControllerLayer layer, string before, string after)
            {
                // 通常Stateからの遷移
                foreach (var state in GetAllState(layer))
                {
                    foreach (var tran in state.transitions)
                    {
                        ReplaceParameter(tran, before, after);
                    }
                }

                // AnyStateからの遷移
                foreach (var tran in layer.stateMachine.anyStateTransitions)
                {
                    ReplaceParameter(tran, before, after);
                }

                // Entryからの遷移
                foreach (var tran in layer.stateMachine.entryTransitions)
                {
                    ReplaceParameter(tran, before, after);
                }

                // BlendTreeで使用されているパラメータ
                foreach (var state in GetAllState(layer))
                {
                    if (state.motion is BlendTree tree)
                    {
                        ReplaceParameter(tree, before, after);
                    }
                }

                // AnimatorState に設定されているパラメータ
                foreach (var state in GetAllState(layer))
                {
                    ReplaceParameter(state, before, after);
                }
            }

            private static void ReplaceParameter(AnimatorStateTransition tran, string before, string after)
            {
                var done = false;
                var cnds = tran.conditions;
                for (int i = 0; i < cnds.Length; i++)
                {
                    if (cnds[i].parameter == before)
                    {
                        cnds[i].parameter = after;
                        done = true;
                    }
                }
                if (done)
                {
                    tran.conditions = cnds;
                }
            }

            private static void ReplaceParameter(AnimatorTransition tran, string before, string after)
            {
                var done = false;
                var cnds = tran.conditions;
                for (int i = 0; i < cnds.Length; i++)
                {
                    if (cnds[i].parameter == before)
                    {
                        cnds[i].parameter = after;
                        done = true;
                    }
                }
                if (done)
                {
                    tran.conditions = cnds;
                }
            }

            private static void ReplaceParameter(BlendTree tree, string before, string after)
            {
                if (tree.blendParameter == before)
                {
                    tree.blendParameter = after;
                }
                if (tree.blendParameterY == before)
                {
                    tree.blendParameterY = after;
                }
                foreach (var child in tree.children)
                {
                    if (child.motion is BlendTree subTree)
                    {
                        ReplaceParameter(subTree, before, after);
                    }
                }
            }

            private static void ReplaceParameter(AnimatorState state, string before, string after)
            {
                if (state.mirrorParameterActive)
                {
                    if (state.mirrorParameter == before)
                    {
                        state.mirrorParameter = after;
                    }
                }
                if (state.cycleOffsetParameterActive)
                {
                    if (state.cycleOffsetParameter == before)
                    {
                        state.cycleOffsetParameter = after;
                    }
                }
                if (state.speedParameterActive)
                {
                    if (state.speedParameter == before)
                    {
                        state.speedParameter = after;
                    }
                }
                if (state.timeParameterActive)
                {
                    if (state.timeParameter == before)
                    {
                        state.timeParameter = after;
                    }
                }
            }

            #endregion

            #region AllState

            public static IEnumerable<AnimatorState> GetAllState(AnimatorController animator)
            {
                if (animator == null)
                {
                    return new AnimatorState[0];
                }
                return animator.layers.SelectMany(GetAllState);
            }

            public static IEnumerable<AnimatorState> GetAllState(AnimatorControllerLayer layer)
            {
                if (layer == null)
                {
                    return new AnimatorState[0];
                }
                return GetAllState(layer.stateMachine);
            }

            public static IEnumerable<AnimatorState> GetAllState(AnimatorStateMachine stateMachine)
            {
                if (stateMachine == null)
                {
                    return new AnimatorState[0];
                }
                var result = new List<AnimatorState>();
                if (stateMachine != null)
                {
                    result.AddRange(stateMachine.states.Select(state => state.state));
                    foreach (var child in stateMachine.stateMachines)
                    {
                        result.AddRange(GetAllState(child.stateMachine));
                    }
                }
                return result;
            }

            #endregion

            #region AnimClip

            public static IEnumerable<AnimationClip> GetAllAnimationClip(AnimatorController animator)
            {
                if (animator == null)
                {
                    return new AnimationClip[0];
                }
                return animator.layers.SelectMany(ly => GetAllAnimationClip(ly)).Distinct();
            }

            public static IEnumerable<AnimationClip> GetAllAnimationClip(AnimatorControllerLayer layer)
            {
                return GetAllState(layer).SelectMany(state => GetAllAnimationClip(state.motion)).Distinct();
            }

            private static IEnumerable<AnimationClip> GetAllAnimationClip(Motion motion, List<AnimationClip> result = null)
            {
                if (result == null)
                {
                    result = new List<AnimationClip>();
                }
                if (motion is AnimationClip clip)
                {
                    if (!result.Contains(clip))
                    {
                        result.Add(clip);
                    }
                }
                else if (motion is BlendTree tree)
                {
                    foreach (var ch in tree.children)
                    {
                        GetAllAnimationClip(ch.motion, result);
                    }
                }
                return result;
            }

            #endregion

            #region Transition

            /// <summary>
            /// AnimatorController 内の全ての AnyState からの AnimatorStateTransition を列挙する。
            /// </summary>
            /// <param name="animator"></param>
            /// <returns></returns>
            public static IEnumerable<AnimatorStateTransition> GetAllAnyStateTransition(AnimatorController animator)
            {
                if (animator == null)
                {
                    return new AnimatorStateTransition[0];
                }
                return animator.layers.SelectMany(ly => GetAllAnyStateTransition(ly)).Distinct();
            }

            /// <summary>
            /// AnimatorControllerLayer 内の全ての AnyState からの AnimatorStateTransition を列挙する。
            /// </summary>
            /// <param name="layer"></param>
            /// <returns></returns>
            public static IEnumerable<AnimatorStateTransition> GetAllAnyStateTransition(AnimatorControllerLayer layer)
            {
                if (layer == null)
                {
                    return new AnimatorStateTransition[0];
                }
                var result = new List<AnimatorStateTransition>();
                result.AddRange(layer.stateMachine.anyStateTransitions);
                return result;
            }

            #endregion
        }

        /// <summary>
        /// AnimatorControllerLayer のコピーを行うサブユーティリティ
        /// </summary>
        private class AnimatorCopyUtility
        {
            /// <summary>
            /// コピー元
            /// </summary>
            public readonly AnimatorController srcAnimator;
            /// <summary>
            /// コピー先
            /// </summary>
            public readonly AnimatorController dstAnimator;

            public AnimatorCopyUtility(AnimatorController srcAnimator, AnimatorController dstAnimator)
            {
                this.srcAnimator = srcAnimator ?? throw new System.ArgumentNullException(nameof(srcAnimator));
                this.dstAnimator = dstAnimator ?? throw new System.ArgumentNullException(nameof(dstAnimator));
            }

            /// <summary>
            /// コピー元の AnimatorController に含まれる指定の srcLayer を、コピー先の AnimatorController に指定の名前 newName でコピーする。
            /// </summary>
            /// <param name="srcLayer"></param>
            /// <param name="newName"></param>
            /// <returns></returns>
            public AnimatorControllerLayer CopyLayer(AnimatorControllerLayer srcLayer, string newName)
            {
                var dstLayer = new AnimatorControllerLayer();
                CopyValue(srcLayer, dstLayer);

                dstLayer.name = newName;

                //// もしsrcLayerが0番目なら、dstLayerのdefaultWeightを1にしておく
                //if (1 <= srcAnimator.layers.Length && srcAnimator.layers[0].name == srcLayer.name)
                //{
                //    dstLayer.defaultWeight = 1;
                //}

                dstAnimator.AddLayer(dstLayer);

                // stateMachine をコピーする前に AddObjectToAsset しないと Animator に保存されない
                if (!string.IsNullOrWhiteSpace(AssetDatabase.GetAssetPath(dstAnimator)))
                {
                    AssetDatabase.AddObjectToAsset(dstLayer.stateMachine, dstAnimator);
                }

                // State と StateMachine を作成する
                CopyValue(srcLayer.stateMachine, dstLayer.stateMachine);
                // Transition を作成する
                ConnectTransitions(srcLayer.stateMachine, dstLayer.stateMachine);

                // StateMachineの名前を変更する
                dstLayer.stateMachine.name = newName;
                dstLayer.stateMachine.hideFlags = HideFlags.HideInHierarchy;

                EditorUtility.SetDirty(dstLayer.stateMachine);
                EditorUtility.SetDirty(dstAnimator);

                return dstLayer;
            }

            private void CopyValue(AnimatorControllerLayer src, AnimatorControllerLayer dst)
            {
                dst.name = src.name;
                dst.avatarMask = src.avatarMask;
                dst.blendingMode = src.blendingMode;
                dst.defaultWeight = src.defaultWeight;
                dst.iKPass = src.iKPass;
                dst.syncedLayerAffectsTiming = src.syncedLayerAffectsTiming;
                dst.syncedLayerIndex = src.syncedLayerIndex;
                dst.stateMachine = new AnimatorStateMachine();
            }

            private void CopyValue(AnimatorStateMachine src, AnimatorStateMachine dst)
            {
                dst.name = src.name;
                dst.anyStatePosition = src.anyStatePosition;
                dst.entryPosition = src.entryPosition;
                dst.exitPosition = src.exitPosition;
                dst.parentStateMachinePosition = src.parentStateMachinePosition;

                // AnimatorState の複製
                foreach (var state in src.states)
                {
                    var newState = dst.AddState(state.state.name, state.position);
                    CopyValue(state.state, newState);
                }

                // AnimatorStateMachine の複製
                foreach (var machine in src.stateMachines)
                {
                    var newMachine = dst.AddStateMachine(machine.stateMachine.name, machine.position);
                    CopyValue(machine.stateMachine, newMachine);
                }
            }

            private void ConnectTransitions(AnimatorStateMachine srcRoot, AnimatorStateMachine dstRoot)
            {
                // src と dst の stateMachines は同じインデックスになっているので、配列上も同じ場所にあるはず
                var srcMachines = GetAllStateMachines(srcRoot);
                var dstMachines = GetAllStateMachines(dstRoot);
                var srcStates = srcMachines.SelectMany(st => st.states).Select(st => st.state).ToArray();
                var dstStates = dstMachines.SelectMany(st => st.states).Select(st => st.state).ToArray();

                // State 間の遷移
                for (int k = 0; k < srcMachines.Length; k++)
                {
                    var src = srcMachines[k];
                    var dst = dstMachines[k];

                    // AnimatorState から出ていく AnimatorStateTransition の複製
                    for (int i = 0; i < src.states.Length; i++)
                    {
                        foreach (var srcTrn in src.states[i].state.transitions)
                        {
                            var destinationState = GetReference(srcStates, dstStates, srcTrn.destinationState);
                            var destinationStateMachine = GetReference(srcMachines, dstMachines, srcTrn.destinationStateMachine);

                            var dstTrn = destinationState != null ? dst.states[i].state.AddTransition(destinationState) :
                                destinationStateMachine != null ? dst.states[i].state.AddTransition(destinationStateMachine) :
                                dst.states[i].state.AddExitTransition();

                            CopyValue(srcTrn, dstTrn); // AnimatorStateTransition
                        }
                    }

                    // AnyState から出ていく AnimatorStateTransition の複製
                    foreach (var srcTrn in src.anyStateTransitions)
                    {
                        var destinationState = GetReference(srcStates, dstStates, srcTrn.destinationState);
                        var destinationStateMachine = GetReference(srcMachines, srcMachines, srcTrn.destinationStateMachine);

                        var dstTrn = destinationState != null ? dst.AddAnyStateTransition(destinationState) :
                            destinationStateMachine != null ? dst.AddAnyStateTransition(destinationStateMachine) : new AnimatorStateTransition();

                        CopyValue(srcTrn, dstTrn); // AnimatorStateTransition
                    }

                    // Entry から出ていく AnimatorTransition の複製
                    foreach (var srcTrn in src.entryTransitions)
                    {
                        var destinationState = GetReference(srcStates, dstStates, srcTrn.destinationState);
                        var destinationStateMachine = GetReference(srcMachines, srcMachines, srcTrn.destinationStateMachine);

                        var dstTrn = destinationState != null ? dst.AddEntryTransition(destinationState) :
                            destinationStateMachine != null ? dst.AddEntryTransition(destinationStateMachine) : new AnimatorTransition();

                        CopyValue(srcTrn, dstTrn); // AnimatorTransition
                    }

                    // DefaultState の設定
                    dst.defaultState = GetReference(srcStates, dstStates, src.defaultState);
                }

                // StateMachine 間の遷移
                for (int k = 0; k < srcMachines.Length; k++)
                {
                    // AnimatorStateMachine から出ていく AnimatorStateTransition の複製
                    for (int i = 0; i < srcMachines.Length; i++)
                    {
                        var baseSm = srcMachines[k];
                        var sourceSm = srcMachines[i];
                        foreach (var srcTrn in baseSm.GetStateMachineTransitions(sourceSm)) // baseSmが保持している、sourceSmから出ていく遷移を取得する
                        {
                            var destinationState = GetReference(srcStates, dstStates, srcTrn.destinationState);
                            var destinationStateMachine = GetReference(srcMachines, dstMachines, srcTrn.destinationStateMachine);

                            var baseSmDest = GetReference(srcMachines, dstMachines, baseSm);
                            var sourceSmDest = GetReference(srcMachines, dstMachines, sourceSm);

                            var dstTrn = destinationState != null ? baseSmDest.AddStateMachineTransition(sourceSmDest, destinationState) :
                                destinationStateMachine != null ? baseSmDest.AddStateMachineTransition(sourceSmDest, destinationStateMachine) :
                                baseSmDest.AddStateMachineExitTransition(sourceSmDest);

                            CopyValue(srcTrn, dstTrn); // AnimatorTransition
                        }
                    }
                }
            }

            private static AnimatorStateMachine[] GetAllStateMachines(AnimatorStateMachine machine, List<AnimatorStateMachine> result = null)
            {
                if (result == null)
                {
                    result = new List<AnimatorStateMachine>();
                }
                if (result.Contains(machine))
                {
                    return result.ToArray();
                }
                result.Add(machine);
                foreach (var subMachine in machine.stateMachines)
                {
                    GetAllStateMachines(subMachine.stateMachine, result);
                }
                return result.ToArray();
            }

            private static void CopyValue(AnimatorStateTransition src, AnimatorStateTransition dst)
            {
                dst.canTransitionToSelf = src.canTransitionToSelf;
                dst.conditions = src.conditions;
                dst.duration = src.duration;
                dst.exitTime = src.exitTime;
                dst.hasExitTime = src.hasExitTime;
                dst.hasFixedDuration = src.hasFixedDuration;
                dst.interruptionSource = src.interruptionSource;
                dst.isExit = src.isExit;
                dst.mute = src.mute;
                dst.name = src.name;
                dst.offset = src.offset;
                dst.orderedInterruption = src.orderedInterruption;
                dst.solo = src.solo;
            }

            private static void CopyValue(AnimatorTransition src, AnimatorTransition dst)
            {
                dst.conditions = src.conditions;
                dst.isExit = src.isExit;
                dst.mute = src.mute;
                dst.name = src.name;
                dst.solo = src.solo;
            }

            private void CopyValue(AnimatorState src, AnimatorState dst)
            {
                dst.cycleOffset = src.cycleOffset;
                dst.cycleOffsetParameter = src.cycleOffsetParameter;
                dst.cycleOffsetParameterActive = src.cycleOffsetParameterActive;
                dst.iKOnFeet = src.iKOnFeet;
                dst.mirror = src.mirror;
                dst.mirrorParameter = src.mirrorParameter;
                dst.mirrorParameterActive = src.mirrorParameterActive;
                dst.motion = CreateOrCopyMotion(src.motion);
                dst.name = src.name;
                dst.speed = src.speed;
                dst.speedParameter = src.speedParameter;
                dst.speedParameterActive = src.speedParameterActive;
                dst.tag = src.tag;
                dst.timeParameter = src.timeParameter;
                dst.timeParameterActive = src.timeParameterActive;
                dst.writeDefaultValues = src.writeDefaultValues;
                dst.behaviours = src.behaviours.Select(b => Object.Instantiate(b)).ToArray();
                for (int i = 0; i < dst.behaviours.Length; i++)
                {
                    EditorUtility.CopySerialized(src.behaviours[i], dst.behaviours[i]);
                    dst.behaviours[i].hideFlags = src.behaviours[i].hideFlags;
                    AssetDatabase.AddObjectToAsset(dst.behaviours[i], dstAnimator);
                }
            }

            private Motion CreateOrCopyMotion(Motion src)
            {
                // null ならばそのまま返す
                if (src == null)
                {
                    return src;
                }
                // pathが取得できてcontrollerではないならばそのまま返す
                var path = AssetDatabase.GetAssetPath(src);
                if (path != null || !path.EndsWith(".controller"))
                {
                    return src;
                }

                // BlendTree をコピーして controller に組込み
                if (src is BlendTree srcTree)
                {
                    var dstTree = new BlendTree();
                    CopyValue(srcTree, dstTree);
                    AssetDatabase.AddObjectToAsset(dstTree, dstAnimator);
                    return dstTree;
                }
                // AnimationClip をコピーして controller に組込み
                if (src is AnimationClip srcClip)
                {
                    var dstClip = new AnimationClip();
                    CopyValue(srcClip, dstClip);
                    AssetDatabase.AddObjectToAsset(dstClip, dstAnimator);
                    return dstClip;
                }

                // 諦めてそのまま返す
                return src;
            }

            private ChildMotion[] CreateOrCopyMotion(ChildMotion[] src)
            {
                var dst = new ChildMotion[src.Length];
                for (int i = 0; i < dst.Length; i++)
                {
                    dst[i] = src[i];
                    dst[i].motion = CreateOrCopyMotion(src[i].motion);
                }
                return dst;
            }

            private void CopyValue(AnimationClip src, AnimationClip dst)
            {
                // 値のコピー
                dst.frameRate = src.frameRate;
                dst.hideFlags = src.hideFlags;
                dst.legacy = src.legacy;
                dst.localBounds = src.localBounds;
                dst.name = src.name;
                dst.wrapMode = src.wrapMode;
                // キーフレームのコピー
                CopyAnimPropertiesIfAbsent(src, dst);
            }

            private void CopyValue(BlendTree src, BlendTree dst)
            {
                // 値のコピー
                dst.blendParameter = src.blendParameter;
                dst.blendParameterY = src.blendParameterY;
                dst.blendType = src.blendType;
                dst.hideFlags = src.hideFlags;
                dst.maxThreshold = src.maxThreshold;
                dst.minThreshold = src.minThreshold;
                dst.name = src.name;
                dst.useAutomaticThresholds = src.useAutomaticThresholds;
                // モーションのコピー
                dst.children = CreateOrCopyMotion(src.children);
            }

            private AnimatorState GetReference(AnimatorState[] srcStates, AnimatorState[] dstStates, AnimatorState value)
            {
                if (value != null)
                {
                    var index = ArrayUtility.IndexOf(srcStates, value);
                    if (0 <= index)
                    {
                        return dstStates[index];
                    }
                }
                return null;
            }

            private AnimatorStateMachine GetReference(AnimatorStateMachine[] srcStateMachines, AnimatorStateMachine[] dstStateMachines, AnimatorStateMachine value)
            {
                if (value != null)
                {
                    var index = ArrayUtility.IndexOf(srcStateMachines, value);
                    if (0 <= index)
                    {
                        return dstStateMachines[index];
                    }
                }
                return null;
            }
        }

        #endregion

        #endregion

        #region AnimationClip編集系

        #region Internal

        private static class InternalAnimClipUtil
        {
            public static bool CopyAnimPropertiesIfAbsent(AnimationClip srcClip, AnimationClip dstClip)
            {
                return CopyAnimPropertiesIfAbsent(srcClip, dstClip,
                    src_bind => AnimationUtility.GetEditorCurve(srcClip, src_bind),
                    src_bind => AnimationUtility.GetObjectReferenceCurve(srcClip, src_bind)
                    );
            }

            public static bool CopyResetAnimPropertiesIfAbsent(AnimationClip srcClip, AnimationClip dstClip, GameObject root)
            {
                return CopyAnimPropertiesIfAbsent(srcClip, dstClip,
                    src_bind =>
                    {
                        if (AnimationUtility.GetFloatValue(root, src_bind, out var value))
                        {
                            if (string.IsNullOrWhiteSpace(src_bind.path) && !string.IsNullOrWhiteSpace(src_bind.propertyName) && src_bind.propertyName != "m_IsActive")
                            {
                            // Animator自身がターゲット ≒ HumanoidRigを操作するカーブのときは、値を0としてカーブを作る
                            return AnimationCurve.Constant(0, 0, 0);
                            }
                            else
                            {
                            // そうでない場合は取得した値をデフォルト値としてカーブを作る
                            return AnimationCurve.Constant(0, 0, value);
                            }
                        }
                    // ターゲットが見つからないときは空のカーブを返す
                    return new AnimationCurve();
                    },
                    src_bind =>
                    {
                        if (AnimationUtility.GetObjectReferenceValue(root, src_bind, out var value))
                        {
                            return new ObjectReferenceKeyframe[] { new ObjectReferenceKeyframe {
                            time = 0,
                            value = value,
                        } };
                        }
                        return new ObjectReferenceKeyframe[0];
                    }
                );
            }

            private static bool CopyAnimPropertiesIfAbsent(AnimationClip srcClip, AnimationClip dstClip,
                System.Func<EditorCurveBinding, AnimationCurve> makeCurve, System.Func<EditorCurveBinding, ObjectReferenceKeyframe[]> makeObjectKeyFrames)
            {

                bool modify = false;

                // EditorCurve
                var bindings1 = AnimationUtility.GetCurveBindings(dstClip);
                foreach (var src_bind in AnimationUtility.GetCurveBindings(srcClip))
                {
                    if (bindings1.Any(dst_bind => src_bind.path == dst_bind.path && src_bind.propertyName == dst_bind.propertyName))
                    {
                        continue;
                    }
                    AnimationUtility.SetEditorCurve(dstClip, src_bind, makeCurve(src_bind));
                    modify = true;
                }

                // ObjectReferenceCurve
                var bindings2 = AnimationUtility.GetObjectReferenceCurveBindings(dstClip);
                foreach (var src_bind in AnimationUtility.GetObjectReferenceCurveBindings(srcClip))
                {
                    if (bindings2.Any(dst_bind => src_bind.path == dst_bind.path && src_bind.propertyName == dst_bind.propertyName))
                    {
                        continue;
                    }
                    AnimationUtility.SetObjectReferenceCurve(dstClip, src_bind, makeObjectKeyFrames(src_bind));
                    modify = true;
                }

                if (modify)
                {
                    EditorUtility.SetDirty(dstClip);
                }
                return modify;
            }

            public static IEnumerable<string> GetAllTransformAnimationPropertyPaths(AnimationClip clip)
            {
                var result = new List<string>();
                foreach (var bind in AnimationUtility.GetCurveBindings(clip))
                {
                    string attribute = bind.propertyName;
                    if (string.IsNullOrWhiteSpace(attribute))
                    {
                        continue;
                    }
                    if (attribute.StartsWith("m_LocalPosition")
                        || attribute.StartsWith("localEulerAnglesRaw")
                        || attribute.StartsWith("m_LocalScale"))
                    {
                        result.Add(bind.path);
                    }
                }
                return result.OrderBy(p => p).Distinct();
            }

            public static IEnumerable<string> GetAllHumanoidPropertyAttributes(AnimationClip clip)
            {
                var result = new List<string>();
                foreach (var bind in AnimationUtility.GetCurveBindings(clip))
                {
                    if (string.IsNullOrWhiteSpace(bind.path))
                    {
                        // Humanoid は Animator 自体が動かすので path が空文字
                        result.Add(bind.propertyName);
                    }
                }
                return result.OrderBy(p => p).Distinct();
            }

            public static bool GetAnimClipLoopTime(AnimationClip[] clips, out bool hasMixedValue)
            {
                return HasValue(clips.Where(clip => clip != null).Select(AnimationUtility.GetAnimationClipSettings).Select(set => set.loopTime), true, out hasMixedValue);
            }

            public static bool SetAnimClipLoopTime(AnimationClip[] clips, bool loopTime)
            {
                bool modify = false;
                foreach (var clip in clips)
                {
                    if (clip == null)
                    {
                        continue;
                    }
                    var set = AnimationUtility.GetAnimationClipSettings(clip);
                    if (set.loopTime == loopTime)
                    {
                        continue;
                    }
                    set.loopTime = loopTime;
                    AnimationUtility.SetAnimationClipSettings(clip, set);
                    EditorUtility.SetDirty(clip);
                    modify = true;
                }
                return modify;
            }
        }

        #endregion

        /// <summary>
        /// AnimationClip 内にないプロパティを別の AnimationClip からコピーする。
        /// </summary>
        /// <param name="srcClip"></param>
        /// <param name="dstClip"></param>
        /// <returns></returns>
        public static bool CopyAnimPropertiesIfAbsent(AnimationClip srcClip, AnimationClip dstClip)
        {
            return InternalAnimClipUtil.CopyAnimPropertiesIfAbsent(srcClip, dstClip);
        }

        /// <summary>
        /// AnimationClip 内にないプロパティを別の AnimationClip からリセット状態のキーにてコピーする。
        /// </summary>
        /// <param name="srcClip"></param>
        /// <param name="dstClip"></param>
        /// <param name="root"></param>
        /// <returns></returns>
        public static bool CopyResetAnimPropertiesIfAbsent(AnimationClip srcClip, AnimationClip dstClip, GameObject root)
        {
            return InternalAnimClipUtil.CopyResetAnimPropertiesIfAbsent(srcClip, dstClip, root);
        }

        /// <summary>
        /// AnimationClip 内に loopTime が true のクリップがあるかどうか返す。
        /// </summary>
        /// <param name="clips"></param>
        /// <param name="hasMixedValue">true/falseが混在しているならばtrue</param>
        /// <returns></returns>
        public static bool GetAnimClipLoopTime(AnimationClip[] clips, out bool hasMixedValue)
        {
            return InternalAnimClipUtil.GetAnimClipLoopTime(clips, out hasMixedValue);
        }

        /// <summary>
        /// 複数の AnimationClip に loopTime を設定する。
        /// </summary>
        /// <param name="clips"></param>
        /// <param name="loopTime"></param>
        /// <returns></returns>
        public static bool SetAnimClipLoopTime(AnimationClip[] clips, bool loopTime)
        {
            return InternalAnimClipUtil.SetAnimClipLoopTime(clips, loopTime);
        }

        /// <summary>
        /// Transformを変更するプロパティをAnimationClipから検索し、パスの配列を返却する。
        /// </summary>
        /// <param name="clips"></param>
        /// <returns></returns>
        public static string[] GetAllTransformAnimationPropertyPaths(IEnumerable<AnimationClip> clips)
        {
            return clips.SelectMany(clip => InternalAnimClipUtil.GetAllTransformAnimationPropertyPaths(clip))
                .OrderBy(p => p).Distinct().ToArray();
        }

        /// <summary>
        /// Humanoidを変更するプロパティをAnimationClipから検索し、プロパティ名の配列を返却する。
        /// </summary>
        /// <param name="clips"></param>
        /// <returns></returns>
        public static string[] GetAllHumanoidPropertyAttributes(IEnumerable<AnimationClip> clips)
        {
            return clips.SelectMany(clip => InternalAnimClipUtil.GetAllHumanoidPropertyAttributes(clip))
                .OrderBy(p => p).Distinct().ToArray();
        }

        #endregion

        #region AvatarMask 編集系

        #region Internal

        private static class InternalAvatarMaskUtil
        {
            public static AvatarMask GenerateAvatarMask(GameObject root)
            {
                // 生成
                var newAvatarMask = new AvatarMask();
                if (root != null)
                {
                    // AvatarRootからTransformを列挙し、avatarMaskに含まれていないものを追加する
                    foreach (var t in root.GetComponentsInChildren<Transform>(true).Distinct())
                    {
                        newAvatarMask.AddTransformPath(t, false); // recursive を false にしないと多重登録される
                    }
                }
                return newAvatarMask;
            }

            public static AvatarMask GenerateAvatarMask(GameObject root, bool enableIK, bool enableHumanoids, bool enableTransforms)
            {
                // 生成
                var newAvatarMask = GenerateAvatarMask(root);

                // 初期値を設定
                for (AvatarMaskBodyPart idx = 0; idx < AvatarMaskBodyPart.LastBodyPart; idx++)
                {
                    newAvatarMask.SetHumanoidBodyPartActive(idx, IKBodyParts.Contains(idx) ? enableIK : enableHumanoids);
                }
                for (int i = 0; i < newAvatarMask.transformCount; i++)
                {
                    newAvatarMask.SetTransformActive(i, enableTransforms);
                }
                return newAvatarMask;
            }

            private static readonly HashSet<AvatarMaskBodyPart> IKBodyParts = new HashSet<AvatarMaskBodyPart>()
            {
                AvatarMaskBodyPart.Root,
                AvatarMaskBodyPart.LeftFootIK,
                AvatarMaskBodyPart.RightFootIK,
                AvatarMaskBodyPart.LeftHandIK,
                AvatarMaskBodyPart.RightHandIK,
            };

            public static void SetAvatarMaskTransformActive(AvatarMask mask, string[] paths, bool active)
            {
                // 足りないなら追加する
                AddAvatarMaskPathIfAbsent(mask, paths);
                // 設定
                foreach (var path in paths)
                {
                    var idx = FindTransformPath(mask, path);
                    if (0 <= idx)
                    {
                        mask.SetTransformActive(idx, active);
                    }
                }
                EditorUtility.SetDirty(mask);
            }

            public static void AddAvatarMaskPathIfAbsent(AvatarMask mask, GameObject root)
            {
                var paths = root.GetComponentsInChildren<Transform>(true)
                    .Select(t => AnimationUtility.CalculateTransformPath(t, root.transform))
                    .OrderBy(p => p).ToArray();
                AddAvatarMaskPathIfAbsent(mask, paths);
            }

            public static void AddAvatarMaskPathIfAbsent(AvatarMask mask, params string[] paths)
            {
                // 一時的に GameObject を作成してダミーの Transform を用意
                var go = EditorUtility.CreateGameObjectWithHideFlags("temp", HideFlags.HideAndDontSave);

                // もしtransformが未登録ならば空を追加、paths に空文字が入っていても Except で除外される
                if (mask.transformCount == 0)
                {
                    mask.AddTransformPath(go.transform, true);
                    // 追加された Transform のパスを path に変更する
                    mask.SetTransformPath(mask.transformCount - 1, "");
                }

                foreach (var path in paths.Except(GetAllTransformPath(mask)))
                {
                    // ダミー追加
                    mask.AddTransformPath(go.transform, false);
                    // 追加された Transform のパスを path に変更する
                    mask.SetTransformPath(mask.transformCount - 1, path);
                }
                EditorUtility.SetDirty(mask);

                // 一時的な GameObject は消去
                Object.DestroyImmediate(go);
            }

            public static void RemoveAvatarMaskPathIfUnmatched(AvatarMask mask, GameObject root)
            {
                var paths = root.GetComponentsInChildren<Transform>(true)
                    .Select(t => AnimationUtility.CalculateTransformPath(t, root.transform))
                    .OrderBy(p => p).ToArray();
                RemoveAvatarMaskPathIfUnmatched(mask, paths);
            }

            public static void RemoveAvatarMaskPathIfUnmatched(AvatarMask mask, params string[] paths)
            {
                // mask かつ paths のパスのみ抽出
                var oldPath = new List<string>(GetAllTransformPath(mask));
                oldPath.RemoveAll(p => !paths.Contains(p));

                // oldPath かつ paths のAvatarMaskを作成し、内容をmaskからコピーする
                var tempMask = new AvatarMask();
                AddAvatarMaskPathIfAbsent(tempMask, oldPath.ToArray());
                CopyAvatarMask(mask, tempMask);
                tempMask.name = mask.name; // 名前もコピー

                // mask に内容を書き戻す
                EditorUtility.CopySerialized(tempMask, mask);
                EditorUtility.SetDirty(mask);
            }

            public static string[] GetAllTransformPath(AvatarMask mask)
            {
                var result = new List<string>();
                for (int i = 0; i < mask.transformCount; i++)
                {
                    result.Add(mask.GetTransformPath(i));
                }
                return result.Distinct().OrderBy(p => p).ToArray();
            }

            public static void CopyAvatarMask(AvatarMask src, AvatarMask dst)
            {
                for (AvatarMaskBodyPart idx = 0; idx < AvatarMaskBodyPart.LastBodyPart; idx++)
                {
                    dst.SetHumanoidBodyPartActive(idx, src.GetHumanoidBodyPartActive(idx));
                }

                for (int i = 0; i < src.transformCount; i++)
                {
                    var idx = FindTransformPath(dst, src.GetTransformPath(i));
                    if (0 <= idx)
                    {
                        // 存在する場合はコピー
                        dst.SetTransformActive(idx, src.GetTransformActive(i));
                    }
                }

                EditorUtility.SetDirty(dst);
            }

            private static int FindTransformPath(AvatarMask m, string path)
            {
                for (int i = 0; i < m.transformCount; i++)
                {
                    if (m.GetTransformPath(i) == path)
                    {
                        return i;
                    }
                }
                return -1;
            }
        }

        #endregion

        /// <summary>
        /// 指定のGameObjectをルートとする新しいAvatarMaskを作成する。
        /// </summary>
        /// <param name="root"></param>
        /// <returns></returns>
        public static AvatarMask GenerateAvatarMask(GameObject root)
        {
            return InternalAvatarMaskUtil.GenerateAvatarMask(root);
        }

        /// <summary>
        /// 指定のGameObjectをルートとする新しいAvatarMaskを作成する。
        /// </summary>
        /// <param name="root"></param>
        /// <returns></returns>
        public static AvatarMask GenerateAvatarMask(GameObject root, bool enableIK, bool enableHumanoids, bool enableTransforms)
        {
            return InternalAvatarMaskUtil.GenerateAvatarMask(root, enableIK, enableHumanoids, enableTransforms);
        }

        /// <summary>
        /// AvatarMask の Transform を設定する。指定のパスが足りない場合は追加する。
        /// </summary>
        /// <param name="mask"></param>
        /// <param name="paths"></param>
        /// <param name="active"></param>
        public static void SetAvatarMaskTransformActive(AvatarMask mask, string[] paths, bool active)
        {
            InternalAvatarMaskUtil.SetAvatarMaskTransformActive(mask, paths, active);
        }

        /// <summary>
        /// AvatarMask に指定のGameObject配下のTransformが存在しない場合は追加する。
        /// </summary>
        /// <param name="mask"></param>
        /// <param name="root"></param>
        public static void AddAvatarMaskPathIfAbsent(AvatarMask mask, GameObject root)
        {
            InternalAvatarMaskUtil.AddAvatarMaskPathIfAbsent(mask, root);
        }

        /// <summary>
        /// AvatarMask に指定のパスが示すTransformが存在しない場合は追加する。
        /// </summary>
        /// <param name="mask"></param>
        /// <param name="paths"></param>
        public static void AddAvatarMaskPathIfAbsent(AvatarMask mask, params string[] paths)
        {
            InternalAvatarMaskUtil.AddAvatarMaskPathIfAbsent(mask, paths);
        }

        /// <summary>
        /// AvatarMask に指定のGameObject配下のTransformが存在しない場合は削除する。
        /// </summary>
        /// <param name="mask"></param>
        /// <param name="root"></param>
        public static void RemoveAvatarMaskPathIfUnmatched(AvatarMask mask, GameObject root)
        {
            InternalAvatarMaskUtil.RemoveAvatarMaskPathIfUnmatched(mask, root);
        }

        /// <summary>
        /// AvatarMask から全ての Transform パスを抽出する。
        /// </summary>
        /// <param name="mask"></param>
        /// <returns></returns>
        public static string[] GetAllTransformPath(AvatarMask mask)
        {
            return InternalAvatarMaskUtil.GetAllTransformPath(mask);
        }

        /// <summary>
        /// AvatarMask の値を別の AvatarMask にコピーする。足りない項目は追加しない。
        /// </summary>
        /// <param name="src"></param>
        /// <param name="dst"></param>
        public static void CopyAvatarMask(AvatarMask src, AvatarMask dst)
        {
            InternalAvatarMaskUtil.CopyAvatarMask(src, dst);
        }

        #endregion

        private static bool HasValue<T>(IEnumerable<T> list, T value, out bool hasMixedValue)
        {
            var values = list.Distinct().ToArray();
            hasMixedValue = 2 <= values.Length;
            return values.Contains(value);
        }
    }
}

#endif
