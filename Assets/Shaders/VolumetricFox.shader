Shader "Unlit/VolumetricFox"
{
    Properties
    {
        [HDR]_Color("Color", Color) = (1, 1, 1, 1)
        [HDR]_Color2("Color 2", Color) = (0.5, 0.5, 1, 1) 
        _MaxDistance("Max distance", float) = 100
        _StepSize("Step size", Range(0.1, 20)) = 1
        _DensityMultiplier("Density multiplier", Range(0, 10)) = 1
        _NoiseOffset("Noise offset", float) = 0
        _MaxHeight("Max height", float) = 100
        _MinDistance("Min distance from fog", float) = 15
         
        _FogNoise("Fog noise", 3D) = "white" {}
        _NoiseTiling("Noise tiling", float) = 1
        _DensityThreshold("Density threshold", Range(0, 10)) = 0.1
         
        _ColorMixNoise("Color Mix Noise", 3D) = "white" {} 
        _ColorMixTiling("Color Mix Tiling", float) = 1    

        [HDR]_LightContribution("Light contribution", Color) = (1, 1, 1, 1)
        _LightScattering("Light scattering", Range(0, 1)) = 0.2
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            float4 _Color;
            float4 _Color2; 
            float _MaxDistance;
            float _DensityMultiplier;
            float _StepSize;
            float _NoiseOffset;
            float _MaxHeight;
            float _MinDistance;
            TEXTURE3D(_FogNoise);
            TEXTURE3D(_ColorMixNoise); 
            float _DensityThreshold;
            float _NoiseTiling;
            float _ColorMixTiling; 
            float4 _LightContribution;
            float _LightScattering;

            float henyey_greenstein(float angle, float scattering)
            {
                return (1.0 - angle * angle) / (4.0 * PI * pow(1.0 + scattering * scattering - (2.0 * scattering) * angle, 1.5f));
            }
             
            float get_density(float3 worldPos)
            {
                float4 noise = _FogNoise.SampleLevel(sampler_TrilinearRepeat, worldPos * 0.01 * _NoiseTiling, 0);
                float density = dot(noise, noise);
                density = saturate(density - _DensityThreshold) * _DensityMultiplier;

                float height_multiplier = 1.0 - step(_MaxHeight, worldPos.y);

                float dist_to_cam = distance(worldPos, _WorldSpaceCameraPos.xyz);
                float distance_multiplier = step(_MinDistance, dist_to_cam);

                return density * height_multiplier * distance_multiplier;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float4 col = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, IN.texcoord);
                float depth = SampleSceneDepth(IN.texcoord);
                float3 worldPos = ComputeWorldSpacePosition(IN.texcoord, depth, UNITY_MATRIX_I_VP);

                float3 entryPoint = _WorldSpaceCameraPos;
                float3 viewDir = worldPos - _WorldSpaceCameraPos;
                float viewLength = length(viewDir);
                float3 rayDir = normalize(viewDir);

                float2 pixelCoords = IN.texcoord * _BlitTexture_TexelSize.zw;
                float distLimit = min(viewLength, _MaxDistance);
                float distTravelled = InterleavedGradientNoise(pixelCoords, (int)(_Time.y / max(HALF_EPS, unity_DeltaTime.x))) * _NoiseOffset;
                
                float transmittance = 1.0;
                float4 accumulatedLight = float4(0, 0, 0, 0);
                float4 accumulatedColorWeight = float4(0, 0, 0, 0);
                float totalDensityWeight = 0;

                while(distTravelled < distLimit)
                {
                    float3 rayPos = entryPoint + rayDir * distTravelled;
                    float density = get_density(rayPos);
                    
                    if (density > 0)
                    {
                        // --- Color Mixing Logic ---
                        float4 colorNoise = _ColorMixNoise.SampleLevel(sampler_TrilinearRepeat, rayPos * 0.01 * _ColorMixTiling, 0);
                        float4 pointColor = lerp(_Color, _Color2, step(0.5, colorNoise.r));
                        
                        // Accumulate the color weighted by its density to find an average color later
                        accumulatedColorWeight += pointColor * density;
                        totalDensityWeight += density;

                        // --- Light Accumulation Logic (from original shader) ---
                        Light mainLight = GetMainLight(TransformWorldToShadowCoord(rayPos));
                        accumulatedLight.rgb += mainLight.color.rgb * _LightContribution.rgb * henyey_greenstein(dot(rayDir, mainLight.direction), _LightScattering) * density * mainLight.shadowAttenuation * _StepSize;
                        
                        // --- Transmittance ---
                        transmittance *= exp(-density * _StepSize);
                    }
                    
                    distTravelled += _StepSize;
                }
                 
                // --- Final Color Calculation ---
                // Calculate the average base color of the fog based on the mixed colors encountered
                float4 averageFogColor = _Color; // Default to the primary color
                if (totalDensityWeight > 0.0)
                {
                    averageFogColor = accumulatedColorWeight / totalDensityWeight;
                }

                // Create the final fog color by adding the accumulated light to the average base color
                // This mimics the original shader's additive lighting behavior
                float4 finalFogColor = averageFogColor;
                finalFogColor.rgb += accumulatedLight.rgb;

                // Use the original lerp function to blend the scene with the final calculated fog color
                return lerp(col, finalFogColor, 1.0 - saturate(transmittance));
            }
            ENDHLSL
        }
    }
}