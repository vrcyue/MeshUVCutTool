using System;
using UnityEditor;
using UnityEngine;

namespace vrc_yue.MeshUVCutTools.MeshModifyTool.UIComponents
{
    /// <summary>
    /// メッシュ作成オプションの階層的なUIパネル
    /// </summary>
    public class MeshCreationOptionsPanel
    {
        // 基本作成モード
        public enum BaseCreationMode
        {
            CreateSelectedOnly,      // 選択部分のみ作成
            CreateWithUnselected    // 選択範囲外のメッシュもあわせて作成
        }
        
        // ウェイト/ボーン設定
        public enum WeightBoneMode
        {
            KeepWeights,            // 通常（ボーンウェイトを保持）
            RemoveWeights,          // ボーンウェイトを削除
            CopyDependentBones      // メッシュが依存するボーンをコピー
        }
        
        // 高度なボーン設定
        public enum AdvancedBoneMode
        {
            IncludeAllParents,              // 全ての親ボーンを含める
            ExcludeParents                  // 親ボーンを除外
        }
        
        // 親ボーン除外時のルート位置設定
        public enum RootPositionMode
        {
            NoAdjustment,                   // 調整なし
            Center,                         // 中心位置
            TopY,                           // 最高Y座標
            BottomY                         // 最低Y座標
        }
        
        // 現在の選択状態
        private BaseCreationMode baseMode = BaseCreationMode.CreateSelectedOnly;
        private WeightBoneMode weightBoneMode = WeightBoneMode.KeepWeights;
        private AdvancedBoneMode advancedBoneMode = AdvancedBoneMode.IncludeAllParents;
        private RootPositionMode rootPositionMode = RootPositionMode.NoAdjustment;
        
        // UI状態
        private bool showWeightBoneOptions = true;
        private bool showAdvancedBoneOptions = false;
        private bool showRootPositionOptions = false;
        
        // イベント
        public event Action<BaseCreationMode, WeightBoneMode, AdvancedBoneMode, RootPositionMode> OnOptionChanged;
        
        /// <summary>
        /// 現在選択されている設定を取得
        /// </summary>
        public (BaseCreationMode baseMode, WeightBoneMode weightBoneMode, AdvancedBoneMode advancedBoneMode, RootPositionMode rootPositionMode) GetCurrentSelection()
        {
            return (baseMode, weightBoneMode, advancedBoneMode, rootPositionMode);
        }
        
        /// <summary>
        /// オプションパネルを描画
        /// </summary>
        public void DrawOptionsPanel()
        {
            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                EditorGUILayout.LabelField(Localization.Get("mesh_creation_mode"), EditorStyles.boldLabel);
                
                // 基本作成モード
                DrawBaseCreationMode();
                
                // ウェイト/ボーン設定（選択部分のみ作成の場合のみ表示）
                if (baseMode == BaseCreationMode.CreateSelectedOnly)
                {
                    EditorGUILayout.Space(5);
                    DrawWeightBoneSettings();
                }
            }
        }
        
        /// <summary>
        /// 基本作成モードを描画
        /// </summary>
        private void DrawBaseCreationMode()
        {
            EditorGUILayout.LabelField(Localization.Get("base_creation_mode"), EditorStyles.miniBoldLabel);
            
            using (new EditorGUILayout.VerticalScope())
            {
                // 選択部分のみ作成
                using (new EditorGUILayout.HorizontalScope())
                {
                    bool isSelected = EditorGUILayout.Toggle(baseMode == BaseCreationMode.CreateSelectedOnly, GUILayout.Width(20));
                    if (isSelected && baseMode != BaseCreationMode.CreateSelectedOnly)
                    {
                        baseMode = BaseCreationMode.CreateSelectedOnly;
                        NotifyOptionChanged();
                    }
                    EditorGUILayout.LabelField(Localization.Get("create_normal"));
                }
                
                // 選択範囲外のメッシュもあわせて作成
                using (new EditorGUILayout.HorizontalScope())
                {
                    bool isSelected = EditorGUILayout.Toggle(baseMode == BaseCreationMode.CreateWithUnselected, GUILayout.Width(20));
                    if (isSelected && baseMode != BaseCreationMode.CreateWithUnselected)
                    {
                        baseMode = BaseCreationMode.CreateWithUnselected;
                        NotifyOptionChanged();
                    }
                    EditorGUILayout.LabelField(Localization.Get("create_unselected"));
                }
            }
        }
        
