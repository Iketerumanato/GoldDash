Shader "Custom/RectangleGradient"
{
    //ChatGPT製
    Properties
    {
        _ColorTopLeft ("Top Left Color", Color) = (1, 0, 0, 1)
        _ColorTopRight ("Top Right Color", Color) = (0, 1, 0, 1)
        _ColorBottomLeft ("Bottom Left Color", Color) = (0, 0, 1, 1)
        _ColorBottomRight ("Bottom Right Color", Color) = (1, 1, 0, 1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            fixed4 _ColorTopLeft;
            fixed4 _ColorTopRight;
            fixed4 _ColorBottomLeft;
            fixed4 _ColorBottomRight;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv; // UV座標をそのまま渡す
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // UV座標を使って色を補間
                fixed4 topColor = lerp(_ColorTopLeft, _ColorTopRight, i.uv.x);
                fixed4 bottomColor = lerp(_ColorBottomLeft, _ColorBottomRight, i.uv.x);
                fixed4 finalColor = lerp(bottomColor, topColor, i.uv.y);

                return finalColor;
            }
            ENDCG
        }
    }
}
