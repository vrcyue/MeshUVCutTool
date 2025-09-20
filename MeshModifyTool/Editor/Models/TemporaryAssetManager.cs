using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UObject = UnityEngine.Object;

namespace vrc_yue.MeshUVCutTools.MeshModifyTool.Models
{
    public class TemporaryAssetManager
    {
        private const string TEMP_DIRECTORY = "Assets/Temp";
        
        public class TemporaryAsset<T> where T : UnityEngine.Object
        {
            public T Asset { get; private set; }
            public string TemporaryPath { get; private set; }
            public bool IsSelected { get; set; }
            public GameObject RelatedGameObject { get; private set; }
            public string SourceDirectory { get; private set; }
            public List<UnityEngine.Object> Dependencies { get; set; } = new List<UnityEngine.Object>();

            public TemporaryAsset(T asset, GameObject gameObject, string tempPath, string sourceDirectory)
            {
                Asset = asset;
                RelatedGameObject = gameObject;
                TemporaryPath = tempPath;
                SourceDirectory = sourceDirectory;
                IsSelected = true;
            }

            /// <summary>
            /// アセットの内容を更新します
            /// </summary>
            /// <param name="newAsset">新しいアセットの内容</param>
            public virtual void UpdateAssetContent(T newAsset)
            {
                if (Asset == null || newAsset == null) return;
                Asset = newAsset;
            }

            public TemporaryAsset(T asset, GameObject gameObject, string tempPath = null)
            {
                Asset = asset;
                RelatedGameObject = gameObject;
                IsSelected = true; // デフォルトで選択状態
            }
        }

        private readonly List<TemporaryAsset<Mesh>> tempMeshes = new List<TemporaryAsset<Mesh>>();
        private readonly List<TemporaryAsset<Material>> tempMaterials = new List<TemporaryAsset<Material>>();
        private readonly List<TemporaryAsset<Texture2D>> tempTextures = new List<TemporaryAsset<Texture2D>>();

        public class TemporaryTextureAsset : TemporaryAsset<Texture2D>
        {
            public TemporaryTextureAsset(Texture2D asset, GameObject gameObject, string tempPath, string sourceDirectory)
                : base(asset, gameObject, tempPath, sourceDirectory) { }

            public override void UpdateAssetContent(Texture2D newAsset)
            {
                if (Asset == null || newAsset == null) return;
                Graphics.CopyTexture(newAsset, Asset);
            }
        }

        public IReadOnlyList<TemporaryAsset<Mesh>> TempMeshes => tempMeshes.AsReadOnly();
        public IReadOnlyList<TemporaryAsset<Material>> TempMaterials => tempMaterials.AsReadOnly();
        public IReadOnlyList<TemporaryAsset<Texture2D>> TempTextures => tempTextures.AsReadOnly();

        private static string GetTemporaryDirectory() => TEMP_DIRECTORY;

        public TemporaryAssetManager()
        {
            EnsureTemporaryDirectory();
        }

        private void EnsureTemporaryDirectory()
        {
            if (!Directory.Exists(TEMP_DIRECTORY))
            {
                Directory.CreateDirectory(TEMP_DIRECTORY);
            }
        }

        public void AddTemporaryMesh(Mesh mesh, GameObject gameObject, string sourceDirectory = null)
        {
            if (sourceDirectory == null && mesh != null)
            {
                string sourcePath = AssetDatabase.GetAssetPath(mesh);
                sourceDirectory = string.IsNullOrEmpty(sourcePath) ? "Assets/Meshes" : Path.GetDirectoryName(sourcePath);
            }

            tempMeshes.Add(new TemporaryAsset<Mesh>(
                mesh,
                gameObject,
                Path.Combine(GetTemporaryDirectory(), $"{mesh.name}.asset"),
                sourceDirectory
            ));
        }

        public void AddTemporaryMaterial(Material material, GameObject gameObject, string sourceDirectory = null)
        {
            if (sourceDirectory == null && material != null)
            {
                string sourcePath = AssetDatabase.GetAssetPath(material);
                sourceDirectory = string.IsNullOrEmpty(sourcePath) ? "Assets/Materials" : Path.GetDirectoryName(sourcePath);
            }

            tempMaterials.Add(new TemporaryAsset<Material>(
                material,
                gameObject,
                Path.Combine(GetTemporaryDirectory(), $"{material.name}.mat"),
                sourceDirectory
            ));
        }

