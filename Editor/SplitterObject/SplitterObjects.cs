/*
 *  The MIT License
 *
 *  Copyright 2023-2026 whiteflare.
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

using System.Text;
using UnityEngine;
using UnityEditor;

namespace WF.Tool.Avatar
{
    internal class SplitterObjects
    {
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

        public static void CreateSplitterObject(string label, int length, int count = 1)
        {
            label = Repeat(label, length);
            for (int i = 0; i < count; i++)
            {
                GameObject go = new GameObject(label);
                go.transform.position = Vector3.zero;
                go.tag = "EditorOnly";
                Undo.RegisterCreatedObjectUndo(go, "Create Separator");
            }
        }

        private static string Repeat(string label, int length)
        {
            StringBuilder sb = new StringBuilder(label.Length * length);
            for (int i = 0; i < length; i++)
            {
                sb.Append(label);
            }
            return sb.ToString();
        }
    }
}

#endif
