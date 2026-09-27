Shader "Custom/PortalEnergy"
{
    Properties
    {
        _Color ("Color", Color) = (0.15, 0.55, 1, 1)
        _Alpha ("Alpha", Float) = 1
        _Intensity ("Intensity", Float) = 2
        _Speed ("Speed", Float) = 0.25
        _Pulse ("Pulse", Float) = 0.5
        _EdgeSoftness ("Edge Softness", Float) = 1
        _BandScale ("Band Scale", Float) = 24
        _WhiteAmount ("White Amount", Float) = 0.25
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Lighting Off
        LOD 100

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
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            fixed4 _Color;
            float _Alpha;
            float _Intensity;
            float _Speed;
            float _Pulse;
            float _EdgeSoftness;
            float _BandScale;
            float _WhiteAmount;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float angle = frac(i.uv.x + _Time.y * _Speed);
                float radial = saturate(i.uv.y);
                float edgeFeather = max(0.01, _EdgeSoftness);
                float edgeMask = smoothstep(0.0, edgeFeather, radial) * (1.0 - smoothstep(1.0 - edgeFeather, 1.0, radial));
                float centerBand = saturate(1.0 - abs(radial * 2.0 - 1.0));

                float blockA = smoothstep(0.48, 0.52, frac(angle * _BandScale));
                float blockB = smoothstep(0.72, 0.76, frac(angle * (_BandScale * 0.5) - _Time.y * _Speed * 0.7));
                float notch = 1.0 - smoothstep(0.0, 0.08, frac(angle * (_BandScale * 1.75) + radial * 0.45));
                float arcs = saturate(max(blockA, blockB) - notch * 0.5);
                float pulse = 1.0 + (smoothstep(0.25, 0.75, frac(angle * 3.0 + _Time.y * 1.4)) - 0.5) * _Pulse * 0.18;
                float whiteStripe = arcs * smoothstep(0.55, 0.9, centerBand) * _WhiteAmount;

                float alpha = _Alpha * edgeMask * (0.68 + arcs * 0.32) * pulse;
                fixed3 color = lerp(_Color.rgb, fixed3(1.0, 1.0, 1.0), whiteStripe) * _Intensity;
                return fixed4(color, saturate(alpha));
            }
            ENDCG
        }
    }
    Fallback Off
}