        public void AddTemporaryTexture(Texture2D texture, GameObject gameObject, string sourceDirectory = null)
        {
            if (sourceDirectory == null && texture != null)
            {
                string sourcePath = AssetDatabase.GetAssetPath(texture);
                sourceDirectory = string.IsNullOrEmpty(sourcePath) ? "Assets/Textures" : Path.GetDirectoryName(sourcePath);
            }

            tempTextures.Add(new TemporaryTextureAsset(
                texture,
                gameObject,
                Path.Combine(GetTemporaryDirectory(), $"{texture.name}.png"),
                sourceDirectory
            ));
        }

        public void SaveSelectedAssets(string suffix = "_New")
        {
            // サフィックスが空の場合はデフォルト値を使用
            if (string.IsNullOrEmpty(suffix))
            {
                suffix = "_New";
            }

            // 保存したアセットを記録するリスト
            var savedTextures = new List<TemporaryAsset<Texture2D>>();
            var savedMaterials = new List<TemporaryAsset<Material>>();
            var savedMeshes = new List<TemporaryAsset<Mesh>>();

            // テクスチャを先に保存
            var savedTexturePaths = new Dictionary<string, string>();
            foreach (var texture in TempTextures.Where(t => t.IsSelected))
            {
                string targetDirectory = texture.SourceDirectory;
                if (!Directory.Exists(targetDirectory))
                {
                    Directory.CreateDirectory(targetDirectory);
                }

                string newName = EditorUtils.ReplaceSuffix(Path.GetFileNameWithoutExtension(texture.TemporaryPath), suffix);
                string newPath = Path.Combine(targetDirectory, newName + Path.GetExtension(texture.TemporaryPath));
                newPath = AssetDatabase.GenerateUniqueAssetPath(newPath);

                byte[] pngData = texture.Asset.EncodeToPNG();
                File.WriteAllBytes(newPath, pngData);
                AssetDatabase.ImportAsset(newPath);

                savedTexturePaths[texture.TemporaryPath] = newPath;
                savedTextures.Add(texture);
            }

            // マテリアルを保存
            foreach (var material in TempMaterials.Where(m => m.IsSelected))
            {
                string targetDirectory = material.SourceDirectory;
                if (!Directory.Exists(targetDirectory))
                {
                    Directory.CreateDirectory(targetDirectory);
                }

                string newName = EditorUtils.ReplaceSuffix(Path.GetFileNameWithoutExtension(material.TemporaryPath), suffix);
                string newPath = Path.Combine(targetDirectory, newName + Path.GetExtension(material.TemporaryPath));
                newPath = AssetDatabase.GenerateUniqueAssetPath(newPath);

                // マテリアルのテクスチャ参照を更新
                UpdateMaterialTextureReferences(material.Asset, savedTexturePaths);

                AssetDatabase.CreateAsset(material.Asset, newPath);
                savedMaterials.Add(material);
            }

            // メッシュを保存
            foreach (var mesh in TempMeshes.Where(m => m.IsSelected))
            {
                string targetDirectory = mesh.SourceDirectory;
                if (!Directory.Exists(targetDirectory))
                {
                    Directory.CreateDirectory(targetDirectory);
                }

                string newName = EditorUtils.ReplaceSuffix(Path.GetFileNameWithoutExtension(mesh.TemporaryPath), suffix);
                string newPath = Path.Combine(targetDirectory, newName + Path.GetExtension(mesh.TemporaryPath));
                newPath = AssetDatabase.GenerateUniqueAssetPath(newPath);
                AssetDatabase.CreateAsset(mesh.Asset, newPath);
                savedMeshes.Add(mesh);
            }

            // ゲームオブジェクトの名前を更新
            var processedObjects = new HashSet<GameObject>();

            void UpdateGameObjectName(GameObject obj, string newName)
            {
                if (obj != null && !processedObjects.Contains(obj))
                {
                    obj.name = EditorUtils.ReplaceSuffix(obj.name, suffix);
                    processedObjects.Add(obj);
                    
                    // オブジェクトが"_Container_Temp"の場合、その全ての子オブジェクトの名前も更新
                    if (obj.name.Contains("_Container"))
                    {
                        foreach (Transform child in obj.transform)
                        {
                            if (!processedObjects.Contains(child.gameObject))
                            {
                                child.gameObject.name = EditorUtils.ReplaceSuffix(child.gameObject.name, suffix);
                                processedObjects.Add(child.gameObject);
                            }
                        }
                    }
                }
            }

            // 選択されたアセットに関連するゲームオブジェクトの名前を更新
            foreach (var texture in TempTextures.Where(t => t.IsSelected))
            {
                UpdateGameObjectName(texture.RelatedGameObject, suffix);
            }

            foreach (var material in TempMaterials.Where(m => m.IsSelected))
            {
                UpdateGameObjectName(material.RelatedGameObject, suffix);
            }

            foreach (var mesh in TempMeshes.Where(m => m.IsSelected))
            {
                UpdateGameObjectName(mesh.RelatedGameObject, suffix);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // 保存したアセットを一時アセットリストから削除
            foreach (var texture in savedTextures)
            {
                tempTextures.Remove(texture as TemporaryTextureAsset);
            }
            foreach (var material in savedMaterials)
            {
                tempMaterials.Remove(material);
            }
            foreach (var mesh in savedMeshes)
            {
                tempMeshes.Remove(mesh);
            }
        }

        /// <summary>
        /// 指定したテクスチャと同じ名前の一時テクスチャを探します
        /// </summary>
        public TemporaryAsset<Texture2D> FindTemporaryTexture(Texture srcTex)
        {
            if (srcTex == null) return null;
            
            foreach (var tempTexture in tempTextures)
            {
                if (tempTexture.Asset.name == srcTex.name)
                {
                    return tempTexture;
                }
            }
            return null;
        }

        private void UpdateMaterialTextureReferences(Material material, Dictionary<string, string> savedTexturePaths)
        {
            var properties = material.GetTexturePropertyNames();
            foreach (var prop in properties)
            {
                var texture = material.GetTexture(prop);
                if (texture != null)
                {
                    var tempTexture = TempTextures.FirstOrDefault(t => t.Asset == texture);
                    if (tempTexture != null && savedTexturePaths.ContainsKey(tempTexture.TemporaryPath))
                    {
                        material.SetTexture(prop, AssetDatabase.LoadAssetAtPath<Texture2D>(savedTexturePaths[tempTexture.TemporaryPath]));
                    }
                }
            }
        }

        public void RemoveTemporaryMesh(TemporaryAsset<Mesh> mesh)
        {
            if (mesh == null) return;
            tempMeshes.Remove(mesh);
        }

        public void CleanupTemporaryAssets()
        {
            // シーン上の一時オブジェクトを削除
            var tempObjects = new List<GameObject>();
            foreach (var tempMesh in tempMeshes)
            {
                if (tempMesh.RelatedGameObject != null)
                {
                    tempObjects.Add(tempMesh.RelatedGameObject);
                }
            }

            foreach (var obj in tempObjects)
            {
                UObject.DestroyImmediate(obj);
            }

            // 一時ファイルの削除
            foreach (var tempMesh in tempMeshes)
            {
                if (tempMesh.Asset != null)
                {
                    UObject.DestroyImmediate(tempMesh.Asset);
                }
                if (File.Exists(tempMesh.TemporaryPath))
                {
                    AssetDatabase.DeleteAsset(tempMesh.TemporaryPath);
                }
            }

            foreach (var tempMaterial in tempMaterials)
            {
                if (tempMaterial.Asset != null)
                {
                    UObject.DestroyImmediate(tempMaterial.Asset);
                }
                if (File.Exists(tempMaterial.TemporaryPath))
                {
                    AssetDatabase.DeleteAsset(tempMaterial.TemporaryPath);
                }
            }

            foreach (var tempTexture in tempTextures)
            {
                if (tempTexture.Asset != null)
                {
                    UObject.DestroyImmediate(tempTexture.Asset);
                }
                if (File.Exists(tempTexture.TemporaryPath))
                {
                    AssetDatabase.DeleteAsset(tempTexture.TemporaryPath);
                }
            }

            // リストのクリア
            tempMeshes.Clear();
            tempMaterials.Clear();
            tempTextures.Clear();

            // 一時ディレクトリが空の場合は削除
            if (Directory.Exists(TEMP_DIRECTORY) && !Directory.EnumerateFileSystemEntries(TEMP_DIRECTORY).Any())
            {
                Directory.Delete(TEMP_DIRECTORY);
                AssetDatabase.Refresh();
            }
        }
    }
}
