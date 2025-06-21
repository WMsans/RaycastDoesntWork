Shader "Unlit/HAL9000"
{
    Properties
    {
        [Header(Colors)]
        _GlowColor("Glow Color", Color) = (1, 0, 0, 1)
        _HotspotColor("Hotspot Color", Color) = (1, 0.9, 0.7, 1)
        _RimColor("Rim Color", Color) = (0.7, 0.7, 0.7, 1)
        _ReflectionColor("Reflection Color", Color) = (0.9, 0.9, 0.9, 1)

        [Header(Shape Falloff)]
        _RimRadius("Rim Radius", Range(0, 1)) = 0.48
        _RimWidth("Rim Width", Range(0, 0.1)) = 0.04
        _GlowFalloff("Glow Falloff Power", Range(1, 10)) = 3.0
        _HotspotSize("Hotspot Size", Range(0, 0.2)) = 0.05
        _HotspotFalloff("Hotspot Falloff Power", Range(1, 20)) = 10.0
    }
    SubShader
    {

        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalRenderPipeline" }
        LOD 100

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS  : SV_POSITION;
                float2 uv           : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _GlowColor;
                float4 _HotspotColor;
                float4 _RimColor;
                float4 _ReflectionColor;
                float _RimRadius;
                float _RimWidth;
                float _GlowFalloff;
                float _HotspotSize;
                float _HotspotFalloff;
            CBUFFER_END

            Varyings vert (Attributes v)
            {
                Varyings o;

                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {

                float2 centered_uv = i.uv - 0.5;

                float dist = length(centered_uv) * 2.0;

                half3 color = half3(0, 0, 0);

                float rim_outer_edge = _RimRadius;
                float rim_inner_edge = _RimRadius - _RimWidth;

                half rim = smoothstep(rim_inner_edge - 0.01, rim_inner_edge + 0.01, dist) - smoothstep(rim_outer_edge - 0.01, rim_outer_edge + 0.01, dist);

                color = lerp(color, _RimColor.rgb, rim);

                float glow_radius = rim_inner_edge;

                half glow_mask = 1.0 - smoothstep(0, glow_radius, dist);

                glow_mask = pow(glow_mask, _GlowFalloff);

                color = lerp(color, _GlowColor.rgb, glow_mask);

                half hotspot_mask = 1.0 - smoothstep(0, _HotspotSize, dist);

                hotspot_mask = pow(hotspot_mask, _HotspotFalloff);

                color += _HotspotColor.rgb * hotspot_mask;

                half reflection_mask = 0;

                half lens_mask = 1.0 - smoothstep(glow_radius - 0.05, glow_radius, dist);

                if(lens_mask > 0)
                {

                    float arc_y1 = 0.35; 
                    float arc_thickness1 = 0.06;
                    float arc_x_start1 = -0.35; 
                    float arc_x_end1 = 0.35;
                    float curve1 = 0.8 * centered_uv.x * centered_uv.x;

                    half band1 = smoothstep(0.0, 0.01, (arc_thickness1 / 2.0) - abs(centered_uv.y - arc_y1 + curve1));

                    half x_mask1 = smoothstep(0.0, 0.02, centered_uv.x - arc_x_start1) * (1.0 - smoothstep(0.0, 0.02, centered_uv.x - arc_x_end1));
                    reflection_mask += band1 * x_mask1;

                    float arc_y2 = 0.42;
                    float arc_thickness2 = 0.04;
                    float arc_x_start2 = -0.4;
                    float arc_x_end2 = 0.4;
                    float curve2 = 0.5 * centered_uv.x * centered_uv.x;
                    half band2 = smoothstep(0.0, 0.01, (arc_thickness2 / 2.0) - abs(centered_uv.y - arc_y2 + curve2));
                    half x_mask2 = smoothstep(0.0, 0.02, centered_uv.x - arc_x_start2) * (1.0 - smoothstep(0.0, 0.02, centered_uv.x - arc_x_end2));
                    reflection_mask += band2 * x_mask2;

                    color = lerp(color, _ReflectionColor.rgb, saturate(reflection_mask) * lens_mask);
                }

                return half4(saturate(color), 1.0);
            }
            ENDHLSL
        }
    }
}