        /// <summary>
        /// ウェイト/ボーン設定を描画
        /// </summary>
        private void DrawWeightBoneSettings()
        {
            showWeightBoneOptions = EditorGUILayout.Foldout(showWeightBoneOptions, Localization.Get("weight_bone_settings"));
            
            if (showWeightBoneOptions)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Space(20); // インデント
                    using (new EditorGUILayout.VerticalScope())
                    {
                        // 通常（ボーンウェイトを保持）
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            bool isSelected = EditorGUILayout.Toggle(weightBoneMode == WeightBoneMode.KeepWeights, GUILayout.Width(20));
                            if (isSelected && weightBoneMode != WeightBoneMode.KeepWeights)
                            {
                                weightBoneMode = WeightBoneMode.KeepWeights;
                                NotifyOptionChanged();
                            }
                            EditorGUILayout.LabelField(Localization.Get("keep_bone_weights"));
                        }
                        
                        // ボーンウェイトを削除
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            bool isSelected = EditorGUILayout.Toggle(weightBoneMode == WeightBoneMode.RemoveWeights, GUILayout.Width(20));
                            if (isSelected && weightBoneMode != WeightBoneMode.RemoveWeights)
                            {
                                weightBoneMode = WeightBoneMode.RemoveWeights;
                                NotifyOptionChanged();
                            }
                            EditorGUILayout.LabelField(Localization.Get("remove_bone_weights"));
                        }
                        
