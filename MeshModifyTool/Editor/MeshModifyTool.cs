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
using vrc_yue.MeshUVCutTools.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using vrc_yue.MeshUVCutTools.MeshModifyTool.Models;
using vrc_yue.MeshUVCutTools.MeshModifyTool.Core;
using vrc_yue.MeshUVCutTools.MeshModifyTool.UIComponents;
using vrc_yue.MeshUVCutTools.MeshModifyTool.Utilities;
#if VRC_SDK_VRCSDK3
using VRC.SDK3.Avatars.Components;
#endif

namespace vrc_yue.MeshUVCutTools.MeshModifyTool
{
    public class MeshModifyTool : EditorWindow
    {
        [MenuItem("Tools/ちゅんちゅんメッシュ＆UVカッター(MeshUVCutTool)")]
        public static void ShowWindow()
        {
            var wnd = GetWindow<MeshModifyTool>();
            wnd.titleContent = new GUIContent("ちゅんちゅんメッシュ＆UVカッター");
            wnd.minSize = new Vector2(800, 400);
        }

        // コア機能
        private MeshEditingCore editingCore;
        private SceneViewIntegration sceneViewIntegration;
        private AssetSaveManager saveManager;
        
        // UIコンポーネント
        private MeshListPanel meshListPanel;
        private UVPreviewPanel uvPreviewPanel;
        private ToolSettingsPanel toolSettingsPanel;
        private MeshCreationOptionsPanel meshCreationOptionsPanel;
        
        // 一時アセット管理
        private TemporaryAssetManager assetManager = new TemporaryAssetManager();
        
        // UI状態
        private bool extendVertexEdit = true;
        private bool extendMeshOption = false;
        private bool extendMeshEditOption = false;
        private bool extendUVEdit = false;
        private bool extendAdjustUVEdit = false;
        private bool extendSave = true;
        
        // カメラビュー表示フラグ
        private bool showCameraView = false;
        
        // アバター情報
        private GameObject avatar;
        private AvatarMonitor avatarMonitor;
        
        // ボタン設定
        private int drawButton = 0;
        private int rotateButton = 1;
        private int moveButton = 2;
        
        // ペンツール設定
        private MeshPenTool.ExtraTool penMode = MeshPenTool.ExtraTool.SelectVertex;
        private float brushPower = 0.001f;
        private float brushWidth = 0.03f;
        private float brushStrength = 1f;
        
        // サフィックス
        private string assetSuffix = "_New";
        
        // 選択設定
        public enum MeshCreateOption
        {
            Normal,                 // 通常（選択部分のみ）
            CreateInverse,          // 選択範囲外も作成
            RemoveBoneWeights,      // ボーンウェイトを削除
            CopyUsedBones,          // 選択部分のみ作成し、メッシュが依存するボーンをコピー
            CopyUsedBonesWithoutParent,  // 選択部分のみ作成し、使用ボーンのみコピー（親を除く）
            CopyUsedBonesWithoutParentAdjusted,  // 選択部分のみ作成し、使用ボーンのみコピー（親を除く・ルート位置調整）
            CopyUsedBonesWithoutParentTopY,      // 選択部分のみ作成し、使用ボーンのみコピー（親を除く・最高Y座標）
            CopyUsedBonesWithoutParentBottomY    // 選択部分のみ作成し、使用ボーンのみコピー（親を除く・最低Y座標）
        }
        private MeshCreateOption meshCreateOption = MeshCreateOption.Normal;
        private float sidebarWidth = 400f;
        private bool isResizing = false;
        private const float MIN_SIDEBAR_WIDTH = 200f;
        private const float MAX_SIDEBAR_WIDTH = 600f;

        // ショートカット取得
        private bool keyboardShortcut = false;
        private bool keyboardShift = false;
        private bool keyboardCtr = false;
        private bool keyboardAlt = false;

        // 編集ツールプリセット
        private MeshPenTool[] _penTools;

        // 拡張編集ツールプリセット
        private MeshPenTool[] _extraTools;
        
        // エラー管理
        private System.Exception lastError = null;
        
        // スクロール位置
        private Vector2 meshEditScrollPosition = Vector2.zero;

        MeshPenTool[] extraTools
        {
            get
            {
                if (_extraTools == null)
                {
                    _extraTools = new MeshPenTool[4]
                    {
                        new MeshPenTool(EnvironmentGUIDs.selectLandToolIcon,"SelectLand",MeshPenTool.ExtraTool.SelectLand,null,null,0f),
                        new MeshPenTool(EnvironmentGUIDs.selectVertexToolIcon, "SelectVertex",
                            MeshPenTool.ExtraTool.SelectVertex, null, null, 0f),
                        new MeshPenTool(EnvironmentGUIDs.unSelectLandToolIcon,"UnSelectLand",MeshPenTool.ExtraTool.UnSelectLand,null,null,0f),
                        new MeshPenTool(EnvironmentGUIDs.unSelectVertexToolIcon, "UnSelectVertex",
                            MeshPenTool.ExtraTool.UnSelectVertex, null, null, 0f),
                    };
                }

                return _extraTools;
            }
        }

        
        /// <summary>
        /// 初期化
        /// </summary>
        private void Initialize()
        {
            if (editingCore == null)
            {
                editingCore = new MeshEditingCore(assetManager);
                saveManager = new AssetSaveManager(assetManager);
                sceneViewIntegration = new SceneViewIntegration(editingCore);
                meshListPanel = new MeshListPanel();
                uvPreviewPanel = new UVPreviewPanel();
                toolSettingsPanel = new ToolSettingsPanel();
                meshCreationOptionsPanel = new MeshCreationOptionsPanel();
                
                // イベント登録
                RegisterEvents();
            }
        }
        
