using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using nadena.dev.ndmf;
using nadena.dev.ndmf.preview;
using UnityEngine;
using nadena.dev.modular_avatar.core;
using nadena.dev.modular_avatar.core.vertex_filters;

[assembly: ExportsPlugin(typeof(vrc_yue.MeshUVCutTools.MeshModifyTool.MeshRemovalPlugin))]

namespace vrc_yue.MeshUVCutTools.MeshModifyTool
{
    public class MeshRemovalPlugin : Plugin<MeshRemovalPlugin>
    {
        public override string QualifiedName => "vrc_yue.meshuvcut.meshremoval";
        public override string DisplayName => "Mesh Removal (MeshUVCutTool)";

        protected override void Configure()
        {
            // ModularAvatarより前に実行して変換処理を行う
            InPhase(BuildPhase.Resolving)
                .BeforePlugin("nadena.dev.modular-avatar")
                .Run("Convert to MA MeshCutter", ctx =>
                {
                    var removalDataComponents = ctx.AvatarRootObject.GetComponentsInChildren<MUCTMeshRemovalData>(true);
                    
                    foreach (var removalData in removalDataComponents)
                    {
                        // ModularAvatarで削除モードの場合のみ処理
                        if (removalData.enabled && removalData.UseModularAvatar)
                        {
                            ConvertToMeshCutter(removalData);
                        }
                    }
                });
                
            InPhase(BuildPhase.Transforming)
                .Run("Apply Mesh Removal", ctx =>
                {
                    // すべてのMUCTMeshRemovalDataコンポーネントを収集（メッシュオブジェクトに直接アタッチされている）
                    var removalDataComponents = ctx.AvatarRootObject.GetComponentsInChildren<MUCTMeshRemovalData>(true);
                    
                    // レンダラーごとに全ての削除頂点を集計
                    var verticesByRenderer = new Dictionary<Renderer, HashSet<int>>();
                    
                    foreach (var removalData in removalDataComponents)
                    {
                        // コンポーネントが無効な場合、またはModularAvatar使用モードの場合はスキップ
                        if (!removalData.enabled || removalData.UseModularAvatar) continue;
                        
                        // MUCTMeshRemovalDataがアタッチされているGameObjectのRendererを取得
                        var renderer = removalData.GetComponent<Renderer>();
                        if (renderer == null) continue;
                        
                        foreach (var info in removalData.removalInfos)
                        {
                            // targetRendererがこのGameObjectのRendererと一致することを確認
                            if (info.targetRenderer != renderer || info.verticesToRemove.Count == 0)
                                continue;
                            
                            if (!verticesByRenderer.ContainsKey(renderer))
                                verticesByRenderer[renderer] = new HashSet<int>();
                            
                            // 頂点を追加（重複は自動的に除外される）
                            foreach (var vertex in info.verticesToRemove)
                                verticesByRenderer[renderer].Add(vertex);
                        }
                    }
                    
                    // 集計した頂点データで実際のメッシュ削除を実行
                    foreach (var kvp in verticesByRenderer)
                    {
                        var renderer = kvp.Key;
                        var vertices = kvp.Value.ToList();
                        
                        Mesh originalMesh = null;
                        if (renderer is SkinnedMeshRenderer skinRenderer)
                        {
                            originalMesh = skinRenderer.sharedMesh;
                        }
                        else if (renderer is MeshRenderer meshRenderer)
                        {
                            var meshFilter = meshRenderer.GetComponent<MeshFilter>();
                            if (meshFilter != null)
                            {
                                originalMesh = meshFilter.sharedMesh;
                            }
                        }
                        
                        if (originalMesh == null)
                            continue;
                        
                        var newMesh = MeshRemovalUtility.RemoveVerticesFromMesh(originalMesh, vertices);
                        newMesh.name = originalMesh.name + "_MeshRemoval";
                        
                        // メッシュを適用
                        if (renderer is SkinnedMeshRenderer skinMeshRenderer)
                        {
                            skinMeshRenderer.sharedMesh = newMesh;
                        }
                        else if (renderer is MeshRenderer)
                        {
                            var meshFilter = renderer.GetComponent<MeshFilter>();
                            if (meshFilter != null)
                            {
                                meshFilter.sharedMesh = newMesh;
                            }
                        }
                    }
                    
                    // 全てのMUCTMeshRemovalDataコンポーネントを削除
                    foreach (var removalData in removalDataComponents)
                    {
                        Object.DestroyImmediate(removalData);
                    }
                })
                .PreviewingWith(MeshRemovalRenderFilter.Instance);
        }
        
