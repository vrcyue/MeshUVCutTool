using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using vrc_yue.MeshUVCutTools.MeshModifyTool.Models;
using vrc_yue.MeshUVCutTools.MeshModifyTool;
using nadena.dev.modular_avatar.core;
using nadena.dev.modular_avatar.core.vertex_filters;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace vrc_yue.MeshUVCutTools.MeshModifyTool.UIComponents
{
    /// <summary>
    /// メッシュリストパネル
    /// </summary>
    public class MeshListPanel
    {
        private Vector2 rendsScroll = Vector2.zero;
        private List<MUCTMeshRemovalData> selectedForMerge = new List<MUCTMeshRemovalData>();
        
        public event Action<int> OnMeshSelected;
        public event Action<int, bool> OnMeshVisibilityChanged;
        public event Action<int, MUCTMeshRemovalData, int> OnRemovalDataVerticesButtonClicked;
        public event Action<MUCTMeshRemovalData, bool> OnRemovalDataEnabledChanged;
        public event Action<List<MUCTMeshRemovalData>> OnMergeRemovalDataRequested;
        public event Action<MUCTMeshRemovalData> OnRemovalDataDeleteRequested;
        public event Action<MUCTMeshRemovalData> OnSetupModularAvatarRequested;
        public event Action<MUCTMeshRemovalData> OnRemoveModularAvatarRequested;
        public event Action<Renderer> OnTemporaryMeshDeleted;
        
        /// <summary>
        /// メッシュリストを描画
        /// </summary>
        public void DrawMeshList(Renderer[] renderers, int selectedIndex, TemporaryAssetManager assetManager)
        {
            if (renderers == null || renderers.Length == 0)
            {
                return; // renderersがnullまたは空の場合は何も描画しない
            }
            
            EditorGUILayout.LabelField(Localization.Get("msg_select_mesh_prompt"), EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.Space(5);
            
            rendsScroll = EditorGUILayout.BeginScrollView(rendsScroll, false, false, GUIStyle.none,
                GUI.skin.verticalScrollbar, GUI.skin.scrollView);
            
            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                for (int i = 0; i < renderers.Length; i++)
                {
                    if (renderers[i] != null)
                    {
                        DrawMeshItem(renderers[i], i, selectedIndex, assetManager);
                    }
                }
            }
            
            EditorGUILayout.EndScrollView();
        }
        
        private void DrawMeshItem(Renderer renderer, int index, int selectedIndex, TemporaryAssetManager assetManager)
        {
            // レンダラーがnullの場合は何も描画しない
            if (renderer == null) return;
            // 元のUIスタイル（メッシュ選択部分）
            using (new EditorGUILayout.HorizontalScope())
            {
                // 可視性トグル
                bool isActive = renderer.gameObject.activeSelf;
                bool newActive = EditorGUILayout.Toggle("", isActive, GUILayout.Width(20));
                if (newActive != isActive)
                {
                    OnMeshVisibilityChanged?.Invoke(index, newActive);
                }
                
                // 選択ボタン（選択中でもクリック可能にしてトグル機能を有効化）
                bool isSelected = selectedIndex == index;
                
                // 選択中のメッシュは背景色で表示
                Color originalColor = GUI.backgroundColor;
                if (isSelected)
                {
                    GUI.backgroundColor = new Color(0.3f, 0.6f, 1f, 1f); // 青っぽい背景色
                }
                
                string buttonText = isSelected ? $"▶ {renderer.name}" : renderer.name;
                if (GUILayout.Button(buttonText, GUILayout.Width(200)))
                {
                    OnMeshSelected?.Invoke(index);
                }
                
                GUI.backgroundColor = originalColor;
                
                // 未保存の表示と削除ボタン
                // 直接のマッチまたは親がRelatedGameObjectの場合をチェック
                var tempMesh = assetManager.TempMeshes.FirstOrDefault(m => 
                    m.RelatedGameObject == renderer.gameObject || 
                    (renderer.transform.parent != null && m.RelatedGameObject == renderer.transform.parent.gameObject));
                    
                if (tempMesh != null)
                {
                    GUILayout.Label(Localization.Get("unsaved"));
                    
                    // 選択中のメッシュの場合のみ削除ボタンを表示
                    if (selectedIndex == index)
                    {
                        GUI.backgroundColor = Color.red;
                        if (GUILayout.Button(Localization.Get("btn_delete_temp_mesh"), GUILayout.Width(50)))
                        {
                            // ゲームオブジェクトを削除
                            if (tempMesh.RelatedGameObject != null)
                            {
                                UnityEngine.Object.DestroyImmediate(tempMesh.RelatedGameObject);
                            }
                            
                            // 一時アセットから削除
                            assetManager.RemoveTemporaryMesh(tempMesh);
                            
                            // イベントを発火（削除前にレンダラーを渡す）
                            OnTemporaryMeshDeleted?.Invoke(renderer);
                        }
                        GUI.backgroundColor = Color.white;
                    }
                }
            }
            
            // 削除データ部分（rendererがnullでないことを確認）
            if (renderer == null) return;
            var removalDataComponents = renderer.GetComponents<MUCTMeshRemovalData>();
            if (removalDataComponents != null && removalDataComponents.Length > 0)
            {
                // 有効な削除データの合計を計算
                int totalRemovalData = 0;
                foreach (var data in removalDataComponents)
                {
                    if (data != null && data.removalInfos.Count > 0)
                    {
                        totalRemovalData += data.removalInfos.Count;
                    }
                }
                
                if (totalRemovalData > 0)
                {
                    EditorGUI.indentLevel++;
                    
                    // 選択されているメッシュの場合のみ削除データを表示
                    if (selectedIndex == index)
                    {
                        // フォールドアウトなしで直接表示
                        EditorGUILayout.LabelField(string.Format(Localization.Get("removal_data_section"), totalRemovalData), EditorStyles.boldLabel);
                        DrawRemovalDataList(removalDataComponents, renderer, index);
                    }
                    else
                    {
                        // 選択されていない場合はグレーアウトして表示
                        using (new EditorGUI.DisabledScope(true))
                        {
                            EditorGUILayout.LabelField(string.Format(Localization.Get("removal_data_select_mesh"), totalRemovalData), EditorStyles.miniLabel);
                        }
                    }
                    
                    EditorGUI.indentLevel--;
                }
            }
        }
        
        private void DrawRemovalDataList(MUCTMeshRemovalData[] removalDataComponents, Renderer renderer, int meshIndex)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                foreach (var removalData in removalDataComponents)
                {
                    if (removalData != null && removalData.removalInfos.Count > 0)
                    {
                        for (int infoIndex = 0; infoIndex < removalData.removalInfos.Count; infoIndex++)
                        {
                            var info = removalData.removalInfos[infoIndex];
                            if (info != null && info.targetRenderer == renderer && info.verticesToRemove.Count > 0)
                            {
                                // ModularAvatarモード切り替え
                                using (new EditorGUILayout.HorizontalScope(GUI.skin.box))
                                {
                                    EditorGUILayout.LabelField(L("削除方法:", "Removal Method:"), GUILayout.Width(80));
                                    
                                    if (!removalData.UseModularAvatar)
                                    {
                                        EditorGUILayout.LabelField(L("通常削除モード", "Normal Delete Mode"), EditorStyles.miniLabel);
                                        if (GUILayout.Button(L("MA連携を有効化", "Enable MA Integration"), GUILayout.Width(120)))
                                        {
                                            OnSetupModularAvatarRequested?.Invoke(removalData);
                                        }
                                    }
                                    else
                                    {
                                        EditorGUILayout.LabelField(L("MA連携モード", "MA Integration Mode"), EditorStyles.miniLabel);
                                        if (GUILayout.Button(L("通常削除に戻す", "Return to Normal"), GUILayout.Width(120)))
                                        {
                                            OnRemoveModularAvatarRequested?.Invoke(removalData);
                                        }
                                    }
                                }
                                
                                EditorGUILayout.Space(2);
                                
                                using (new EditorGUILayout.HorizontalScope())
                                {
                                    // [ON/OFF]ボタン
                                    bool isEnabled = removalData.enabled;
                                    string enabledText = isEnabled ? Localization.Get("btn_enable") : Localization.Get("btn_disable");
                                    GUI.backgroundColor = isEnabled ? new Color(0.5f, 1f, 0.5f) : Color.gray;
                                    if (GUILayout.Button(new GUIContent(enabledText, Localization.Get("toggle_enable_disable")), GUILayout.Width(50)))
                                    {
                                        OnRemovalDataEnabledChanged?.Invoke(removalData, !isEnabled);
                                    }
                                    GUI.backgroundColor = Color.white;
                                    
                                    // >> 頂点を選択ボタン
                                    string selectButtonText = string.Format(Localization.Get("btn_select_vertices"), info.verticesToRemove.Count);
                                    if (GUILayout.Button(selectButtonText, GUILayout.Width(180)))
                                    {
                                        OnRemovalDataVerticesButtonClicked?.Invoke(meshIndex, removalData, infoIndex);
                                    }
                                    
                                    GUILayout.FlexibleSpace();
                                    
                                    // 統合対象ボタン
                                    bool isSelectedForMerge = selectedForMerge.Contains(removalData);
                                    string mergeButtonText = isSelectedForMerge ? Localization.Get("btn_remove_from_merge") : Localization.Get("btn_add_to_merge");
                                    GUI.backgroundColor = isSelectedForMerge ? Color.yellow : Color.white;
                                    if (GUILayout.Button(mergeButtonText, GUILayout.Width(120)))
                                    {
                                        if (isSelectedForMerge)
                                        {
                                            selectedForMerge.Remove(removalData);
                                        }
                                        else
                                        {
                                            selectedForMerge.Add(removalData);
                                        }
                                    }
                                    GUI.backgroundColor = Color.white;
                                    
                                    // 削除ボタン
                                    GUI.backgroundColor = Color.red;
                                    if (GUILayout.Button(Localization.Get("btn_delete"), GUILayout.Width(50)))
                                    {
                                        if (EditorUtility.DisplayDialog(
                                            Localization.Get("confirm_delete_title"),
                                            Localization.Get("confirm_delete_message"),
                                            Localization.Get("btn_delete"),
                                            Localization.Get("btn_cancel")))
                                        {
                                            OnRemovalDataDeleteRequested?.Invoke(removalData);
                                            // 統合対象リストからも削除
                                            selectedForMerge.Remove(removalData);
                                        }
                                    }
                                    GUI.backgroundColor = Color.white;
                                }
                            }
                        }
                    }
                }
                
                // ローカライズメソッド
                string L(string ja, string en)
                {
                    return Application.systemLanguage == SystemLanguage.Japanese ? ja : en;
                }
                
                // 統合ボタン（フォールドアウト内の最下部）
                if (selectedForMerge.Count >= 2)
                {
                    EditorGUILayout.Space(5);
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.FlexibleSpace();
                        string mergeButtonText = string.Format(Localization.Get("btn_merge_selected"), selectedForMerge.Count);
                        if (GUILayout.Button(mergeButtonText, GUILayout.Width(150)))
                        {
                            OnMergeRemovalDataRequested?.Invoke(new List<MUCTMeshRemovalData>(selectedForMerge));
                            selectedForMerge.Clear();
                        }
                    }
                }
            }
        }
    }
}