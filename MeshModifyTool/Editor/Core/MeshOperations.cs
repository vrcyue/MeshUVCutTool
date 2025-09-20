using vrc_yue.MeshUVCutTools.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using vrc_yue.MeshUVCutTools.MeshModifyTool.Models;

namespace vrc_yue.MeshUVCutTools.MeshModifyTool.Core
{
    /// <summary>
    /// メッシュ操作（作成、UV編集、変形など）を管理するクラス
    /// </summary>
    public class MeshOperations
    {
        private TemporaryAssetManager assetManager;
        private TextureEditor textureEditor;
        private MaterialDuplicator materialDuplicator;
        
        // 設定
        private float normalOffset = 0.0f;
        
        public MeshOperations(TemporaryAssetManager assetManager, TextureEditor textureEditor, 
            MaterialDuplicator materialDuplicator)
        {
            this.assetManager = assetManager;
            this.textureEditor = textureEditor;
            this.materialDuplicator = materialDuplicator;
        }
        
        /// <summary>
        /// ノーマルオフセット値
        /// </summary>
        public float NormalOffset
        {
            get => normalOffset;
            set => normalOffset = value;
        }
        
        /// <summary>
        /// 選択された頂点から新しいメッシュを作成
        /// </summary>
        public GameObject CreateMeshFromSelection(MeshCreater sourceMeshCreater, List<int> selectedVertices, 
            GameObject avatar, string suffix, MeshCreationOptions options = null)
        {
            if (sourceMeshCreater == null || selectedVertices.Count == 0) return null;
            
            if (options == null)
            {
                options = new MeshCreationOptions();
            }
            
            // 選択された頂点からメッシュを作成
            var newMeshCreater = sourceMeshCreater.CreateFromSelectedVertices(selectedVertices, options);
            
            // 頂点をノーマル方向にオフセット
            if (Math.Abs(normalOffset) > 0.0001f)
            {
                var vertices = new Vector3[newMeshCreater.VertexsCount()];
                for (int i = 0; i < newMeshCreater.VertexsCount(); i++)
                {
                    var normal = newMeshCreater.GetNormal(i);
                    var vertex = newMeshCreater.GetPosition(i);
                    vertices[i] = vertex + normal * normalOffset;
                }
                newMeshCreater.TransformMesh(vertices);
            }
            
            // GameObjectを作成
            var originalMeshName = sourceMeshCreater.RendBone.GetComponent<SkinnedMeshRenderer>().sharedMesh.name;
            GameObject newObj;
            GameObject parentObj = null;
            
            // copyUsedBonesOnlyの場合は、親となる空オブジェクトを作成
            if (options.CopyUsedBonesOnly && !options.RemoveBoneWeights)
            {
                parentObj = new GameObject($"{originalMeshName}_Container_Temp");
                parentObj.transform.SetParent(avatar.transform);
                parentObj.transform.localPosition = Vector3.zero;
                parentObj.transform.localRotation = Quaternion.identity;
                parentObj.transform.localScale = Vector3.one;
            }
            
            if (options.RemoveBoneWeights)
            {
                // ボーンウェイトなしの場合はMeshRendererとして作成
                newObj = newMeshCreater.ToMesh($"{originalMeshName}{suffix}", parentObj != null ? parentObj.transform : avatar.transform);
            }
            else
            {
                // 通常はSkinnedMeshRendererとして作成
                newObj = newMeshCreater.ToSkinMesh($"{originalMeshName}{suffix}", parentObj != null ? parentObj.transform : avatar.transform);
            }
            
            newObj.transform.localPosition = Vector3.zero;
            newObj.transform.localRotation = Quaternion.identity;
            newObj.transform.localScale = Vector3.one;
            
            // メッシュを一時アセットとして管理
            if (options.RemoveBoneWeights)
            {
                // MeshRendererの場合
                var meshFilter = newObj.GetComponent<MeshFilter>();
                var meshRenderer = newObj.GetComponent<MeshRenderer>();
                if (meshFilter != null && meshFilter.sharedMesh != null)
                {
                    var tempMesh = UnityEngine.Object.Instantiate(meshFilter.sharedMesh);
                    tempMesh.name = EditorUtils.EnsureSuffix(newObj.name);
                    meshFilter.sharedMesh = tempMesh;
                    
                    // 元のメッシュのパスを取得
                    string sourceDirectory = Path.GetDirectoryName(
                        AssetDatabase.GetAssetPath(sourceMeshCreater.RendBone.GetComponent<SkinnedMeshRenderer>()
                            .sharedMesh));
                    // parentObjがある場合はそれを登録、なければnewObjを登録
                    assetManager.AddTemporaryMesh(tempMesh, parentObj != null ? parentObj : newObj, sourceDirectory);
                }
                
                // マテリアルを複製して適用
                if (meshRenderer != null && meshRenderer.sharedMaterials.Length > 0)
                {
                    var newMaterial = materialDuplicator.DuplicateMaterial(meshRenderer.sharedMaterials[0]);
                    materialDuplicator.ApplyMaterialToMesh(newObj, newMaterial);
                }
            }
            else
            {
                // SkinnedMeshRendererの場合
                var renderer = newObj.GetComponent<SkinnedMeshRenderer>();
                if (renderer != null && renderer.sharedMesh != null)
                {
                    var tempMesh = UnityEngine.Object.Instantiate(renderer.sharedMesh);
                    tempMesh.name = EditorUtils.EnsureSuffix(newObj.name);
                    renderer.sharedMesh = tempMesh;
                    
                    // 元のメッシュのパスを取得
                    string sourceDirectory = Path.GetDirectoryName(
                        AssetDatabase.GetAssetPath(sourceMeshCreater.RendBone.GetComponent<SkinnedMeshRenderer>()
                            .sharedMesh));
                    // parentObjがある場合はそれを登録、なければnewObjを登録
                    assetManager.AddTemporaryMesh(tempMesh, parentObj != null ? parentObj : newObj, sourceDirectory);
                }
                
                // BlendShapeの値をコピー
                CopyBlendShapeWeights(sourceMeshCreater.RendBone.GetComponent<SkinnedMeshRenderer>(), renderer);
                
                // マテリアルを複製して適用
                var newMaterial = materialDuplicator.DuplicateMaterial(renderer.sharedMaterial);
                materialDuplicator.ApplyMaterialToMesh(newObj, newMaterial);
            }
            
            // parentObjがある場合はそれを返す（Container）、そうでなければnewObjを返す
            return parentObj != null ? parentObj : newObj;
        }
        
