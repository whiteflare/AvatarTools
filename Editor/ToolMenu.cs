/*
 *  The MIT License
 *
 *  Copyright 2023 whiteflare.
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

using UnityEditor;

namespace WF.Tool.Avatar
{
    internal class ToolMenu
    {
        #region Tools

        [MenuItem("Tools/whiteflare/Anim Edit Utility", priority = 11)]
        public static void Menu_AnimEditUtility()
        {
            AnimEdit.AnimEditUtilWindow.ShowWindow();
        }

        [MenuItem("Tools/whiteflare/Avatar Copy Utility", priority = 12)]
        public static void Menu_AvatarCopyUtility()
        {
            AvatarCopyUtility.ShowWindow();
        }

        [MenuItem("Tools/whiteflare/Avatar Texture Tool", priority = 13)]
        public static void Menu_AvatarTextureTool()
        {
            AvTexTool.AvatarTexTool.ShowWindow();
        }

        [MenuItem("Tools/whiteflare/Bounds Unificator", priority = 14)]
        public static void Menu_BoundsUniticator()
        {
            BoundsUnificator.ShowWindow();
        }

        [MenuItem("Tools/whiteflare/Hierarchy Helper", priority = 15)]
        public static void Menu_HierarchyHelper()
        {
            HierarchyHelper.ShowWindow();
        }

        [MenuItem("Tools/whiteflare/Mesh Poly Counter", priority = 16)]
        public static void Menu_MeshPolyCounter()
        {
            MeshPolyCounter.ShowWindow();
        }

        [MenuItem("Tools/whiteflare/PhysBone Setup Helper", priority = 17)]
        public static void Menu_PBSetupHelper()
        {
            PhysBoneSetupHelper.ShowWindow();
        }

        #endregion

        #region GameObject

#if ENV_VRCSDK3_AVATAR
        [MenuItem("GameObject/WriteDefaultをオフにする", priority = 10)]
        public static void GoMenu_WriteDefault()
        {
            AnimEdit.AnimEditUtilWindow.ShowWindowWriteDefault();
        }

        [MenuItem("GameObject/AvatarMaskのセットアップ", priority = 10)]
        public static void GoMenu_AvatarMask()
        {
            AnimEdit.AnimEditUtilWindow.ShowWindowAvatarMask();
        }
#endif

        [MenuItem("GameObject/Create Other/Splitter/Splitter - 32")]
        public static void CreateSplitter01()
        {
            SplitterObjects.CreateSplitterObject("-", 32);
        }

        [MenuItem("GameObject/Create Other/Splitter/Splitter - 32 x 16")]
        public static void CreateSplitter02()
        {
            SplitterObjects.CreateSplitterObject("-", 32, 16);
        }

        [MenuItem("GameObject/Create Other/Splitter/Splitter = 24")]
        public static void CreateSplitter03()
        {
            SplitterObjects.CreateSplitterObject("=", 24);
        }

        [MenuItem("GameObject/Create Other/Splitter/Splitter = 24 x 16")]
        public static void CreateSplitter04()
        {
            SplitterObjects.CreateSplitterObject("=", 24, 16);
        }

        #endregion
    }
}

#endif