        private static void ConvertToMeshCutter(MUCTMeshRemovalData removalData)
        {
            var renderer = removalData.GetComponent<SkinnedMeshRenderer>();
            if (renderer == null || renderer.sharedMesh == null) return;
            
            // 子GameObjectからModularAvatarコンポーネントを探す
            GameObject toggleObject = null;
            for (int i = 0; i < removalData.transform.childCount; i++)
            {
                var child = removalData.transform.GetChild(i);
                if (child.name == "MUCT Delete Toggle")
                {
                    toggleObject = child.gameObject;
                    break;
                }
            }
            
            if (toggleObject == null)
            {
                Debug.LogError($"[MeshUVCutTool] トグルオブジェクトが見つかりません: {removalData.gameObject.name}");
                return;
            }
            
            var meshCutter = toggleObject.GetComponent<ModularAvatarMeshCutter>();
            var shapeFilter = toggleObject.GetComponent<VertexFilterByShapeComponent>();
            if (meshCutter == null || shapeFilter == null)
            {
                Debug.LogError($"[MeshUVCutTool] ModularAvatarコンポーネントが見つかりません: {toggleObject.name}");
                return;
            }
            
            // 全ての削除頂点を収集
            var allVerticesToRemove = new HashSet<int>();
            foreach (var info in removalData.removalInfos)
            {
                if (info.targetRenderer == renderer)
                {
                    foreach (var vertex in info.verticesToRemove)
                    {
                        allVerticesToRemove.Add(vertex);
                    }
                }
            }
            
            if (allVerticesToRemove.Count == 0) return;
            
            // VertexFilterByShapeComponentから設定されたBlendShape名を取得
            string shapeName = "MUCT_Delete"; // デフォルト
            if (shapeFilter != null && shapeFilter.Shapes.Count > 0)
            {
                shapeName = shapeFilter.Shapes[0];
                Debug.Log($"[MeshUVCutTool] BlendShape名: {shapeName}, Threshold: {shapeFilter.Threshold}");
            }
            
            // すでにBlendShapeが存在する場合はスキップ
            var existingIndex = renderer.sharedMesh.GetBlendShapeIndex(shapeName);
            if (existingIndex >= 0)
            {
                return;
            }
            
            // メッシュを複製（非破壊）
            var mesh = Object.Instantiate(renderer.sharedMesh);
            mesh.name = renderer.sharedMesh.name + "_MUCT";
            
            // 削除用BlendShapeを追加
            AddDeletionBlendShape(mesh, allVerticesToRemove, shapeName);
            
            // メッシュを適用
            renderer.sharedMesh = mesh;
            
            Debug.Log($"[MeshUVCutTool] BlendShape追加完了: {shapeName} (頂点数: {allVerticesToRemove.Count})");
            
            // MUCTMeshRemovalDataは削除しない（エディタでの切り替えのため）
        }
        
