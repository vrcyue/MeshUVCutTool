using vrc_yue.MeshUVCutTools.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using vrc_yue.MeshUVCutTools.MeshModifyTool.Utilities;

namespace vrc_yue.MeshUVCutTools.MeshModifyTool.Core
{
    /// <summary>
    /// メッシュの頂点・面選択を管理するクラス
    /// </summary>
    public class MeshSelectionHandler
    {
        // 選択モード
        public enum SelectionMode
        {
            SelectLand,
            UnSelectLand,
            SelectVertex,
            UnSelectVertex
        }
        
        // 選択された頂点のリスト
        private List<int> selectedVertices = new List<int>();
        
        // 選択モード
        private SelectionMode currentMode = SelectionMode.SelectVertex;
        
        // 選択設定
        private bool isSelectOverlappingVertices = false;
        private bool backfaceCulling = true;
        
        // イベント
        public event Action<List<int>> OnSelectionChanged;
        
        /// <summary>
        /// 現在の選択モードを取得・設定
        /// </summary>
        public SelectionMode CurrentMode
        {
            get => currentMode;
            set => currentMode = value;
        }
        
        /// <summary>
        /// 重複頂点の選択設定
        /// </summary>
        public bool IsSelectOverlappingVertices
        {
            get => isSelectOverlappingVertices;
            set => isSelectOverlappingVertices = value;
        }
        
        /// <summary>
        /// バックフェイスカリング設定
        /// </summary>
        public bool BackfaceCulling
        {
            get => backfaceCulling;
            set => backfaceCulling = value;
        }
        
        /// <summary>
        /// 選択された頂点のリストを取得
        /// </summary>
        public List<int> GetSelectedVertices() => selectedVertices != null ? new List<int>(selectedVertices) : new List<int>();
        
        /// <summary>
        /// 選択モードかどうか
        /// </summary>
        public bool IsSelectionMode => currentMode == SelectionMode.SelectLand ||
                                       currentMode == SelectionMode.UnSelectLand ||
                                       currentMode == SelectionMode.SelectVertex ||
                                       currentMode == SelectionMode.UnSelectVertex;
        
        /// <summary>
        /// 頂点選択の処理（クリック）
        /// </summary>
        public void HandleVertexClick(MeshCreater meshCreater, MeshCollider meshCollider, 
            AvatarMonitor avatarMonitor, Vector2 mousePosition)
        {
            if (meshCreater == null || meshCollider == null) return;
            
            switch (currentMode)
            {
                case SelectionMode.SelectLand:
                    HandleLandSelection(meshCreater, meshCollider, avatarMonitor, true);
                    break;
                    
                case SelectionMode.UnSelectLand:
                    HandleLandSelection(meshCreater, meshCollider, avatarMonitor, false);
                    break;
                    
                case SelectionMode.SelectVertex:
                    HandleTriangleSelection(meshCreater, meshCollider, avatarMonitor, true);
                    break;
                    
                case SelectionMode.UnSelectVertex:
                    HandleTriangleSelection(meshCreater, meshCollider, avatarMonitor, false);
                    break;
            }
        }
        
        /// <summary>
        /// 面選択の処理
        /// </summary>
        private void HandleLandSelection(MeshCreater meshCreater, MeshCollider meshCollider, 
            AvatarMonitor avatarMonitor, bool select)
        {
            avatarMonitor.GetTriangle(meshCollider, hitInfo =>
            {
                meshCreater.ComputeLandVertexes(meshCreater.GetVertexIndex(hitInfo)[0], vertex =>
                {
                    if (select && !selectedVertices.Contains(vertex))
                    {
                        selectedVertices.Add(vertex);
                    }
                    else if (!select && selectedVertices.Contains(vertex))
                    {
                        selectedVertices.Remove(vertex);
                    }
                }, _ =>
                {
                    OnSelectionChanged?.Invoke(GetSelectedVertices());
                }, isSelectOverlappingVertices);
            });
        }
        
