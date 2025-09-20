using System.Collections.Generic;
using UnityEditor;

namespace vrc_yue.MeshUVCutTools.MeshModifyTool
{
    public static class Localization
    {
        public enum Language
        {
            Japanese,
            English
        }

        private static Language currentLanguage = Language.Japanese;
        
        public static Language CurrentLanguage 
        { 
            get => currentLanguage;
            set
            {
                currentLanguage = value;
                EditorPrefs.SetInt("MeshModifyTool_Language", (int)value);
            }
        }

        static Localization()
        {
            // EditorPrefsから言語設定を読み込み
            currentLanguage = (Language)EditorPrefs.GetInt("MeshModifyTool_Language", 0);
        }

        private static Dictionary<string, Dictionary<Language, string>> texts = new Dictionary<string, Dictionary<Language, string>>
        {
            // Window Title
            ["window_title"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "ちゅんちゅんメッシュ＆UVカッター",
                [Language.English] = "ChunChun Mesh & UV Cutter"
            },
            ["window_description"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "Unityだけでアバターのメッシュ分割とUV編集ができるツールです",
                [Language.English] = "Tool for avatar mesh splitting and UV editing directly in Unity"
            },
            
            // Main Sections
            ["section_mesh_edit"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "メッシュ編集",
                [Language.English] = "Mesh Editing"
            },
            ["section_uv_edit"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "UV編集",
                [Language.English] = "UV Editing"
            },
            ["section_save"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "一時的に作成したメッシュを保存",
                [Language.English] = "Save Temporarily Created Mesh"
            },
            
            // Mesh Selection
            ["mesh_select"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "メッシュ選択",
                [Language.English] = "Mesh Selection"
            },
            ["backface_culling"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "ドラッグ時、表面のメッシュのみ選択",
                [Language.English] = "Select only front-facing mesh when dragging"
            },
            ["select_overlapping"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "一括選択時、重複頂点も選択（実行に時間がかかる場合があります）",
                [Language.English] = "Include overlapping vertices in batch selection (May take time to execute)"
            },
            
            // Buttons
            ["btn_select_all"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "すべて選択",
                [Language.English] = "Select All"
            },
            ["btn_select_none"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "全選択解除",
                [Language.English] = "Deselect All"
            },
            ["btn_revert_select"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "選択反転",
                [Language.English] = "Invert Selection"
            },
            ["btn_create_mesh"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "選択頂点のメッシュを作成（一時的）",
                [Language.English] = "Create Mesh from Selected Vertices (Temporary)"
            },
            ["temp_mesh_description"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "一時的なメッシュはこのウィンドウを閉じると削除されます\n下の「一時的に作成したメッシュを保存」を押すと、アセットとして保存されます",
                [Language.English] = "Temporary meshes will be deleted when this window is closed\nPress \"Save Temporarily Created Mesh\" below to save as assets"
            },
            ["btn_remove_mesh_ndmf"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "選択頂点を削除（NDMF非破壊削除）",
                [Language.English] = "Remove Selected (NDMF Non-Destructive)"
            },
            ["btn_back"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "戻る",
                [Language.English] = "Back"
            },
            
            // Create Mesh Options
            ["create_mesh"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "メッシュ編集",
                [Language.English] = "Mesh Edit"
            },
            ["mesh_create_option"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "メッシュ作成オプション",
                [Language.English] = "Mesh Creation Options"
            },
            ["create_option"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "作成オプション",
                [Language.English] = "Creation Option"
            },
            ["create_normal"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "選択部分のみ",
                [Language.English] = "Selected Only"
            },
            ["mesh_edit_option"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "メッシュ選択オプション",
                [Language.English] = "Mesh Selection Options"
            },
            ["normal_offset"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "頂点を押し出し",
                [Language.English] = "Push Vertices Along Normals"
            },
            ["create_unselected"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "選択範囲外も含めて作成",
                [Language.English] = "Include Unselected"
            },
            ["remove_bone_weights"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "ボーンウェイトを削除（MeshRenderer出力）",
                [Language.English] = "Remove Bone Weights (MeshRenderer)"
            },
            ["copy_used_bones"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "依存するボーンをコピー",
                [Language.English] = "Copy Dependent Bones"
            },
            ["copy_used_bones_without_parent"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "使用ボーンのみ（親を除く）",
                [Language.English] = "Used Bones Only (Exclude Parents)"
            },
            ["copy_used_bones_without_parent_adjusted"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "使用ボーンのみ（親を除く・ルート位置調整）",
                [Language.English] = "Used Bones Only (Exclude Parents, Adjust Root)"
            },
            ["copy_used_bones_without_parent_top_y"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "使用ボーンのみ（親を除く・最高Y座標）",
                [Language.English] = "Used Bones Only (Exclude Parents, Top Y)"
            },
            ["copy_used_bones_without_parent_bottom_y"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "使用ボーンのみ（親を除く・最低Y座標）",
                [Language.English] = "Used Bones Only (Exclude Parents, Bottom Y)"
            },
            ["show_camera_view"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "独自カメラビューを表示",
                [Language.English] = "Show Custom Camera View"
            },
            
            // UV Edit
            ["texture_preview"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "テクスチャプレビュー",
                [Language.English] = "Texture Preview"
            },
            ["uv_texture_edit"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "UVテクスチャ編集",
                [Language.English] = "UV & Texture Editing"
            },
            ["uv_fit"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "UVフィット（0-1範囲に収める）",
                [Language.English] = "Fit UV to 0-1 Range"
            },
            ["uv_reconstruct"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "UV再構築（実験的機能）",
                [Language.English] = "Rebuild UV (Experimental)"
            },
            ["uv_adjust"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "UV調整（実験的機能）",
                [Language.English] = "UV Adjustments (Experimental)"
            },
            ["flip_horizontal"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "左右反転",
                [Language.English] = "Flip Horizontal"
            },
            ["flip_vertical"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "上下反転",
                [Language.English] = "Flip Vertical"
            },
            ["rotate_90"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "90度回転",
                [Language.English] = "Rotate 90°"
            },
            ["rotate_minus_90"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "-90度回転",
                [Language.English] = "Rotate -90°"
            },
            
            // Save
            ["save_suffix"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "保存時の接尾辞",
                [Language.English] = "Save Suffix"
            },
            ["save_selected"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "一時的に作成したメッシュを保存",
                [Language.English] = "Save Temporarily Created Mesh"
            },
            ["save_description"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "メッシュに関係するテクスチャやマテリアルも保存されます\n保存場所は元のメッシュ、テクスチャ、マテリアルがあった場所と同一です",
                [Language.English] = "Related textures and materials will also be saved\nFiles will be saved in the same location as the original mesh, textures, and materials"
            },
            ["unsaved"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "● 未保存",
                [Language.English] = "● Unsaved"
            },
            
            // UI Settings
            ["ui_settings"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "UI設定 & デバッグ",
                [Language.English] = "UI Settings & Debug"
            },
            ["selected_vertices"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "選択頂点数:",
                [Language.English] = "Selected Vertices:"
            },
            ["not_selected_vertices"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "非選択頂点数:",
                [Language.English] = "Not Selected Vertices:"
            },
            
            // Messages
            ["msg_select_preview"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "メッシュを選択するとテクスチャプレビューが表示されます",
                [Language.English] = "Select a mesh to display texture preview"
            },
            ["msg_saved_object_warning"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "保存済みのオブジェクトが選択されています。\n元のオブジェクトを上書き変更する場合のみ操作してください",
                [Language.English] = "A saved object is selected.\nProceed only if you want to overwrite the original object"
            },
            ["msg_save_complete"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "選択したアセットを保存しました",
                [Language.English] = "Successfully saved selected assets"
            },
            ["msg_asset_saved"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "アセットの保存",
                [Language.English] = "Save Assets"
            },
            
            // Additional UI elements
            ["wire_frame_color"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "ワイヤーフレーム色",
                [Language.English] = "Wire Frame Color"
            },
            ["normal"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "法線表示",
                [Language.English] = "Normal Display"
            },
            ["uv"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "UV表示",
                [Language.English] = "UV Display"
            },
            ["debug"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "デバッグ",
                [Language.English] = "Debug"
            },
            ["msg_select_mesh_prompt"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "以下のメッシュをクリックして編集を開始してください",
                [Language.English] = "Click on a mesh below to start editing"
            },
            ["btn_select_removal_vertices_count"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "削除対象頂点選択 ({0}頂点)",
                [Language.English] = "Select Removal Vertices ({0})"
            },
            ["language_label"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "Change Language / 言語変更:",
                [Language.English] = "Change Language / 言語変更:"
            },
            ["msg_removal_registered"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "削除情報を登録しました",
                [Language.English] = "Removal Info Registered"
            },
            ["msg_removal_info"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "ビルド時に以下の頂点が削除されます:",
                [Language.English] = "The following vertices will be removed during build:"
            },
            ["active_avatars_description"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "シーン内のアクティブなアバターを選択してすぐに編集を開始できます",
                [Language.English] = "Select an active avatar in the scene to start editing immediately"
            },
            
            // 新しい削除データUI関連
            ["removal_data_section"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "削除データ ({0} 項目)",
                [Language.English] = "Removal Data ({0} items)"
            },
            ["removal_data_select_mesh"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "削除データ ({0} 項目) - メッシュを選択してください",
                [Language.English] = "Removal Data ({0} items) - Please select the mesh"
            },
            ["btn_enable"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "[ON]",
                [Language.English] = "[ON]"
            },
            ["btn_disable"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "[OFF]",
                [Language.English] = "[OFF]"
            },
            ["btn_select_vertices"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = ">> 頂点を選択 ({0}個)",
                [Language.English] = ">> Select Vertices ({0})"
            },
            ["btn_add_to_merge"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "統合対象にする",
                [Language.English] = "Add to Merge"
            },
            ["btn_remove_from_merge"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "統合対象から外す",
                [Language.English] = "Remove from Merge"
            },
            ["btn_merge_selected"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "選択した{0}個を統合",
                [Language.English] = "Merge {0} Selected"
            },
            ["toggle_enable_disable"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "クリックして有効/無効を切り替え",
                [Language.English] = "Click to toggle enable/disable"
            },
            ["btn_delete"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "削除",
                [Language.English] = "Delete"
            },
            ["btn_cancel"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "キャンセル",
                [Language.English] = "Cancel"
            },
            ["confirm_delete_title"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "削除の確認",
                [Language.English] = "Confirm Delete"
            },
            ["confirm_delete_message"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "この削除データを削除してもよろしいですか？\nこの操作は元に戻せません。",
                [Language.English] = "Are you sure you want to delete this removal data?\nThis action cannot be undone."
            },
            ["msg_unsaved_mesh_warning"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "新しい一時メッシュを作成する場合は未保存メッシュを保存するか削除してください",
                [Language.English] = "To create a new temporary mesh, please save or delete the unsaved mesh"
            },
            ["msg_mesh_selection_mode"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "メッシュ選択モード中",
                [Language.English] = "Mesh Selection Mode Active"
            },
            ["msg_gizmo_warning"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "※ Gizmo等の操作ができない場合があります",
                [Language.English] = "※ Gizmo operations may be disabled"
            },
            ["btn_delete_temp_mesh"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "削除",
                [Language.English] = "Delete"
            },
            
            // 新しいメッシュ作成オプションUI用
            ["mesh_creation_mode"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "メッシュ作成モード",
                [Language.English] = "Mesh Creation Mode"
            },
            ["base_creation_mode"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "基本作成モード",
                [Language.English] = "Base Creation Mode"
            },
            ["weight_bone_settings"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "ウェイト/ボーン設定",
                [Language.English] = "Weight/Bone Settings"
            },
            ["advanced_bone_settings"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "高度なボーン設定",
                [Language.English] = "Advanced Bone Settings"
            },
            ["keep_bone_weights"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "通常（ボーンウェイトを保持）",
                [Language.English] = "Normal (Keep Bone Weights)"
            },
            ["include_all_parent_bones"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "親ボーンを含めてボーン階層全体をコピー",
                [Language.English] = "Copy entire bone hierarchy including parent bones"
            },
            ["exclude_parent_bones"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "親ボーンを除外（実際に使用されているボーンのみコピー）",
                [Language.English] = "Exclude parent bones (copy only actually used bones)"
            },
            ["root_position_settings"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "ボーンの位置設定",
                [Language.English] = "Bone Position Settings"
            },
            ["root_no_adjustment"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "位置調整なし（元の位置を維持）",
                [Language.English] = "No adjustment (keep original position)"
            },
            ["root_center_position"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "メッシュ境界ボックスの中心に配置",
                [Language.English] = "Place at mesh bounding box center"
            },
            ["root_top_y"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "メッシュの最高Y座標位置に配置",
                [Language.English] = "Place at mesh top Y position"
            },
            ["root_bottom_y"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "メッシュの最低Y座標位置に配置",
                [Language.English] = "Place at mesh bottom Y position"
            },
            
            // Dialog texts
            ["dialog_title_warning"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "警告",
                [Language.English] = "Warning"
            },
            ["dialog_ok"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "OK",
                [Language.English] = "OK"
            },
            ["dialog_cancel"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "キャンセル",
                [Language.English] = "Cancel"
            }
        };

        public static string Get(string key)
        {
            if (texts.ContainsKey(key) && texts[key].ContainsKey(currentLanguage))
            {
                return texts[key][currentLanguage];
            }
            return key; // キーが見つからない場合はキーをそのまま返す
        }
    }
}