Shader "Custom/URP_ScreenRipple"
{
    Properties
    {
        _RippleCenter ("Ripple Center", Vector) = (0.5, 0.5, 0, 0)
        _RippleStrength ("Ripple Strength", Range(0, 1)) = 0.1
        _RippleRadius ("Ripple Radius", Range(0, 2)) = 0.5
        _RippleWidth ("Ripple Width", Range(0, 1)) = 0.1
    }
    SubShader
    {
        Tags 
        { 
            "RenderType"="Transparent" 
            "Queue"="Transparent" 
            "RenderPipeline"="UniversalPipeline"
        }

        Pass
        {
            Name "RipplePass"
            ZWrite Off
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 screenPos : TEXCOORD1;
            };

            TEXTURE2D(_CameraSortingLayerTexture);
            SAMPLER(sampler_CameraSortingLayerTexture);

            CBUFFER_START(UnityPerMaterial)
                float4 _RippleCenter;
                float _RippleStrength;
                float _RippleRadius;
                float _RippleWidth;
            CBUFFER_END

            float4 _GlobalCameraRect; // Khai báo biến toàn cục lấy từ AspectRatioFitter

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.screenPos = ComputeScreenPos(output.positionCS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Lấy UV màn hình cực kỳ chuẩn xác dựa trên tọa độ Pixel thực tế (SV_POSITION)
                // Điều này xử lý hoàn hảo mọi trường hợp ép tỉ lệ viền đen (Letterbox)
                float2 screenUV = input.positionCS.xy / _ScreenParams.xy;

                // Tính khoảng cách từ tâm object (uv 0.5, 0.5)
                float2 centerDir = input.uv - float2(0.5, 0.5);
                float dist = length(centerDir);

                // Tính toán độ biến dạng (Ripple Mask) cho phần VIỀN SÓNG
                // Khoảng cách từ gợn sóng hiện tại (RippleRadius) tới pixel này
                float diff = dist - _RippleRadius;
                float ringMask = smoothstep(_RippleWidth, 0.0, abs(diff));

                // Tạo thêm độ lồi (Lens/Bulge) cho phần BÊN TRONG tâm sóng
                // Càng gần tâm (dist nhỏ) thì độ lồi càng mạnh, giảm dần khi ra tới viền sóng
                float insideBulge = 1.0 - smoothstep(0.0, _RippleRadius, dist);
                
                // Kết hợp cả viền sóng và độ lồi ở giữa (độ lồi ở giữa nhẹ hơn viền một chút để tự nhiên)
                float rippleMask = max(ringMask, insideBulge * 0.8);

                // Nếu hoàn toàn không có sóng ở pixel này, HỦY vẽ pixel này luôn
                if (rippleMask < 0.02) discard;

                // Hướng biến dạng từ tâm ra ngoài
                float2 distortionDir = normalize(centerDir);
                
                // Áp dụng biến dạng vào screen UV
                float2 distortedUV = screenUV + distortionDir * rippleMask * _RippleStrength;

                // Lấy màu từ màn hình
                half4 color = SAMPLE_TEXTURE2D(_CameraSortingLayerTexture, sampler_CameraSortingLayerTexture, distortedUV);

                // Làm cho viền của gợn sóng mờ dần đều ra xung quanh để hòa vào background
                color.a = rippleMask;

                return color;
            }
            ENDHLSL
        }
    }
}
