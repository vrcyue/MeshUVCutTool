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
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace vrc_yue.MeshUVCutTools.Core
{
    /// <summary>
    /// 編集画面のカメラ画像出すための補助クラス
    /// </summary>
    public class AvatarMonitor
    {
        private GameObject baseObject;
        private Camera camera;
        private RenderTexture targetTexture;
        private Rect rect;

        private const int previewLayer = 2;
        
        // SceneViewモード用の追加フィールド
        private bool useSceneViewMode = false;
        private SceneView activeSceneView = null;

        public float dragSpeedRate { get; set; } = 1f;
        public float scrollSpeedRate { get; set; } = 1f;
        public float rotateSpeedRate { get; set; } = 1f;

        private float dragSpeedBase = 0.1f;
        private float scrollSpeedBase = 0.01f;
        private float rotateSpeedBase = 0.1f;
        
        private float dragSpeed => dragSpeedBase * dragSpeedRate;
        private float scrollSpeed => scrollSpeedBase * scrollSpeedRate;
        private float rotateSpeed => rotateSpeedBase * rotateSpeedRate;

        private float layRange = 50f;

        private float bound = 1f;
        public float GetBound => bound;

        private Vector2 dragStartPosition;
        private bool isDragging;

        private List<GameObject> debugMarkers = new List<GameObject>();
        private MeshCollider meshCollider;

        private bool enableBackfaceCulling = true; // 裏面選択を制御するフラグ

        /// <summary>
        /// 裏面の頂点を選択対象から除外するかどうか
        /// </summary>
        private bool isRightMouseDown = false;
        private float speedMultiplier = 1.0f;

        public bool EnableBackfaceCulling
        {
            get { return enableBackfaceCulling; }
            set { enableBackfaceCulling = value; }
        }
        
        /// <summary>
        /// SceneViewモードの切り替え
        /// </summary>
        public void SetSceneViewMode(bool enable, SceneView sceneView = null)
        {
            useSceneViewMode = enable;
            activeSceneView = sceneView ?? SceneView.lastActiveSceneView;
        }
        
        /// <summary>
        /// 現在のカメラを取得（モードに応じて切り替え）
        /// </summary>
        private Camera GetActiveCamera()
        {
            if (useSceneViewMode && activeSceneView != null)
                return activeSceneView.camera;
            return camera;
        }



        public AvatarMonitor(Transform root)
        {
            baseObject = new GameObject("CameraRoot");
            baseObject.transform.SetParent(root);
            baseObject.transform.localPosition = Vector3.zero;
            ;
            baseObject.transform.localRotation = Quaternion.identity;
            baseObject.hideFlags = HideFlags.HideAndDontSave;

            var c = new GameObject("Camera");
            c.transform.SetParent(baseObject.transform);
            c.transform.localPosition = new Vector3(0f, 0f, -0.03f);
            ;
            c.transform.localRotation = Quaternion.identity;
            c.hideFlags = HideFlags.HideAndDontSave;

            camera = c.AddComponent<Camera>();
            targetTexture = new RenderTexture(1000, 1000, 1);
            camera.targetTexture = targetTexture;

            var bounds = GetMaxBounds(root.gameObject);
            bound = Mathf.Max(bounds.extents.x , bounds.extents.y , bounds.extents.z);
            camera.nearClipPlane = bound*0.01f;
            camera.farClipPlane = bound*10f;
        }
        
        Bounds GetMaxBounds(GameObject g) {
            var renderers = g.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return new Bounds(g.transform.position, Vector3.zero);
            var b = renderers[0].bounds;
            foreach (Renderer r in renderers) {
                b.Encapsulate(r.bounds);
            }
            return b;
        }

        ~AvatarMonitor()
        {
            Release();
        }

        public Vector3 WorldSpaceCameraVec()
        {
            return camera.transform.forward;
        }
        
        public Vector3 WorldSpaceCameraUp()
        {
            return camera.transform.up;
        }

        public void Release()
        {
            GameObject.DestroyImmediate(baseObject.gameObject);
            targetTexture.Release();
        }

        public void ResizeTexture(int width, int height)
        {
            targetTexture = new RenderTexture(width, height, 1);
            camera.targetTexture = targetTexture;
        }

        public Texture GetTexture()
        {
            return targetTexture as Texture;
            ;
        }

        public void SetSpeed(float move = 1f, float rotate = 1f, float wheel = 1f)
        {
            dragSpeedRate = move;
            rotateSpeedRate = rotate;
            scrollSpeedRate = wheel;
        }

        public void Display(int width, int height, int rotationDrag = 1, int positionDrag = 2,bool canTouch = true,bool canWheel = true)
        {
            if (width != targetTexture.width || height != targetTexture.height)
            {
                ResizeTexture(width, height);
            }

            rect = GUILayoutUtility.GetRect(targetTexture.width, targetTexture.height, GUI.skin.box);
            EditorGUI.DrawPreviewTexture(rect, GetTexture());

            var e = Event.current;

            if (rect.Contains(e.mousePosition))
            {
                if (canTouch)
                {
                    if (e.type == EventType.MouseDrag && e.button == rotationDrag)
                    {
                        // Drag
                        var r = baseObject.transform.rotation;
                        baseObject.transform.RotateAround(baseObject.transform.position, Vector3.up,
                            e.delta.x * rotateSpeed);
                        baseObject.transform.RotateAround(baseObject.transform.position, baseObject.transform.right,
                            e.delta.y * 0.1f);
                    }
                    if (e.type == EventType.MouseDrag && e.button == positionDrag)
                    {
                        // Drag
                        var r = baseObject.transform.rotation;
                        baseObject.transform.position =
                            baseObject.transform.position + camera.transform.up * e.delta.y * dragSpeed * bound;
                        baseObject.transform.position =
                            baseObject.transform.position + camera.transform.right * -e.delta.x * dragSpeed * bound;
                    }

                    // WASDキーでのカメラ操作
                    if (rect.Contains(e.mousePosition))
                    {
                        // マウスの右クリック状態を確認
                        if (e.type == EventType.MouseDown && e.button == rotationDrag)
                        {
                            isRightMouseDown = true;
                        }

                        // 右クリック中のスクロールで移動速度を調整
                        if (e.type == EventType.ScrollWheel && isRightMouseDown)
                        {
                            float scrollDelta = -e.delta.y * 0.1f;
                            speedMultiplier = Mathf.Clamp(speedMultiplier + scrollDelta, 0.1f, 10f);
                            e.Use();
                        }

                        // WASDキーでのカメラ操作
                        if (e.type == EventType.KeyDown && isRightMouseDown)
                        {
                            float speed = dragSpeedBase * bound * speedMultiplier;
                            if (e.shift) { speed *= 4f; }
                            if (e.control) { speed *= 0.25f; }

                            Vector3 movement = Vector3.zero;
                            switch (e.keyCode)
                            {
                                case KeyCode.W:
                                    movement = camera.transform.forward;
                                    break;
                                case KeyCode.S:
                                    movement = -camera.transform.forward;
                                    break;
                                case KeyCode.A:
                                    movement = -camera.transform.right;
                                    break;
                                case KeyCode.D:
                                    movement = camera.transform.right;
                                    break;
                            }

                            if (movement != Vector3.zero)
                            {
                                baseObject.transform.position += movement * speed;
                                EditorUtility.SetDirty(baseObject);
                                SceneView.RepaintAll();
                                e.Use();
                            }
                        }
                    }
                }
                if(canWheel)
                {
                    if (e.type == EventType.ScrollWheel)
                    {
                        baseObject.transform.position =
                            baseObject.transform.position + camera.transform.forward * -e.delta.y * scrollSpeed * bound;
                    }
                }
            }

            camera.Render();
        }

        public bool IsInDisplay(Vector2 pos)
        {
            if (useSceneViewMode)
            {
                // SceneViewモードでは常にtrue
                return true;
            }
            return rect.Contains(pos);
        }

        public void GetControllPoint(MeshCollider meshCollider, bool isSelectVertex, Action<Vector3> onHit)
        {
            var e = Event.current;
            if (useSceneViewMode || rect.Contains(e.mousePosition))
            {
                Ray ray = GetRayFromMousePosition(e.mousePosition);
                if (isSelectVertex)
                {
                    GetVertexPosition(meshCollider, ray, h => { onHit?.Invoke(h); });
                }
                else
                {
                    GetHit(meshCollider, ray, h => { onHit?.Invoke(h); });
                }
            }
        }
        
        /// <summary>
        /// マウス位置からRayを生成（モードに応じて切り替え）
        /// </summary>
        private Ray GetRayFromMousePosition(Vector2 mousePos)
        {
            if (useSceneViewMode)
            {
                return HandleUtility.GUIPointToWorldRay(mousePos);
            }
            else
            {
                var p = new Vector3(mousePos.x - rect.x, 
                    targetTexture.height - mousePos.y + rect.y, 1f);
                return camera.ScreenPointToRay(p);
            }
        }

        public void GetHit(MeshCollider meshCollider, Ray ray, Action<Vector3> onHit = null)
        {
            var hits = Physics.RaycastAll(ray, bound*layRange, 1 << previewLayer);

            foreach (var hit in hits)
            {
                MeshCollider mc = hit.collider as MeshCollider;

                if (mc == meshCollider)
                {
                    onHit?.Invoke(meshCollider.transform.InverseTransformPoint(hit.point));
                    return;
                }
            }
        }

        public void GetTriangle(MeshCollider meshCollider, Action<int> onhit = null)
        {
            var e = Event.current;
            if (useSceneViewMode || rect.Contains(e.mousePosition))
            {
                var ray = GetRayFromMousePosition(e.mousePosition);
                var hits = Physics.RaycastAll(ray, bound*layRange, 1 << previewLayer);

                foreach (var hit in hits)
                {
                    MeshCollider mc = hit.collider as MeshCollider;

                    if (mc == meshCollider)
                    {
                        onhit?.Invoke(hit.triangleIndex);
                        return;
                    }
                }
            }
        }
        
        public void GetTriangle(MeshCollider meshCollider, Action<int,Vector3> onhit = null)
        {
            var e = Event.current;
            
            if (useSceneViewMode || rect.Contains(e.mousePosition))
            {
                var ray = GetRayFromMousePosition(e.mousePosition);
                var hits = Physics.RaycastAll(ray, bound*layRange, 1 << previewLayer);

                foreach (var hit in hits)
                {
                    MeshCollider mc = hit.collider as MeshCollider;

                    if (mc == meshCollider)
                    {
                        onhit?.Invoke(hit.triangleIndex,meshCollider.transform.InverseTransformPoint(hit.point));
                        return;
                    }
                }
            }
        }
        
        public void GetDragTriangle(MeshCollider meshCollider, Action<int,Vector3,int,Vector3> onhit = null)
        {
            var e = Event.current;
            
            if (rect.Contains(e.mousePosition))
            {
                var p = new Vector3(e.mousePosition.x - rect.x, rect.height - e.mousePosition.y + rect.y,1f);
                var ray = camera.ScreenPointToRay(p);
                var hits = Physics.RaycastAll(ray, bound*layRange, 1 << previewLayer);
                
                // drag前
                var pd = new Vector3(e.mousePosition.x - rect.x - e.delta.x, rect.height - e.mousePosition.y + rect.y + e.delta.y,1f);
                var rayd = camera.ScreenPointToRay(pd);
                var hitsd = Physics.RaycastAll(rayd, bound*layRange, 1 << previewLayer);

                foreach (var hit in hits)
                {
                    MeshCollider mc = hit.collider as MeshCollider;
                    if (mc == meshCollider)
                    {
                        foreach (var hitd in hitsd)
                        {
                            MeshCollider mcd = hitd.collider as MeshCollider;

                            if (mcd == meshCollider)
                            {
                                onhit?.Invoke(hitd.triangleIndex,
                                    meshCollider.transform.InverseTransformPoint(hitd.point),
                                    hit.triangleIndex,
                                    meshCollider.transform.InverseTransformPoint(hit.point));
                                return;
                            }
                        }
                        return;
                    }
                }
            
            }
        }
        
        public void GetVertex(MeshCollider meshCollider, Action<int,Vector3> onHit)
        {
            var e = Event.current;
            
            if (useSceneViewMode || rect.Contains(e.mousePosition))
            {
                var ray = GetRayFromMousePosition(e.mousePosition);
                var hits = Physics.RaycastAll(ray, bound*layRange, 1 << previewLayer);

                foreach (var hit in hits)
                {
                    MeshCollider mc = hit.collider as MeshCollider;

                    if (mc == meshCollider)
                    {
                        Mesh mesh = meshCollider.sharedMesh;
                        Vector3[] vertices = mesh.vertices;
                        int[] triangles = mesh.triangles;
                        int[] indexs = new int[3] {triangles[hit.triangleIndex * 3 + 0],triangles[hit.triangleIndex * 3 + 1],triangles[hit.triangleIndex * 3 + 2]};

                        int i = GetNearIndex(
                            meshCollider.transform.InverseTransformPoint(hit.point),
                            new Vector3[] {vertices[indexs[0]], vertices[indexs[1]], vertices[indexs[2]]});

                        int index = indexs[i];

                        onHit?.Invoke(index,vertices[index]);

                        return;
                    }
                }
            }
        }

        public void GetVertexPosition(MeshCollider meshCollider, Ray ray, Action<Vector3> onhit = null)
        {
            var hits = Physics.RaycastAll(ray, bound*layRange, 1 << previewLayer);

            foreach (var hit in hits)
            {
                MeshCollider mc = hit.collider as MeshCollider;

                if (mc == meshCollider)
                {
                    Mesh mesh = meshCollider.sharedMesh;
                    Vector3[] vertices = mesh.vertices;
                    int[] triangles = mesh.triangles;
                    Vector3 p0 = vertices[triangles[hit.triangleIndex * 3 + 0]];
                    Vector3 p1 = vertices[triangles[hit.triangleIndex * 3 + 1]];
                    Vector3 p2 = vertices[triangles[hit.triangleIndex * 3 + 2]];

                    onhit?.Invoke(GetNearPoint(
                        meshCollider.transform.InverseTransformPoint(hit.point),
                        new Vector3[] {p0, p1, p2}));

                    return;
                }
            }
        }

        Vector3 GetNearPoint(Vector3 origin, Vector3[] points)
        {
            float near = 1000f;
            Vector3 output = Vector3.zero;
            foreach (var point in points)
            {
                var d = Vector3.Distance(origin, point);
                if (d < near)
                {
                    output = point;
                    near = d;
                }
            }

            return output;
        }
        
        int GetNearIndex(Vector3 origin, Vector3[] points)
        {
            float near = 1000f;
            int output = -1;
            for (int i = 0; i < points.Length; i++)
            {
                var d = Vector3.Distance(origin, points[i]);
                if (d < near)
                {
                    output = i;
                    near = d;
                }
            }

            return output;
        }


        /// <summary>
        /// 選択範囲内に含まれる頂点を検出し、コールバックで通知する
        /// </summary>
        /// <param name="meshCollider">対象のメッシュコライダー</param>
        /// <param name="selectionRect">GUI座標系での選択範囲</param>
        /// <param name="onVertexFound">頂点が見つかった時のコールバック。引数は頂点のインデックス</param>
        public void GetVerticesInRect(MeshCollider meshCollider, Rect selectionRect, Action<int> onVertexFound)
        {
            // Debug.Log($"GetVerticesInRect - Start checking vertices in rect: {selectionRect}");
            if (meshCollider == null || onVertexFound == null)
            {
                Debug.LogError("GetVerticesInRect - meshCollider or callback is null");
                return;
            }

            var mesh = meshCollider.sharedMesh;
            if (mesh == null)
            {
                Debug.LogError("GetVerticesInRect - mesh is null");
                return;
            }

            // デバッグ表示用の選択範囲の可視化
            // DebugVisualizeSelectionBox(meshCollider, selectionRect);

            // 各頂点をチェック
            var vertices = mesh.vertices;
            var normals = mesh.normals;
            int foundCount = 0;

            for (int i = 0; i < vertices.Length; i++)
            {
                var localPos = vertices[i];
                // ワールド座標に変換
                var worldPos = meshCollider.transform.TransformPoint(localPos);
                
                if (enableBackfaceCulling)
                {
                    var activeCamera = GetActiveCamera();
                    
                    // カメラからの視線ベクトル
                    var viewDirection = (worldPos - activeCamera.transform.position).normalized;
                    
                    // 法線をワールド座標系に変換
                    var worldNormal = meshCollider.transform.TransformDirection(normals[i]).normalized;
                    
                    // 表面かどうかを判定（内積が負なら表）
                    var dotProduct = Vector3.Dot(viewDirection, worldNormal);
                    if (dotProduct >= 0) continue; // 裏面は選択しない
                    
                    // レイキャストで可視判定
                    var ray = new Ray(activeCamera.transform.position, worldPos - activeCamera.transform.position);
                    var hits = Physics.RaycastAll(ray, bound*layRange, 1 << previewLayer);
                    
                    // 最前面のヒットポイントを見つける
                    float nearestHitDistance = float.MaxValue;
                    foreach (var hit in hits)
                    {
                        if (hit.collider == meshCollider && hit.distance < nearestHitDistance)
                        {
                            nearestHitDistance = hit.distance;
                        }
                    }
                    
                    // 頂点までの距離
                    float vertexDistance = Vector3.Distance(activeCamera.transform.position, worldPos);
                    
                    // 頂点が最前面より奥にある場合はスキップ
                    if (vertexDistance > nearestHitDistance + 0.01f) continue; // 0.01fは誤差マージン
                }
                
                // スクリーン座標に変換
                Vector2 guiPos;
                if (useSceneViewMode)
                {
                    // SceneViewモードでは直接スクリーン座標を使用
                    var screenPos = GetActiveCamera().WorldToScreenPoint(worldPos);
                    guiPos = HandleUtility.WorldToGUIPoint(worldPos);
                }
                else
                {
                    // 既存のRenderTextureモード
                    var screenPos = camera.WorldToScreenPoint(worldPos);
                    guiPos = new Vector2(
                        screenPos.x + rect.x,
                        rect.height - (screenPos.y - rect.y)
                    );
                }
                
                // 選択範囲内かチェック
                if (selectionRect.Contains(guiPos))
                {
                    onVertexFound(i);
                    foundCount++;
                }
            }
        }
    

        /// <summary>
        /// 選択範囲の描画を開始する
        /// </summary>
        /// <param name="position">ドラッグ開始位置（GUI座標）</param>
        public void BeginDrag(Vector2 position)
        {
            isDragging = true;
            dragStartPosition = position;
        }

        /// <summary>
        /// 選択範囲の描画を終了する
        /// </summary>
        public void EndDrag()
        {
            isDragging = false;
        }
    
        /// <summary>
        /// 現在のドラッグ状態から選択範囲のRectを計算する
        /// </summary>
        /// <returns>選択範囲を表すRect。ドラッグ中でない場合はRect.zero</returns>
        public Rect GetSelectionRect()
        {
            if (!isDragging) return Rect.zero;

            var currentPosition = Event.current.mousePosition;
            var xMin = Mathf.Min(dragStartPosition.x, currentPosition.x);
            var xMax = Mathf.Max(dragStartPosition.x, currentPosition.x);
            var yMin = Mathf.Min(dragStartPosition.y, currentPosition.y);
            var yMax = Mathf.Max(dragStartPosition.y, currentPosition.y);
            return new Rect(xMin, yMin, xMax - xMin, yMax - yMin);
        }

        

        /// <summary>
        /// エディタウィンドウ上に選択範囲を描画する
        /// </summary>
        public void DrawSelectionRect()
        {
            if (!isDragging) return;

            // SceneViewモードの場合は描画しない（SceneViewIntegrationが処理する）
            if (useSceneViewMode) return;

            // 選択範囲を描画
            Handles.BeginGUI();
            var selectionRect = GetSelectionRect();
            
            // 半透明の塗りつぶし
            EditorGUI.DrawRect(selectionRect, new Color(0.3f, 0.5f, 1f, 0.1f));
            
            // 枠線
            var lineColor = new Color(0.3f, 0.5f, 1f, 0.8f);
            Handles.color = lineColor;
            Handles.DrawLine(new Vector3(selectionRect.x, selectionRect.y), 
                           new Vector3(selectionRect.x + selectionRect.width, selectionRect.y));
            Handles.DrawLine(new Vector3(selectionRect.x + selectionRect.width, selectionRect.y), 
                           new Vector3(selectionRect.x + selectionRect.width, selectionRect.y + selectionRect.height));
            Handles.DrawLine(new Vector3(selectionRect.x + selectionRect.width, selectionRect.y + selectionRect.height), 
                           new Vector3(selectionRect.x, selectionRect.y + selectionRect.height));
            Handles.DrawLine(new Vector3(selectionRect.x, selectionRect.y + selectionRect.height), 
                           new Vector3(selectionRect.x, selectionRect.y));
            
            Handles.EndGUI();
        }
    }
}