        /// <summary>
        /// イベント登録
        /// </summary>
        private void RegisterEvents()
        {
            // メッシュリストパネル
            meshListPanel.OnMeshSelected += (index) =>
            {
                // 現在選択中のメッシュと同じ場合は選択解除（トグル）
                if (editingCore.GetEditIndex() == index)
                {
                    editingCore.SelectMesh(-1);
                    
                    // SceneView統合を無効化
                    if (sceneViewIntegration != null)
                    {
                        sceneViewIntegration.Disable();
                    }
                }
                else
                {
                    editingCore.GetRenderers()[index].gameObject.SetActive(true);
                    editingCore.SelectMesh(index);
                    
                    // UVプレビューパネルを初期化
                    var renderer = editingCore.GetRenderers()[index] as SkinnedMeshRenderer;
                    if (renderer != null && renderer.sharedMaterial != null)
                    {
                        uvPreviewPanel.InitializePreview(renderer.sharedMaterial);
                    }
                    
                    // SceneView統合を有効化
                    if (sceneViewIntegration != null && avatarMonitor != null)
                    {
                        sceneViewIntegration.Enable(avatarMonitor);
                    }
                }
            };
            
            meshListPanel.OnMeshVisibilityChanged += (index, isActive) =>
            {
                editingCore.GetRenderers()[index].gameObject.SetActive(isActive);
            };
            
            meshListPanel.OnTemporaryMeshDeleted += (deletedRenderer) =>
            {
                // 削除されたレンダラーのインデックスを探す
                var renderers = editingCore.GetRenderers();
                int indexToRemove = -1;
                for (int i = 0; i < renderers.Length; i++)
                {
                    if (renderers[i] == deletedRenderer)
                    {
                        indexToRemove = i;
                        break;
                    }
                }
                
                // レンダラー配列から削除
                if (indexToRemove != -1)
                {
                    editingCore.RemoveRenderer(indexToRemove);
                }
                
                // メッシュリストを更新
                Repaint();
            };
            
            meshListPanel.OnRemovalDataVerticesButtonClicked += (index, removalData, infoIndex) =>
            {
                // まず対象のメッシュを選択
                editingCore.SelectMesh(index);
                
                // RemovalDataから頂点リストを取得して選択
                if (removalData != null && infoIndex < removalData.removalInfos.Count)
                {
                    var info = removalData.removalInfos[infoIndex];
                    if (info != null && info.verticesToRemove != null && info.verticesToRemove.Count > 0)
                    {
                        var selectionHandler = editingCore.GetSelectionHandler();
                        selectionHandler.SetSelectedVertices(info.verticesToRemove);
                    }
                }
            };
            
            meshListPanel.OnRemovalDataEnabledChanged += (removalData, enabled) =>
            {
                if (removalData != null)
                {
                    removalData.enabled = enabled;
                    EditorUtility.SetDirty(removalData);
                    
                    // NDMFプレビューを更新
                    MeshRemovalUtility.RefreshNDMFPreview();
                }
            };
            
            meshListPanel.OnMergeRemovalDataRequested += (removalDataList) =>
            {
                MeshRemovalUtility.MergeRemovalData(removalDataList);
                Repaint(); // UIを更新
            };
            
            meshListPanel.OnRemovalDataDeleteRequested += (removalData) =>
            {
                if (removalData != null)
                {
                    DestroyImmediate(removalData);
                    Repaint();
                    
                    // NDMFプレビューを更新
                    MeshRemovalUtility.RefreshNDMFPreview();
                }
            };
            
            // ModularAvatar関連イベント
            meshListPanel.OnSetupModularAvatarRequested += (removalData) =>
            {
                if (removalData != null)
                {
                    var editor = Editor.CreateEditor(removalData) as MeshRemovalDataEditor;
                    if (editor != null)
                    {
                        editor.SetupModularAvatarComponentsPublic(removalData);
                        DestroyImmediate(editor);
                    }
                }
            };
            
            meshListPanel.OnRemoveModularAvatarRequested += (removalData) =>
            {
                if (removalData != null)
                {
                    var editor = Editor.CreateEditor(removalData) as MeshRemovalDataEditor;
                    if (editor != null)
                    {
                        editor.RemoveModularAvatarComponentsPublic(removalData);
                        DestroyImmediate(editor);
                    }
                }
            };
            
            // 選択ハンドラ
            var selectionHandler = editingCore.GetSelectionHandler();
            selectionHandler.OnSelectionChanged += (vertices) =>
            {
                editingCore.UpdateVertexSelection(vertices);
            };
            
            // 設定パネル
            toolSettingsPanel.OnWireFrameColorChanged += (color) =>
            {
                var previewController = editingCore.GetPreviewController();
                previewController.WireFrameColor = color;
            };
            
            toolSettingsPanel.OnNormalAlphaChanged += (alpha) =>
            {
                var previewController = editingCore.GetPreviewController();
                previewController.NormalAlpha = alpha;
                if (editingCore.GetEditIndex() != -1)
                {
                    previewController.ApplyRenderMode(editingCore.GetRenderers()[editingCore.GetEditIndex()]);
                }
            };
            
            toolSettingsPanel.OnUVAlphaChanged += (alpha) =>
            {
                var previewController = editingCore.GetPreviewController();
                previewController.UVAlpha = alpha;
                if (editingCore.GetEditIndex() != -1)
                {
                    previewController.ApplyRenderMode(editingCore.GetRenderers()[editingCore.GetEditIndex()]);
                }
            };
            
            toolSettingsPanel.OnCameraViewChanged += (show) =>
            {
                showCameraView = show;
                if (avatarMonitor != null)
                {
                    avatarMonitor.SetSceneViewMode(!showCameraView);
                }
            };
            
            // メッシュ作成オプションパネル
            meshCreationOptionsPanel.OnOptionChanged += (baseMode, weightBoneMode, advancedBoneMode, rootPositionMode) =>
            {
                // 新しい選択を従来のenumに変換
                meshCreateOption = MeshCreationOptionsPanel.ConvertToLegacyOption(baseMode, weightBoneMode, advancedBoneMode, rootPositionMode);
            };
        }



