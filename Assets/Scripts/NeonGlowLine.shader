Shader "Neon/GlowLine"
{
    // Additive, radial-falloff shader for LineRenderer-based neon outlines.
    // Built-in Render Pipeline. Assign to the material used by NeonOutlineManager's
    // LineRenderers (see DefaultLineMaterial()).
    //
    // How it works:
    //   - LineRenderer UVs run 0..1 across the line's WIDTH on the V axis and along its
    //     LENGTH on the U axis (with textureMode = Stretch).
    //   - We treat V as "distance from the centreline" (0.5 = centre, 0/1 = edges) and use a
    //     smooth falloff so the middle is bright/opaque and the edges fade to fully transparent.
    //   - Additive blending (Blend One One) means overlapping glow stacks up instead of
    //     occluding, which reads as "light" rather than "paint".
    //   - _GlowIntensity lets you push brightness above 1 for a hot core without needing an
    //     HDR camera target or bloom post-process.
    Properties
    {
        _Color ("Tint", Color) = (1,1,1,1)
        _GlowIntensity ("Glow Intensity", Range(1, 6)) = 2.5
        _CoreSharpness ("Core Sharpness", Range(0.5, 8)) = 3
        _PulseSpeed ("Pulse Speed (0 = off)", Range(0, 10)) = 0
        _PulseAmount ("Pulse Amount", Range(0, 1)) = 0.25
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off
        Lighting Off
        ZWrite Off
        // "Lighten" blend: takes the brighter of source/dest per channel instead of summing.
        // Reads as additive glow against a dark background (same look as Blend One One), but
        // where LineRenderer join geometry overlaps itself (thin platforms, tight corners) it
        // can't double-brighten into a visible seam the way true additive blending does.
        Blend One One
        BlendOp Max

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Color;
            float _GlowIntensity;
            float _CoreSharpness;
            float _PulseSpeed;
            float _PulseAmount;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR; // LineRenderer's per-vertex start/end colour
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Distance from centreline, 0 at centre, 1 at the outer edge of the line width.
                float d = abs(i.uv.y - 0.5) * 2.0;

                // Smooth radial falloff: bright/opaque core, soft transparent edge.
                float falloff = pow(saturate(1.0 - d), _CoreSharpness);

                float pulse = 1.0;
                if (_PulseSpeed > 0.0)
                    pulse = 1.0 + _PulseAmount * sin(_Time.y * _PulseSpeed);

                fixed4 col = _Color * i.color;
                col.rgb *= _GlowIntensity * pulse * falloff;
                col.a *= falloff;
                return col;
            }
            ENDCG
        }
    }
}
