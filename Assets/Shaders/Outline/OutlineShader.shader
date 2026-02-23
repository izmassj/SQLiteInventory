Shader "Custom/OutlineShader"
{
    Properties
    {
        _MainColor("Main Color", Color)=(1, 1, 1, 1)
        _MainTexture("Main Texture", 2D)="white" {}
        _OutlineColor("Outline Color", Color)=(1,1,1,1)
        _OutlineSize("Outline Size", Float)=0.03
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "LightMode"="UniversalForward"  }

        Pass
        {
            Stencil
            {
                Ref 1
                Comp always
                Pass replace
                Fail keep
                ZFail keep
            }

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_MainTexture);
            SAMPLER(sampler_MainTexture);

            CBUFFER_START(UnityPerMaterial)
                half4 _MainColor;
                float4 _MainTexture_ST;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTexture);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 color = SAMPLE_TEXTURE2D(_MainTexture, sampler_MainTexture, IN.uv) * _MainColor;
                return color;
            }
            ENDHLSL
        }

        Pass
        {
            Name "Outline"
            Tags { "LightMode"="SRPDefaultUnlit" }

            Cull Front
            ZWrite On
            ZTest LEqual

            Stencil
            {
                Ref 1
                Comp NotEqual
            }

            HLSLPROGRAM
    
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };
    
            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
            };
    
            CBUFFER_START(UnityPerMaterial)
                half4 _OutlineColor;
                float _OutlineSize;
            CBUFFER_END
    
            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                float3 pos = IN.positionOS.xyz + normalize(IN.normalOS) * _OutlineSize;

                OUT.positionHCS = TransformObjectToHClip(pos);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                return _OutlineColor;
            }

            ENDHLSL
        }



    }
}
