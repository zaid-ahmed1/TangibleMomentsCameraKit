Shader "Custom/URPWebcamMapping"
{
    Properties
    {
        _MainTex("Webcam Texture", 2D) = "white" {}
        _TintColor("Tint Color", Color) = (1,1,1,1)
        _IntrinsicResolution("Intrinsic Resolution", Vector) = (1920,1080,0,0)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 200

        Pass
        {
            Name "WebcamMapping"
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_TexelSize;
            float4 _TintColor;

            float3 _CameraPos;
            float2 _FocalLength;
            float2 _PrincipalPoint;
            float2 _IntrinsicResolution;
            float4x4 _CameraRotationMatrix;

            struct Attributes { float4 vertex : POSITION; };

            struct Varyings
            {
                float4 clipPos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.clipPos = TransformObjectToHClip(IN.vertex);
                float4 worldPos = mul(unity_ObjectToWorld, IN.vertex);
                OUT.worldPos = worldPos.xyz;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 diff = IN.worldPos - _CameraPos;
                float3 localPos = mul(_CameraRotationMatrix, float4(diff, 1.0)).xyz;
                if (localPos.z < 0.001)
                {
                    discard;
                }
                
                float uImage = _FocalLength.x * (localPos.x / localPos.z) + _PrincipalPoint.x;
                float vImage = _FocalLength.y * (localPos.y / localPos.z) + _PrincipalPoint.y;
                
                float scaleX = _MainTex_TexelSize.z / _IntrinsicResolution.x;
                float scaleY = _MainTex_TexelSize.w / _IntrinsicResolution.y;
                
                uImage *= scaleX;
                vImage *= scaleY;

                float u = uImage / _MainTex_TexelSize.z;
                float v = vImage / _MainTex_TexelSize.w;
                
                float2 computedUV = float2(u, v);
                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, computedUV);
                
                col *= _TintColor;
                return col;
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/InternalErrorShader"
}
