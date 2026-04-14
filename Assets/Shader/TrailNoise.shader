Shader "Custom/URP/TrailNoise"
{
    Properties
    {
        [Header(Main Settings)]
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        [MainColor] [HDR] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        _EmissionStrength("Emission Multiplier", Float) = 1.0

        [Header(Noise Settings)]
        _NoiseMap("Noise Texture", 2D) = "white" {}
        _NoiseScrollSpeed("Noise Scroll Speed (XY)", Vector) = (1, 0, 0, 0)
        _NoiseScale("Noise Scale (XY)", Vector) = (1, 1, 0, 0)
        _NoiseStrength("Noise Strength", Range(0, 1)) = 0.8

        [Header(Vertex Displacement)]
        _VertexNoiseStrength("Displacement Strength", Float) = 0.5
        _VertexNoiseScale("Displacement Scale", Float) = 2.0
        _VertexNoiseSpeed("Displacement Speed", Float) = 3.0

        [Header(Blending and Rendering)]
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend("Src Blend", Float) = 5 // 5 = SrcAlpha
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend("Dst Blend", Float) = 10 // 10 = OneMinusSrcAlpha
        [Enum(Off, 0, On, 1)] _ZWrite("Z Write", Float) = 0
        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull Mode", Float) = 0 // 0 = Off (Double Sided)
    }
    
    SubShader
    {
        // Setup for Transparent URP Rendering
        Tags 
        { 
            "RenderType" = "Transparent" 
            "Queue" = "Transparent" 
            "RenderPipeline" = "UniversalPipeline" 
            "IgnoreProjector" = "True"
        }
        LOD 100

        Blend [_SrcBlend] [_DstBlend]
        ZWrite [_ZWrite]
        Cull [_Cull]

        Pass
        {
            Name "Unlit"
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            // Include core URP shader library
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                // The Trail Renderer passes its Color Gradient via Vertex Colors
                float4 color      : COLOR; 
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
                float  fogCoord   : TEXCOORD1;
            };

            // CBUFFER ensures SRP Batcher compatibility for better performance
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float4 _NoiseMap_ST;
                float2 _NoiseScrollSpeed;
                float2 _NoiseScale;
            float  _NoiseStrength;
            float  _EmissionStrength;
            
            float  _VertexNoiseStrength;
            float  _VertexNoiseScale;
            float  _VertexNoiseSpeed;
        CBUFFER_END

        // Texture and Sampler declarations
            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_NoiseMap);
            SAMPLER(sampler_NoiseMap);

            Varyings vert(Attributes input)
        {
            Varyings output;
            
            // --- Vertex Displacement Logic ---
            // We sample the noise texture using the length of the trail (uv.x) 
            // so that the entire width of the trail moves together as a single ribbon.
            float2 vNoiseUV = float2(input.uv.x * _VertexNoiseScale, 0.0) + float2(_Time.y * _VertexNoiseSpeed, 0.0);
            
            // Sample the texture 3 times with offsets to get pseudo-random X, Y, Z directions 
            // from a single grayscale noise texture. LOD 0 must be used in the vertex shader.
            float noiseX = SAMPLE_TEXTURE2D_LOD(_NoiseMap, sampler_NoiseMap, vNoiseUV, 0).r;
            float noiseY = SAMPLE_TEXTURE2D_LOD(_NoiseMap, sampler_NoiseMap, vNoiseUV + float2(0.31, 0.0), 0).r;
            float noiseZ = SAMPLE_TEXTURE2D_LOD(_NoiseMap, sampler_NoiseMap, vNoiseUV + float2(0.73, 0.0), 0).r;
            
            // Remap from [0, 1] to [-1, 1]
            float3 displacement = float3(noiseX, noiseY, noiseZ);
            displacement = (displacement - 0.5) * 2.0;
            
            // Apply the displacement to the Object Space position.
            // Multiplying by input.color.a tapers the wiggling off at the fading tail end.
            input.positionOS.xyz += displacement * _VertexNoiseStrength * input.color.a;

            // Transform Object space to Clip space
            output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
            
            // Pass UVs and apply tiling/offset from the material
            output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                
                // Pass the vertex color from the Trail Renderer
                output.color = input.color; 
                
                // Calculate fog
                output.fogCoord = ComputeFogFactor(output.positionCS.z);
                
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // 1. Sample the Base Texture
                half4 baseColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;

                // 2. Calculate Panning Noise UVs
                // We use _Time.y to scroll the noise over time based on the speed vector
                float2 noiseUV = (input.uv * _NoiseScale) + (_Time.y * _NoiseScrollSpeed);

                // 3. Sample the Noise Texture
                half4 noise = SAMPLE_TEXTURE2D(_NoiseMap, sampler_NoiseMap, noiseUV);

                // 4. Calculate Noise Influence
                // Lerp between a flat white value (no noise) and the noise value based on strength
                half noiseEffect = lerp(1.0, noise.r, _NoiseStrength);

                // 5. Combine everything
                half4 finalColor = baseColor;
                finalColor.rgb *= noiseEffect * _EmissionStrength; // Apply noise to color and boost emission
                finalColor.a *= noiseEffect;                       // Apply noise to alpha to create breaks/dissolves

                // 6. Apply Vertex Color (Crucial for Trail Renderer length fades)
                finalColor *= input.color;

                // 7. Apply standard URP Fog
                finalColor.rgb = MixFog(finalColor.rgb, input.fogCoord);

                return finalColor;
            }
            ENDHLSL
        }
    }
}