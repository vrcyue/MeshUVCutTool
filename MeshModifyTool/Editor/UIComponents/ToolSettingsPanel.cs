using System;
using UnityEditor;
using UnityEngine;

namespace vrc_yue.MeshUVCutTools.MeshModifyTool.UIComponents
{
    /// <summary>
    /// ツール設定パネル
    /// </summary>
    public class ToolSettingsPanel
    {
        // UI表示設定
        private bool extendExperimental = false;
        
        // イベント
#pragma warning disable CS0067 // イベントは使用されていません
        public event Action<Color> OnWireFrameColorChanged;
        public event Action<float> OnNormalAlphaChanged;
        public event Action<float> OnUVAlphaChanged;
#pragma warning restore CS0067
        public event Action<bool> OnCameraViewChanged;
        
        /// <summary>
        /// UI設定パネルを描画
        /// </summary>
        public void DrawSettings(Color wireFrameColor, float normalAlpha, float uvAlpha, 
            int selectedVertexCount, int totalVertexCount, bool showCameraView)
        {
            extendExperimental = EditorGUILayout.Foldout(extendExperimental, Localization.Get("ui_settings"));
            
            if (extendExperimental)
            {
                // TODO 正しく動作しないためコメントアウト
                // using (var check = new EditorGUI.ChangeCheckScope())
                // {
                //     // ワイヤーフレームカラー
                //     var newWireFrameColor = EditorGUILayout.ColorField(Localization.Get("wire_frame_color"), wireFrameColor);
                //     if (newWireFrameColor != wireFrameColor)
                //     {
                //         OnWireFrameColorChanged?.Invoke(newWireFrameColor);
                //     }
                //     
                //     // 法線表示アルファ
                //     var newNormalAlpha = EditorGUILayout.Slider(Localization.Get("normal"), normalAlpha, 0f, 1f);
                //     if (Math.Abs(newNormalAlpha - normalAlpha) > 0.001f)
                //     {
                //         OnNormalAlphaChanged?.Invoke(newNormalAlpha);
                //     }
                //     
                //     // UV表示アルファ
                //     var newUVAlpha = EditorGUILayout.Slider(Localization.Get("uv"), uvAlpha, 0f, 1f);
                //     if (Math.Abs(newUVAlpha - uvAlpha) > 0.001f)
                //     {
                //         OnUVAlphaChanged?.Invoke(newUVAlpha);
                //     }
                // }
                //
                // EditorGUILayout.Space();
                
                // デバッグ情報
                DrawDebugInfo(selectedVertexCount, totalVertexCount);
                
                EditorGUILayout.Space();
                
                // TODO 旧機能であり、Deprecatedとしてコメントアウト
                // カメラビュー設定
                // DrawCameraViewSetting(showCameraView);
            }
        }
        
        /// <summary>
        /// デバッグ情報を描画
        /// </summary>
        private void DrawDebugInfo(int selectedVertexCount, int totalVertexCount)
        {
            EditorGUILayout.LabelField(Localization.Get("debug"), EditorStyles.boldLabel);
            
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(Localization.Get("selected_vertices"));
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField($"{selectedVertexCount}");
            }
            
            int notSelectedVerticesCount = totalVertexCount - selectedVertexCount;
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(Localization.Get("not_selected_vertices"));
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField($"{notSelectedVerticesCount}");
            }
        }
        
        /// <summary>
        /// カメラビュー設定を描画
        /// </summary>
        private void DrawCameraViewSetting(bool showCameraView)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUILayout.VerticalScope())
                {
                    EditorGUILayout.LabelField(Localization.Get("show_camera_view"));
                }
                GUILayout.FlexibleSpace();
                
                var newCameraView = EditorGUILayout.Toggle(showCameraView, GUILayout.Width(20));
                if (newCameraView != showCameraView)
                {
                    OnCameraViewChanged?.Invoke(newCameraView);
                }
            }
        }
    }
}