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

using UnityEditor;

namespace WF.Tool.Avatar
{
    internal class ToolMenu
    {
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

        [MenuItem("Tools/whiteflare/BakeKillerFinder改", priority = 14)]
        public static void Menu_BakeKillerFinder()
        {
            VKetEditorTools.BakeKillerFinder.BakeKillerFinderZweiWindow.ShowWindow();
        }

        [MenuItem("Tools/whiteflare/Bounds Unificator", priority = 15)]
        public static void Menu_BoundsUniticator()
        {
            BoundsUnificator.ShowWindow();
        }

        [MenuItem("Tools/whiteflare/Hierarchy Helper", priority = 16)]
        public static void Menu_HierarchyHelper()
        {
            HierarchyHelper.ShowWindow();
        }

        [MenuItem("Tools/whiteflare/Mesh Poly Counter", priority = 17)]
        public static void Menu_MeshPolyCounter()
        {
            MeshPolyCounter.ShowWindow();
        }

        [MenuItem("Tools/whiteflare/PhysBone Setup Helper", priority = 18)]
        public static void Menu_PBSetupHelper()
        {
            PhysBoneSetupHelper.ShowWindow();
        }
    }
}

#endif
