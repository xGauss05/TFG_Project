Shader "Custom/ScaleTilingShader"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.5, 0.5, 0.5, 1)
        _GridTexture ("Grid Texture", 2D) = "white" {}
        _GridColor ("Grid Color", Color) = (1, 1, 1, 1)
        _OverlayTexture ("Overlay Texture", 2D) = "black" {}
        _OverlayColor ("Overlay Color", Color) = (1, 1, 1, 1)
        _NormalMap ("Normal Map", 2D) = "bump" {}
        _Metallic ("Metallic", Range(0, 1)) = 0
        _Smoothness ("Smoothness", Range(0, 1)) = 0.5
        _Tiling ("Tiling", Vector) = (1, 1, 0, 0)
        _Offset ("Offset", Vector) = (0.5, 0.5, 0, 0)
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 300

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #include "UnityCG.cginc"

        sampler2D _GridTexture;
        sampler2D _OverlayTexture;
        sampler2D _NormalMap;

        fixed4 _BaseColor;
        fixed4 _GridColor;
        fixed4 _OverlayColor;
        float _Metallic;
        float _Smoothness;
        float4 _Tiling;
        float4 _Offset;

        struct Input
        {
            float2 uv_GridTexture;
            float3 worldPos;
            float3 worldNormal;
            INTERNAL_DATA
        };

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            float3 worldNormal = WorldNormalVector(IN, o.Normal);
            float3 absN = abs(worldNormal);
            float3 projUV = IN.worldPos;

            float2 uv;
            if (absN.z >= absN.x && absN.z >= absN.y)
                uv = projUV.xy;
            else if (absN.x >= absN.y)
                uv = projUV.zy;
            else
                uv = projUV.xz;

            uv = uv * _Tiling.xy + _Offset.xy;

            fixed4 gridColor = tex2D(_GridTexture, uv) * _GridColor;
            fixed4 overlayColor = tex2D(_OverlayTexture, uv) * _OverlayColor;
            fixed4 baseColor = lerp(_BaseColor, gridColor, gridColor.a);
            fixed4 finalColor = lerp(baseColor, overlayColor, overlayColor.a);

            o.Albedo = finalColor.rgb;
            o.Normal = UnpackNormal(tex2D(_NormalMap, uv));
            o.Metallic = _Metallic;
            o.Smoothness = _Smoothness;
            o.Alpha = finalColor.a;
        }

        ENDCG
    }
    FallBack "Standard"
}
