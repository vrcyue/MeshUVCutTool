using UnityEngine;
using uobject = UnityEngine.Object;
using UnityEditor;
using System.IO;
using System.Linq;

namespace vrc_yue.MeshUVCutTools.MeshModifyTool.Models
{
    public class TextureEditor
    {
        private RenderTexture renderTexture;
        private Material processingMaterial;
        private TemporaryAssetManager assetManager;

        public TextureEditor(TemporaryAssetManager manager)
        {
            assetManager = manager;
        }

        public Texture2D SaveAsNewTexture(Texture sourceTexture, string sourceDirectory = null)
        {
            if (sourceTexture == null) return null;

            Texture2D texture2D = null;
            RenderTexture.active = null;
            var currentRT = RenderTexture.active;

            try
            {
                // テクスチャの準備
                if (sourceTexture is RenderTexture renderTex)
                {
                    // RenderTextureの場合、Texture2Dに変換
                    texture2D = new Texture2D(renderTex.width, renderTex.height);
                    RenderTexture.active = renderTex;
                    texture2D.ReadPixels(new Rect(0, 0, renderTex.width, renderTex.height), 0, 0);
                    texture2D.Apply();
                }
                else if (sourceTexture is Texture2D tex2D)
                {
                    // 既にTexture2Dの場合は新しいインスタンスを作成
                    texture2D = new Texture2D(tex2D.width, tex2D.height, tex2D.format, tex2D.mipmapCount > 1);
                    Graphics.CopyTexture(tex2D, texture2D);
                }
                else
                {
                    Debug.LogError($"Unsupported texture type: {sourceTexture.GetType()}");
                    return null;
                }

                texture2D.name = EditorUtils.EnsureSuffix(sourceTexture.name);

                // 元のテクスチャのディレクトリを取得
                if (sourceDirectory == null)
                {
                    string assetPath = AssetDatabase.GetAssetPath(sourceTexture);
                    sourceDirectory = !string.IsNullOrEmpty(assetPath)
                        ? Path.GetDirectoryName(assetPath)
                        : "Assets/Textures";
                }

                // 一時アセットとして管理
                assetManager.AddTemporaryTexture(texture2D, null, sourceDirectory);

                return texture2D;
            }
            finally
            {
                // RenderTextureのアクティブ状態を元に戻す
                RenderTexture.active = currentRT;
            }
        }

        public Texture[] CropTexturesByUV(Texture[] textures, Vector2 minUV, Vector2 maxUV)
        {
            if (textures == null || textures.Length == 0)
                return null;

            Texture[] results = new Texture[textures.Length];
            for (int i = 0; i < textures.Length; i++)
            {
                results[i] = CropTextureByUV(textures[i], minUV, maxUV);
            }

            return results;
        }

        public Texture CropTextureByUV(Texture texture, Vector2 minUV, Vector2 maxUV)
        {
            if (texture == null)
            {
                Debug.LogWarning("CropTextureByUV: Input texture is null");
                return null;
            }

            Texture2D newTexture = null;
            RenderTexture tempRT = null;

            try
            {
                // UV座標をピクセル座標に変換
                int startX = Mathf.FloorToInt(minUV.x * texture.width);
                int startY = Mathf.FloorToInt((1 - maxUV.y) * texture.height); // yは反転
                int width = Mathf.FloorToInt((maxUV.x - minUV.x) * texture.width);
                int height = Mathf.FloorToInt((maxUV.y - minUV.y) * texture.height);

                // 一時的なRenderTextureを作成
                tempRT = RenderTexture.GetTemporary(texture.width, texture.height, 0);
                Graphics.Blit(texture, tempRT);

                // 新しいテクスチャを作成（切り取ったサイズ）
                newTexture = new Texture2D(width, height, TextureFormat.RGBA32, true);
                RenderTexture.active = tempRT; // RenderTextureをアクティブに
                newTexture.ReadPixels(new Rect(startX, startY, width, height), 0, 0); // Y軸は反転
                newTexture.Apply();

                newTexture.name = EditorUtils.EnsureSuffix(texture.name);
                return SaveOrUpdateTexture(newTexture, texture);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error in CropTextureByUV: {e.Message}\n{e.StackTrace}");
                return null;
            }
            finally
            {
                if (tempRT != null)
                {
                    RenderTexture.ReleaseTemporary(tempRT);
                }

                if (newTexture != null)
                {
                    Object.DestroyImmediate(newTexture);
                }

                RenderTexture.active = null;
            }
        }


        public Texture[] BakeToUVs(Texture[] srcTexs, Mesh meshWithUV2)
        {
            if (srcTexs == null || srcTexs.Length == 0) return null;

            Texture[] results = new Texture[srcTexs.Length];
            for (int i = 0; i < srcTexs.Length; i++)
            {
                results[i] = BakeToUV(srcTexs[i], meshWithUV2);
            }

            return results;
        }

        // srcTex     : 旧テクスチャ（UV0 用）
        // meshWithUV2: GenerateSecondaryUVSet → UV2 コピー後の「頂点増えていない」メッシュ
        // resolution : 出力テクスチャ解像度（例 2048）
        public Texture BakeToUV(Texture srcTex, Mesh meshWithUV2)
        {
            // // --- 1. 転写用マテリアルを用意 ------------------------
            Shader s = Shader.Find("Hidden/UV0_to_Target");
            var overrideMat = new Material(s);

            overrideMat.mainTexture = srcTex;

            int resolutionWidth = srcTex.width;
            int resolutionHeight = srcTex.height;
            // --- 2. レンダーテクスチャ確保 ------------------------
            Texture2D tex = srcTex as Texture2D;
            var rt = new RenderTexture(resolutionWidth, resolutionHeight, 0,
                RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            rt.wrapMode = TextureWrapMode.Clamp;

            // --- 3. 描画 ------------------------------------------
            Graphics.SetRenderTarget(rt);
            GL.Clear(true, true, Color.clear);
            GL.PushMatrix();
            GL.LoadOrtho(); // 0‑1 正規化座標

            overrideMat.SetPass(0);
            Graphics.DrawMeshNow(meshWithUV2, Matrix4x4.identity);

            GL.PopMatrix();
            Graphics.SetRenderTarget(null);

            // --- 4. テクスチャへコピー ----------------------------
            var newtex = new Texture2D(resolutionWidth, resolutionHeight, TextureFormat.RGBA32, true, false);
            RenderTexture.active = rt;
            newtex.ReadPixels(new Rect(0, 0, resolutionWidth, resolutionHeight), 0, 0);
            newtex.Apply();
            RenderTexture.active = null;
            uobject.DestroyImmediate(rt);
            newtex.name = EditorUtils.EnsureSuffix(srcTex.name);
            return SaveOrUpdateTexture(newtex, srcTex);
        }

        private Texture SaveOrUpdateTexture(Texture2D newTexture, Texture sourceTexture)
        {
            var existingAsset = assetManager.FindTemporaryTexture(sourceTexture);
            if (existingAsset != null)
            {
                existingAsset.UpdateAssetContent(newTexture);
                Object.DestroyImmediate(newTexture);
                return existingAsset.Asset;
            }
            else
            {
                string sourcePath = AssetDatabase.GetAssetPath(sourceTexture);
                string sourceDirectory = !string.IsNullOrEmpty(sourcePath) ? Path.GetDirectoryName(sourcePath) : null;
                return SaveAsNewTexture(newTexture, sourceDirectory);
            }
        }
    }
}