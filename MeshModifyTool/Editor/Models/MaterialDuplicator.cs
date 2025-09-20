using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace vrc_yue.MeshUVCutTools.MeshModifyTool.Models
{
    /// <summary>
    /// マテリアルごとのテクスチャ情報
    /// </summary>
    public class MaterialTextureInfo
    {
        public Material Material { get; }
        public Dictionary<string, Texture> TexturesByProperty { get; }

        public MaterialTextureInfo(Material material, Dictionary<string, Texture> texturesByProperty)
        {
            Material = material;
            TexturesByProperty = texturesByProperty;
        }
    }

    /// <summary>
    /// マテリアルの複製と管理を担当するクラス
    /// </summary>
    public class MaterialDuplicator
    {
        /// <summary>
        /// マテリアルを複製して新しい名前を付け、アセットとして保存
        /// </summary>
        /// <param name="source">元のマテリアル</param>
        /// <param name="newName">新しいマテリアルの名前（拡張子なし）</param>
        /// <returns>複製されたマテリアル</returns>
        private TemporaryAssetManager assetManager;

        public MaterialDuplicator(TemporaryAssetManager manager)
        {
            assetManager = manager;
        }

        public Material DuplicateMaterial(Material source)
        {
            if (source == null)
                return null;

            // マテリアルを複製
            Material newMaterial = new Material(source);
            newMaterial.name = EditorUtils.EnsureSuffix(source.name);

            // 元のマテリアルのディレクトリを取得
            string sourceDirectory = Path.GetDirectoryName(AssetDatabase.GetAssetPath(source));

            // 一時アセットとして管理
            assetManager.AddTemporaryMaterial(newMaterial, null, sourceDirectory);

            return newMaterial;
        }
        
        /// <summary>
        /// 指定されたGameObjectのRendererにマテリアルを適用
        /// </summary>
        /// <param name="target">対象のGameObject</param>
        /// <param name="material">適用するマテリアル</param>
        public void ApplyMaterialToMesh(GameObject obj, Material material)
        {
            var meshRenderer = obj.GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                meshRenderer.sharedMaterial = material;
            }

            var skinnedMeshRenderer = obj.GetComponent<SkinnedMeshRenderer>();
            if (skinnedMeshRenderer != null)
            {
                skinnedMeshRenderer.sharedMaterial = material;
            }
        }

        /// <summary>
        /// 処理済みテクスチャをマテリアルに適用
        /// </summary>
        /// <param name="materialTextureInfos">マテリアルごとのテクスチャ情報</param>
        /// <param name="processedTextures">処理前のテクスチャから処理済みテクスチャへのマッピング</param>
        public void ApplyProcessedTextures(List<MaterialTextureInfo> materialTextureInfos, Dictionary<Texture, Texture> processedTextures)
        {
            if (materialTextureInfos == null || processedTextures == null) return;

            foreach (var info in materialTextureInfos)
            {
                if (info.Material == null) continue;

                foreach (var kvp in info.TexturesByProperty)
                {
                    if (processedTextures.TryGetValue(kvp.Value, out var processedTexture))
                    {
                        info.Material.SetTexture(kvp.Key, processedTexture);
                        EditorUtility.SetDirty(info.Material);
                    }
                }
            }
        }

        /// <summary>
        /// マテリアル配列から参照されているすべてのテクスチャとそのプロパティ名を取得
        /// </summary>
        /// <param name="materials">対象のマテリアル配列</param>
        /// <returns>テクスチャとプロパティ名の配列のタプル</returns>
        public (Texture[] textures, string[] propertyNames) GetAllTextures(Material[] materials)
        {
            if (materials == null || materials.Length == 0)
            {
                return (Array.Empty<Texture>(), Array.Empty<string>());
            }

            var textureList = new List<Texture>();
            var propertyNameList = new List<string>();

            foreach (var mat in materials.Where(m => m != null))
            {
                var propertyNames = mat.GetTexturePropertyNames();
                foreach (var propertyName in propertyNames)
                {
                    var texture = mat.GetTexture(propertyName);
                    if (texture != null)
                    {
                        textureList.Add(texture);
                        propertyNameList.Add(propertyName);
                    }
                }
            }

            return (textureList.ToArray(), propertyNameList.ToArray());
        }

        /// <summary>
        /// マテリアル配列から各マテリアルのテクスチャ情報を取得
        /// </summary>
        /// <param name="materials">対象のマテリアル配列</param>
        /// <returns>マテリアルごとのテクスチャ情報のリスト</returns>
        public List<MaterialTextureInfo> GetMaterialTextureInfos(Material[] materials)
        {
            if (materials == null || materials.Length == 0)
            {
                return new List<MaterialTextureInfo>();
            }

            var result = new List<MaterialTextureInfo>();

            foreach (var mat in materials.Where(m => m != null))
            {
                var texturesByProperty = new Dictionary<string, Texture>();
                var propertyNames = mat.GetTexturePropertyNames();

                foreach (var propertyName in propertyNames)
                {
                    var texture = mat.GetTexture(propertyName);
                    if (texture != null)
                    {
                        texturesByProperty[propertyName] = texture;
                    }
                }

                if (texturesByProperty.Any())
                {
                    result.Add(new MaterialTextureInfo(mat, texturesByProperty));
                }
            }

            return result;
        }
    }
}