        private static void AddDeletionBlendShape(Mesh mesh, HashSet<int> verticesToRemove, string shapeName)
        {
            var vertices = mesh.vertices;
            var deltaPositions = new Vector3[vertices.Length];
            var deltaNormals = new Vector3[vertices.Length];
            var deltaTangents = new Vector3[vertices.Length];
            
            // 削除する頂点を大きく移動させる（VertexFilterByShapeが検出できるように）
            foreach (var vertexIndex in verticesToRemove)
            {
                if (vertexIndex >= 0 && vertexIndex < vertices.Length)
                {
                    deltaPositions[vertexIndex] = Vector3.one * 10f; // 大きく移動
                }
            }
            
            // BlendShapeを追加
            mesh.AddBlendShapeFrame(shapeName, 100f, deltaPositions, deltaNormals, deltaTangents);
        }
    }
    
    internal class MeshRemovalRenderInfo
    {
        public MUCTMeshRemovalData.RemovalInfo removalInfo;
        public MUCTMeshRemovalData removalData;
        public bool useBlendShape;
    }
    
    internal class MeshRemovalRenderFilter : IRenderFilter
    {
        private readonly TogglablePreviewNode _toggleNode;
        
        private MeshRemovalRenderFilter()
        {
            _toggleNode = TogglablePreviewNode.Create(
                () => "Mesh Removal Preview", 
                "yue.meshuvcut.meshremoval"
            );
        }
        
        public static MeshRemovalRenderFilter Instance { get; } = new MeshRemovalRenderFilter();
        
        public IEnumerable<TogglablePreviewNode> GetPreviewControlNodes() => new[] { _toggleNode };
        public bool IsEnabled(ComputeContext context) => context.Observe(_toggleNode.IsEnabled);
        
        public ImmutableList<RenderGroup> GetTargetGroups(ComputeContext context)
        {
            var removalDataComponents = context.GetComponentsByType<MUCTMeshRemovalData>();
            var verticesByRenderer = new Dictionary<Renderer, List<int>>();
            var blendShapeRemovalByRenderer = new Dictionary<Renderer, MUCTMeshRemovalData>();
            
            // レンダラーごとに全ての削除頂点を集計
            foreach (var removalData in removalDataComponents)
            {
                // コンポーネント自体を監視（enabled状態の変更も含む）
                context.Observe(removalData);
                
                // コンポーネントが無効な場合はスキップ
                if (!removalData.enabled) continue;
                
                // MUCTMeshRemovalDataがアタッチされているGameObjectのRendererを取得
                var renderer = removalData.GetComponent<Renderer>();
                if (renderer == null) continue;
                
                // メッシュの存在確認
                bool hasMesh = false;
                if (renderer is SkinnedMeshRenderer skinRenderer && skinRenderer.sharedMesh != null)
                    hasMesh = true;
                else if (renderer is MeshRenderer)
                {
                    var meshFilter = renderer.GetComponent<MeshFilter>();
                    hasMesh = meshFilter != null && meshFilter.sharedMesh != null;
                }
                
                if (!hasMesh) continue;
                
                // ModularAvatar使用モードの場合はBlendShapeプレビュー用に別処理
                if (removalData.UseModularAvatar)
                {
                    blendShapeRemovalByRenderer[renderer] = removalData;
                    continue;
                }
                
                // 通常削除モード
                foreach (var info in removalData.removalInfos)
                {
                    // targetRendererがこのGameObjectのRendererと一致する場合のみ処理
                    if (info.targetRenderer == renderer && 
                        info.verticesToRemove.Count > 0)
                    {
                        if (!verticesByRenderer.ContainsKey(renderer))
                            verticesByRenderer[renderer] = new List<int>();
                        
                        // 頂点を追加
                        verticesByRenderer[renderer].AddRange(info.verticesToRemove);
                    }
                }
            }
            
            // 集計した結果をRenderGroupとして生成
            var groups = new List<RenderGroup>();
            
            // 通常削除モードのグループ
            foreach (var kvp in verticesByRenderer)
            {
                var renderer = kvp.Key;
                var vertices = kvp.Value.Distinct().ToList(); // 重複を除去
                
                if (vertices.Count > 0)
                {
                    var aggregatedInfo = new MeshRemovalRenderInfo
                    {
                        removalInfo = new MUCTMeshRemovalData.RemovalInfo(renderer)
                        {
                            verticesToRemove = vertices
                        },
                        useBlendShape = false
                    };
                    groups.Add(RenderGroup.For(renderer).WithData(aggregatedInfo));
                }
            }
            
            // BlendShape削除モードのグループ
            foreach (var kvp in blendShapeRemovalByRenderer)
            {
                var renderer = kvp.Key;
                var removalData = kvp.Value;
                
                var aggregatedInfo = new MeshRemovalRenderInfo
                {
                    removalData = removalData,
                    useBlendShape = true
                };
                groups.Add(RenderGroup.For(renderer).WithData(aggregatedInfo));
            }
            
            return groups.ToImmutableList();
        }
        
