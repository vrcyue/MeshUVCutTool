using UnityEngine;

namespace vrc_yue.MeshUVCutTools.MeshModifyTool.Models
{
    /// <summary>
    /// テクスチャ生成時の設定を保持するクラス
    /// </summary>
    [System.Serializable]
    public class TextureSettings
    {
        /// <summary>
        /// 新しいテクスチャのサイズ
        /// </summary>
        public Vector2Int size = new Vector2Int(1024, 1024);

        /// <summary>
        /// テクスチャのパディング（ピクセル単位）
        /// </summary>
        public int padding = 1;

        /// <summary>
        /// 背景色
        /// </summary>
        public Color backgroundColor = Color.clear;

        /// <summary>
        /// アスペクト比を維持するかどうか
        /// </summary>
        public bool maintainAspectRatio = true;

        /// <summary>
        /// テクスチャフォーマット
        /// </summary>
        public TextureFormat format = TextureFormat.RGBA32;

        /// <summary>
        /// ミップマップを生成するかどうか
        /// </summary>
        public bool generateMipMaps = true;

        /// <summary>
        /// テクスチャの圧縮設定
        /// </summary>
        public bool compressed = true;

        /// <summary>
        /// 設定値を検証し、必要に応じて調整する
        /// </summary>
        public void Validate()
        {
            // テクスチャサイズは2の累乗に制限
            size.x = Mathf.ClosestPowerOfTwo(size.x);
            size.y = Mathf.ClosestPowerOfTwo(size.y);

            // 最小サイズを設定
            size.x = Mathf.Max(size.x, 32);
            size.y = Mathf.Max(size.y, 32);

            // パディングの制限
            padding = Mathf.Clamp(padding, 0, 16);
        }
    }
}
