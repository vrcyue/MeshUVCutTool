using UnityEditor;
using UnityEngine;
using vrc_yue.MeshUVCutTools.Core;

namespace vrc_yue.MeshUVCutTools.MeshModifyTool.Utilities
{
    /// <summary>
    /// 編集ツールの設定値保存用クラス
    /// </summary>
    public class MeshPenTool
    {
        private Texture icon;
        private string name;
        
        private ExtraTool? extraTool;
        private float? brushPower;
        private float? brushWidth;
        private float? brushStrength;
        
        public MeshPenTool(string guid, string n,
            ExtraTool? e = null,
            float? s = null,
            float? p = null,
            float? w = null)
        {
            if (!string.IsNullOrWhiteSpace(guid))
            {
                var i = AssetUtility.LoadAssetAtGuid<Texture>(guid);
                icon = i;
            }
            
            name = n;
            extraTool = e;
            brushPower = p;
            brushWidth = w;
            brushStrength = s;
        }
        
        public MeshPenTool(Texture i, string n,
            ExtraTool? e = null,
            float? s = null,
            float? p = null,
            float? w = null)
        {
            icon = i;
            name = n;
            extraTool = e;
            brushPower = p;
            brushWidth = w;
            brushStrength = s;
        }
        
        public MeshPenTool(Texture2D i, string n,
            ExtraTool? e = null,
            float? s = null,
            float? p = null,
            float? w = null)
        {
            icon = i;
            name = n;
            extraTool = e;
            brushPower = p;
            brushWidth = w;
            brushStrength = s;
        }
        
        public bool Button(ref ExtraTool e, ref float p, ref float w, ref float s)
        {
            using (new EditorGUI.DisabledScope(
                       e == (extraTool ?? e) &&
                       p == (brushPower ?? p) &&
                       w == (brushWidth ?? w) &&
                       s == (brushStrength ?? s)))
            {
                if (icon)
                {
                    if (GUILayout.Button(icon))
                    {
                        e = extraTool ?? e;
                        p = brushPower ?? p;
                        w = brushWidth ?? w;
                        s = brushStrength ?? s;
                        return true;
                    }
                }
                else
                {
                    if (GUILayout.Button(name))
                    {
                        e = extraTool ?? e;
                        p = brushPower ?? p;
                        w = brushWidth ?? w;
                        s = brushStrength ?? s;
                        return true;
                    }
                }
            }
            
            return false;
        }
        
        public enum ExtraTool
        {
            Default,
            DetailMode,
            TriangleEraser,
            SelectLand,
            UnSelectLand,
            SelectVertex,
            UnSelectVertex,
            WeightCopy,
            Decimate,
            Subdivision
        }
        
        private bool FloatEqual(float a, float? b)
        {
            return Mathf.Abs(a - b ?? a) < 0.01f;
        }
    }
}