Shader "Custom/ShinySpriteSweep"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        
        [Header(Fake 3D Rim Light Effect)]
        [HDR] _RimColor ("Rim Color (Màu viền)", Color) = (1.0, 0.95, 0.5, 1) // Vàng ánh kim
        _RimWidth ("Rim Thickness (Độ dày viền px)", Range(0.1, 50.0)) = 6.0
        _RimSpeed ("Rotation Speed (Tốc độ xoay)", Range(0.1, 10.0)) = 3.0
        _RimIntensity ("Rim Brightness (Độ sáng viền)", Range(0.1, 10.0)) = 2.5
        _BasePulse ("Base Glow (Sáng nền nhẹ)", Range(0.0, 2.0)) = 0.3
        
        // Vẫn giữ biến này ẩn để C# hoạt động
        [HideInInspector] _ShinyEnabled ("Shiny Enabled", Float) = 0.0
        [HideInInspector] [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
    }

    SubShader
    {
        Tags
        { 
            "Queue"="Transparent" 
            "IgnoreProjector"="True" 
            "RenderType"="Transparent" 
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
        CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ PIXELSNAP_ON
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord  : TEXCOORD0;
            };

            fixed4 _Color;
            sampler2D _MainTex;
            float4 _MainTex_TexelSize; // Unity tự động điền kích thước Pixel của ảnh vào đây
            
            fixed4 _RimColor;
            float _RimWidth;
            float _RimSpeed;
            float _RimIntensity;
            float _BasePulse;
            float _ShinyEnabled;

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color * _Color;
                #ifdef PIXELSNAP_ON
                OUT.vertex = UnityPixelSnap (OUT.vertex);
                #endif
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, IN.texcoord) * IN.color;
                
                if (_ShinyEnabled > 0.5 && c.a > 0.05)
                {
                    // 1. Tính toán vector ánh sáng xoay vòng quanh nhân vật (Radar / Sun orbit)
                    float angle = _Time.y * _RimSpeed;
                    float2 lightDir = float2(cos(angle), sin(angle));
                    
                    // 2. Lấy mẫu điểm ảnh cách đó 1 khoảng _RimWidth hướng về phía "nguồn sáng"
                    // Kỹ thuật này giả lập Normal Map cho hình ảnh 2D tĩnh
                    float offsetA = tex2D(_MainTex, IN.texcoord + lightDir * _RimWidth * _MainTex_TexelSize.xy).a;
                    
                    // 3. Nếu điểm ảnh hiện tại nằm trong nhân vật (c.a = 1) 
                    // nhưng điểm lấy mẫu lại nằm ngoài không khí (offsetA = 0)
                    // -> Suy ra điểm ảnh hiện tại ĐANG NẰM TRÊN MÉP VIỀN hứng sáng!
                    float rim = saturate(c.a - offsetA);
                    
                    // 4. Nhịp thở nền mờ ảo (để nhân vật không bị tối đen ở giữa)
                    // Chạy chậm bằng một nửa tốc độ xoay
                    float baseGlow = (sin(_Time.y * _RimSpeed * 0.5) * 0.5 + 0.5) * _BasePulse;
                    
                    // 5. Kết hợp Viền rực sáng (bám theo góc xoay) + Ánh sáng nền (nhịp thở)
                    float finalGlow = (rim * _RimIntensity) + baseGlow;
                    
                    // Cộng thẳng màu sắc vào nhân vật
                    c.rgb += _RimColor.rgb * finalGlow * c.a;
                }
                
                c.rgb *= c.a; // Trả về chuẩn Premultiplied Alpha cho Sprite
                return c;
            }
        ENDCG
        }
    }
}
