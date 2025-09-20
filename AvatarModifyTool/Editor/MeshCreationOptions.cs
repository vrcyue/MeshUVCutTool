/*
MeshUVCutTool
https://github.com/vrcyue/MeshUVCutTool

Copyright (c) 2025 @vrcyue

This software is released under the MIT License.
http://opensource.org/licenses/mit-license.php

以下のコードをベースに実装

AvatarModifyTools
https://github.com/HhotateA/AvatarModifyTools

Copyright (c) 2021 @HhotateA_xR

This software is released under the MIT License.
http://opensource.org/licenses/mit-license.php
*/
namespace vrc_yue.MeshUVCutTools.Core
{
    /// <summary>
    /// メッシュ作成時のオプションを管理するクラス
    /// </summary>
    public class MeshCreationOptions
    {
        /// <summary>
        /// 選択範囲を反転するかどうか（選択されていない部分を作成）
        /// </summary>
        public bool Inverse { get; set; } = false;

        /// <summary>
        /// ボーンウェイトを削除するかどうか
        /// </summary>
        public bool RemoveBoneWeights { get; set; } = false;

        /// <summary>
        /// 使用されているボーンのみをコピーするかどうか
        /// </summary>
        public bool CopyUsedBonesOnly { get; set; } = false;

        /// <summary>
        /// 親ボーンを除外するかどうか
        /// </summary>
        public bool ExcludeParentBones { get; set; } = false;

        /// <summary>
        /// ボーンルートの調整方法
        /// </summary>
        public BoneRootAdjustment RootAdjustment { get; set; } = BoneRootAdjustment.None;

        /// <summary>
        /// デフォルトコンストラクタ
        /// </summary>
        public MeshCreationOptions()
        {
        }

        /// <summary>
        /// 既存のbool引数から変換するコンストラクタ（互換性のため）
        /// </summary>
        public MeshCreationOptions(bool inverse = false, bool removeBoneWeights = false, 
            bool copyUsedBonesOnly = false, bool excludeParentBones = false, 
            bool adjustBoneRoot = false)
        {
            Inverse = inverse;
            RemoveBoneWeights = removeBoneWeights;
            CopyUsedBonesOnly = copyUsedBonesOnly;
            ExcludeParentBones = excludeParentBones;
            RootAdjustment = adjustBoneRoot ? BoneRootAdjustment.Center : BoneRootAdjustment.None;
        }
    }

    /// <summary>
    /// ボーンルートの調整方法
    /// </summary>
    public enum BoneRootAdjustment
    {
        /// <summary>
        /// 調整なし
        /// </summary>
        None,

        /// <summary>
        /// 頂点の中心位置に配置
        /// </summary>
        Center,

        /// <summary>
        /// 最も高いY座標に配置
        /// </summary>
        TopY,

        /// <summary>
        /// 最も低いY座標に配置
        /// </summary>
        BottomY
    }
}