/*
 *  The MIT License
 *
 *  Copyright 2020-2024 whiteflare.
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
using UnityEngine;
using System.Text.RegularExpressions;

namespace WF.Tool.Avatar.BU
{
    internal class BoundsRecalculator: ScriptableObject
    {
        public Bounds bounds;
        public List<SkinnedMeshRenderer> skinMeshRenderers;
        public List<MeshRenderer> meshRenderers;
        public Transform rootBone;
        public Transform anchorTarget;

        public BoundsRecalculator()
        {
            Clear();
        }

        public void Clear()
        {
            bounds = new Bounds(Vector3.zero, Vector3.one * 2);
            skinMeshRenderers = new List<SkinnedMeshRenderer>();
            meshRenderers = new List<MeshRenderer>();
            rootBone = null;
            anchorTarget = null;
        }

        public void SetAvatarRoot(GameObject rootObject)
        {
            Clear();

            if (rootObject == null)
            {
                return;
            }

            // AnchorTarget の検索
            var removeSpace = new Regex(@"\s+", RegexOptions.Compiled);
            foreach (var t in rootObject.GetComponentsInChildren<Transform>())
            {
                string name = removeSpace.Replace(t.name, "");
                if (string.Equals(name, "AnchorOverride", System.StringComparison.OrdinalIgnoreCase) || string.Equals(name, "AnchorTarget", System.StringComparison.OrdinalIgnoreCase))
                {
                    anchorTarget = t;
                    break;
                }
            }
            // Humanoid Transform の検索
            foreach (var anim in rootObject.GetComponentsInChildren<Animator>(true))
            {
                if (anim != null && anim.isHuman)
                {
                    rootBone = anim.GetBoneTransform(HumanBodyBones.Hips);
                    if (anchorTarget == null)
                    {
                        anchorTarget = anim.GetBoneTransform(HumanBodyBones.Chest);
                    }
                    break;
                }
            }
            if (rootBone == null)
            {
                rootBone = rootObject.transform;
            }
            if (anchorTarget == null)
            {
                anchorTarget = rootObject.transform;
            }

            skinMeshRenderers.AddRange(rootObject.GetComponentsInChildren<SkinnedMeshRenderer>(true));
            meshRenderers.AddRange(rootObject.GetComponentsInChildren<MeshRenderer>(true));
        }

        public void CalcBounds(BoundsCalcMode calcMode = BoundsCalcMode.SkinnedVertex)
        {
            if (rootBone == null)
            {
                return;
            }
            VertexCollector collector = new VertexCollector(rootBone);
            foreach (var r in skinMeshRenderers)
            {
                if (r == null)
                {
                    continue;
                }
                if (IsSkinMeshRendererWithoutBones(r))
                {
                    continue;
                }

                switch (calcMode)
                {
                    case BoundsCalcMode.CurrentValueOnly:
                        CalcCurrentValueOnly(collector, r);
                        break;
                    case BoundsCalcMode.PrefabValue:
                        CalcPrefabValue(collector, r);
                        break;
                    case BoundsCalcMode.BoneTransform:
                        CalcBoneTransform(collector, r);
                        break;
                    case BoundsCalcMode.SkinnedVertex:
                        CalcSkinnedVertex(collector, r);
                        break;
                }
            }
            if (collector.count == 0)
            {
                return;
            }

            // AABBの作成
            var rawBounds = CreateBounds(collector, rootBone);
            var center = rawBounds.center;
            var extents = rawBounds.extents;

            // centerの調整
            center.x = Mathf.RoundToInt(center.x * 100) / 100f;
            center.y = Mathf.RoundToInt(center.y * 100) / 100f;
            center.z = Mathf.RoundToInt(center.z * 100) / 100f;
            // extentsの拡張
            extents.x += Mathf.Abs(center.x - rawBounds.center.x);
            extents.y += Mathf.Abs(center.y - rawBounds.center.y);
            extents.z += Mathf.Abs(center.z - rawBounds.center.z);
            // extentsの調整
            extents.x = Mathf.RoundToInt(extents.x * 100) / 100f;
            extents.y = Mathf.RoundToInt(extents.y * 100) / 100f;
            extents.z = Mathf.RoundToInt(extents.z * 100) / 100f;

            // 作成したAABBをセット
            bounds = new Bounds(center, extents * 2);
        }

        public void ApplyBounds()
        {
            Undo.RecordObjects(skinMeshRenderers.Union<Component>(meshRenderers).Where(cmp => cmp != null).ToArray(), "set bounds");
            ApplyBoundsWithoutUndo();
        }

        public void ApplyBoundsWithoutUndo()
        {
            foreach (var r in skinMeshRenderers)
            {
                if (r == null)
                {
                    continue;
                }
                r.probeAnchor = anchorTarget;

                if (r.sharedMesh != null && IsSkinMeshRendererWithoutBones(r))
                {
                    // ボーンで動いていない場合はリセットできない
                    continue;
                }

                // ボーンで動いている SkinnedMeshRenderer は、トランスフォームをリセット可能
                var transform = r.transform;
                transform.localPosition = Vector3.zero;
                transform.localRotation = Quaternion.identity;
                transform.localScale = Vector3.one;

                r.rootBone = rootBone;
                r.localBounds = bounds;
            }

            foreach (var r in meshRenderers)
            {
                if (r == null)
                {
                    continue;
                }
                r.probeAnchor = anchorTarget;
            }
        }

        private IEnumerable<Vector3> IterWorldSpaceCorner(Bounds wb, Transform origBase, Transform newBase)
        {
            if (origBase == null)
            {
                origBase = newBase;
            }
            var scale = SafeDivide(newBase.lossyScale, origBase.lossyScale);
            return IterWorldSpaceCorner(wb)
                .Select(p => p + (newBase.position - origBase.position))
                .Select(p => LerpUnclamped(newBase.position, p, scale));
        }

        private static Vector3 SafeDivide(Vector3 x, Vector3 y)
        {
            return new Vector3(y.x == 0 ? 1 : x.x / y.x, y.y == 0 ? 1 : x.y / y.y, y.z == 0 ? 1 : x.z / y.z);
        }

        private static Vector3 LerpUnclamped(Vector3 b, Vector3 v, Vector3 scale)
        {
            return new Vector3(Mathf.LerpUnclamped(b.x, v.x, scale.x), Mathf.LerpUnclamped(b.y, v.y, scale.y), Mathf.LerpUnclamped(b.z, v.z, scale.z));
        }

        private IEnumerable<Vector3> IterWorldSpaceCorner(Bounds wb)
        {
            yield return new Vector3(wb.min.x, wb.min.y, wb.min.z);
            yield return new Vector3(wb.min.x, wb.min.y, wb.max.z);
            yield return new Vector3(wb.min.x, wb.max.y, wb.min.z);
            yield return new Vector3(wb.min.x, wb.max.y, wb.max.z);
            yield return new Vector3(wb.max.x, wb.min.y, wb.min.z);
            yield return new Vector3(wb.max.x, wb.min.y, wb.max.z);
            yield return new Vector3(wb.max.x, wb.max.y, wb.min.z);
            yield return new Vector3(wb.max.x, wb.max.y, wb.max.z);
        }

        private void CalcPrefabValue(VertexCollector result, SkinnedMeshRenderer r)
        {
            // Prefab側の bounds の頂点8箇所のワールド座標を追加
            if (r.bounds.extents != Vector3.zero)
            {
                var orig = PrefabUtility.GetCorrespondingObjectFromOriginalSource(r);
                if (orig != null)
                {
                    result.AddRange(IterWorldSpaceCorner(orig.bounds, orig.rootBone, rootBone));
                }
                else
                {
                    result.AddRange(IterWorldSpaceCorner(r.bounds));
                }
            }
        }

        private void CalcCurrentValueOnly(VertexCollector result, SkinnedMeshRenderer r)
        {
            // bounds の頂点8箇所のワールド座標を追加
            if (r.bounds.extents != Vector3.zero)
            {
                result.AddRange(IterWorldSpaceCorner(r.bounds));
            }
        }

        private void CalcBoneTransform(VertexCollector result, SkinnedMeshRenderer r)
        {
            // ボーンのワールド座標をすべて追加
            foreach (var t in r.bones)
            {
                var d = t.parent == null ? 0 : t.InverseTransformPoint(t.parent.position).magnitude;
                result.Add(t.position, d);
            }
        }

        private void CalcSkinnedVertex(VertexCollector result, SkinnedMeshRenderer r)
        {
            // スキニングされた頂点座標
            var go = new GameObject();
            var smr = go.AddComponent<SkinnedMeshRenderer>();
            EditorUtility.CopySerialized(r, smr);

            var mesh = new Mesh();
            smr.BakeMesh(mesh);
            foreach(var v in mesh.vertices)
            {
                result.Add(v, 0.1f);
            }

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(smr);
            Object.DestroyImmediate(mesh);
        }

        private static Bounds CreateBounds(VertexCollector ps, Transform rootBone)
        {
            var result = new Bounds();
            if (ps.count != 0)
            {
                result.SetMinMax(ps.min, ps.max);
            }
            return result;
        }

        private bool IsSkinMeshRendererWithoutBones(SkinnedMeshRenderer r)
        {
            return r.bones == null || r.bones.Length == 0;
        }

        class VertexCollector
        {
            public Vector3 min = Vector3.zero;
            public Vector3 max = Vector3.zero;
            public int count = 0;
            private readonly Transform rootBone;

            private readonly Vector3[] margin =
            {
                new Vector3(0, 0, -1),
                new Vector3(0, 0, +1),
                new Vector3(0, -1, 0),
                new Vector3(0, +1, 0),
                new Vector3(-1, 0, 0),
                new Vector3(+1, 0, 0),
            };

            public VertexCollector(Transform rootBone)
            {
                this.rootBone = rootBone;
            }

            public void AddLocal(Vector3 v)
            {
                if (count == 0)
                {
                    min = max = v;
                }
                else
                {
                    min = Vector3.Min(min, v);
                    max = Vector3.Max(max, v);
                }
                count++;
            }

            public void Add(Vector3 v, float d = 0)
            {
                v = rootBone.worldToLocalMatrix.MultiplyPoint(v);
                AddLocal(v);
                if (0 < d)
                {
                    foreach(var m in margin)
                    {
                        AddLocal(v + m * d);
                    }
                }
            }

            public void AddRange(IEnumerable<Vector3> e, float d = 0)
            {
                foreach (var v in e)
                {
                    Add(v, d);
                }
            }
        }
    }

    internal enum BoundsCalcMode
    {
        SkinnedVertex,
        PrefabValue,
        CurrentValueOnly,
        BoneTransform,
    }
}

#endif
