/*
 *  The MIT License
 *
 *  Copyright 2020-2026 whiteflare.
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

using UnityEngine;

namespace WF.Tool.Avatar.BU
{
#if ENV_VRCSDK3_AVATAR && ENV_NDMF
    [AddComponentMenu("Flare's Avatar Tools/Bounds Unificator/[BU] Set Anchor And Bounds")]
#endif
    [DisallowMultipleComponent]
    public class SetAnchorAndBounds : MonoBehaviour
#if ENV_VRCSDK3_AVATAR
        , VRC.SDKBase.IEditorOnly
#endif
    {
        public Transform customAnchorOverride = null;
        public Transform customRootBone = null;

        private void OnEnable()
        {
            // これ入れておくとInspectorに有効無効のチェックボックスが追加される
        }
    }
}
