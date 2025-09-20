/*
AvatarModifyTools
https://github.com/HhotateA/AvatarModifyTools

Copyright (c) 2021 @HhotateA_xR

This software is released under the MIT License.
http://opensource.org/licenses/mit-license.php
*/
Shader "HhotateA/MeshModifyTool/UV"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _UVAlpha ("UV Visualization", Range(0,1)) = 0.5
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            Tags {"Queue"="Opaque"}
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _UVAlpha;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // オリジナルのテクスチャ色を取得
                fixed4 texColor = tex2D(_MainTex, i.uv);
                
                // UVを可視化する色を作成（赤=U, 緑=V）
                fixed4 uvColor = fixed4(i.uv.x, i.uv.y, 0, 1);
                
                // テクスチャ色とUV可視化色をブレンド
                return lerp(texColor, uvColor, _UVAlpha);
            }
            ENDCG
        }
    }
}
