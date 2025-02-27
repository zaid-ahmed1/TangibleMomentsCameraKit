Shader "Custom/URPWebcamMapping_Aligned"
{
    Properties
    {
        // This is the webcam texture (assign it via your script)
        _MainTex("Webcam Texture", 2D) = "white" {}
        // Tint controls the color and overall transparency.
        _TintColor("Tint Color", Color) = (1, 1, 1, 1)
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
            // URP core functions:
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // The webcam texture is assigned to _MainTex.
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            // _MainTex_TexelSize is automatically provided (xy = 1/width, 1/height; zw = width, height)
            float4 _MainTex_TexelSize;
            float4 _TintColor;

            // These uniforms must be updated by your controller script:
            float3 _CameraPos;            // The current world-space position of your camera.
            float4x4 _CameraRotationMatrix; // The inverse of your camera's rotation (world → camera space).
            float2 _FocalLength;          // (fx, fy) in pixels.
            float2 _PrincipalPoint;       // (cx, cy) in pixels.

            struct Attributes
            {
                float4 vertex : POSITION;
            };

            struct Varyings
            {
                float4 clipPos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                // Compute clip-space position.
                OUT.clipPos = TransformObjectToHClip(IN.vertex);
                // Compute world-space position.
                float4 worldPos = mul(unity_ObjectToWorld, IN.vertex);
                OUT.worldPos = worldPos.xyz;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // Compute the difference between the fragment and the camera.
                float3 diff = IN.worldPos - _CameraPos;
                // Transform into camera-local space.
                float3 localPos = mul(_CameraRotationMatrix, float4(diff, 1.0)).xyz;
                // Avoid division by zero.
                if (localPos.z < 0.001)
                    discard;

                // Use a pinhole camera projection to compute image-plane coordinates (in pixels).
                float uImage = _FocalLength.x * (localPos.x / localPos.z) + _PrincipalPoint.x;
                float vImage = _FocalLength.y * (localPos.y / localPos.z) + _PrincipalPoint.y;
                // Convert pixel coordinates to normalized UV coordinates (0–1).
                float u = uImage / _MainTex_TexelSize.z;
                float v = vImage / _MainTex_TexelSize.w;
                float2 computedUV = float2(u, v);

                // Sample the webcam texture using the computed UV.
                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, computedUV);
                // Multiply by the tint color (which may include transparency).
                col *= _TintColor;
                return col;
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/InternalErrorShader"
}