        /// <summary>
        /// UV編集操作（フィット、再構築）
        /// </summary>
        public void UpdateMeshUV(MeshCreater meshCreater, bool refit = false, bool reconstruct = false)
        {
            if (meshCreater == null) return;
            
            // SkinnedMeshRendererまたはMeshRendererを取得
            var renderer = meshCreater.RendBone.GetComponent<Renderer>();
            if (renderer == null) return;
            
            // 全頂点のUV範囲を計算
            var vertexIndices = Enumerable.Range(0, meshCreater.VertexsCount());
            var (minUV, maxUV) = CalculateUVRange(meshCreater, vertexIndices);
            
            Mesh meshForTexConvert = null;
            
            if (refit)
            {
                // UVを0-1の範囲に再マッピング
                ApplyUVRefit(meshCreater, minUV, maxUV);
            }
            else if (reconstruct)
            {
                meshForTexConvert = meshCreater.GenerateUVWithoutDuplication();
            }
            
            // テクスチャの処理
            ProcessTextures(renderer, meshCreater, refit, reconstruct, minUV, maxUV, meshForTexConvert);
            
            // メッシュを更新
            renderer.SetMesh(meshCreater.Create(false));
        }
        
        /// <summary>
        /// UV変換操作（反転、回転）
        /// </summary>
        public void TransformUV(MeshCreater meshCreater, TransformType type)
        {
            if (meshCreater == null) return;
            
            switch (type)
            {
                case TransformType.FlipHorizontal:
                    meshCreater.FlipUV(true, false);
                    break;
                case TransformType.FlipVertical:
                    meshCreater.FlipUV(false, true);
                    break;
                case TransformType.Rotate90:
                    meshCreater.RotateUV(90);
                    break;
                case TransformType.RotateMinus90:
                    meshCreater.RotateUV(-90);
                    break;
            }
            
            UpdateMeshUV(meshCreater);
        }
        
