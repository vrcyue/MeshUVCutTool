using vrc_yue.MeshUVCutTools.Core;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace vrc_yue.MeshUVCutTools.MeshModifyTool.Core
{
    /// <summary>
    /// メッシュのプレビュー表示（ワイヤーフレーム、マテリアル切り替え）を管理するクラス
    /// </summary>
    public class MeshPreviewController
    {
        // プレビュー関連
        private const int previewLayer = 2;
        private AvatarMonitor avatarMonitor;
        
        // ワイヤーフレーム表示用
        private GameObject controllMesh_edit;
        private MeshCollider controllMesh_editCollider;
        private MeshFilter controllMesh_editFilter;
        
        // 選択メッシュ表示用
        private GameObject controllMesh_select;
        
        // マテリアル関連
        private Material wireFrameMaterial;
        private Material[] defaultMaterials;
        private Material[] normalMaterials;
        private Material[] uvMaterials;
        
        // 表示設定
        private Color wireFrameColor = Color.white;
        private float normalAlpha = 0f;
        private float uvAlpha = 0f;
        
        /// <summary>
        /// ワイヤーフレームカラーの取得・設定
        /// </summary>
        public Color WireFrameColor
        {
            get => wireFrameColor;
            set
            {
                wireFrameColor = value;
                if (wireFrameMaterial != null)
                {
                    wireFrameMaterial.SetColor("_Color", wireFrameColor);
                }
            }
        }
        
        /// <summary>
        /// 法線表示のアルファ値
        /// </summary>
        public float NormalAlpha
        {
            get => normalAlpha;
            set => normalAlpha = value;
        }
        
        /// <summary>
        /// UV表示のアルファ値
        /// </summary>
        public float UVAlpha
        {
            get => uvAlpha;
            set => uvAlpha = value;
        }
        
        /// <summary>
        /// 初期化
        /// </summary>
        public void Initialize(AvatarMonitor monitor)
        {
            this.avatarMonitor = monitor;
        }
        
        /// <summary>
        /// メッシュコライダーを取得
        /// </summary>
        public MeshCollider GetEditMeshCollider() => controllMesh_editCollider;
        
        /// <summary>
        /// メッシュのリロード
        /// </summary>
        public void ReloadMesh(MeshCreater meshCreater, Renderer renderer, List<int> selectedVertices, bool addCaches = true)
        {
            if (meshCreater == null || renderer == null) return;
            
            // 削除予定頂点を取得
            var verticesToRemove = GetVerticesToRemove(renderer);
            
            // 編集メッシュの作成・更新
            var editMesh = CreateEditMesh(meshCreater, selectedVertices);
            SetEditMesh(meshCreater, editMesh);
            
            // SetEditMeshの後で削除予定頂点の頂点カラーを設定
            if (verticesToRemove != null && verticesToRemove.Count > 0 && controllMesh_editFilter != null && controllMesh_editFilter.sharedMesh != null)
            {
                var mesh = controllMesh_editFilter.sharedMesh;
                var colors = mesh.colors;
                
                if (colors != null && colors.Length > 0)
                {
                    foreach (var vertexIndex in verticesToRemove)
                    {
                        if (vertexIndex >= 0 && vertexIndex < colors.Length)
                        {
                            // 赤色の頂点はそのまま、それ以外は青色半透明に
                            if (colors[vertexIndex] != Color.red)
                            {
                                colors[vertexIndex] = new Color(0f, 0f, 1f, 0.3f); // 青色半透明
                            }
                        }
                    }
                    
                    mesh.colors = colors;
                }
            }
            
            // マテリアルの準備
            PrepareRenderMaterials(renderer);
            
            // 表示モードに応じてマテリアルを適用
            ApplyRenderMode(renderer);
            
            if (addCaches)
            {
                meshCreater.AddCaches();
            }
            
            // 選択メッシュの削除
            DestroySelectMesh();
        }
        
        /// <summary>
        /// 表示モードを適用
        /// </summary>
        public void ApplyRenderMode(Renderer renderer)
        {
            if (renderer == null || defaultMaterials == null) return;
            
            if (uvAlpha > 0.1f && uvMaterials != null)
            {
                foreach (var mat in uvMaterials)
                {
                    mat.SetFloat("_UVAlpha", uvAlpha);
                }
                renderer.sharedMaterials = uvMaterials;
            }
            else if (normalAlpha > 0.1f && normalMaterials != null)
            {
                foreach (var mat in normalMaterials)
                {
                    mat.SetFloat("_NormalAlpha", normalAlpha);
                }
                renderer.sharedMaterials = normalMaterials;
            }
            else
            {
                renderer.sharedMaterials = defaultMaterials;
            }
        }
        
        /// <summary>
        /// 全てのコントロールメッシュを削除
        /// </summary>
        public void DestroyAllControlMeshes()
        {
            DestroyEditMesh();
            DestroySelectMesh();
        }
        
        /// <summary>
        /// クリーンアップ
        /// </summary>
        public void Cleanup()
        {
            DestroyAllControlMeshes();
            
            if (wireFrameMaterial != null)
            {
                Object.DestroyImmediate(wireFrameMaterial);
                wireFrameMaterial = null;
            }
        }
        
        /// <summary>
        /// ワイヤーフレーム用メッシュの作成
        /// </summary>
        private Mesh CreateEditMesh(MeshCreater meshCreater, List<int> selectedVertices)
        {
            if (selectedVertices == null || selectedVertices.Count == 0)
            {
                return controllMesh_editFilter == null 
                    ? meshCreater.CreateEditMesh(null, null)
                    : meshCreater.CreateEditMesh(null, null, controllMesh_editFilter.sharedMesh);
            }
            else
            {
                return controllMesh_editFilter == null
                    ? meshCreater.CreateEditMesh(selectedVertices, null, null)
                    : meshCreater.CreateEditMesh(selectedVertices, null, controllMesh_editFilter.sharedMesh);
            }
        }
        
        /// <summary>
        /// ワイヤーフレーム用メッシュの設定
        /// </summary>
        private GameObject SetEditMesh(MeshCreater meshCreater, Mesh mesh)
        {
            if (controllMesh_edit == null)
            {
                controllMesh_edit = meshCreater.ToMesh("EditMesh", null, true);
                controllMesh_edit.hideFlags = HideFlags.HideAndDontSave;
                controllMesh_edit.layer = previewLayer;
                controllMesh_editCollider = controllMesh_edit.GetComponent<MeshCollider>();
                controllMesh_editFilter = controllMesh_edit.GetComponent<MeshFilter>();
                
                var rend = controllMesh_edit.GetComponent<MeshRenderer>();
                if (wireFrameMaterial == null)
                {
                    wireFrameMaterial = new Material(
                        AssetUtility.LoadAssetAtGuid<Material>(EnvironmentGUIDs.OverlayWireFrameMaterial));
                    wireFrameMaterial.SetFloat("_ZTest", 4);
                    wireFrameMaterial.SetColor("_Color", wireFrameColor);
                }
                
                rend.sharedMaterials = Enumerable.Range(0, rend.sharedMaterials.Length)
                    .Select(_ => wireFrameMaterial)
                    .ToArray();
            }
            
            controllMesh_editFilter.sharedMesh = mesh;
            controllMesh_editCollider.sharedMesh = controllMesh_editFilter.sharedMesh;
            
            return controllMesh_edit;
        }
        
        /// <summary>
        /// レンダリング用マテリアルの準備
        /// </summary>
        private void PrepareRenderMaterials(Renderer renderer)
        {
            defaultMaterials = renderer.sharedMaterials.ToArray();
            
            // Normal表示用マテリアル
            normalMaterials = renderer.sharedMaterials.Select(mat =>
            {
                var m = new Material(AssetUtility.LoadAssetAtGuid<Material>(EnvironmentGUIDs.NormalMaterial));
                m.mainTexture = mat.mainTexture;
                return m;
            }).ToArray();
            
            // UV表示用マテリアル
            uvMaterials = renderer.sharedMaterials.Select(mat =>
            {
                var m = new Material(AssetUtility.LoadAssetAtGuid<Material>(EnvironmentGUIDs.UVMaterial));
                m.mainTexture = mat.mainTexture;
                return m;
            }).ToArray();
        }
        
        /// <summary>
        /// ワイヤーフレーム用メッシュの削除
        /// </summary>
        private void DestroyEditMesh()
        {
            if (controllMesh_edit)
            {
                Object.DestroyImmediate(controllMesh_edit);
                controllMesh_edit = null;
                controllMesh_editCollider = null;
                controllMesh_editFilter = null;
            }
        }
        
        /// <summary>
        /// 選択メッシュの削除
        /// </summary>
        private void DestroySelectMesh()
        {
            if (controllMesh_select)
            {
                Object.DestroyImmediate(controllMesh_select);
                controllMesh_select = null;
            }
        }
        
        /// <summary>
        /// デフォルトマテリアルを取得
        /// </summary>
        public Material[] GetDefaultMaterials() => defaultMaterials;
        
        /// <summary>
        /// 削除予定頂点を取得
        /// </summary>
        private List<int> GetVerticesToRemove(Renderer renderer)
        {
            var verticesToRemove = new List<int>();
            
            if (renderer == null) 
                return verticesToRemove;
            
            // レンダラーのGameObjectから全てのMUCTMeshRemovalDataコンポーネントを取得（複数存在する可能性がある）
            var removalDataComponents = renderer.GetComponents<MUCTMeshRemovalData>();
            
            foreach (var removalData in removalDataComponents)
            {
                if (removalData != null && removalData.enabled)
                {
                    foreach (var info in removalData.removalInfos)
                    {
                        if (info.targetRenderer == renderer && info.verticesToRemove != null)
                        {
                            verticesToRemove.AddRange(info.verticesToRemove);
                        }
                    }
                }
            }
            
            // 重複を除去
            return verticesToRemove.Distinct().ToList();
        }
    }
}