        /// <summary>
        /// 表示部，実装は置かないこと
        /// </summary>
        private void OnGUI()
        {
            try
            {
                OnGUIInternal();
            }
            catch (ExitGUIException)
            {
                // ExitGUIExceptionは正常な動作なので再スローする
                throw;
            }
            catch (System.Exception e)
            {
                lastError = e;
                Debug.LogError("[MeshModifyTool] OnGUI Error: " + e.ToString());
                throw;
            }
        }
        
        /// <summary>
        /// OnGUIの実際の処理
        /// </summary>
        private void OnGUIInternal()
        {
            Initialize();
            
            // エラーがある場合は最初に表示
            if (lastError != null)
            {
                EditorGUILayout.HelpBox("エラーが発生しました。動作が不安定な可能性があります。\nウィンドウを閉じて再度開くと改善される場合があります。\n\nエラー詳細: " + lastError.Message, MessageType.Error);
                
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("ウィンドウを再起動"))
                    {
                        Close();
                        EditorApplication.delayCall += () =>
                        {
                            ShowWindow();
                        };
                    }
                    
                    if (GUILayout.Button("ウィンドウを閉じる"))
                    {
                        Close();
                    }
                }
                return;
            }
            
            using (new EditorGUILayout.HorizontalScope())
            {
                if (editingCore.GetRenderers() == null || editingCore.GetRenderers().Length == 0)
                {
                    using (new EditorGUILayout.VerticalScope())
                    {
                        // 言語切り替えボタンを右上に配置
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            GUILayout.FlexibleSpace();
                            EditorGUILayout.LabelField(Localization.Get("language_label"), GUILayout.Width(180));
                            if (GUILayout.Button(Localization.CurrentLanguage == Localization.Language.Japanese ? "EN" : "JP", GUILayout.Width(40)))
                            {
                                Localization.CurrentLanguage = Localization.CurrentLanguage == Localization.Language.Japanese 
                                    ? Localization.Language.English 
                                    : Localization.Language.Japanese;
                            }
                        }
                        
                        WindowBase.TitleStyle(Localization.Get("window_title"));
                        WindowBase.DetailStyle(Localization.Get("window_description"));
                            
                            //, EnvironmentGUIDs.readme);
                        avatar = EditorGUILayout.ObjectField("", avatar, typeof(GameObject), true) as GameObject;
                        if (GUILayout.Button("Setup"))
                        {
                            Setup(avatar);
                        }

                        // VRCAvatarDescriptorを持つアクティブなオブジェクトを検索して表示
                        EditorGUILayout.Space();
                        EditorGUILayout.LabelField(Localization.Get("active_avatars_description"));
                        
#if VRC_SDK_VRCSDK3
                        var avatarDescriptors = FindObjectsOfType<VRCAvatarDescriptor>();
                        var activeAvatars = new List<VRCAvatarDescriptor>();
                        
                        foreach (var desc in avatarDescriptors)
                        {
                            if (desc.gameObject.activeInHierarchy)
                            {
                                activeAvatars.Add(desc);
                            }
                        }
                        
                        if (activeAvatars.Count > 0)
                        {
                            using (var scrollView = new EditorGUILayout.ScrollViewScope(Vector2.zero, GUILayout.MaxHeight(200)))
                            {
                                foreach (var avatarDesc in activeAvatars)
                                {
                                    using (new EditorGUILayout.HorizontalScope(GUI.skin.box))
                                    {
                                        EditorGUILayout.LabelField(avatarDesc.gameObject.name, GUILayout.ExpandWidth(true));
                                        if (GUILayout.Button("Select", GUILayout.Width(60)))
                                        {
                                            avatar = avatarDesc.gameObject;
                                            Setup(avatar);
                                        }
                                    }
                                }
                            }
                        }
#endif

                        WindowBase.Signature();
                        return;
                    }
                }

