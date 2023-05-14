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

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.Rendering;

namespace WF.Tool.Avatar.AvTexTool
{
    internal class AvatarTexTool : EditorWindow
    {
        public static void ShowWindow()
        {
            var window = GetWindow<AvatarTexTool>("Avatar Texture Tool");
            var go = Selection.activeGameObject;
            if (go != null)
            {
                window.rootObject = go;
                window.UpdateTreeView();
            }
        }

        private static AvatarTexTool currentWindow = null;

        public TreeViewState treeViewState;
        private TextureListView treeView;
        private GameObject rootObject;
        private float previewSize = 16;

        private void OnEnable()
        {
            currentWindow = this;
            if (treeViewState == null)
            {
                treeViewState = new TreeViewState();
            }
            treeView = new TextureListView(treeViewState);
            UpdateTreeView();
        }

        private void OnHierarchyChange()
        {
            if (rootObject == null)
            {
                UpdateTreeView();
            }
        }

        private void UpdateTreeView()
        {
            treeView.SetTexturePreviewSize(previewSize);
            treeView.items = CreateTreeViewItems(rootObject);
            treeView.SortItems();
            treeView.Reload();
            ClearViewSelection();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            {
                EditorGUI.BeginChangeCheck();
                rootObject = EditorGUILayout.ObjectField("Root Object", rootObject, typeof(GameObject), true) as GameObject;
                if (EditorGUI.EndChangeCheck())
                {
                    UpdateTreeView();
                }
                if (GUILayout.Button("Refresh", GUILayout.Width(80)))
                {
                    UpdateTreeView();
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            {
                var size = GetTotalVRAMSize();
                GUILayout.Label(
#if ENV_VRCSDK3_AVATAR
                    string.Format("Total VRAM: {0} ({1})", ToPrettyString(size), GetPerformanceRank(size))
#else
                    string.Format("Total VRAM: {0}", ToPrettyString(size))
#endif
                    , EditorStyles.boldLabel);

                GUILayout.FlexibleSpace();

                EditorGUI.BeginChangeCheck();
                previewSize = GUILayout.HorizontalSlider(previewSize, 16, 128, GUILayout.Width(55));
                if (EditorGUI.EndChangeCheck())
                {
                    treeView.SetTexturePreviewSize(previewSize);
                }

                GUILayout.Space(32);

                using (new EditorGUI.DisabledGroupScope(!treeView.items.Any(tv => tv.HasDirtyValue())))
                {
                    if (GUILayout.Button("Apply"))
                    {
                        DoApply();
                    }
                    if (GUILayout.Button("Revert"))
                    {
                        DoRevert();
                    }
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            treeView.OnGUI(GUILayoutUtility.GetRect(0, float.MaxValue, 0, float.MaxValue));
        }

        private void UpdateVRAMSize()
        {
            foreach (var tv in treeView.items)
            {
                tv.UpdateVRAMSize();
            }
        }

        private void ClearViewSelection()
        {
            treeView.SetSelection(new int[0]);
        }

        private void DoApply()
        {
            var paths = new List<string>();
            foreach (var tv in treeView.items)
            {
                if (tv.ApplyValue())
                {
                    var path = AssetDatabase.GetAssetPath(tv.texture);
                    if (!string.IsNullOrEmpty(path))
                    {
                        paths.Add(path);
                    }
                }
            }

            AssetDatabase.SaveAssets();

            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var path in paths)
                {
                    AssetDatabase.ImportAsset(path);
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            RegisterDelayedRefleshVRAMSize();
            ClearViewSelection();
        }

        private void DoRevert()
        {
            foreach (var tv in treeView.items)
            {
                tv.ClearDirtyValue();
            }
            RegisterDelayedRefleshVRAMSize();
            ClearViewSelection();
        }

        private bool registered = false;
        private void RegisterDelayedRefleshVRAMSize()
        {
            if (registered)
            {
                return;
            }
            registered = true;
            EditorApplication.delayCall += () =>
            {
                UpdateVRAMSize();
                registered = false;
            };
        }

        internal class TexImportHook : AssetPostprocessor
        {
            void OnPostprocessTexture(Texture2D texture)
            {
                if (currentWindow == null)
                {
                    return;
                }
                currentWindow.RegisterDelayedRefleshVRAMSize();
            }
        }

        private string GetPerformanceRank(long bytes)
        {
            if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android)
            {
                if (bytes < 10 * 1024 * 1024)
                {
                    return "Excellent";
                }
                if (bytes < 18 * 1024 * 1024)
                {
                    return "Good";
                }
                if (bytes < 25 * 1024 * 1024)
                {
                    return "Medium";
                }
                if (bytes < 40 * 1024 * 1024)
                {
                    return "Poor";
                }
                return "Very Poor";
            }
            else
            {
                if (bytes < 40 * 1024 * 1024)
                {
                    return "Excellent";
                }
                if (bytes < 75 * 1024 * 1024)
                {
                    return "Good";
                }
                if (bytes < 110 * 1024 * 1024)
                {
                    return "Medium";
                }
                if (bytes < 150 * 1024 * 1024)
                {
                    return "Poor";
                }
                return "Very Poor";
            }
        }

        private static string ToPrettyString(long bytes)
        {
            if (bytes < 1024)
                return bytes + " bytes";
            if (bytes < 1024 * 1024)
                return Math.Round(bytes / 1024.0, 2) + " KiB";
            if (bytes < 1024 * 1024 * 1024)
                return Math.Round(bytes / 1024.0 / 1024.0, 2) + " MiB";
            return Math.Round(bytes / 1024.0 / 1024.0 / 1024.0, 2) + " GiB";
        }

        private long GetTotalVRAMSize()
        {
            return treeView.items.Where(item => item != null).Select(item => item.vramSize).Sum();
        }

        private TxTreeViewItem[] CreateTreeViewItems(GameObject root)
        {
#if ENV_VRCSDK3_AVATAR
            // VRCSDK3 Avatar で未指定のときは何も取らない
            if (root == null)
            {
                return new TxTreeViewItem[0];
            }
#endif
            // シーンからマテリアル→テクスチャを検索
            var seeker = new MaterialSeeker();
            seeker.FilterHierarchy = IsNotEditorOnly;
            var mats = root != null ? seeker.GetAllMaterials(root) : seeker.GetAllMaterialsInScene();
            var texs = new List<Texture>();
            texs.AddRange(mats.SelectMany(GetAllTextures));

            // root未指定ならばライトマップを追加
            if (root == null)
            {
                foreach(var lm in LightmapSettings.lightmaps)
                {
                    if (lm != null)
                    {
                        texs.Add(lm.lightmapColor);
                        texs.Add(lm.lightmapDir);
                        texs.Add(lm.shadowMask);
                    }
                }
            }

            return texs.Where(tex => tex != null).Distinct().Select(tex => new TxTreeViewItem(tex)).ToArray();
        }

        private static bool IsNotEditorOnly(Component cmp)
        {
            while (cmp != null)
            {
                if (cmp.gameObject.CompareTag("EditorOnly"))
                {
                    return false;
                }
                cmp = cmp.transform.parent;
            }
            return true;
        }

        private static Texture[] GetAllTextures(Material mat)
        {
            var result = new List<Texture>();
            for (int i = 0; i < mat.shader.GetPropertyCount(); i++)
            {
                if (mat.shader.GetPropertyType(i) == ShaderPropertyType.Texture)
                {
                    var tex = mat.GetTexture(mat.shader.GetPropertyName(i));
                    if (tex != null)
                    {
                        result.Add(tex);
                    }
                }
            }
            return result.ToArray();
        }

        const int COL_TextureName = 0;
        const int COL_TextureType = 1;
        const int COL_TextureWidth = 2;
        const int COL_TextureHeight = 3;
        const int COL_MipmapCount = 4;
        const int COL_VRAMSize = 5;
        const int COL_MaxSize = 6;
        const int COL_Compression = 7;
        const int COL_CrunchCompression = 8;
        const int COL_CrunchQuality = 9;
        const int COL_ActualFormat = 10;
        const int COL_GenerateMipmap = 11;
        const int COL_StreamingMipmap = 12;

        internal class TxTreeViewItem
        {
            public readonly Texture texture;
            public readonly TextureImporter importer;

            public long vramSize;

            private int? changedMaxSize = null;
            private TextureImporterCompression? changedTextureCompression = null;
            private bool? changedCrunchCompression = null;
            private int? changedCrunchQuality = null;
            private bool? changedGenerateMipmaps = null;
            private bool? changedStreamingMipmaps = null;

            public TxTreeViewItem(Texture texture)
            {
                this.texture = texture;

                var path = AssetDatabase.GetAssetPath(texture);
                if (string.IsNullOrWhiteSpace(path))
                    this.importer = null;
                else
                    this.importer = AssetImporter.GetAtPath(path) as TextureImporter;

                UpdateVRAMSize();

                var tf = GetCurrentTextureImporterFormat();
                var f = GetCurrentTextureFormat();
                if (tf != null && f != null && tf.ToString() != f.ToString())
                {
                    Debug.LogWarningFormat(texture, "mismatch texture {0} format, importer={1}, actual={2}", texture, tf, f);
                }
            }

            public void UpdateVRAMSize()
            {
                this.vramSize = CalcTextureRuntimeSize(texture);
            }

            private long CalcTextureRuntimeSize(Texture tex)
            {
                var bit = GetCurrentTextureBitsPerPixel();
                if (bit != null)
                {
                    long total = 0;
                    GetTextureSize(tex, out var w, out var h, out var d);
                    for (int i = 0; i < texture.mipmapCount; i++)
                    {
                        total += (long)Math.Ceiling(((double)bit) * w * h * d);
                        w /= 2;
                        h /= 2;
                    }
                    return total / 8;
                }
                return UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(tex);
            }

            private void GetTextureSize(Texture tex, out int width, out int height, out int depth)
            {
                width = tex.width;
                height = tex.height;
                depth = 1;
                if (tex is Texture2DArray t2a)
                    depth = t2a.depth;
            }

            private double? GetCurrentTextureBitsPerPixel()
            {
                switch (texture.dimension)
                {
                    case TextureDimension.Tex2D:
                        {
                            var rf = GetCurrentRenderTextureFormat();
                            if (rf != null)
                            {
                                return getTextureBitsPerPixel(rf) + ((RenderTexture)texture).depth;
                            }
                            var tf = GetCurrentTextureFormat();
                            if (tf != null)
                            {
                                return getTextureBitsPerPixel(tf);
                            }
                            var tif = GetCurrentTextureImporterFormat();
                            if (tif != null)
                            {
                                return getTextureBitsPerPixel(tif);
                            }
                        }
                        break;
                    case TextureDimension.Tex2DArray:
                        {
                            var tf = GetCurrentTextureFormat();
                            if (tf != null)
                            {
                                return getTextureBitsPerPixel(tf);
                            }
                        }
                        break;
                }
                return null;
            }

            public bool HasDirtyValue()
            {
                return changedMaxSize != null
                    || changedTextureCompression != null
                    || changedCrunchCompression != null
                    || changedCrunchQuality != null
                    || changedGenerateMipmaps != null
                    || changedStreamingMipmaps != null
                    ;
            }

            public void ClearDirtyValue()
            {
                changedMaxSize = null;
                changedTextureCompression = null;
                changedCrunchCompression = null;
                changedCrunchQuality = null;
                changedGenerateMipmaps = null;
                changedStreamingMipmaps = null;
            }

            public bool ApplyValue()
            {
                if (!HasDirtyValue())
                {
                    return false;
                }

                if (importer != null)
                {
                    var settings = importer.GetPlatformTextureSettings(GetCurrentPlatformString());
                    if (settings != null && settings.overridden)
                    {
                        if (changedMaxSize != null)
                            settings.maxTextureSize = (int)changedMaxSize;
                        importer.SetPlatformTextureSettings(settings);
                    }

                    if (changedMaxSize != null)
                        importer.maxTextureSize = (int)changedMaxSize;

                    if (!IsCurrentPlatformSettingsOverridden())
                    {
                        if (changedTextureCompression != null && IsFormatAutomatic())
                            importer.textureCompression = (TextureImporterCompression)changedTextureCompression;
                        if (changedCrunchCompression != null)
                            importer.crunchedCompression = (bool)changedCrunchCompression;
                        if (changedCrunchQuality != null)
                            importer.compressionQuality = (int)changedCrunchQuality;
                    }

                    if (changedGenerateMipmaps != null)
                        importer.mipmapEnabled = (bool)changedGenerateMipmaps;
                    if (importer.mipmapEnabled && changedStreamingMipmaps != null)
                        importer.streamingMipmaps = (bool)changedStreamingMipmaps;

                    ClearDirtyValue();

                    EditorUtility.SetDirty(importer);
                    return true;
                }
                return false;
            }

            public object GetValue(int idx)
            {
                if (texture == null)
                {
                    return null; // 途中でobjectがdestroyされた場合は何もしない
                }
                switch (idx)
                {
                    case COL_TextureName:
                        return texture.name;
                    case COL_TextureWidth:
                        return texture.width;
                    case COL_TextureHeight:
                        return texture.height;
                    case COL_MipmapCount:
                        if (texture.mipmapCount <= 1)
                            return null;
                        return texture.mipmapCount - 1;

                    case COL_VRAMSize:
                        return vramSize;

                    case COL_TextureType:
                        if (texture is RenderTexture)
                            return "RenderTexture";
                        return GetTextureType();
                    case COL_ActualFormat:
                        return GetCurrentRenderTextureFormat() ?? (object) GetCurrentTextureFormat();

                    case COL_MaxSize:
                        return GetTextureMaxSize();
                    case COL_Compression:
                        return GetTextureCompression();
                    case COL_CrunchCompression:
                        return IsCrunchCompression();
                    case COL_CrunchQuality:
                        return GetCrunchQuality();

                    case COL_GenerateMipmap:
                        return GetGenerateMipmaps();
                    case COL_StreamingMipmap:
                        return GetStreamingMipmaps();

                    default:
                        return null;
                }
            }

            public bool IsDirtyValue(int idx)
            {
                switch (idx)
                {
                    case COL_MaxSize:
                        return changedMaxSize != null;
                    case COL_Compression:
                        return IsFormatAutomatic() && changedTextureCompression != null;
                    case COL_CrunchCompression:
                        return changedCrunchCompression != null;
                    case COL_CrunchQuality:
                        return changedCrunchQuality != null;
                    case COL_GenerateMipmap:
                        return changedGenerateMipmaps != null;
                    case COL_StreamingMipmap:
                        return changedStreamingMipmaps != null;

                    default:
                        return false;
                }
            }

            public string GetTextureType()
            {
                if (importer == null)
                {
                    return null;
                }
                var t = importer.textureType;
                switch (t)
                {
                    case TextureImporterType.Default:
                    case TextureImporterType.NormalMap:
                    case TextureImporterType.SingleChannel:
                        var s = importer.textureShape;
                        if (s == TextureImporterShape.Texture2D)
                            return string.Format("{0} {1}", t, "2D");
                        if (s == TextureImporterShape.TextureCube)
                            return string.Format("{0} {1}", t, "Cube");
                        return string.Format("{0} {1}", t, s);

                    default:
                        return t.ToString();
                }
            }

            public int? GetTextureMaxSize()
            {
                if (importer == null)
                {
                    return null;
                }
                if (changedMaxSize != null)
                {
                    return changedMaxSize;
                }
                var settings = GetCurrentPlatformSettingsOverridden() ?? importer.GetDefaultPlatformTextureSettings();
                return settings.maxTextureSize;
            }

            public TextureImporterCompression? GetTextureCompression()
            {
                if (importer == null || IsCurrentPlatformSettingsOverridden() || !IsFormatAutomatic())
                {
                    return null;
                }
                if (changedTextureCompression != null)
                {
                    return changedTextureCompression;
                }
                return importer.GetDefaultPlatformTextureSettings().textureCompression;
            }

            public bool? IsCrunchCompression()
            {
                if (importer == null || IsCurrentPlatformSettingsOverridden())
                {
                    return null;
                }
                if (changedCrunchCompression != null)
                {
                    return changedCrunchCompression;
                }
                var format = GetCurrentTextureFormat();
                if (format == null || !isCrunchSupported(format))
                {
                    return null;
                }
                return importer.GetDefaultPlatformTextureSettings().crunchedCompression;
            }

            public int? GetCrunchQuality()
            {
                if (IsCrunchCompression() == true) // Overridden されているときは null が返ってくる
                {
                    if (changedCrunchQuality != null)
                    {
                        return changedCrunchQuality;
                    }
                    return importer.GetDefaultPlatformTextureSettings().compressionQuality;
                }
                return null;
            }


            public bool? GetGenerateMipmaps()
            {
                //if (texture is RenderTexture)
                //{
                //    if (changedGenerateMipmaps != null)
                //        return changedGenerateMipmaps;
                //    return ((RenderTexture)texture).autoGenerateMips;
                //}
                if (importer != null)
                {
                    if (changedGenerateMipmaps != null)
                        return changedGenerateMipmaps;
                    return importer.mipmapEnabled;
                }
                return null;
            }

            public bool? GetStreamingMipmaps()
            {
                if (importer != null)
                {
                    if (GetGenerateMipmaps() == true)
                    {
                        if (changedStreamingMipmaps != null)
                            return changedStreamingMipmaps;
                        return importer.streamingMipmaps;
                    }
                }
                return null;
            }

            public TextureImporterFormat? GetCurrentTextureImporterFormat()
            {
                var result = GetCurrentTextureImporterFormatRaw();
                if (result == TextureImporterFormat.Automatic)
                {
                    result = importer.GetAutomaticFormat(GetCurrentPlatformString());
                }
                return result;
            }

            public TextureImporterFormat? GetCurrentTextureImporterFormatRaw()
            {
                if (importer == null)
                {
                    return null;
                }
                var settings = GetCurrentPlatformSettingsOverridden() ?? importer.GetDefaultPlatformTextureSettings();
                return settings.format;
            }

            public bool IsFormatAutomatic()
            {
                return GetCurrentTextureImporterFormatRaw() == TextureImporterFormat.Automatic;
            }

            private bool IsCurrentPlatformSettingsOverridden()
            {
                if (importer != null)
                {
                    var settings = importer.GetPlatformTextureSettings(GetCurrentPlatformString());
                    if (settings != null && settings.overridden)
                    {
                        return true;
                    }
                }
                return false;
            }

            private TextureImporterPlatformSettings GetCurrentPlatformSettingsOverridden()
            {
                if (importer != null)
                {
                    var settings = importer.GetPlatformTextureSettings(GetCurrentPlatformString());
                    if (settings != null && settings.overridden)
                    {
                        return settings;
                    }
                }
                return null;
            }

            private static string GetCurrentPlatformString()
            {
                if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android)
                    return "Android";
                return "Standalone";
            }

            public RenderTextureFormat? GetCurrentRenderTextureFormat()
            {
                if (texture is RenderTexture)
                {
                    return ((RenderTexture)texture).format;
                }
                return null;
            }

            public TextureFormat? GetCurrentTextureFormat()
            {
                if (texture is Texture2D t2d)
                {
                    return t2d.format;
                }
                if (texture is Texture2DArray t2a)
                {
                    return t2a.format;
                }
                if (texture is Texture3D t3d)
                {
                    return t3d.format;
                }
                return null;
            }

            public void SetTextureMaxSize(int v)
            {
                if (GetTextureMaxSize() != v)
                {
                    changedMaxSize = v;
                }
            }

            public void SetTextureCompression(TextureImporterCompression v)
            {
                if (IsFormatAutomatic() && GetTextureCompression() != v)
                {
                    changedTextureCompression = v;
                }
            }

            public void SetCrunchCompression(bool v)
            {
                if (IsCrunchCompression() != v)
                {
                    changedCrunchCompression = v;
                }
            }

            public void SetCrunchQuality(int v)
            {
                v = Math.Max(0, Math.Min(100, v));
                if (GetCrunchQuality() != v)
                {
                    changedCrunchQuality = v;
                }
            }

            public void SetGenerateMipmaps(bool v)
            {
                if (GetGenerateMipmaps() != v)
                {
                    changedGenerateMipmaps = v;
                }
            }

            public void SetStreamingMipmaps(bool v)
            {
                if (GetStreamingMipmaps() != v)
                {
                    changedStreamingMipmaps = v;
                }
            }


            private static bool isCrunchSupported(TextureImporterFormat? format)
            {
                switch (format)
                {
                    case TextureImporterFormat.DXT1:
                    case TextureImporterFormat.DXT1Crunched:
                    case TextureImporterFormat.DXT5:
                    case TextureImporterFormat.DXT5Crunched:
                    case TextureImporterFormat.ETC2_RGBA8:
                    case TextureImporterFormat.ETC2_RGBA8Crunched:
                    case TextureImporterFormat.ETC_RGB4:
                    case TextureImporterFormat.ETC_RGB4Crunched:
                        return true;

                    default:
                        return false;
                }
            }

            private static bool isCrunchSupported(TextureFormat? format)
            {
                switch (format)
                {
                    case TextureFormat.DXT1:
                    case TextureFormat.DXT1Crunched:
                    case TextureFormat.DXT5:
                    case TextureFormat.DXT5Crunched:
                    case TextureFormat.ETC2_RGBA8:
                    case TextureFormat.ETC2_RGBA8Crunched:
                    case TextureFormat.ETC_RGB4:
                    case TextureFormat.ETC_RGB4Crunched:
                        return true;

                    default:
                        return false;
                }
            }

            private static double? getTextureBitsPerPixel(TextureImporterFormat? format)
            {
                // https://docs.unity3d.com/Manual/class-TextureImporterOverride.html
                switch (format)
                {
                    // DXT
                    case TextureImporterFormat.DXT1:
                    case TextureImporterFormat.DXT1Crunched:
                        return 4;
                    case TextureImporterFormat.DXT5:
                    case TextureImporterFormat.DXT5Crunched:
                        return 8;

                    // BC
                    case TextureImporterFormat.BC4:
                        return 4;
                    case TextureImporterFormat.BC5:
                    case TextureImporterFormat.BC6H:
                    case TextureImporterFormat.BC7:
                        return 8;

                    // ETC
                    case TextureImporterFormat.ETC_RGB4:
                    case TextureImporterFormat.ETC_RGB4Crunched:
                    case TextureImporterFormat.ETC2_RGB4:
                        return 4;
                    case TextureImporterFormat.ETC2_RGBA8:
                    case TextureImporterFormat.ETC2_RGBA8Crunched:
                        return 8;

                    // ASTC
                    case TextureImporterFormat.ASTC_4x4:
                    case TextureImporterFormat.ASTC_HDR_4x4:
                        return 8;
                    case TextureImporterFormat.ASTC_5x5:
                    case TextureImporterFormat.ASTC_HDR_5x5:
                        return 5.12;
                    case TextureImporterFormat.ASTC_6x6:
                    case TextureImporterFormat.ASTC_HDR_6x6:
                        return 3.56;
                    case TextureImporterFormat.ASTC_8x8:
                    case TextureImporterFormat.ASTC_HDR_8x8:
                        return 2;
                    case TextureImporterFormat.ASTC_10x10:
                    case TextureImporterFormat.ASTC_HDR_10x10:
                        return 1.28;
                    case TextureImporterFormat.ASTC_12x12:
                    case TextureImporterFormat.ASTC_HDR_12x12:
                        return 0.89;

#if !UNITY_2020_1_OR_NEWER
                    case TextureImporterFormat.ASTC_RGBA_4x4:
                        return 8;
                    case TextureImporterFormat.ASTC_RGBA_5x5:
                        return 5.12;
                    case TextureImporterFormat.ASTC_RGBA_6x6:
                        return 3.56;
                    case TextureImporterFormat.ASTC_RGBA_8x8:
                        return 2;
                    case TextureImporterFormat.ASTC_RGBA_10x10:
                        return 1.28;
                    case TextureImporterFormat.ASTC_RGBA_12x12:
                        return 0.89;
#endif

                    case TextureImporterFormat.RGB16:
                        return 16;
                    case TextureImporterFormat.RGB24:
                        return 32;
                    case TextureImporterFormat.RGB48:
                        return 64;
                    case TextureImporterFormat.RGBA16:
                        return 16;
                    case TextureImporterFormat.RGBA32:
                        return 32;
                    case TextureImporterFormat.RGBA64:
                    case TextureImporterFormat.RGBAHalf:
                        return 64;

                    case TextureImporterFormat.Alpha8:
                        return 8;

                    default:
                        return null;
                }
            }

            private static double? getTextureBitsPerPixel(TextureFormat? format)
            {
                switch (format)
                {
                    // DXT
                    case TextureFormat.DXT1:
                    case TextureFormat.DXT1Crunched:
                        return 4;
                    case TextureFormat.DXT5:
                    case TextureFormat.DXT5Crunched:
                        return 8;

                    // BC
                    case TextureFormat.BC4:
                        return 4;
                    case TextureFormat.BC5:
                    case TextureFormat.BC6H:
                    case TextureFormat.BC7:
                        return 8;

                    // ETC
                    case TextureFormat.ETC_RGB4:
                    case TextureFormat.ETC_RGB4Crunched:
                        return 4;
                    case TextureFormat.ETC2_RGBA8:
                    case TextureFormat.ETC2_RGBA8Crunched:
                        return 8;

                    // ASTC
                    case TextureFormat.ASTC_4x4:
                    case TextureFormat.ASTC_HDR_4x4:
                        return 8;
                    case TextureFormat.ASTC_5x5:
                    case TextureFormat.ASTC_HDR_5x5:
                        return 5.12;
                    case TextureFormat.ASTC_6x6:
                    case TextureFormat.ASTC_HDR_6x6:
                        return 3.56;
                    case TextureFormat.ASTC_8x8:
                    case TextureFormat.ASTC_HDR_8x8:
                        return 2;
                    case TextureFormat.ASTC_10x10:
                    case TextureFormat.ASTC_HDR_10x10:
                        return 1.28;
                    case TextureFormat.ASTC_12x12:
                    case TextureFormat.ASTC_HDR_12x12:
                        return 0.89;

#if !UNITY_2020_1_OR_NEWER
                    case TextureFormat.ASTC_RGBA_4x4:
                        return 8;
                    case TextureFormat.ASTC_RGBA_5x5:
                        return 5.12;
                    case TextureFormat.ASTC_RGBA_6x6:
                        return 3.56;
                    case TextureFormat.ASTC_RGBA_8x8:
                        return 2;
                    case TextureFormat.ASTC_RGBA_10x10:
                        return 1.28;
                    case TextureFormat.ASTC_RGBA_12x12:
                        return 0.89;
#endif

                    case TextureFormat.RGB24:
                        return 32;
                    case TextureFormat.RGB48:
                        return 64;
                    case TextureFormat.RGBA32:
                        return 32;
                    case TextureFormat.RGBA64:
                    case TextureFormat.RGBAHalf:
                        return 64;

                    case TextureFormat.Alpha8:
                        return 8;

                    default:
                        return null;
                }
            }

            private static double? getTextureBitsPerPixel(RenderTextureFormat? format)
            {
                switch (format)
                {
                    case RenderTextureFormat.ARGB4444:
                    case RenderTextureFormat.ARGB1555:
                        return 16;
                    case RenderTextureFormat.ARGB32:
                    case RenderTextureFormat.ARGB2101010:
                        return 32;
                    case RenderTextureFormat.ARGB64:
                    case RenderTextureFormat.ARGBHalf:
                        return 64;
                    case RenderTextureFormat.ARGBFloat:
                    case RenderTextureFormat.ARGBInt:
                        return 128;

                    case RenderTextureFormat.BGRA32:
                        return 32;

                    case RenderTextureFormat.R8:
                        return 8;
                    case RenderTextureFormat.R16:
                    case RenderTextureFormat.RHalf:
                        return 16;
                    case RenderTextureFormat.RFloat:
                    case RenderTextureFormat.RInt:
                        return 32;

                    case RenderTextureFormat.RG16:
                        return 16;
                    case RenderTextureFormat.RG32:
                    case RenderTextureFormat.RGHalf:
                        return 32;
                    case RenderTextureFormat.RGFloat:
                    case RenderTextureFormat.RGInt:
                        return 64;

                    case RenderTextureFormat.RGB565:
                        return 16;
                    case RenderTextureFormat.RGB111110Float:
                        return 32;

                    case RenderTextureFormat.RGBAUShort:
                        return 64;

                    case RenderTextureFormat.Default:
                        return 32; // ARGB32
                    case RenderTextureFormat.DefaultHDR:
                        return 64; // ARGBHalf
                    case RenderTextureFormat.Depth:
                        if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android)
                            return 24; // OpenGL
                        return 32; // D3D9

                    default:
                        return null;
                }
            }
        }

        internal class TextureListView : TreeView
        {
            public TxTreeViewItem[] items = new TxTreeViewItem[0];
            private readonly float originalRowHeight;
            private float previewSize;

            public TextureListView(TreeViewState state) : base(state, NewHeader())
            {
                multiColumnHeader.sortingChanged += OnSortingChanged;
                originalRowHeight = rowHeight;
                SetTexturePreviewSize(16);
            }

            private static MultiColumnHeader NewHeader()
            {
                var column = new List<MultiColumnHeaderState.Column>();
                column.Add(new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Texture", "Texture"),
                    width = 240,
                });
                column.Add(new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Type", "Type"),
                    width = 120,
                });
                column.Add(new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Width", "Width"),
                    width = 50,
                });
                column.Add(new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Height", "Height"),
                    width = 50,
                });
                column.Add(new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("MipMap Count", "MipMap Count"),
                    width = 50,
                });
                column.Add(new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("VRAM", "VRAM"),
                    width = 90,
                });
                column.Add(new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Max Size", "Max Size"),
                    width = 90,
                });
                column.Add(new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Compression", "Compression"),
                    width = 90,
                });
                column.Add(new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Use Crunch Compression", "Use Crunch Compression"),
                    width = 50,
                });
                column.Add(new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Compressor Quality", "Compressor Quality"),
                    width = 50,
                });
                column.Add(new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Format", "Format"),
                    width = 120,
                });
                column.Add(new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Generate Mipmaps", "Generate Mipmaps"),
                    width = 50,
                });
                column.Add(new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Streaming Mipmaps", "Streaming Mipmaps"),
                    width = 50,
                });

                var state = new MultiColumnHeaderState(column.ToArray());
                var header = new MultiColumnHeader(state);
                header.SetSorting(COL_VRAMSize, false);

                return header;
            }

            protected override TreeViewItem BuildRoot()
            {
                var id = 0;
                var root = new TreeViewItem { id = id++, depth = -1, displayName = "Root" };

                var list = new List<TreeViewItem>();
                foreach (var item in items)
                {
                    list.Add(new ExTreeViewItem { id = id++, depth = 0, displayName = item.texture.name, info = item });
                }

                SetupParentsAndChildrenFromDepths(root, list);
                return root;
            }

            protected override void SelectionChanged(IList<int> selectedIds)
            {
                Selection.objects = getSelectedTextures();
            }

            private ExTreeViewItem[] getSelectedTreeViewItem(ExTreeViewItem current = null)
            {
                // TreeView から今選択されている ExTreeViewItem を取得
                var items = GetSelection().Select(id => FindItem(id, rootItem) as ExTreeViewItem)
                    .Where(item => item != null)
                    .ToArray();

                // もし current が null ではなく、Selection に入っていないならば、かわりに current のみを対象にする
                if (current != null && !items.Contains(current))
                {
                    return new ExTreeViewItem[] { current };
                }
                return items;
            }

            protected Texture[] getSelectedTextures(ExTreeViewItem current = null)
            {
                return getSelectedTreeViewItem(current)
                    .Where(item => item.info.texture != null)
                    .Select(item => item.info.texture)
                    .ToArray();
            }

            protected Material[] getTextureUsingMaterials(ExTreeViewItem current = null)
            {
                if (currentWindow == null || currentWindow.rootObject == null)
                {
                    return new Material[0];
                }
                var rootObject = currentWindow.rootObject;
                var textures = getSelectedTextures(current);
                return new MaterialSeeker().GetAllMaterials(rootObject)
                    .Where(mat => GetAllTextures(mat).Any(textures.Contains)).ToArray();
            }

            protected override void ContextClickedItem(int id)
            {
                var ev = Event.current;
                ev.Use();

                var menu = new GenericMenu();
                menu.AddItem(new GUIContent("Find Reference in Materials"), false, () =>
                {
                    var select = getTextureUsingMaterials();
                    if (select.Length == 0)
                    {
                        return;
                    }
                    Selection.objects = select;
                });
                menu.ShowAsContext();
            }

            private static readonly Color MARK_MODIFIED = Color.yellow;

            protected override void RowGUI(RowGUIArgs args)
            {
                var item = (ExTreeViewItem)args.item;
                var info = item.info;

                for (int i = 0; i < args.GetNumVisibleColumns(); i++)
                {
                    if (info.texture == null)
                    {
                        continue; // 途中でobjectがdestroyされた場合は何もしない
                    }

                    var cellRect = args.GetCellRect(i);
                    var texRect = cellRect;
                    CenterRectUsingSingleLineHeight(ref cellRect);

                    int idx = args.GetColumn(i);
                    switch (idx)
                    {
                        case COL_TextureName:
                            var labelRect = cellRect;
                            if (0 < previewSize)
                            {
                                texRect.width = Mathf.Min(texRect.width, previewSize);
                                texRect.height = Mathf.Min(texRect.height, previewSize);
                                DrawPreviewTexture(texRect, info.texture);
                                labelRect.x += previewSize + 2;
                                labelRect.width -= previewSize - 2;
                            }
                            GUI.Label(labelRect, "" + info.GetValue(idx));
                            break;

                        case COL_MaxSize:
                            var siz = info.GetTextureMaxSize();
                            if (siz != null)
                            {
                                DrawCellGUI(item,
                                    () =>
                                    {
                                        using (new ModifiedMarkScope(info.IsDirtyValue(idx), MARK_MODIFIED))
                                        {
                                            return DrawMaxSizeProperty(cellRect, (int)siz);
                                        }
                                    },
                                    (t, v) => t.SetTextureMaxSize(v));
                            }
                            break;

                        case COL_Compression:
                            var cmp = info.GetTextureCompression();
                            if (cmp != null)
                            {
                                DrawCellGUI(item,
                                    () =>
                                    {
                                        using (new ModifiedMarkScope(info.IsDirtyValue(idx), MARK_MODIFIED))
                                        {
                                            return DrawCompressQualityProperty(cellRect, cmp);
                                        }
                                    },
                                    (t, v) => t.SetTextureCompression(v));
                            }
                            break;

                        case COL_CrunchCompression:
                            var ch = info.IsCrunchCompression();
                            if (ch != null)
                            {
                                DrawCellGUI(item,
                                    () =>
                                    {
                                        using (new ModifiedMarkScope(info.IsDirtyValue(idx), MARK_MODIFIED))
                                        {
                                            return EditorGUI.ToggleLeft(cellRect, "", (bool)ch);
                                        }
                                    },
                                    (t, v) => t.SetCrunchCompression(v));
                            }
                            break;

                        case COL_CrunchQuality:
                            var cq = info.GetCrunchQuality();
                            if (cq != null)
                            {
                                DrawCellGUI(item,
                                    () =>
                                    {
                                        using (new ModifiedMarkScope(info.IsDirtyValue(idx), MARK_MODIFIED))
                                        {
                                            return EditorGUI.DelayedIntField(cellRect, (int)cq);
                                        }
                                    },
                                    (t, v) => t.SetCrunchQuality(v));
                            }
                            break;

                        case COL_GenerateMipmap:
                            var gm = info.GetGenerateMipmaps();
                            if (gm != null)
                            {
                                DrawCellGUI(item,
                                    () =>
                                    {
                                        using (new ModifiedMarkScope(info.IsDirtyValue(idx), MARK_MODIFIED))
                                        {
                                            return EditorGUI.ToggleLeft(cellRect, "", (bool)gm);
                                        }
                                    },
                                    (t, v) => t.SetGenerateMipmaps(v));
                            }
                            break;

                        case COL_StreamingMipmap:
                            var sm = info.GetStreamingMipmaps();
                            if (sm != null)
                            {
                                DrawCellGUI(item,
                                    () =>
                                    {
                                        using (new ModifiedMarkScope(info.IsDirtyValue(idx), MARK_MODIFIED))
                                        {
                                            return EditorGUI.ToggleLeft(cellRect, "", (bool)sm);
                                        }
                                    },
                                    (t, v) => t.SetStreamingMipmaps(v));
                            }
                            break;

                        case COL_VRAMSize:
                            var size = (long)info.GetValue(COL_VRAMSize);
                            GUI.Label(cellRect, ToPrettyString((long)info.GetValue(idx)));
                            break;

                        default:
                            GUI.Label(cellRect, "" + info.GetValue(idx));
                            break;
                    }
                }
            }

            private static void DrawPreviewTexture(Rect rect, Texture texture)
            {
                if (texture is Texture2D)
                {
                    EditorGUI.DrawPreviewTexture(rect, texture);
                }
                else
                {
                    var tex2d = AssetPreview.GetAssetPreview(texture);
                    if (tex2d != null)
                    {
                        EditorGUI.DrawPreviewTexture(rect, tex2d);
                    }
                    else
                    {
                        EditorGUI.DrawPreviewTexture(rect, texture);
                    }
                }
            }

            private static TextureImporterCompression DrawCompressQualityProperty(Rect cellRect, TextureImporterCompression? cmp)
            {
                var label = new string[] { "None", "Low Quality", "Normal Quality", "High Quality" };
                int value;
                switch (cmp)
                {
                    default:
                    case TextureImporterCompression.Uncompressed:
                        value = 0;
                        break;
                    case TextureImporterCompression.CompressedLQ:
                        value = 1;
                        break;
                    case TextureImporterCompression.Compressed:
                        value = 2;
                        break;
                    case TextureImporterCompression.CompressedHQ:
                        value = 3;
                        break;
                }
                value = EditorGUI.Popup(cellRect, "", value, label);
                switch (value)
                {
                    default:
                    case 0:
                        return TextureImporterCompression.Uncompressed;
                    case 1:
                        return TextureImporterCompression.CompressedLQ;
                    case 2:
                        return TextureImporterCompression.Compressed;
                    case 3:
                        return TextureImporterCompression.CompressedHQ;
                }
            }

            static readonly int[] MAX_SIZE_VALUE = { 32, 64, 128, 256, 512, 1024, 2048, 4096, 8192 };
            static readonly string[] MAX_SIZE_TEXT = MAX_SIZE_VALUE.Select(v => v.ToString()).ToArray();

            private int DrawMaxSizeProperty(Rect rect, int size)
            {
                int index = 0;
                for (; MAX_SIZE_VALUE[index] < size; index++) ;
                index = EditorGUI.Popup(rect, index, MAX_SIZE_TEXT);
                return MAX_SIZE_VALUE[index];
            }

            private class ModifiedMarkScope : GUI.Scope
            {
                public readonly bool isDirty;
                private readonly Color oldColor;

                public ModifiedMarkScope(bool isDirty, Color mark)
                {
                    this.isDirty = isDirty;
                    this.oldColor = GUI.color;
                    if (isDirty)
                    {
                        GUI.color = mark;
                    }
                }

                protected override void CloseScope()
                {
                    if (isDirty)
                    {
                        GUI.color = oldColor;
                    }
                }
            }

            private void DrawCellGUI<V>(ExTreeViewItem current, Func<V> gui, Action<TxTreeViewItem, V> setter)
            {
                EditorGUI.BeginChangeCheck();

                // GUI表示
                var ret = gui();

                // 変更されているなら
                if (EditorGUI.EndChangeCheck())
                {
                    var targets = getSelectedTreeViewItem(current);

                    // 変更
                    foreach (var t in targets)
                    {
                        setter(t.info, ret);
                    }
                }
            }

            public void SortItems()
            {
                var idx = multiColumnHeader.sortedColumnIndex;
                if (idx < 0)
                {
                    return;
                }
                items = (multiColumnHeader.IsSortedAscending(idx) ?
                    items.OrderBy(mci => mci.GetValue(idx)) :
                    items.OrderByDescending(mci => mci.GetValue(idx))).ToArray();
            }

            void OnSortingChanged(MultiColumnHeader header)
            {
                SortItems();
                Reload();
            }

            internal void SetTexturePreviewSize(float size)
            {
                previewSize = Math.Max(0, size);
                rowHeight = Math.Max(previewSize, originalRowHeight);
            }

            internal class ExTreeViewItem : TreeViewItem
            {
                public TxTreeViewItem info;
            }
        }
    }
}

#endif
