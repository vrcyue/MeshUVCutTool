using vrc_yue.MeshUVCutTools.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using vrc_yue.MeshUVCutTools.MeshModifyTool.Models;

namespace vrc_yue.MeshUVCutTools.MeshModifyTool.Core
{
    /// <summary>
    /// メッシュ編集のコア機能を管理するクラス
    /// </summary>
    public class MeshEditingCore
    {
        // 編集対象の管理
        private GameObject avatar;
        private Renderer[] rends;
        private MeshCreater[] meshsCreaters;
        private Mesh[] defaultMeshs;
        private int editIndex = -1;
        
        // 選択管理
        private MeshSelectionHandler selectionHandler;
        
        // メッシュ操作
        private MeshOperations meshOperations;
        
        // プレビュー管理
        private MeshPreviewController previewController;
        
        // アセット管理
        private TemporaryAssetManager assetManager;
        private TextureEditor textureEditor;
        private MaterialDuplicator materialDuplicator;
        
        // イベント
        public event Action<int> OnMeshSelected;
        public event Action<List<int>> OnVerticesSelected;
        
        public MeshEditingCore(TemporaryAssetManager assetManager)
        {
            this.assetManager = assetManager;
            this.textureEditor = new TextureEditor(assetManager);
            this.materialDuplicator = new MaterialDuplicator(assetManager);
            
            this.selectionHandler = new MeshSelectionHandler();
            this.meshOperations = new MeshOperations(assetManager, textureEditor, materialDuplicator);
            this.previewController = new MeshPreviewController();
        }
        
        /// <summary>
        /// 現在編集中のMeshCreaterを取得
        /// </summary>
        public MeshCreater EditMeshCreater
        {
            get
            {
                if (meshsCreaters == null) return null;
                if (0 <= editIndex && editIndex < meshsCreaters.Length)
                {
                    return meshsCreaters[editIndex];
                }
                return null;
            }
        }
        
        /// <summary>
        /// レンダラー配列を取得
        /// </summary>
        public Renderer[] GetRenderers() => rends;
        
        /// <summary>
        /// 現在の編集インデックスを取得
        /// </summary>
        public int GetEditIndex() => editIndex;
        
        /// <summary>
        /// 選択ハンドラーを取得
        /// </summary>
        public MeshSelectionHandler GetSelectionHandler() => selectionHandler;
        
        /// <summary>
        /// メッシュ操作オブジェクトを取得
        /// </summary>
        public MeshOperations GetMeshOperations() => meshOperations;
        
        /// <summary>
        /// プレビューコントローラーを取得
        /// </summary>
        public MeshPreviewController GetPreviewController() => previewController;
        
        /// <summary>
        /// アバターのセットアップ
        /// </summary>
        public void Setup(GameObject avatar, AvatarMonitor avatarMonitor)
        {
            this.avatar = avatar;
            
            // 既存のメッシュをクリーンアップ
            previewController.DestroyAllControlMeshes();
            selectionHandler.ResetSelection();
            
            // レンダラーの取得
            rends = avatar.transform.GetComponentsInChildren<Renderer>()
                .Where(r => r.GetMesh() != null).ToArray();
            defaultMeshs = rends.Select(m => m.GetMesh()).ToArray();
            meshsCreaters = rends.Select(m => new MeshCreater(m, avatar.transform)).ToArray();
            
            // プレビューコントローラーの初期化
            previewController.Initialize(avatarMonitor);
            
            editIndex = -1;
        }
        
        /// <summary>
        /// メッシュを選択
        /// </summary>
        public void SelectMesh(int index)
        {
            previewController.DestroyAllControlMeshes();
            editIndex = index;
            
            if (editIndex != -1 && EditMeshCreater != null)
            {
                var renderer = rends[editIndex];
                var selectedVertices = selectionHandler.GetSelectedVertices();
                
                previewController.ReloadMesh(EditMeshCreater, renderer, selectedVertices, false);
                OnMeshSelected?.Invoke(editIndex);
            }
        }
        
        /// <summary>
        /// 頂点選択の更新
        /// </summary>
        public void UpdateVertexSelection(List<int> vertices)
        {
            if (EditMeshCreater != null && editIndex != -1)
            {
                var renderer = rends[editIndex];
                previewController.ReloadMesh(EditMeshCreater, renderer, vertices, false);
                OnVerticesSelected?.Invoke(vertices);
            }
        }
        
        /// <summary>
        /// レンダラーを追加
        /// </summary>
        public void AddRenderer(Renderer rend, bool select = true)
        {
            if (rend == null || rend.GetMesh() == null) return;
            
            int i = rends.Length;
            rends = rends.Append(rend).ToArray();
            defaultMeshs = defaultMeshs.Append(rend.GetMesh()).ToArray();
            meshsCreaters = meshsCreaters.Append(new MeshCreater(rend, avatar.transform)).ToArray();
            
            if (select)
            {
                SelectMesh(i);
            }
        }
        
        /// <summary>
        /// レンダラーを削除
        /// </summary>
        public void RemoveRenderer(int index)
        {
            if (index < 0 || index >= rends.Length) return;
            
            // 削除対象が選択中の場合は選択を解除
            if (editIndex == index)
            {
                SelectMesh(-1);
            }
            else if (editIndex > index)
            {
                // 削除対象より後ろの選択インデックスを調整
                editIndex--;
            }
            
            // 配列から削除
            var rendList = rends.ToList();
            var defaultMeshList = defaultMeshs.ToList();
            var meshCreaterList = meshsCreaters.ToList();
            
            rendList.RemoveAt(index);
            defaultMeshList.RemoveAt(index);
            meshCreaterList.RemoveAt(index);
            
            rends = rendList.ToArray();
            defaultMeshs = defaultMeshList.ToArray();
            meshsCreaters = meshCreaterList.ToArray();
        }
        
        /// <summary>
        /// クリーンアップ
        /// </summary>
        public void Cleanup()
        {
            // プレビューのクリーンアップ
            previewController.Cleanup();
            
            // メッシュを元の状態に戻す
            if (rends != null && defaultMeshs != null)
            {
                for (int i = 0; i < rends.Length && i < defaultMeshs.Length; i++)
                {
                    if (rends[i] != null)
                    {
                        rends[i].SetMesh(defaultMeshs[i]);
                    }
                }
            }
            
            // 配列をクリア
            rends = null;
            meshsCreaters = null;
            defaultMeshs = null;
            editIndex = -1;
        }
        
        /// <summary>
        /// アバターを取得
        /// </summary>
        public GameObject GetAvatar() => avatar;
        
        /// <summary>
        /// デフォルトメッシュ配列を取得
        /// </summary>
        public Mesh[] GetDefaultMeshes() => defaultMeshs;
    }
}