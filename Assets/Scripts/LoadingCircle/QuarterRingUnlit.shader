Shader "Custom/TorusArcUnlit"
{
    Properties
    {
        _Color ("Color", Color) = (0.3, 0.7, 1, 1)
        _Arc ("Arc Size (0-1)", Range(0,1)) = 0.25
        _Rotation ("Rotation", Range(0,1)) = 0
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionOS : TEXCOORD0;
            };

            float4 _Color;
            float _Arc;
            float _Rotation;

            Varyings vert (Attributes v)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                o.positionOS = v.positionOS.xyz; // local space
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                // Project position onto XZ plane (torus ring direction)
                float2 p = normalize(float2(i.positionOS.x, i.positionOS.z));

                // Angle around torus
                float angle = atan2(p.y, p.x);
                angle = (angle + PI) / (2 * PI); // 0–1
                angle = frac(angle + _Rotation);

                // Mask arc
                if (angle > _Arc)
                    discard;

                return _Color;
            }
            ENDHLSL
        }
    }
}