        public async Task<IRenderFilterNode> Instantiate(
            RenderGroup group,
            IEnumerable<(Renderer, Renderer)> proxyPairs,
            ComputeContext context)
        {
            var pair = proxyPairs.Single();
            var original = pair.Item1;
            var proxy = pair.Item2;
            
            var renderInfo = group.GetData<MeshRemovalRenderInfo>();
            
            if (renderInfo.useBlendShape)
            {
                var node = new MeshRemovalBlendShapeRenderFilterNode();
                await node.Process(original, proxy, renderInfo.removalData);
                return node;
            }
            else
            {
                var node = new MeshRemovalRenderFilterNode();
                await node.Process(original, proxy, renderInfo.removalInfo);
                return node;
            }
        }
    }
    
    internal class MeshRemovalRenderFilterNode : IRenderFilterNode
    {
        private Mesh _duplicated;
        
        public RenderAspects WhatChanged => RenderAspects.Mesh;
        
#pragma warning disable CS1998
        public async Task Process(
            Renderer original,
            Renderer proxy,
            MUCTMeshRemovalData.RemovalInfo info)
#pragma warning restore CS1998
        {
            Mesh originalMesh = null;
            
            // プロキシからメッシュを取得
            if (proxy is SkinnedMeshRenderer skinProxy)
            {
                originalMesh = skinProxy.sharedMesh;
            }
            else if (proxy is MeshRenderer)
            {
                var meshFilter = proxy.GetComponent<MeshFilter>();
                if (meshFilter != null)
                {
                    originalMesh = meshFilter.sharedMesh;
                }
            }
            
            if (originalMesh == null) return;
            
            // Clone the mesh
            _duplicated = Object.Instantiate(originalMesh);
            _duplicated.name = originalMesh.name + " (MeshRemoval Preview)";
            
            // Apply removal
            var newMesh = MeshRemovalUtility.RemoveVerticesFromMesh(
                _duplicated, 
                info.verticesToRemove
            );
            
            // Replace the mesh
            if (proxy is SkinnedMeshRenderer skinMeshProxy)
            {
                skinMeshProxy.sharedMesh = newMesh;
            }
            else if (proxy is MeshRenderer)
            {
                var meshFilter = proxy.GetComponent<MeshFilter>();
                if (meshFilter != null)
                {
                    meshFilter.sharedMesh = newMesh;
                }
            }
            
            _duplicated = newMesh;
        }
        
        public void OnFrame(Renderer original, Renderer proxy)
        {
            if (_duplicated == null) return;
            
            if (proxy is SkinnedMeshRenderer smr)
            {
                smr.sharedMesh = _duplicated;
            }
            else if (proxy is MeshRenderer)
            {
                var meshFilter = proxy.GetComponent<MeshFilter>();
                if (meshFilter != null)
                {
                    meshFilter.sharedMesh = _duplicated;
                }
            }
        }
        
        public void Dispose()
        {
            if (_duplicated != null)
            {
                Object.DestroyImmediate(_duplicated);
                _duplicated = null;
            }
        }
    }
    
