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

#if UNITY_EDITOR && ENV_NDMF

using UnityEngine;
using nadena.dev.ndmf;
#if ENV_AAO
using Anatawa12.AvatarOptimizer.API;
#endif

[assembly: ExportsPlugin(typeof(WF.Tool.Avatar.BU.BoundsUnificatorNdmfPlugin))]

namespace WF.Tool.Avatar.BU
{
    public class BoundsUnificatorNdmfPlugin : Plugin<BoundsUnificatorNdmfPlugin>
    {
        public override string QualifiedName => "jp.whiteflare.avatartools.bu.set-anchor-and-bounds";

        public override string DisplayName => "Set Anchor And Bounds";

        protected override void Configure()
        {
#if ENV_AAO
            InPhase(BuildPhase.Optimizing)
                .BeforePlugin("com.anatawa12.avatar-optimizer")
                .Run("PreExecute", ctx => Execute(ctx, BoundsCalcMode.CurrentValueOnly, false));
#endif
            InPhase(BuildPhase.Optimizing)
                .AfterPlugin("com.anatawa12.avatar-optimizer")
                .Run("Execute", ctx => Execute(ctx, BoundsCalcMode.SkinnedVertex, true));
        }

        private void Execute(BuildContext ctx, BoundsCalcMode calcMode, bool destroy)
        {
            var avatarRoot = ctx.AvatarRootObject;
            var component = avatarRoot?.GetComponent<SetAnchorAndBounds>();
            if (component == null)
            {
                return;
            }
            if (component.enabled)
            {
                var calculator = ScriptableObject.CreateInstance<BoundsRecalculator>();
                calculator.SetAvatarRoot(avatarRoot);
                SetCustomSettings(component, calculator);
                calculator.CalcBounds(calcMode);
                calculator.ApplyBoundsWithoutUndo();
            }
            if (destroy)
            {
                Object.DestroyImmediate(component);
            }
        }

        private static void SetCustomSettings(SetAnchorAndBounds component, BoundsRecalculator calculator)
        {
            if (component.customAnchorOverride != null)
            {
                calculator.anchorTarget = component.customAnchorOverride;
            }
            if (component.customRootBone != null)
            {
                calculator.rootBone = component.customRootBone;
            }
        }
    }

#if ENV_AAO

    [ComponentInformation(typeof(SetAnchorAndBounds))]
    internal class BoundsUnificatorNdmfPluginInformation : ComponentInformation<SetAnchorAndBounds>
    {
        protected override void CollectDependency(SetAnchorAndBounds component, ComponentDependencyCollector collector)
        {
            collector.MarkEntrypoint();
        }
    }

#endif
}

#endif
