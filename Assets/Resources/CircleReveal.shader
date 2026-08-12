Shader "UI/CircleReveal"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Radius ("Radius", Range(0, 2)) = 1
        _Center ("Center", Vector) = (0.5, 0.5, 0, 0)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        LOD 100

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _Radius;
            float4 _Center;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv - _Center.xy;
                float angle = atan2(uv.y, uv.x);
                float dist = length(uv);
                
                // Khử gợn sóng khi vòng tròn đóng hẳn hoặc mở hẳn để tránh lỗi viền
                float waveAmp = smoothstep(0.05, 0.3, _Radius) * smoothstep(1.2, 0.8, _Radius);
                
                // Hiệu ứng lượn sóng loang lổ (chuyển động theo thời gian)
                float wave = (sin(angle * 6.0 + _Time.y * 5.0) * 0.05 
                           + cos(angle * 9.0 - _Time.y * 3.0) * 0.03
                           + sin(angle * 3.0 + _Time.y * 2.0) * 0.06) * waveAmp;
                
                float blobDist = dist + wave;

                // Màu sắc Hawaii (Gradient từ Cam hoàng hôn -> Hồng -> Xanh dương biển)
                fixed3 bottomCol = fixed3(1.0, 0.6, 0.1); // Cam / Vàng cát
                fixed3 midCol = fixed3(1.0, 0.3, 0.5); // Hồng hoàng hôn
                fixed3 topCol = fixed3(0.0, 0.7, 0.9); // Xanh nước biển
                
                // Lerp tạo gradient dựa trên chiều dọc (uv.y)
                fixed3 hawaiiCol = lerp(bottomCol, midCol, smoothstep(0.0, 0.5, i.uv.y));
                hawaiiCol = lerp(hawaiiCol, topCol, smoothstep(0.5, 1.0, i.uv.y));
                
                // Alpha: Cắt rỗng ở giữa (phần nhỏ hơn _Radius)
                float alpha = smoothstep(_Radius - 0.05, _Radius, blobDist);
                
                // Tạo một lớp bọt biển màu trắng ở ngay mép
                float foamAlpha = smoothstep(_Radius - 0.05, _Radius, blobDist) 
                                - smoothstep(_Radius, _Radius + 0.08, blobDist);
                // Giấu bọt biển khi màn hình kín đen hoặc mở toang
                foamAlpha *= smoothstep(0.0, 0.1, _Radius) * smoothstep(1.5, 1.2, _Radius);
                
                fixed4 finalCol = fixed4(hawaiiCol, alpha);
                
                // Gắn thêm màu trắng bọt biển vào phần rìa
                finalCol.rgb = lerp(finalCol.rgb, fixed3(1.0, 1.0, 1.0), foamAlpha * 0.9);
                
                // Nếu _Radius quá nhỏ (đã đóng kín), ép alpha = 1 để không bị hở khe
                if (_Radius <= 0.01) {
                    finalCol.a = 1.0;
                }
                
                return finalCol;
            }
            ENDCG
        }
    }
}