        /// <summary>
        /// UV変換タイプ
        /// </summary>
        public enum TransformType
        {
            FlipHorizontal,
            FlipVertical,
            Rotate90,
            RotateMinus90
        }
        
        /// <summary>
        /// UV範囲を計算
        /// </summary>
        public (Vector2 min, Vector2 max) CalculateUVRange(MeshCreater meshCreater, IEnumerable<int> vertexIndices)
        {
            Vector2 minUV = Vector2.one * float.MaxValue;
            Vector2 maxUV = Vector2.one * float.MinValue;
            
            foreach (var vertexIndex in vertexIndices)
            {
                var uvs = meshCreater.GetUVs(vertexIndex);
                if (uvs != null && uvs.Length > 0)
                {
                    var uv = uvs[0]; // UV channel 0を使用
                    minUV.x = Mathf.Min(minUV.x, uv.x);
                    minUV.y = Mathf.Min(minUV.y, uv.y);
                    maxUV.x = Mathf.Max(maxUV.x, uv.x);
                    maxUV.y = Mathf.Max(maxUV.y, uv.y);
                }
            }
            
            return (minUV, maxUV);
        }
        
        /// <summary>
        /// BlendShapeの重みをコピー
        /// </summary>
        private void CopyBlendShapeWeights(SkinnedMeshRenderer source, SkinnedMeshRenderer destination)
        {
            if (source == null || destination == null) return;
            
            for (int i = 0; i < destination.sharedMesh.blendShapeCount; i++)
            {
                var blendShapeName = destination.sharedMesh.GetBlendShapeName(i);
                for (int j = 0; j < source.sharedMesh.blendShapeCount; j++)
                {
                    if (source.sharedMesh.GetBlendShapeName(j) == blendShapeName)
                    {
                        destination.SetBlendShapeWeight(i, source.GetBlendShapeWeight(j));
                        break;
                    }
                }
            }
        }
        
        /// <summary>
        /// UVリフィットを適用
        /// </summary>
        private void ApplyUVRefit(MeshCreater meshCreater, Vector2 minUV, Vector2 maxUV)
        {
            float uvWidth = maxUV.x - minUV.x;
            float uvHeight = maxUV.y - minUV.y;
            
            var uvScale = new Vector2(
                uvWidth > 0.0001f ? 1f / uvWidth : 1f,
                uvHeight > 0.0001f ? 1f / uvHeight : 1f
            );
            
            var uvCenter = new Vector2(
                (maxUV.x + minUV.x) * 0.5f,
                (maxUV.y + minUV.y) * 0.5f
            );
            
            var uvPosition = new Vector2(0.5f, 0.5f) - uvCenter * uvScale;
            
            meshCreater.TransformUV(uvScale, uvPosition);
        }
        
        /// <summary>
        /// テクスチャ処理
        /// </summary>
        private void ProcessTextures(Renderer renderer, MeshCreater meshCreater, 
            bool refit, bool reconstruct, Vector2 minUV, Vector2 maxUV, Mesh meshForTexConvert)
        {
            var materials = renderer.sharedMaterials;
            var materialTextureInfos = materialDuplicator.GetMaterialTextureInfos(materials);
            
            if (refit || reconstruct)
            {
                var uniqueTextures = materialTextureInfos
                    .SelectMany(info => info.TexturesByProperty.Values)
                    .Distinct()
                    .ToArray();
                
                Dictionary<Texture, Texture> processedTextures = new Dictionary<Texture, Texture>();
                
                if (refit)
                {
                    var processed = textureEditor.CropTexturesByUV(uniqueTextures, minUV, maxUV);
                    for (int i = 0; i < uniqueTextures.Length; i++)
                    {
                        processedTextures[uniqueTextures[i]] = processed[i];
                    }
                }
                else if (reconstruct && meshForTexConvert != null)
                {
                    var processed = textureEditor.BakeToUVs(uniqueTextures, meshForTexConvert);
                    for (int i = 0; i < uniqueTextures.Length; i++)
                    {
                        processedTextures[uniqueTextures[i]] = processed[i];
                    }
                }
                
                materialDuplicator.ApplyProcessedTextures(materialTextureInfos, processedTextures);
            }
        }
        
    }
}