    internal class MeshRemovalBlendShapeRenderFilterNode : IRenderFilterNode
    {
        private Mesh _duplicated;
        private string _shapeName;
        private int _shapeIndex = -1;
        
        public RenderAspects WhatChanged => RenderAspects.Mesh;
        
#pragma warning disable CS1998
        public async Task Process(
            Renderer original,
            Renderer proxy,
            MUCTMeshRemovalData removalData)
#pragma warning restore CS1998
        {
            if (!(proxy is SkinnedMeshRenderer skinProxy)) return;
            
            var originalMesh = skinProxy.sharedMesh;
            if (originalMesh == null) return;
            
            // 子GameObjectからVertexFilterByShapeComponentを探してBlendShape名を取得
            GameObject toggleObject = null;
            for (int i = 0; i < removalData.transform.childCount; i++)
            {
                var child = removalData.transform.GetChild(i);
                if (child.name == "MUCT Delete Toggle")
                {
                    toggleObject = child.gameObject;
                    break;
                }
            }
            
            if (toggleObject != null)
            {
                var shapeFilter = toggleObject.GetComponent<VertexFilterByShapeComponent>();
                if (shapeFilter != null && shapeFilter.Shapes.Count > 0)
                {
                    _shapeName = shapeFilter.Shapes[0];
                }
            }
            
            // BlendShape名が取得できない場合はデフォルト名を使用
            if (string.IsNullOrEmpty(_shapeName))
            {
                _shapeName = "MUCT_Delete_Preview";
            }
            
            // 全ての削除頂点を収集
            var allVerticesToRemove = new HashSet<int>();
            foreach (var info in removalData.removalInfos)
            {
                if (info.targetRenderer == original)
                {
                    foreach (var vertex in info.verticesToRemove)
                    {
                        allVerticesToRemove.Add(vertex);
                    }
                }
            }
            
            if (allVerticesToRemove.Count == 0) return;
            
            // メッシュを複製
            _duplicated = Object.Instantiate(originalMesh);
            _duplicated.name = originalMesh.name + " (BlendShape Preview)";
            
            // 既存のBlendShapeをチェック
            _shapeIndex = _duplicated.GetBlendShapeIndex(_shapeName);
            
            // BlendShapeが存在しない場合は追加
            if (_shapeIndex < 0)
            {
                var vertices = _duplicated.vertices;
                var deltaPositions = new Vector3[vertices.Length];
                var deltaNormals = new Vector3[vertices.Length];
                var deltaTangents = new Vector3[vertices.Length];
                
                // 削除する頂点を大きく移動させる
                foreach (var vertexIndex in allVerticesToRemove)
                {
                    if (vertexIndex >= 0 && vertexIndex < vertices.Length)
                    {
                        deltaPositions[vertexIndex] = Vector3.one * 10f; // 大きく移動
                    }
                }
                
                // BlendShapeを追加
                _duplicated.AddBlendShapeFrame(_shapeName, 100f, deltaPositions, deltaNormals, deltaTangents);
                _shapeIndex = _duplicated.blendShapeCount - 1;
            }
            
            // メッシュを適用
            skinProxy.sharedMesh = _duplicated;
            
            // BlendShape値を100に設定（削除効果を表示）
            skinProxy.SetBlendShapeWeight(_shapeIndex, 100f);
        }
        
        public void OnFrame(Renderer original, Renderer proxy)
        {
            if (_duplicated == null || !(proxy is SkinnedMeshRenderer skinProxy)) return;
            
            skinProxy.sharedMesh = _duplicated;
            
            // BlendShape値を維持
            if (_shapeIndex >= 0)
            {
                skinProxy.SetBlendShapeWeight(_shapeIndex, 100f);
            }
        }
        
        public void Dispose()
        {
            if (_duplicated != null)
            {
                Object.DestroyImmediate(_duplicated);
                _duplicated = null;
            }
        }
    }
}