using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Object = UnityEngine.Object;

namespace vrc_yue.MeshUVCutTools.MeshModifyTool
{
    public static class MeshRemovalUtility
    {
        /// <summary>
        /// NDMFプレビューをリフレッシュする
        /// </summary>
        public static void RefreshNDMFPreview()
        {
            try
            {
                // MeshRemovalRenderFilterのTogglablePreviewNodeを取得
                var meshRemovalFilter = MeshRemovalRenderFilter.Instance;
                if (meshRemovalFilter != null)
                {
                    var previewNodes = meshRemovalFilter.GetPreviewControlNodes();
                    foreach (var node in previewNodes)
                    {
                        if (node != null && node.IsEnabled.Value)
                        {
                            // TODO 汚いリフレッシュ方法
                            // 一時的に無効化してから再度有効化することでリフレッシュ
                            node.IsEnabled.Value = false;
                            
                            // 少し待機してから再有効化（即座に切り替えると反映されない場合があるため）
                            EditorApplication.delayCall += () =>
                            {
                                if (node != null)
                                {
                                    node.IsEnabled.Value = true;
                                }
                            };
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[MeshRemovalUtility] Failed to refresh NDMF preview: {e.Message}");
            }
            
            // SceneViewを再描画
            SceneView.RepaintAll();
        }
        
        /// <summary>
        /// 複数のMeshRemovalDataを統合する
        /// </summary>
        public static void MergeRemovalData(List<MUCTMeshRemovalData> removalDataList)
        {
            if (removalDataList == null || removalDataList.Count < 2)
            {
                EditorUtility.DisplayDialog("統合エラー", "2つ以上の削除データを選択してください。", "OK");
                return;
            }
            
            // 同じGameObjectに属するもののみ統合可能
            var gameObject = removalDataList[0].gameObject;
            if (!removalDataList.All(data => data.gameObject == gameObject))
            {
                EditorUtility.DisplayDialog("統合エラー", "異なるGameObjectの削除データは統合できません。", "OK");
                return;
            }
            
            // 新しいMeshRemovalDataを作成
            var mergedData = gameObject.AddComponent<MUCTMeshRemovalData>();
            
            // 全ての削除情報を統合
            var mergedInfoDict = new Dictionary<Renderer, HashSet<int>>();
            foreach (var data in removalDataList)
            {
                foreach (var info in data.removalInfos)
                {
                    if (!mergedInfoDict.ContainsKey(info.targetRenderer))
                    {
                        mergedInfoDict[info.targetRenderer] = new HashSet<int>();
                    }
                    // 重複を除去しながら頂点インデックスを追加
                    foreach (var vertexIndex in info.verticesToRemove)
                    {
                        mergedInfoDict[info.targetRenderer].Add(vertexIndex);
                    }
                }
            }
            
            // 統合された情報をRemovalInfoに変換
            foreach (var kvp in mergedInfoDict)
            {
                var removalInfo = new MUCTMeshRemovalData.RemovalInfo(kvp.Key);
                removalInfo.verticesToRemove = kvp.Value.OrderBy(v => v).ToList();
                mergedData.removalInfos.Add(removalInfo);
            }
            
            // 元のデータを削除するか確認
            if (EditorUtility.DisplayDialog("統合完了", 
                $"{removalDataList.Count}個の削除データを統合しました。\n元のデータを削除しますか？", "削除", "保持"))
            {
                foreach (var data in removalDataList)
                {
                    Object.DestroyImmediate(data);
                }
            }
            
            // 統合されたデータを保存
            EditorUtility.SetDirty(mergedData);
            EditorUtility.SetDirty(gameObject);
        }
        public static Mesh RemoveVerticesFromMesh(Mesh originalMesh, List<int> verticesToRemove)
        {
            if (originalMesh == null || verticesToRemove == null || verticesToRemove.Count == 0)
                return originalMesh;

            var vertices = originalMesh.vertices;
            var normals = originalMesh.normals;
            var tangents = originalMesh.tangents;
            var uvs = new List<Vector2>();
            originalMesh.GetUVs(0, uvs);
            var uv2s = new List<Vector2>();
            originalMesh.GetUVs(1, uv2s);
            var uv3s = new List<Vector2>();
            originalMesh.GetUVs(2, uv3s);
            var uv4s = new List<Vector2>();
            originalMesh.GetUVs(3, uv4s);
            var uv5s = new List<Vector2>();
            originalMesh.GetUVs(4, uv5s);
            var uv6s = new List<Vector2>();
            originalMesh.GetUVs(5, uv6s);
            var uv7s = new List<Vector2>();
            originalMesh.GetUVs(6, uv7s);
            var uv8s = new List<Vector2>();
            originalMesh.GetUVs(7, uv8s);
            var colors = originalMesh.colors;
            var boneWeights = originalMesh.boneWeights;

            var vertexSet = new HashSet<int>(verticesToRemove);
            
            var newTriangles = new List<int>();
            var triangles = originalMesh.triangles;
            
            for (int i = 0; i < triangles.Length; i += 3)
            {
                var v0 = triangles[i];
                var v1 = triangles[i + 1];
                var v2 = triangles[i + 2];
                
                if (!vertexSet.Contains(v0) && !vertexSet.Contains(v1) && !vertexSet.Contains(v2))
                {
                    newTriangles.Add(v0);
                    newTriangles.Add(v1);
                    newTriangles.Add(v2);
                }
            }

            var newVertexIndices = new Dictionary<int, int>();
            var newVertices = new List<Vector3>();
            var newNormals = new List<Vector3>();
            var newTangents = new List<Vector4>();
            var newUvs = new List<Vector2>();
            var newUv2s = new List<Vector2>();
            var newUv3s = new List<Vector2>();
            var newUv4s = new List<Vector2>();
            var newUv5s = new List<Vector2>();
            var newUv6s = new List<Vector2>();
            var newUv7s = new List<Vector2>();
            var newUv8s = new List<Vector2>();
            var newColors = new List<Color>();
            var newBoneWeights = new List<BoneWeight>();

            var usedVertices = new HashSet<int>(newTriangles);
            var sortedUsedVertices = usedVertices.OrderBy(v => v).ToList();

            foreach (var oldIndex in sortedUsedVertices)
            {
                newVertexIndices[oldIndex] = newVertices.Count;
                newVertices.Add(vertices[oldIndex]);
                
                if (normals.Length > oldIndex)
                    newNormals.Add(normals[oldIndex]);
                
                if (tangents.Length > oldIndex)
                    newTangents.Add(tangents[oldIndex]);
                
                if (uvs.Count > oldIndex)
                    newUvs.Add(uvs[oldIndex]);
                
                if (uv2s.Count > oldIndex)
                    newUv2s.Add(uv2s[oldIndex]);
                
                if (uv3s.Count > oldIndex)
                    newUv3s.Add(uv3s[oldIndex]);
                
                if (uv4s.Count > oldIndex)
                    newUv4s.Add(uv4s[oldIndex]);
                
                if (uv5s.Count > oldIndex)
                    newUv5s.Add(uv5s[oldIndex]);
                
                if (uv6s.Count > oldIndex)
                    newUv6s.Add(uv6s[oldIndex]);
                
                if (uv7s.Count > oldIndex)
                    newUv7s.Add(uv7s[oldIndex]);
                
                if (uv8s.Count > oldIndex)
                    newUv8s.Add(uv8s[oldIndex]);
                
                if (colors.Length > oldIndex)
                    newColors.Add(colors[oldIndex]);
                
                if (boneWeights.Length > oldIndex)
                    newBoneWeights.Add(boneWeights[oldIndex]);
            }

            for (int i = 0; i < newTriangles.Count; i++)
            {
                newTriangles[i] = newVertexIndices[newTriangles[i]];
            }

            var newMesh = new Mesh();
            newMesh.name = originalMesh.name + "_Removed";
            newMesh.vertices = newVertices.ToArray();
            
            if (newNormals.Count > 0)
                newMesh.normals = newNormals.ToArray();
            
            if (newTangents.Count > 0)
                newMesh.tangents = newTangents.ToArray();
            
            if (newUvs.Count > 0)
                newMesh.SetUVs(0, newUvs);
            
            if (newUv2s.Count > 0)
                newMesh.SetUVs(1, newUv2s);
            
            if (newUv3s.Count > 0)
                newMesh.SetUVs(2, newUv3s);
            
            if (newUv4s.Count > 0)
                newMesh.SetUVs(3, newUv4s);
            
            if (newUv5s.Count > 0)
                newMesh.SetUVs(4, newUv5s);
            
            if (newUv6s.Count > 0)
                newMesh.SetUVs(5, newUv6s);
            
            if (newUv7s.Count > 0)
                newMesh.SetUVs(6, newUv7s);
            
            if (newUv8s.Count > 0)
                newMesh.SetUVs(7, newUv8s);
            
            if (newColors.Count > 0)
                newMesh.colors = newColors.ToArray();
            
            if (newBoneWeights.Count > 0)
                newMesh.boneWeights = newBoneWeights.ToArray();

            newMesh.triangles = newTriangles.ToArray();
            newMesh.bindposes = originalMesh.bindposes;

            newMesh.RecalculateBounds();

            for (int i = 0; i < originalMesh.blendShapeCount; i++)
            {
                var shapeName = originalMesh.GetBlendShapeName(i);
                var frameCount = originalMesh.GetBlendShapeFrameCount(i);
                
                for (int frame = 0; frame < frameCount; frame++)
                {
                    var weight = originalMesh.GetBlendShapeFrameWeight(i, frame);
                    var deltaVertices = new Vector3[vertices.Length];
                    var deltaNormals = new Vector3[vertices.Length];
                    var deltaTangents = new Vector3[vertices.Length];
                    
                    originalMesh.GetBlendShapeFrameVertices(i, frame, deltaVertices, deltaNormals, deltaTangents);
                    
                    var newDeltaVertices = new List<Vector3>();
                    var newDeltaNormals = new List<Vector3>();
                    var newDeltaTangents = new List<Vector3>();
                    
                    foreach (var oldIndex in sortedUsedVertices)
                    {
                        newDeltaVertices.Add(deltaVertices[oldIndex]);
                        newDeltaNormals.Add(deltaNormals[oldIndex]);
                        newDeltaTangents.Add(deltaTangents[oldIndex]);
                    }
                    
                    newMesh.AddBlendShapeFrame(shapeName, weight, 
                        newDeltaVertices.ToArray(), 
                        newDeltaNormals.ToArray(), 
                        newDeltaTangents.ToArray());
                }
            }

            newMesh.subMeshCount = originalMesh.subMeshCount;
            for (int i = 0; i < originalMesh.subMeshCount; i++)
            {
                var subMesh = originalMesh.GetSubMesh(i);
                var subMeshTriangles = new List<int>();
                
                for (int j = subMesh.indexStart; j < subMesh.indexStart + subMesh.indexCount; j += 3)
                {
                    var v0 = triangles[j];
                    var v1 = triangles[j + 1];
                    var v2 = triangles[j + 2];
                    
                    if (!vertexSet.Contains(v0) && !vertexSet.Contains(v1) && !vertexSet.Contains(v2))
                    {
                        subMeshTriangles.Add(newVertexIndices[v0]);
                        subMeshTriangles.Add(newVertexIndices[v1]);
                        subMeshTriangles.Add(newVertexIndices[v2]);
                    }
                }
                
                if (subMeshTriangles.Count > 0)
                {
                    newMesh.SetTriangles(subMeshTriangles, i);
                }
            }

            return newMesh;
        }
    }
}