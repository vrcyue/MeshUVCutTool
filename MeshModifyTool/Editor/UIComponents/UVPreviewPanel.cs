using vrc_yue.MeshUVCutTools.Core;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace vrc_yue.MeshUVCutTools.MeshModifyTool.UIComponents
{
    /// <summary>
    /// UV範囲プレビューパネル
    /// </summary>
    public class UVPreviewPanel
    {
        private RenderTexture uvPreviewTexture;
        private Material uvHighlightMaterial;
        private Material originalMaterial;
        
        // プレビューテクスチャの解像度
        private const int PREVIEW_TEXTURE_SIZE = 512;
        // UI上での表示サイズ
        private const int PREVIEW_DISPLAY_SIZE = 256;
        
        // テクスチャのアスペクト比
        private float textureAspectRatio = 1.0f;
        
        /// <summary>
        /// プレビューテクスチャを取得
        /// </summary>
        public RenderTexture PreviewTexture => uvPreviewTexture;
        
        /// <summary>
        /// UV範囲プレビューを描画
        /// </summary>
        public void DrawUVPreview()
        {
            EditorGUILayout.LabelField(Localization.Get("texture_preview"), EditorStyles.boldLabel);
            
            // メッシュが選択されていない場合でも黒い画像を表示
            if (uvPreviewTexture == null || !uvPreviewTexture.IsCreated())
            {
                if (uvPreviewTexture != null)
                {
                    uvPreviewTexture.Release();
                }
                
                // デフォルトは正方形
                int textureWidth = PREVIEW_TEXTURE_SIZE;
                int textureHeight = PREVIEW_TEXTURE_SIZE;
                
                if (textureAspectRatio > 1.0f)
                {
                    textureHeight = Mathf.RoundToInt(textureWidth / textureAspectRatio);
                }
                else if (textureAspectRatio < 1.0f)
                {
                    textureWidth = Mathf.RoundToInt(textureHeight * textureAspectRatio);
                }
                
                uvPreviewTexture = new RenderTexture(textureWidth, textureHeight, 0);
                uvPreviewTexture.Create();
                
                // 黒色で塗りつぶし
                RenderTexture.active = uvPreviewTexture;
                GL.Clear(true, true, Color.black);
                RenderTexture.active = null;
            }
            
            // アスペクト比を考慮したサイズを計算
            float displayWidth = PREVIEW_DISPLAY_SIZE;
            float displayHeight = PREVIEW_DISPLAY_SIZE;
            
            if (textureAspectRatio > 1.0f)
            {
                // 横長の場合
                displayHeight = displayWidth / textureAspectRatio;
            }
            else if (textureAspectRatio < 1.0f)
            {
                // 縦長の場合
                displayHeight = displayWidth / textureAspectRatio;
            }
            
            // 固定幅・高さでレイアウトを確保
            var rect = GUILayoutUtility.GetRect(displayWidth, displayHeight, GUILayout.Width(displayWidth), GUILayout.Height(displayHeight));
            EditorGUI.DrawPreviewTexture(rect, uvPreviewTexture);
        }
        
        /// <summary>
        /// メッシュ選択時にテクスチャをプレビュー表示する初期化
        /// </summary>
        public void InitializePreview(Material material)
        {
            originalMaterial = material;
            
            // アスペクト比を計算
            if (material != null && material.mainTexture != null)
            {
                textureAspectRatio = (float)material.mainTexture.width / (float)material.mainTexture.height;
            }
            else
            {
                textureAspectRatio = 1.0f;
            }
            
            // アスペクト比を考慮したRenderTextureのサイズを計算
            int textureWidth = PREVIEW_TEXTURE_SIZE;
            int textureHeight = PREVIEW_TEXTURE_SIZE;
            
            if (textureAspectRatio > 1.0f)
            {
                // 横長の場合
                textureHeight = Mathf.RoundToInt(textureWidth / textureAspectRatio);
            }
            else if (textureAspectRatio < 1.0f)
            {
                // 縦長の場合
                textureWidth = Mathf.RoundToInt(textureHeight * textureAspectRatio);
            }
            
            // RenderTextureが未作成または破棄されている場合、またはサイズが異なる場合は新規作成
            if (uvPreviewTexture == null || !uvPreviewTexture.IsCreated() || 
                uvPreviewTexture.width != textureWidth || uvPreviewTexture.height != textureHeight)
            {
                if (uvPreviewTexture != null)
                {
                    uvPreviewTexture.Release();
                }
                
                uvPreviewTexture = new RenderTexture(textureWidth, textureHeight, 0);
                uvPreviewTexture.Create();
            }
            
            // 元のテクスチャを描画
            if (material != null && material.mainTexture != null)
            {
                Graphics.Blit(material.mainTexture, uvPreviewTexture);
            }
            else
            {
                // テクスチャがない場合は白色で塗りつぶし
                RenderTexture.active = uvPreviewTexture;
                GL.Clear(true, true, Color.white);
                RenderTexture.active = null;
            }
        }
        
        /// <summary>
        /// 選択された頂点のUV範囲を更新して表示
        /// </summary>
        public void UpdateVerticesUVRange(MeshCreater meshCreater, List<int> selectedVertices)
        {
            if (meshCreater == null || Event.current.type != EventType.Repaint) return;
            
            // テクスチャを初期化
            InitializePreviewTexture();
            
            // 全体のUV範囲を計算して表示（枠線のみ）
            var allVertices = Enumerable.Range(0, meshCreater.VertexsCount());
            var (totalMinUV, totalMaxUV) = CalculateUVRange(meshCreater, allVertices);
            DrawUVPreviewRect(totalMinUV, totalMaxUV, fill: false);
            
            // 選択頂点のUV範囲を表示
            if (selectedVertices != null && selectedVertices.Count > 0)
            {
                var (minUV, maxUV) = CalculateUVRange(meshCreater, selectedVertices);
                DrawUVPreviewRect(minUV, maxUV);
            }
        }
        
        /// <summary>
        /// マテリアルを更新
        /// </summary>
        public void UpdateWithMaterial(Material material)
        {
            if (material != null)
            {
                originalMaterial = material;
                
                // テクスチャアスペクト比を再計算
                if (material.mainTexture != null)
                {
                    textureAspectRatio = (float)material.mainTexture.width / (float)material.mainTexture.height;
                }
                else
                {
                    textureAspectRatio = 1.0f;
                }
            }
        }
        
        /// <summary>
        /// UV編集後にプレビューを強制更新
        /// </summary>
        public void ForceUpdatePreview(MeshCreater meshCreater, List<int> selectedVertices)
        {
            if (meshCreater == null) return;
            
            // テクスチャを初期化
            InitializePreviewTexture();
            
            // 全体のUV範囲を計算して表示（枠線のみ）
            var allVertices = Enumerable.Range(0, meshCreater.VertexsCount());
            var (totalMinUV, totalMaxUV) = CalculateUVRange(meshCreater, allVertices);
            DrawUVPreviewRect(totalMinUV, totalMaxUV, fill: false);
            
            // 選択頂点のUV範囲を表示
            if (selectedVertices != null && selectedVertices.Count > 0)
            {
                var (minUV, maxUV) = CalculateUVRange(meshCreater, selectedVertices);
                DrawUVPreviewRect(minUV, maxUV);
            }
        }
        
        /// <summary>
        /// クリーンアップ
        /// </summary>
        public void Cleanup()
        {
            if (uvPreviewTexture != null)
            {
                uvPreviewTexture.Release();
                uvPreviewTexture = null;
            }
            
            if (uvHighlightMaterial != null)
            {
                Object.DestroyImmediate(uvHighlightMaterial);
                uvHighlightMaterial = null;
            }
        }
        
        /// <summary>
        /// UV範囲を計算
        /// </summary>
        private (Vector2 min, Vector2 max) CalculateUVRange(MeshCreater meshCreater, IEnumerable<int> vertexIndices)
        {
            Vector2 minUV = Vector2.one * float.MaxValue;
            Vector2 maxUV = Vector2.one * float.MinValue;
            
            foreach (var vertexIndex in vertexIndices)
            {
                var uvs = meshCreater.GetUVs(vertexIndex);
                if (uvs != null && uvs.Length > 0)
                {
                    var uv = uvs[0];
                    minUV.x = Mathf.Min(minUV.x, uv.x);
                    minUV.y = Mathf.Min(minUV.y, uv.y);
                    maxUV.x = Mathf.Max(maxUV.x, uv.x);
                    maxUV.y = Mathf.Max(maxUV.y, uv.y);
                }
            }
            
            return (minUV, maxUV);
        }
        
        /// <summary>
        /// UVプレビューのテクスチャを初期化
        /// </summary>
        private void InitializePreviewTexture()
        {
            if (uvPreviewTexture == null) return;
            
            // 元のテクスチャを再描画
            if (originalMaterial != null && originalMaterial.mainTexture != null)
            {
                // テクスチャキャッシュをクリアして最新のテクスチャを使用
                var texture = originalMaterial.mainTexture;
                
                // RenderTextureをクリアしてから描画
                RenderTexture.active = uvPreviewTexture;
                GL.Clear(true, true, Color.black);
                RenderTexture.active = null;
                
                // 最新のテクスチャを描画
                Graphics.Blit(texture, uvPreviewTexture);
            }
            else
            {
                RenderTexture.active = uvPreviewTexture;
                GL.Clear(true, true, Color.white);
                RenderTexture.active = null;
            }
            
            // ハイライト用マテリアルが未作成の場合は新規作成
            if (uvHighlightMaterial == null)
            {
                var shader = Shader.Find("Hidden/Internal-Colored");
                uvHighlightMaterial = new Material(shader);
                uvHighlightMaterial.hideFlags = HideFlags.HideAndDontSave;
            }
        }
        
        /// <summary>
        /// UV範囲を矩形で表示
        /// </summary>
        private void DrawUVPreviewRect(Vector2 minUV, Vector2 maxUV, Color? fillColor = null,
            Color? outlineColor = null, bool fill = true)
        {
            if (uvPreviewTexture == null || uvHighlightMaterial == null) return;
            
            // デフォルトの色を設定
            var defaultFillColor = new Color(1f, 0f, 0f, 0.2f);
            var defaultOutlineColor = new Color(1f, 0f, 0f, 1f);
            
            // GL描画の設定
            RenderTexture.active = uvPreviewTexture;
            GL.PushMatrix();
            GL.LoadPixelMatrix(0, uvPreviewTexture.width, 0, uvPreviewTexture.height);
            
            uvHighlightMaterial.SetPass(0);
            
            if (fill)
            {
                // 塗りつぶしの描画
                GL.Begin(GL.QUADS);
                GL.Color(fillColor ?? defaultFillColor);
                GL.Vertex3(minUV.x * uvPreviewTexture.width, minUV.y * uvPreviewTexture.height, 0);
                GL.Vertex3(maxUV.x * uvPreviewTexture.width, minUV.y * uvPreviewTexture.height, 0);
                GL.Vertex3(maxUV.x * uvPreviewTexture.width, maxUV.y * uvPreviewTexture.height, 0);
                GL.Vertex3(minUV.x * uvPreviewTexture.width, maxUV.y * uvPreviewTexture.height, 0);
                GL.End();
            }
            
            // 枠線の描画
            GL.Begin(GL.LINES);
            GL.Color(outlineColor ?? defaultOutlineColor);
            // 下辺
            GL.Vertex3(minUV.x * uvPreviewTexture.width, minUV.y * uvPreviewTexture.height, 0);
            GL.Vertex3(maxUV.x * uvPreviewTexture.width, minUV.y * uvPreviewTexture.height, 0);
            // 右辺
            GL.Vertex3(maxUV.x * uvPreviewTexture.width, minUV.y * uvPreviewTexture.height, 0);
            GL.Vertex3(maxUV.x * uvPreviewTexture.width, maxUV.y * uvPreviewTexture.height, 0);
            // 上辺
            GL.Vertex3(maxUV.x * uvPreviewTexture.width, maxUV.y * uvPreviewTexture.height, 0);
            GL.Vertex3(minUV.x * uvPreviewTexture.width, maxUV.y * uvPreviewTexture.height, 0);
            // 左辺
            GL.Vertex3(minUV.x * uvPreviewTexture.width, maxUV.y * uvPreviewTexture.height, 0);
            GL.Vertex3(minUV.x * uvPreviewTexture.width, minUV.y * uvPreviewTexture.height, 0);
            GL.End();
            
            GL.PopMatrix();
            RenderTexture.active = null;
        }
    }
}