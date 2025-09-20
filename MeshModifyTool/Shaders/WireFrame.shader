/*
AvatarModifyTools
https://github.com/HhotateA/AvatarModifyTools

Copyright (c) 2021 @HhotateA_xR

This software is released under the MIT License.
http://opensource.org/licenses/mit-license.php
*/
Shader "HhotateA/MeshModifyTool/OverlayWireFrame"
{
    Properties
    {
		[Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("ZTest",float) = 4
        _Color ("Color", Color) = (1,1,1,1)
        _SelectedColor ("Selected Color", Color) = (1,0,0,0.5)  // アルファ値を0.5に変更
        _ZOffset ("Z Offset", Range(-10, 10)) = 0.1  // Z offsetの制御用
    }
    SubShader
    {
        Tags {"Queue"="Transparent" "RenderType"="Transparent"}  // Transparentに変更
        ZTest [_ZTest]
        ZWrite On  // Z writeを有効化

        // メッシュ本体を描画するパス
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha  // アルファブレンドを追加
            Cull Back  // 背面カリングを有効化
            
            CGPROGRAM
            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float3 normal : NORMAL;  // 法線情報を追加
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
                noperspective float3 dist : TEXCOORD0;
            };

            float _ZOffset;
            fixed4 _SelectedColor;

            v2f vert (appdata v)
            {
                v2f o;
                float4 worldPos = mul(unity_ObjectToWorld, v.vertex);
                float3 worldNormal = UnityObjectToWorldNormal(v.normal);
                worldPos.xyz += worldNormal * _ZOffset * 0.0001;
                o.vertex = mul(UNITY_MATRIX_VP, worldPos);
                o.color = v.color;
                o.dist = float3(0, 0, 0);
                return o;
            }

            [maxvertexcount(3)]
            void geom(triangle v2f IN[3], inout TriangleStream<v2f> triStream)
            {
                // 三角形の全頂点が赤色（選択されている）かチェック
                float3 isRed = float3(
                    length(IN[0].color.rgb - float3(1,0,0)) < 0.01,
                    length(IN[1].color.rgb - float3(1,0,0)) < 0.01,
                    length(IN[2].color.rgb - float3(1,0,0)) < 0.01
                );
                
                // 全頂点が赤色の場合のみ出力
                if (isRed.x > 0 && isRed.y > 0 && isRed.z > 0)
                {
                    triStream.Append(IN[0]);
                    triStream.Append(IN[1]);
                    triStream.Append(IN[2]);
                }
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return _SelectedColor;
            }
            ENDCG
        }

        // ワイヤーフレームを描画するパス
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha  // アルファブレンドを有効化
            ZWrite Off  // 透明オブジェクトなのでZWriteをオフ
            
            CGPROGRAM
            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
            };

            appdata vert (appdata v)
            {
                return v;
            }

            fixed4 _Color;
            
            float _NormalAlpha;

            [maxvertexcount(6)]
            void geom(triangle appdata input[3], inout LineStream<v2f> outStream)
            {
                v2f output[3];
                for(int i=0;i<3;i++)
                {
                    output[i].vertex = UnityObjectToClipPos(input[i].vertex);
                    output[i].color = input[i].color;
                }
                outStream.Append(output[0]);
                outStream.Append(output[1]);
                outStream.RestartStrip();
                
                outStream.Append(output[1]);
                outStream.Append(output[2]);
                outStream.RestartStrip();
                
                outStream.Append(output[2]);
                outStream.Append(output[0]);
                outStream.RestartStrip();
            }

            fixed4 frag (v2f i) : SV_Target
            {
                clip(0.99-_NormalAlpha);
                // 頂点カラーのアルファ値を適用
                fixed4 finalColor = _Color;
                finalColor.rgb *= i.color.rgb;
                finalColor.a = i.color.a;  // 頂点カラーのアルファ値を直接使用
                return finalColor;
            }
            ENDCG
        }
    }
}
