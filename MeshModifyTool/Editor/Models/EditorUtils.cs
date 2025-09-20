using UnityEngine;

namespace vrc_yue.MeshUVCutTools.MeshModifyTool.Models
{
    public static class EditorUtils
    {
        public const string TEMP_SUFFIX = "_Temp";

        /// <summary>
        /// 文字列に_Tempサフィックスを付与します（既に付いている場合は付与しません）
        /// </summary>
        public static string EnsureSuffix(string name)
        {
            if (string.IsNullOrEmpty(name)) return TEMP_SUFFIX;
            return name.EndsWith(TEMP_SUFFIX) ? name : name + TEMP_SUFFIX;
        }

        /// <summary>
        /// 一時ファイル名から新しいファイル名を生成します
        /// </summary>
        /// <param name="tempName">一時ファイル名</param>
        /// <param name="newSuffix">新しいサフィックス</param>
        /// <returns>新しいファイル名</returns>
        public static string ReplaceSuffix(string tempName, string newSuffix)
        {
            if (string.IsNullOrEmpty(tempName)) return newSuffix;
            if (string.IsNullOrEmpty(newSuffix)) return tempName;

            // 既に新しいサフィックスが付いている場合はそのまま返す
            if (tempName.EndsWith(newSuffix))
            {
                return tempName;
            }

            // _Tempサフィックスを削除
            string baseName = tempName;
            if (tempName.EndsWith(TEMP_SUFFIX))
            {
                baseName = tempName.Substring(0, tempName.Length - TEMP_SUFFIX.Length);
            }

            // 新しいサフィックスを付与
            return baseName + newSuffix;
        }
    }
}
