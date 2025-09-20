using System.Linq;
using UnityEditor;
using UnityEngine;
using vrc_yue.MeshUVCutTools.MeshModifyTool.Models;

namespace vrc_yue.MeshUVCutTools.MeshModifyTool.Utilities
{
    /// <summary>
    /// アセット保存機能を管理するクラス
    /// </summary>
    public class AssetSaveManager
    {
        private TemporaryAssetManager assetManager;
        
        public AssetSaveManager(TemporaryAssetManager assetManager)
        {
            this.assetManager = assetManager;
        }
        
        /// <summary>
        /// 一時アセットをファイルに保存
        /// </summary>
        public void SaveTemporaryAssetsToFiles(int editIndex, Renderer[] renderers, string suffix)
        {
            if (editIndex == -1 || renderers == null) return;
            
            // 選択されたメッシュを保存
            // 直接のマッチまたは親がRelatedGameObjectの場合をチェック
            var selectedMesh = assetManager.TempMeshes.FirstOrDefault(m => 
                m.RelatedGameObject == renderers[editIndex].gameObject || 
                (renderers[editIndex].transform.parent != null && m.RelatedGameObject == renderers[editIndex].transform.parent.gameObject));
            if (selectedMesh == null) return;
            
            // 選択されたメッシュを保存対象としてマーク
            selectedMesh.IsSelected = true;
            
            // アセットを保存（サフィックス付きで）
            assetManager.SaveSelectedAssets(suffix);
            
            // 注: SaveSelectedAssetsメソッド内で保存されたアセットは自動的に一時リストから削除されるため、
            // ここでRemoveTemporaryMeshを呼び出す必要はありません
            
            EditorUtility.DisplayDialog(Localization.Get("msg_asset_saved"), Localization.Get("msg_save_complete"), "OK");
        }
        
        /// <summary>
        /// 選択した頂点の削除情報をMeshRemovalDataコンポーネントに登録
        /// </summary>
        public void RegisterMeshRemoval(GameObject avatar, Renderer renderer, System.Collections.Generic.List<int> selectedVertices)
        {
            if (avatar == null || renderer == null || selectedVertices.Count == 0)
                return;
            
            // 毎回新しいMUCTMeshRemovalDataコンポーネントを追加
            var removalData = renderer.gameObject.AddComponent<MUCTMeshRemovalData>();
            
            var info = removalData.GetOrCreateRemovalInfo(renderer);
            
            info.verticesToRemove.Clear();
            info.verticesToRemove.AddRange(selectedVertices);
            
            // MeshRendererの場合はMeshFilterからメッシュを取得
            Mesh mesh = null;
            if (renderer is SkinnedMeshRenderer skinRenderer)
            {
                mesh = skinRenderer.sharedMesh;
            }
            else if (renderer is MeshRenderer)
            {
                var meshFilter = renderer.GetComponent<MeshFilter>();
                if (meshFilter != null)
                {
                    mesh = meshFilter.sharedMesh;
                }
            }
            
            if (mesh != null)
            {
                var meshPath = AssetDatabase.GetAssetPath(mesh);
                if (!string.IsNullOrEmpty(meshPath))
                {
                    info.originalMeshPath = meshPath;
                }
            }
            
            
            EditorUtility.DisplayDialog(
                Localization.Get("msg_removal_registered"), 
                $"{Localization.Get("msg_removal_info")}\nRenderer: {renderer.name}\nVertices: {selectedVertices.Count}", 
                "OK");
            
            // MeshRemovalDataコンポーネントを持つオブジェクトを選択してインスペクタに表示
            Selection.activeGameObject = renderer.gameObject;
            EditorGUIUtility.PingObject(removalData);
            
            // NDMFプレビューをリフレッシュ
            MeshRemovalUtility.RefreshNDMFPreview();
        }
    }
}