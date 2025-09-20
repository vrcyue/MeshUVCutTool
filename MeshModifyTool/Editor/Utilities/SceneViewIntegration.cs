using UnityEditor;
using UnityEngine;
using vrc_yue.MeshUVCutTools.Core;
using vrc_yue.MeshUVCutTools.MeshModifyTool.Core;
using vrc_yue.MeshUVCutTools.MeshModifyTool;

namespace vrc_yue.MeshUVCutTools.MeshModifyTool.Utilities
{
    /// <summary>
    /// SceneViewとの統合機能を管理するクラス
    /// </summary>
    public class SceneViewIntegration
    {
        private MeshEditingCore editingCore;
        private AvatarMonitor avatarMonitor;
        private bool isEnabled = false;
        
        // マウス操作設定
        private int drawButton = 0;
        
        // ドラッグ選択用
        private bool isDragging = false;
        private Vector2 dragStartPos;
        private Vector2 dragEndPos;
        
        public SceneViewIntegration(MeshEditingCore editingCore)
        {
            this.editingCore = editingCore;
        }
        
        /// <summary>
        /// SceneView統合を有効化
        /// </summary>
        public void Enable(AvatarMonitor monitor)
        {
            if (!isEnabled)
            {
                avatarMonitor = monitor;
                SceneView.duringSceneGui += OnSceneGUI;
                isEnabled = true;
            }
        }
        
        /// <summary>
        /// SceneView統合を無効化
        /// </summary>
        public void Disable()
        {
            if (isEnabled)
            {
                SceneView.duringSceneGui -= OnSceneGUI;
                isEnabled = false;
            }
        }
        
        /// <summary>
        /// SceneViewでの描画処理
        /// </summary>
        private void OnSceneGUI(SceneView sceneView)
        {
            if (avatarMonitor == null || editingCore == null) return;
            
            var selectionHandler = editingCore.GetSelectionHandler();
            var previewController = editingCore.GetPreviewController();
            
            // 選択モードでない場合は何もしない（SceneViewの通常操作を妨げない）
            if (selectionHandler == null || !selectionHandler.IsSelectionMode) return;
            if (editingCore.GetEditIndex() == -1) return;
            
            var meshCollider = previewController.GetEditMeshCollider();
            if (meshCollider == null) return;
            
            var e = Event.current;
            
            // Alt押下時はカメラ操作を優先
            if (e.alt) return;
            
            // 選択モード中のみマウスイベントを取得
            if (e.type == EventType.MouseDown || e.type == EventType.MouseDrag || e.type == EventType.MouseUp)
            {
                if (e.button == drawButton)
                {
                    HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
                }
            }
            
            // マウスダウン処理（どこでもドラッグ開始可能）
            if (e.type == EventType.MouseDown && e.button == drawButton)
            {
                avatarMonitor.SetSceneViewMode(true, sceneView);
                avatarMonitor.BeginDrag(e.mousePosition);
                
                // ドラッグ開始
                isDragging = true;
                dragStartPos = e.mousePosition;
                dragEndPos = e.mousePosition;
                
                // 単体選択処理（クリックのフォールバック）
                selectionHandler.HandleVertexClick(
                    editingCore.EditMeshCreater, 
                    meshCollider, 
                    avatarMonitor, 
                    e.mousePosition);
                
                e.Use();
            }
            
            // マウスドラッグ処理
            if (e.type == EventType.MouseDrag && e.button == drawButton && isDragging)
            {
                dragEndPos = e.mousePosition;
                e.Use();
            }
            
            // マウスアップ処理
            if (e.type == EventType.MouseUp && e.button == drawButton && isDragging)
            {
                isDragging = false;
                
                var rect = avatarMonitor.GetSelectionRect();
                if (rect.size.magnitude > 0)
                {
                    selectionHandler.HandleDragSelection(
                        editingCore.EditMeshCreater,
                        meshCollider,
                        avatarMonitor,
                        rect);
                }
                avatarMonitor.EndDrag();
                e.Use();
            }
            
            // 選択範囲の描画
            DrawSelectionRect();
            avatarMonitor.DrawSelectionRect();
            
            // 警告文の表示
            DrawWarningMessage(sceneView);
            
            // SceneViewを再描画
            sceneView.Repaint();
        }
        
        /// <summary>
        /// ドラッグ選択範囲の描画
        /// </summary>
        private void DrawSelectionRect()
        {
            if (!isDragging) return;
            
            var rect = new Rect(
                Mathf.Min(dragStartPos.x, dragEndPos.x),
                Mathf.Min(dragStartPos.y, dragEndPos.y),
                Mathf.Abs(dragEndPos.x - dragStartPos.x),
                Mathf.Abs(dragEndPos.y - dragStartPos.y)
            );
            
            if (rect.width > 1 && rect.height > 1)
            {
                // GUI座標系で描画
                Handles.BeginGUI();
                
                // 枠線の描画
                var borderColor = new Color(0.5f, 0.7f, 1f, 1f); // 薄い青色
                GUI.color = borderColor;
                GUI.DrawTexture(new Rect(rect.x - 1, rect.y - 1, rect.width + 2, 1), EditorGUIUtility.whiteTexture);
                GUI.DrawTexture(new Rect(rect.x - 1, rect.y + rect.height, rect.width + 2, 1), EditorGUIUtility.whiteTexture);
                GUI.DrawTexture(new Rect(rect.x - 1, rect.y, 1, rect.height), EditorGUIUtility.whiteTexture);
                GUI.DrawTexture(new Rect(rect.x + rect.width, rect.y, 1, rect.height), EditorGUIUtility.whiteTexture);
                
                // 半透明の塗りつぶし
                var fillColor = new Color(0.5f, 0.7f, 1f, 0.1f); // 半透明の青色
                GUI.color = fillColor;
                GUI.DrawTexture(rect, EditorGUIUtility.whiteTexture);
                
                GUI.color = Color.white;
                Handles.EndGUI();
            }
        }
        
        /// <summary>
        /// 警告メッセージを描画
        /// </summary>
        private void DrawWarningMessage(SceneView sceneView)
        {
            Handles.BeginGUI();
            
            // 警告文のスタイル設定
            var warningStyle = new GUIStyle("box");
            warningStyle.normal.textColor = Color.white;
            warningStyle.alignment = TextAnchor.MiddleCenter;
            warningStyle.fontSize = 14;
            warningStyle.padding = new RectOffset(10, 10, 5, 5);
            
            // 警告文の内容（多言語対応）
            string warningText = Localization.Get("msg_mesh_selection_mode") + "\n" + Localization.Get("msg_gizmo_warning");
            
            // 背景のサイズを計算
            var content = new GUIContent(warningText);
            var size = warningStyle.CalcSize(content);
            
            // 表示位置（SceneViewの上部中央）
            var rect = new Rect(
                (sceneView.position.width - size.x) / 2,
                30,
                size.x,
                size.y
            );
            
            // 背景を描画（半透明の赤）
            var backgroundColor = new Color(0.8f, 0.2f, 0.2f, 0.8f);
            var oldColor = GUI.color;
            GUI.color = backgroundColor;
            GUI.Box(rect, GUIContent.none, warningStyle);
            
            // テキストを描画
            GUI.color = Color.white;
            GUI.Label(rect, warningText, warningStyle);
            
            GUI.color = oldColor;
            Handles.EndGUI();
        }
    }
}