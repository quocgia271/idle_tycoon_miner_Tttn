Shader "Custom/ScrollingSprite"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _ScrollX ("Scroll X Speed", Float) = 0
        _ScrollY ("Scroll Y Speed", Float) = 0
    }
    SubShader
    {
        Tags 
        { 
            "Queue"="Transparent" 
            "IgnoreProjector"="True" 
            "RenderType"="Transparent" 
            "PreviewType"="Plane" 
            "CanUseSpriteAtlas"="False" 
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
                float2 texcoord : TEXCOORD0;
            };

            sampler2D _MainTex;
            fixed4 _Color;
            float _ScrollX;
            float _ScrollY;

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                // Giữ nguyên hệ trục tọa độ của Sprite Tiled
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                float2 uv = IN.texcoord;
                
                // Trượt UV theo thời gian thực để tạo ảo giác cuộn xoắn
                uv.x += _Time.y * _ScrollX;
                uv.y += _Time.y * _ScrollY;
                
                fixed4 c = tex2D(_MainTex, uv) * IN.color;
                
                // Premultiplied Alpha
                c.rgb *= c.a;
                return c;
            }
            ENDCG
        }
    }
}