                        // メッシュが依存するボーンをコピー
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            bool isSelected = EditorGUILayout.Toggle(weightBoneMode == WeightBoneMode.CopyDependentBones, GUILayout.Width(20));
                            if (isSelected && weightBoneMode != WeightBoneMode.CopyDependentBones)
                            {
                                weightBoneMode = WeightBoneMode.CopyDependentBones;
                                NotifyOptionChanged();
                            }
                            EditorGUILayout.LabelField(Localization.Get("copy_used_bones"));
                        }
                        
                        // 高度なボーン設定（メッシュが依存するボーンをコピーの場合のみ）
                        if (weightBoneMode == WeightBoneMode.CopyDependentBones)
                        {
                            EditorGUILayout.Space(5);
                            DrawAdvancedBoneSettings();
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// 高度なボーン設定を描画
        /// </summary>
        private void DrawAdvancedBoneSettings()
        {
            showAdvancedBoneOptions = EditorGUILayout.Foldout(showAdvancedBoneOptions, Localization.Get("advanced_bone_settings"));
            
            if (showAdvancedBoneOptions)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Space(20); // さらにインデント
                    using (new EditorGUILayout.VerticalScope())
                    {
                        // 全ての親ボーンを含める
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            bool isSelected = EditorGUILayout.Toggle(advancedBoneMode == AdvancedBoneMode.IncludeAllParents, GUILayout.Width(20));
                            if (isSelected && advancedBoneMode != AdvancedBoneMode.IncludeAllParents)
                            {
                                advancedBoneMode = AdvancedBoneMode.IncludeAllParents;
                                NotifyOptionChanged();
                            }
                            EditorGUILayout.LabelField(Localization.Get("include_all_parent_bones"));
                        }
                        
                        // 親ボーンを除外
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            bool isSelected = EditorGUILayout.Toggle(advancedBoneMode == AdvancedBoneMode.ExcludeParents, GUILayout.Width(20));
                            if (isSelected && advancedBoneMode != AdvancedBoneMode.ExcludeParents)
                            {
                                advancedBoneMode = AdvancedBoneMode.ExcludeParents;
                                NotifyOptionChanged();
                            }
                            EditorGUILayout.LabelField(Localization.Get("exclude_parent_bones"));
                        }
                        
                        // 親ボーン除外時のルート位置設定
                        if (advancedBoneMode == AdvancedBoneMode.ExcludeParents)
                        {
                            EditorGUILayout.Space(5);
                            DrawRootPositionSettings();
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// ルート位置設定を描画
        /// </summary>
        private void DrawRootPositionSettings()
        {
            showRootPositionOptions = EditorGUILayout.Foldout(showRootPositionOptions, Localization.Get("root_position_settings"));
            
            if (showRootPositionOptions)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Space(20); // さらにインデント
                    using (new EditorGUILayout.VerticalScope())
                    {
                        // 調整なし
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            bool isSelected = EditorGUILayout.Toggle(rootPositionMode == RootPositionMode.NoAdjustment, GUILayout.Width(20));
                            if (isSelected && rootPositionMode != RootPositionMode.NoAdjustment)
                            {
                                rootPositionMode = RootPositionMode.NoAdjustment;
                                NotifyOptionChanged();
                            }
                            EditorGUILayout.LabelField(Localization.Get("root_no_adjustment"));
                        }
                        
                        // 中心位置
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            bool isSelected = EditorGUILayout.Toggle(rootPositionMode == RootPositionMode.Center, GUILayout.Width(20));
                            if (isSelected && rootPositionMode != RootPositionMode.Center)
                            {
                                rootPositionMode = RootPositionMode.Center;
                                NotifyOptionChanged();
                            }
                            EditorGUILayout.LabelField(Localization.Get("root_center_position"));
                        }
                        
                        // 最高Y座標
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            bool isSelected = EditorGUILayout.Toggle(rootPositionMode == RootPositionMode.TopY, GUILayout.Width(20));
                            if (isSelected && rootPositionMode != RootPositionMode.TopY)
                            {
                                rootPositionMode = RootPositionMode.TopY;
                                NotifyOptionChanged();
                            }
                            EditorGUILayout.LabelField(Localization.Get("root_top_y"));
                        }
                        
                        // 最低Y座標
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            bool isSelected = EditorGUILayout.Toggle(rootPositionMode == RootPositionMode.BottomY, GUILayout.Width(20));
                            if (isSelected && rootPositionMode != RootPositionMode.BottomY)
                            {
                                rootPositionMode = RootPositionMode.BottomY;
                                NotifyOptionChanged();
                            }
                            EditorGUILayout.LabelField(Localization.Get("root_bottom_y"));
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// オプション変更を通知
        /// </summary>
        private void NotifyOptionChanged()
        {
            OnOptionChanged?.Invoke(baseMode, weightBoneMode, advancedBoneMode, rootPositionMode);
        }
        
        /// <summary>
        /// 現在の設定から従来のMeshCreateOptionに変換
        /// </summary>
        public static MeshModifyTool.MeshCreateOption ConvertToLegacyOption(
            BaseCreationMode baseMode,
            WeightBoneMode weightBoneMode,
            AdvancedBoneMode advancedBoneMode,
            RootPositionMode rootPositionMode)
        {
            // 選択範囲外のメッシュもあわせて作成
            if (baseMode == BaseCreationMode.CreateWithUnselected)
            {
                return MeshModifyTool.MeshCreateOption.CreateInverse;
            }
            
            // 選択部分のみ作成の場合
            switch (weightBoneMode)
            {
                case WeightBoneMode.RemoveWeights:
                    return MeshModifyTool.MeshCreateOption.RemoveBoneWeights;
                    
                case WeightBoneMode.CopyDependentBones:
                    if (advancedBoneMode == AdvancedBoneMode.IncludeAllParents)
                    {
                        return MeshModifyTool.MeshCreateOption.CopyUsedBones;
                    }
                    else // ExcludeParents
                    {
                        switch (rootPositionMode)
                        {
                            case RootPositionMode.NoAdjustment:
                                return MeshModifyTool.MeshCreateOption.CopyUsedBonesWithoutParent;
                            case RootPositionMode.Center:
                                return MeshModifyTool.MeshCreateOption.CopyUsedBonesWithoutParentAdjusted;
                            case RootPositionMode.TopY:
                                return MeshModifyTool.MeshCreateOption.CopyUsedBonesWithoutParentTopY;
                            case RootPositionMode.BottomY:
                                return MeshModifyTool.MeshCreateOption.CopyUsedBonesWithoutParentBottomY;
                        }
                    }
                    break;
                    
                case WeightBoneMode.KeepWeights:
                default:
                    return MeshModifyTool.MeshCreateOption.Normal;
            }
            
            return MeshModifyTool.MeshCreateOption.Normal;
        }
        
        /// <summary>
        /// 従来のMeshCreateOptionから現在の設定に変換
        /// </summary>
        public void SetFromLegacyOption(MeshModifyTool.MeshCreateOption legacyOption)
        {
            switch (legacyOption)
            {
                case MeshModifyTool.MeshCreateOption.Normal:
                    baseMode = BaseCreationMode.CreateSelectedOnly;
                    weightBoneMode = WeightBoneMode.KeepWeights;
                    break;
                    
                case MeshModifyTool.MeshCreateOption.CreateInverse:
                    baseMode = BaseCreationMode.CreateWithUnselected;
                    break;
                    
                case MeshModifyTool.MeshCreateOption.RemoveBoneWeights:
                    baseMode = BaseCreationMode.CreateSelectedOnly;
                    weightBoneMode = WeightBoneMode.RemoveWeights;
                    break;
                    
                case MeshModifyTool.MeshCreateOption.CopyUsedBones:
                    baseMode = BaseCreationMode.CreateSelectedOnly;
                    weightBoneMode = WeightBoneMode.CopyDependentBones;
                    advancedBoneMode = AdvancedBoneMode.IncludeAllParents;
                    break;
                    
                case MeshModifyTool.MeshCreateOption.CopyUsedBonesWithoutParent:
                    baseMode = BaseCreationMode.CreateSelectedOnly;
                    weightBoneMode = WeightBoneMode.CopyDependentBones;
                    advancedBoneMode = AdvancedBoneMode.ExcludeParents;
                    rootPositionMode = RootPositionMode.NoAdjustment;
                    break;
                    
                case MeshModifyTool.MeshCreateOption.CopyUsedBonesWithoutParentAdjusted:
                    baseMode = BaseCreationMode.CreateSelectedOnly;
                    weightBoneMode = WeightBoneMode.CopyDependentBones;
                    advancedBoneMode = AdvancedBoneMode.ExcludeParents;
                    rootPositionMode = RootPositionMode.Center;
                    break;
                    
                case MeshModifyTool.MeshCreateOption.CopyUsedBonesWithoutParentTopY:
                    baseMode = BaseCreationMode.CreateSelectedOnly;
                    weightBoneMode = WeightBoneMode.CopyDependentBones;
                    advancedBoneMode = AdvancedBoneMode.ExcludeParents;
                    rootPositionMode = RootPositionMode.TopY;
                    break;
                    
                case MeshModifyTool.MeshCreateOption.CopyUsedBonesWithoutParentBottomY:
                    baseMode = BaseCreationMode.CreateSelectedOnly;
                    weightBoneMode = WeightBoneMode.CopyDependentBones;
                    advancedBoneMode = AdvancedBoneMode.ExcludeParents;
                    rootPositionMode = RootPositionMode.BottomY;
                    break;
            }
        }
    }
}