                // ウィンドウ左側
                using (showCameraView 
                    ? new EditorGUILayout.VerticalScope(GUILayout.Width(sidebarWidth))
                    : new EditorGUILayout.VerticalScope())
                {
                    // 戻るボタンと言語切り替えボタンを上部に配置
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button(Localization.Get("btn_back"), GUILayout.Width(60)))
                        {
                            // 初期状態に戻す
                            avatar = null;
                            editingCore.Cleanup();
                            
                            // 一時アセットのクリーンアップ
                            assetManager.CleanupTemporaryAssets();
                            
                            if (avatarMonitor != null)
                            {
                                avatarMonitor.Release();
                                avatarMonitor = null;
                            }
                            sceneViewIntegration.Disable();
                            return; // 戻るボタンが押されたらOnGUIの残りの処理をスキップ
                        }
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.LabelField(Localization.Get("language_label"), GUILayout.Width(180));
                        if (GUILayout.Button(Localization.CurrentLanguage == Localization.Language.Japanese ? "EN" : "JP", GUILayout.Width(40)))
                        {
                            Localization.CurrentLanguage = Localization.CurrentLanguage == Localization.Language.Japanese 
                                ? Localization.Language.English 
                                : Localization.Language.Japanese;
                        }
                    }
                    // カメラビュー表示時のみリサイズハンドルを表示
                    if (showCameraView)
                    {
                        // リサイズハンドルの処理
                        var resizeRect = new Rect(sidebarWidth - 5, 0, 10, position.height);
                        EditorGUIUtility.AddCursorRect(resizeRect, MouseCursor.ResizeHorizontal);

                        if (Event.current.type == EventType.MouseDown && resizeRect.Contains(Event.current.mousePosition))
                        {
                            isResizing = true;
                        }
                        else if (Event.current.type == EventType.MouseUp)
                        {
                            isResizing = false;
                        }
                        else if (Event.current.type == EventType.MouseDrag && isResizing)
                        {
                            sidebarWidth = Mathf.Clamp(Event.current.mousePosition.x, MIN_SIDEBAR_WIDTH, MAX_SIDEBAR_WIDTH);
                            Repaint();
                        }
                    }


                    // 選択中オブジェクトが非アクティブになったら，選択解除
                    var currentEditIndex = editingCore.GetEditIndex();
                    if (currentEditIndex != -1)
                    {
                        var renderers = editingCore.GetRenderers();
                        if (currentEditIndex < renderers.Length && renderers[currentEditIndex] != null)
                        {
                            if (!renderers[currentEditIndex].gameObject.activeSelf)
                            {
                                editingCore.SelectMesh(-1);
                                
                                // SceneView統合を無効化
                                if (sceneViewIntegration != null)
                                {
                                    sceneViewIntegration.Disable();
                                }
                            }
                        }
                        else
                        {
                            // レンダラーが削除されている場合も選択解除
                            editingCore.SelectMesh(-1);
                            
                            // SceneView統合を無効化
                            if (sceneViewIntegration != null)
                            {
                                sceneViewIntegration.Disable();
                            }
                        }
                    }

                    // メッシュリストパネルを描画
                    meshListPanel.DrawMeshList(editingCore.GetRenderers(), editingCore.GetEditIndex(), assetManager);

                    // スクロールビューの開始（メッシュ編集以降全体を含む）
                    meshEditScrollPosition = EditorGUILayout.BeginScrollView(
                        meshEditScrollPosition,
                        GUILayout.ExpandHeight(true) // 利用可能な高さ全体を使用
                    );

                    extendVertexEdit = EditorGUILayout.Foldout(extendVertexEdit, Localization.Get("section_mesh_edit"));
                    if (extendVertexEdit)
                    {
                        var selectionHandler = editingCore.GetSelectionHandler();
                        var editMeshCreater = editingCore.EditMeshCreater;
                        var meshOperations = editingCore.GetMeshOperations();
                        
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            GUILayout.Space(5); // インデント用のスペース
                            using (new EditorGUILayout.VerticalScope())
                            {
                                using (new EditorGUILayout.VerticalScope(GUI.skin.box))
                                {
                                    EditorGUILayout.LabelField(Localization.Get("mesh_select"), EditorStyles.boldLabel);

                                    // TODO
                                    // keyboardShortcut = EditorGUILayout.Toggle(new GUIContent("Keyboard Shortcut",
                                    //     "Shortcuts : \n" +
                                    //     "   Alt + Right Drag : Move \n" +
                                    //     "   Alt + Left Drag : Rotate \n" +
                                    //     "   Ctr + Z : Undo \n" +
                                    //     "   Ctr + Y : Redo \n" +
                                    //     "   Shift + Wheel : Power Change \n" +
                                    //     "   Ctr Hold: Reverse Power \n" +
                                    //     "   Alt + Wheel : Strength Change \n" +
                                    //     "   SelectMode : \n" +
                                    //     "      Shift Hold: SelectLand \n" +
                                    //     "     Ctr Hold: UnSelect \n"), keyboardShortcut);

                                    using (new EditorGUILayout.HorizontalScope())
                                    {
                                        foreach (var penTool in extraTools)
                                        {
                                            if (penTool.Button(ref penMode, ref brushPower, ref brushWidth,
                                                    ref brushStrength))
                                            {
                                                // 選択モードを更新
                                                switch (penMode)
                                                {
                                                    case MeshPenTool.ExtraTool.SelectLand:
                                                        selectionHandler.CurrentMode = MeshSelectionHandler.SelectionMode.SelectLand;
                                                        break;
                                                    case MeshPenTool.ExtraTool.UnSelectLand:
                                                        selectionHandler.CurrentMode = MeshSelectionHandler.SelectionMode.UnSelectLand;
                                                        break;
                                                    case MeshPenTool.ExtraTool.SelectVertex:
                                                        selectionHandler.CurrentMode = MeshSelectionHandler.SelectionMode.SelectVertex;
                                                        break;
                                                    case MeshPenTool.ExtraTool.UnSelectVertex:
                                                        selectionHandler.CurrentMode = MeshSelectionHandler.SelectionMode.UnSelectVertex;
                                                        break;
                                                }
                                            }
                                        }
                                    }

                                    if (selectionHandler.IsSelectionMode)
                                    {
                                        using (new EditorGUI.DisabledScope(editMeshCreater?.IsComputeLandVertexes() ?? false))
                                        {
                                            using (new EditorGUILayout.HorizontalScope())
                                            {
                                                if (GUILayout.Button(Localization.Get("btn_select_all")))
                                                {
                                                    selectionHandler.SelectAll(editMeshCreater.VertexsCount());
                                                }

                                                if (GUILayout.Button(Localization.Get("btn_select_none")))
                                                {
                                                    selectionHandler.ClearSelection();
                                                }

                                                if (GUILayout.Button(Localization.Get("btn_revert_select")))
                                                {
                                                    selectionHandler.InvertSelection(editMeshCreater.VertexsCount());
                                                }
                                            }
                                        }
                                    }
                                }

                                EditorGUILayout.Space();

                                // メッシュ編集オプション
                                extendMeshEditOption = EditorGUILayout.Foldout(extendMeshEditOption, Localization.Get("mesh_edit_option"));
                                if (extendMeshEditOption)
                                {
                                    using (new EditorGUILayout.HorizontalScope())
                                    {
                                        GUILayout.Space(20); // インデント用のスペース
                                        using (new EditorGUILayout.VerticalScope())
                                        {
                                            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
                                            {
                                                using (new EditorGUILayout.HorizontalScope())
                                                {
                                                    EditorGUILayout.LabelField(Localization.Get("backface_culling"),GUILayout.ExpandWidth(true));
                                                    
                                                    var backfaceCulling = selectionHandler.BackfaceCulling;
                                                    var newBackfaceCulling = EditorGUILayout.Toggle(backfaceCulling, GUILayout.Width(20));
                                                    if (newBackfaceCulling != backfaceCulling)
                                                    {
                                                        selectionHandler.BackfaceCulling = newBackfaceCulling;
                                                        if (avatarMonitor != null)
                                                        {
                                                            avatarMonitor.EnableBackfaceCulling = newBackfaceCulling;
                                                        }
                                                    }
                                                }

                                                using (new EditorGUILayout.HorizontalScope())
                                                {
                                                    EditorGUILayout.LabelField(Localization.Get("select_overlapping"),GUILayout.ExpandWidth(true));
                                                    var isSelectOverlapping = selectionHandler.IsSelectOverlappingVertices;
                                                    var newSelectOverlapping = EditorGUILayout.Toggle(isSelectOverlapping, GUILayout.Width(20));
                                                    if (newSelectOverlapping != isSelectOverlapping)
                                                    {
                                                        selectionHandler.IsSelectOverlappingVertices = newSelectOverlapping;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }

                                EditorGUILayout.Space();

                                using (new EditorGUILayout.VerticalScope(GUI.skin.box))
                                {
                                    EditorGUILayout.LabelField(Localization.Get("create_mesh"), EditorStyles.boldLabel);

                                    using (new EditorGUILayout.HorizontalScope())
                                    {
                                        var selectedVerts = selectionHandler.GetSelectedVertices();
                                        if (selectedVerts != null)
                                        {
                                            using (new EditorGUI.DisabledScope(selectedVerts.Count == 0))
                                            {
                                                if (GUILayout.Button(Localization.Get("btn_remove_mesh_ndmf")))
                                                {
                                                    var renderer = editMeshCreater.RendBone.GetComponent<SkinnedMeshRenderer>();
                                                    saveManager.RegisterMeshRemoval(avatar, renderer, selectedVerts);
                                                    
                                                    // 削除予定頂点の青色表示を即座に更新
                                                    editingCore.SelectMesh(editingCore.GetEditIndex());
                                                }
                                            }
                                        }
                                    }
                                    
                                    EditorGUILayout.Space();
                                    
                                    // 未保存メッシュが存在するかチェック
                                    bool hasUnsavedMesh = assetManager.TempMeshes.Any(m => m.Asset != null && !AssetDatabase.Contains(m.Asset));
                                    
                                    // 未保存メッシュ存在時の警告メッセージ
                                    if (hasUnsavedMesh)
                                    {
                                        EditorGUILayout.HelpBox(Localization.Get("msg_unsaved_mesh_warning"), MessageType.Warning);
                                    }
                                    
                                    using (new EditorGUILayout.HorizontalScope())
                                    {
                                        var selectedVertices = selectionHandler.GetSelectedVertices();
                                        using (new EditorGUI.DisabledScope(selectedVertices.Count == 0 || hasUnsavedMesh))
                                        {
                                            if (GUILayout.Button(Localization.Get("btn_create_mesh")))
                                            {
                                                // メッシュ作成オプションを設定
                                                var options = new vrc_yue.MeshUVCutTools.Core.MeshCreationOptions();
                                                
                                                switch (meshCreateOption)
                                                {
                                                    case MeshCreateOption.Normal:
                                                        // デフォルト設定（何も変更しない）
                                                        break;
                                                    case MeshCreateOption.CreateInverse:
                                                        options.Inverse = true;
                                                        break;
                                                    case MeshCreateOption.RemoveBoneWeights:
                                                        options.RemoveBoneWeights = true;
                                                        break;
                                                    case MeshCreateOption.CopyUsedBones:
                                                        options.CopyUsedBonesOnly = true;
                                                        break;
                                                    case MeshCreateOption.CopyUsedBonesWithoutParent:
                                                        options.CopyUsedBonesOnly = true;
                                                        options.ExcludeParentBones = true;
                                                        break;
                                                    case MeshCreateOption.CopyUsedBonesWithoutParentAdjusted:
                                                        options.CopyUsedBonesOnly = true;
                                                        options.ExcludeParentBones = true;
                                                        options.RootAdjustment = vrc_yue.MeshUVCutTools.Core.BoneRootAdjustment.Center;
                                                        break;
                                                    case MeshCreateOption.CopyUsedBonesWithoutParentTopY:
                                                        options.CopyUsedBonesOnly = true;
                                                        options.ExcludeParentBones = true;
                                                        options.RootAdjustment = vrc_yue.MeshUVCutTools.Core.BoneRootAdjustment.TopY;
                                                        break;
                                                    case MeshCreateOption.CopyUsedBonesWithoutParentBottomY:
                                                        options.CopyUsedBonesOnly = true;
                                                        options.ExcludeParentBones = true;
                                                        options.RootAdjustment = vrc_yue.MeshUVCutTools.Core.BoneRootAdjustment.BottomY;
                                                        break;
                                                }
                                                
                                                if (options.Inverse)
                                                {
                                                    // 選択されていない頂点からメッシュを作成
                                                    var allVertices = Enumerable.Range(0, editMeshCreater.VertexsCount()).ToList();
                                                    var inverseVertices = allVertices.Except(selectedVertices).ToList();
                                                    var inverseOptions = new vrc_yue.MeshUVCutTools.Core.MeshCreationOptions();
                                                    var inverseObj = meshOperations.CreateMeshFromSelection(editMeshCreater, inverseVertices, avatar, "_Temp", inverseOptions);
                                                    if (inverseObj != null)
                                                    {
                                                        var inverseRenderer = inverseObj.GetComponent<SkinnedMeshRenderer>();
                                                        if (inverseRenderer != null)
                                                            editingCore.AddRenderer(inverseRenderer, false);
                                                    }
                                                }
                                                
                                                // 選択された頂点からメッシュを作成
                                                var newObj = meshOperations.CreateMeshFromSelection(editMeshCreater, selectedVertices, avatar, "_Temp", options);
                                                if (newObj != null)
                                                {
                                                    Renderer newRenderer = null;
                                                    
                                                    // copyUsedBonesOnlyの場合はContainerの子からRendererを探す
                                                    if (options.CopyUsedBonesOnly && !options.RemoveBoneWeights)
                                                    {
                                                        var childRenderer = newObj.GetComponentInChildren<SkinnedMeshRenderer>();
                                                        newRenderer = childRenderer;
                                                    }
                                                    else
                                                    {
                                                        newRenderer = options.RemoveBoneWeights 
                                                            ? newObj.GetComponent<MeshRenderer>() 
                                                            : newObj.GetComponent<SkinnedMeshRenderer>();
                                                    }
                                                    
                                                    if (newRenderer != null)
                                                        editingCore.AddRenderer(newRenderer, false);
                                                }
                                                
                                                // メッシュ作成後は選択を解除
                                                editingCore.SelectMesh(-1);
                                                
                                                // SceneView統合を無効化
                                                if (sceneViewIntegration != null)
                                                {
                                                    sceneViewIntegration.Disable();
                                                }
                                            }
                                        }
                                    }
                                    
                                    // 一時メッシュの説明文を追加
                                    EditorGUILayout.Space();
                                    EditorGUILayout.LabelField(Localization.Get("temp_mesh_description"), EditorStyles.wordWrappedLabel);
                                    
                                    EditorGUILayout.Space();

                                    extendMeshOption = EditorGUILayout.Foldout(extendMeshOption, Localization.Get("mesh_create_option"));
                                    if (extendMeshOption)
                                    {
                                        // 初期値を設定
                                        meshCreationOptionsPanel.SetFromLegacyOption(meshCreateOption);
                                        
                                        // 新しいオプションパネルを描画
                                        meshCreationOptionsPanel.DrawOptionsPanel();
                                        
                                        EditorGUILayout.Space();
                                        
                                        using (new EditorGUILayout.HorizontalScope())
                                        {
                                            using (new EditorGUILayout.VerticalScope())
                                            {
                                                EditorGUILayout.LabelField(Localization.Get("normal_offset"));
                                                var normalOffset = meshOperations.NormalOffset;
                                                var newNormalOffset = EditorGUILayout.Slider(normalOffset, -1f, 1f);
                                                if (Math.Abs(newNormalOffset - normalOffset) > 0.001f)
                                                {
                                                    meshOperations.NormalOffset = newNormalOffset;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    extendUVEdit = EditorGUILayout.Foldout(extendUVEdit, Localization.Get("section_uv_edit"));
                    if (extendUVEdit)
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            GUILayout.Space(5); // インデント用のスペース
                            using (new EditorGUILayout.VerticalScope())
                            {
                                using (new EditorGUILayout.VerticalScope(GUI.skin.box))
                                {
                                    // UVプレビューパネルを描画
                                    uvPreviewPanel.DrawUVPreview();
                                }

                                using (new EditorGUILayout.VerticalScope(GUI.skin.box))
                                {
                                    EditorGUILayout.LabelField(Localization.Get("uv_texture_edit"), EditorStyles.boldLabel);

                                    // 未保存メッシュでない場合の警告表示
                                    var editIdx = editingCore.GetEditIndex();
                                    bool isTempMesh = true; // デフォルトはtrue（編集可能）
                                    
                                    if (editIdx != -1)
                                    {
                                        var renderers = editingCore.GetRenderers();
                                        var rendererGameObject = renderers[editIdx].gameObject;
                                        
                                        // メッシュ自体、または親（Container）が一時アセットとして登録されているかチェック
                                        isTempMesh = assetManager.TempMeshes.Any(m => 
                                            m.RelatedGameObject == rendererGameObject || 
                                            (rendererGameObject.transform.parent != null && 
                                             m.RelatedGameObject == rendererGameObject.transform.parent.gameObject));
                                        
                                        if (!isTempMesh)
                                        {
                                            EditorGUILayout.HelpBox(Localization.Get("msg_saved_object_warning"), MessageType.Warning);
                                        }
                                    }

                                    if (GUILayout.Button(Localization.Get("uv_fit")))
                                    {
                                        // 保存済みオブジェクトが選択されている場合、確認ダイアログを表示
                                        if (!isTempMesh)
                                        {
                                            if (!EditorUtility.DisplayDialog(
                                                Localization.Get("dialog_title_warning"),
                                                Localization.Get("msg_saved_object_warning"),
                                                Localization.Get("dialog_ok"),
                                                Localization.Get("dialog_cancel")))
                                            {
                                                return;
                                            }
                                        }
                                        
                                        var ops = editingCore.GetMeshOperations();
                                        ops.UpdateMeshUV(editingCore.EditMeshCreater, true, false);
                                        // テクスチャプレビューを更新
                                        UpdateUVPreview();
                                        Repaint();
                                    }

                                    if (GUILayout.Button(Localization.Get("uv_reconstruct")))
                                    {
                                        // 保存済みオブジェクトが選択されている場合、確認ダイアログを表示
                                        if (!isTempMesh)
                                        {
                                            if (!EditorUtility.DisplayDialog(
                                                Localization.Get("dialog_title_warning"),
                                                Localization.Get("msg_saved_object_warning"),
                                                Localization.Get("dialog_ok"),
                                                Localization.Get("dialog_cancel")))
                                            {
                                                return;
                                            }
                                        }
                                        
                                        var ops = editingCore.GetMeshOperations();
                                        ops.UpdateMeshUV(editingCore.EditMeshCreater, false, true);
                                        // テクスチャプレビューを更新
                                        UpdateUVPreview();
                                        Repaint();
                                    }

                                    EditorGUILayout.Space();
                                    extendAdjustUVEdit = EditorGUILayout.Foldout(extendAdjustUVEdit, Localization.Get("uv_adjust"));
                                    if (extendAdjustUVEdit)
                                    {
                                        using (new EditorGUILayout.HorizontalScope())
                                        {
                                            GUILayout.Space(5); // インデント用のスペース
                                            using (new EditorGUILayout.VerticalScope())
                                            {

                                                if (GUILayout.Button(Localization.Get("flip_horizontal")))
                                                {
                                                    // 保存済みオブジェクトが選択されている場合、確認ダイアログを表示
                                                    if (!isTempMesh)
                                                    {
                                                        if (!EditorUtility.DisplayDialog(
                                                            Localization.Get("dialog_title_warning"),
                                                            Localization.Get("msg_saved_object_warning"),
                                                            Localization.Get("dialog_ok"),
                                                            Localization.Get("dialog_cancel")))
                                                        {
                                                            return;
                                                        }
                                                    }
                                                    
                                                    var ops = editingCore.GetMeshOperations();
                                                    ops.TransformUV(editingCore.EditMeshCreater, MeshOperations.TransformType.FlipHorizontal);
                                                    // テクスチャプレビューを更新
                                                    UpdateUVPreview();
                                                    Repaint();
                                                }

                                                if (GUILayout.Button(Localization.Get("flip_vertical")))
                                                {
                                                    // 保存済みオブジェクトが選択されている場合、確認ダイアログを表示
                                                    if (!isTempMesh)
                                                    {
                                                        if (!EditorUtility.DisplayDialog(
                                                            Localization.Get("dialog_title_warning"),
                                                            Localization.Get("msg_saved_object_warning"),
                                                            Localization.Get("dialog_ok"),
                                                            Localization.Get("dialog_cancel")))
                                                        {
                                                            return;
                                                        }
                                                    }
                                                    
                                                    var ops = editingCore.GetMeshOperations();
                                                    ops.TransformUV(editingCore.EditMeshCreater, MeshOperations.TransformType.FlipVertical);
                                                    // テクスチャプレビューを更新
                                                    UpdateUVPreview();
                                                    Repaint();
                                                }

                                                if (GUILayout.Button(Localization.Get("rotate_90")))
                                                {
                                                    // 保存済みオブジェクトが選択されている場合、確認ダイアログを表示
                                                    if (!isTempMesh)
                                                    {
                                                        if (!EditorUtility.DisplayDialog(
                                                            Localization.Get("dialog_title_warning"),
                                                            Localization.Get("msg_saved_object_warning"),
                                                            Localization.Get("dialog_ok"),
                                                            Localization.Get("dialog_cancel")))
                                                        {
                                                            return;
                                                        }
                                                    }
                                                    
                                                    var ops = editingCore.GetMeshOperations();
                                                    ops.TransformUV(editingCore.EditMeshCreater, MeshOperations.TransformType.Rotate90);
                                                    // テクスチャプレビューを更新
                                                    UpdateUVPreview();
                                                    Repaint();
                                                }

                                                if (GUILayout.Button(Localization.Get("rotate_minus_90")))
                                                {
                                                    // 保存済みオブジェクトが選択されている場合、確認ダイアログを表示
                                                    if (!isTempMesh)
                                                    {
                                                        if (!EditorUtility.DisplayDialog(
                                                            Localization.Get("dialog_title_warning"),
                                                            Localization.Get("msg_saved_object_warning"),
                                                            Localization.Get("dialog_ok"),
                                                            Localization.Get("dialog_cancel")))
                                                        {
                                                            return;
                                                        }
                                                    }
                                                    
                                                    var ops = editingCore.GetMeshOperations();
                                                    ops.TransformUV(editingCore.EditMeshCreater, MeshOperations.TransformType.RotateMinus90);
                                                    // テクスチャプレビューを更新
                                                    UpdateUVPreview();
                                                    Repaint();
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    extendSave = EditorGUILayout.Foldout(extendSave, Localization.Get("section_save"));
                    if (extendSave)
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            GUILayout.Space(5); // インデント用のスペース
                            // 保存ボタン
                            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
                            {
                                EditorGUILayout.LabelField(Localization.Get("section_save"), EditorStyles.boldLabel);
                                
                                // 保存説明文を追加
                                EditorGUILayout.LabelField(Localization.Get("save_description"), EditorStyles.wordWrappedLabel);
                                EditorGUILayout.Space();
                                
                                using (new EditorGUILayout.HorizontalScope())
                                {
                                    EditorGUILayout.LabelField(Localization.Get("save_suffix"));
                                    GUILayout.ExpandWidth(true);
                                    assetSuffix = EditorGUILayout.TextField(assetSuffix, GUILayout.Width(100));
                                }

                                bool hasSelectedAssets = assetManager.TempMeshes.Any(m => m.IsSelected) ||
                                    assetManager.TempMaterials.Any(m => m.IsSelected) ||
                                    assetManager.TempTextures.Any(t => t.IsSelected);

                                using (new EditorGUI.DisabledScope(!hasSelectedAssets))
                                {
                                    var saveEditIndex = editingCore.GetEditIndex();
                                    var renderers = editingCore.GetRenderers();
                                    // 直接のマッチまたは親がRelatedGameObjectの場合をチェック
                                    using (new EditorGUI.DisabledScope(saveEditIndex == -1 || !assetManager.TempMeshes.Any(m => 
                                        m.RelatedGameObject == renderers[saveEditIndex].gameObject || 
                                        (renderers[saveEditIndex].transform.parent != null && m.RelatedGameObject == renderers[saveEditIndex].transform.parent.gameObject))))
                                    {
                                        if (GUILayout.Button(Localization.Get("save_selected")))
                                        {
                                            saveManager.SaveTemporaryAssetsToFiles(saveEditIndex, renderers, assetSuffix);
                                            
                                            // 保存後は選択を解除
                                            editingCore.SelectMesh(-1);
                                            
                                            // SceneView統合を無効化
                                            if (sceneViewIntegration != null)
                                            {
                                                sceneViewIntegration.Disable();
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    // 設定パネルを描画
                    var previewController = editingCore.GetPreviewController();
                    var selHandler = editingCore.GetSelectionHandler();
                    var selectedVertexCount = selHandler.GetSelectedVertices().Count;
                    var totalVertexCount = editingCore.EditMeshCreater?.VertexsCount() ?? 0;
                    
                    // TODO 呼び出し引数が汚い
                    toolSettingsPanel.DrawSettings(
                        previewController.WireFrameColor,
                        previewController.NormalAlpha,
                        previewController.UVAlpha,
                        selectedVertexCount,
                        totalVertexCount,
                        showCameraView
                    );
                    
                    // スクロールビューの終了
                    EditorGUILayout.EndScrollView();
                }

                using (new EditorGUILayout.VerticalScope())
                {
                    if (avatarMonitor != null && showCameraView)
                    {
                        var avatarMonitorWidth = 300;
                        int positionDrag = keyboardShortcut && keyboardAlt ? drawButton : moveButton;
                        bool canNotTouch = keyboardShortcut && (keyboardShift || keyboardCtr);
                        bool canNotWheel = keyboardShortcut && (keyboardShift || keyboardAlt);
                        if (keyboardShortcut && keyboardAlt)
                        {
                            avatarMonitor.SetSpeed(0.1f, 0.5f, 0.3f);
                        }
                        else
                        {
                            avatarMonitor.SetSpeed();
                        }

                        avatarMonitor.Display((int)position.width - avatarMonitorWidth, (int)position.height - 10,
                            rotateButton, positionDrag, !canNotTouch, !canNotWheel);
                        if (editingCore.GetEditIndex() != -1)
                        {
                            HandleAvatarMonitorTouch();
                        }
                    }
                }
            }

            HandleVertexSelection();
            UpdateUVPreview();
        }

        /// <summary>
        /// 毎フレーム更新する
        /// </summary>
        private void Update()
        {
            Repaint();
        }
        
        private void OnEnable()
        {
            Initialize();
            if (sceneViewIntegration != null && avatarMonitor != null)
            {
                sceneViewIntegration.Enable(avatarMonitor);
            }
        }
        
        private void OnDisable()
        {
            if (sceneViewIntegration != null)
            {
                sceneViewIntegration.Disable();
            }
        }
        /// <summary>
        /// ハンドル頂点選択
        /// </summary>
        private void HandleVertexSelection()
        {
            if (editingCore == null || editingCore.GetEditIndex() == -1) return;
            
            var selectionHandler = editingCore.GetSelectionHandler();
            if (!selectionHandler.IsSelectionMode) return;
            
            var e = Event.current;
            var previewController = editingCore.GetPreviewController();
            var meshCollider = previewController.GetEditMeshCollider();
            
            if (meshCollider == null) return;
            
            // マウスドラッグ開始（メッシュチェックなしで無条件に開始）
            if (e.type == EventType.MouseDown && e.button == drawButton &&
                avatarMonitor.IsInDisplay(e.mousePosition))
            {
                avatarMonitor.BeginDrag(e.mousePosition);
                
                // ドラッグではなく単純なクリックの場合のために、ここでも頂点選択処理を呼ぶ
                // （ドラッグが開始されなかった場合のフォールバック）
                selectionHandler.HandleVertexClick(
                    editingCore.EditMeshCreater,
                    meshCollider,
                    avatarMonitor,
                    e.mousePosition);
            }
            
            // マウスドラッグ終了
            if (e.type == EventType.MouseUp && e.button == drawButton)
            {
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
            }
            
            // 選択範囲の描画
            avatarMonitor.DrawSelectionRect();
            Repaint();
        }
        
        /// <summary>
        /// アバターモニタータッチ処理
        /// </summary>
        private void HandleAvatarMonitorTouch()
        {
            // この処理はHandleVertexSelectionに統合されたため、何もしない
            // 互換性のためにメソッドは残しておく
        }
        
        /// <summary>
        /// UVプレビューを更新
        /// </summary>
        private void UpdateUVPreview()
        {
            if (editingCore == null || editingCore.EditMeshCreater == null) return;
            
            var selectionHandler = editingCore.GetSelectionHandler();
            var selectedVertices = selectionHandler.GetSelectedVertices();
            
            // 現在の編集インデックスから最新のレンダラーとマテリアルを取得
            var editIndex = editingCore.GetEditIndex();
            if (editIndex >= 0 && editIndex < editingCore.GetRenderers().Length)
            {
                var renderer = editingCore.GetRenderers()[editIndex] as SkinnedMeshRenderer;
                if (renderer != null && renderer.sharedMaterial != null)
                {
                    uvPreviewPanel.UpdateWithMaterial(renderer.sharedMaterial);
                }
            }
            
            uvPreviewPanel.ForceUpdatePreview(editingCore.EditMeshCreater, selectedVertices);
        }



        private void OnDestroy()
        {
            // まずコア機能のクリーンアップで参照をクリア
            editingCore?.Cleanup();
            
            // その後、一時アセットを削除
            assetManager.CleanupTemporaryAssets();
            
            // AvatarMonitorのクリーンアップ
            avatarMonitor?.Release();
            avatarMonitor = null;
            
            // UVプレビューパネルのクリーンアップ
            uvPreviewPanel?.Cleanup();
        }

        void Setup(GameObject anim)
        {
            this.avatar = anim;
            
            if (avatarMonitor != null) avatarMonitor.Release();
            avatarMonitor = new AvatarMonitor(anim.transform);
            
            // カメラビューがOFFならSceneViewモードをON（反転関係）
            avatarMonitor.SetSceneViewMode(!showCameraView);
            
            // コア機能のセットアップ
            Initialize();
            editingCore.Setup(anim, avatarMonitor);
            
            // SceneView統合を有効化
            sceneViewIntegration.Enable(avatarMonitor);
        }
        

    }
}