        /// <summary>
        /// 三角形頂点選択の処理
        /// </summary>
        private void HandleTriangleSelection(MeshCreater meshCreater, MeshCollider meshCollider, 
            AvatarMonitor avatarMonitor, bool select)
        {
            avatarMonitor.GetTriangle(meshCollider, hitInfo =>
            {
                var vertices = meshCreater.GetVertexIndex(hitInfo);
                for (int i = 0; i < 3; i++)
                {
                    var vertex = vertices[i];
                    if (select && !selectedVertices.Contains(vertex))
                    {
                        selectedVertices.Add(vertex);
                    }
                    else if (!select && selectedVertices.Contains(vertex))
                    {
                        selectedVertices.Remove(vertex);
                    }
                }
                
                OnSelectionChanged?.Invoke(GetSelectedVertices());
            });
        }
        
        /// <summary>
        /// ドラッグ選択の処理
        /// </summary>
        public void HandleDragSelection(MeshCreater meshCreater, MeshCollider meshCollider,
            AvatarMonitor avatarMonitor, Rect selectionRect)
        {
            if (meshCollider == null || meshCreater == null) return;
            
            if (currentMode == SelectionMode.SelectLand || currentMode == SelectionMode.UnSelectLand)
            {
                // Land選択モードの処理
                var processedLands = new HashSet<int>();
                
                avatarMonitor.GetVerticesInRect(meshCollider, selectionRect, vertexIndex =>
                {
                    // この頂点が属するLandをすでに処理済みか確認
                    if (!processedLands.Contains(vertexIndex))
                    {
                        // ComputeLandVertexesを使用してLand全体を選択
                        meshCreater.ComputeLandVertexes(vertexIndex, v =>
                        {
                            processedLands.Add(v);
                            
                            if (currentMode == SelectionMode.SelectLand && !selectedVertices.Contains(v))
                            {
                                selectedVertices.Add(v);
                            }
                            else if (currentMode == SelectionMode.UnSelectLand && selectedVertices.Contains(v))
                            {
                                selectedVertices.Remove(v);
                            }
                        }, _ => { }, isSelectOverlappingVertices);
                    }
                });
            }
            else
            {
                // 頂点選択モードの処理（既存のコード）
                avatarMonitor.GetVerticesInRect(meshCollider, selectionRect, vertexIndex =>
                {
                    if (currentMode == SelectionMode.SelectVertex && !selectedVertices.Contains(vertexIndex))
                    {
                        selectedVertices.Add(vertexIndex);
                    }
                    else if (currentMode == SelectionMode.UnSelectVertex && selectedVertices.Contains(vertexIndex))
                    {
                        selectedVertices.Remove(vertexIndex);
                    }
                });
            }
            
            OnSelectionChanged?.Invoke(GetSelectedVertices());
        }
        
        /// <summary>
        /// 全頂点を選択
        /// </summary>
        public void SelectAll(int maxVertexCount)
        {
            selectedVertices.Clear();
            selectedVertices.AddRange(Enumerable.Range(0, maxVertexCount));
            OnSelectionChanged?.Invoke(GetSelectedVertices());
        }
        
        /// <summary>
        /// 選択をクリア
        /// </summary>
        public void ClearSelection()
        {
            selectedVertices.Clear();
            OnSelectionChanged?.Invoke(GetSelectedVertices());
        }
        
        /// <summary>
        /// 選択を反転
        /// </summary>
        public void InvertSelection(int maxVertexCount)
        {
            var newSelection = Enumerable.Range(0, maxVertexCount)
                .Where(n => !selectedVertices.Contains(n))
                .ToList();
            selectedVertices = newSelection;
            OnSelectionChanged?.Invoke(GetSelectedVertices());
        }
        
        /// <summary>
        /// 選択をリセット
        /// </summary>
        public void ResetSelection()
        {
            selectedVertices.Clear();
        }
        
        /// <summary>
        /// 頂点選択を設定
        /// </summary>
        public void SetSelectedVertices(List<int> vertices)
        {
            selectedVertices = new List<int>(vertices);
            OnSelectionChanged?.Invoke(GetSelectedVertices());
        }
    }
}