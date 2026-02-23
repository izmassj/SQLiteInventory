//
//  OutlineFill.shader
//  QuickOutline (Modified with Alpha Support)
//
//  Created by Chris Nolet on 2/21/18.
//  Modified to support alpha/transparency
//

Shader "Custom/Outline Fill" {
  Properties {
    [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest("ZTest", Float) = 0

    _OutlineColor("Outline Color", Color) = (1, 1, 1, 1)
    _OutlineWidth("Outline Width", Range(0, 10)) = 2
    
    [Toggle] _UseAlpha("Ignore Transparent Areas", Float) = 1
    _AlphaThreshold("Alpha Threshold", Range(0, 1)) = 0.1
  }

  SubShader {
    Tags {
      "Queue" = "Transparent+110"
      "RenderType" = "Transparent"
      "DisableBatching" = "True"
    }

    Pass {
      Name "Fill"
      Cull Off
      ZTest [_ZTest]
      ZWrite Off
      Blend One OneMinusSrcAlpha  // Changed from SrcAlpha OneMinusSrcAlpha to prevent darkening on overlaps
      ColorMask RGB

      Stencil {
        Ref 1
        Comp NotEqual
      }

      CGPROGRAM
      #include "UnityCG.cginc"

      #pragma vertex vert
      #pragma fragment frag
      #pragma multi_compile _ _USEALPHA_ON

      struct appdata {
        float4 vertex : POSITION;
        float3 normal : NORMAL;
        float3 smoothNormal : TEXCOORD3;
        float4 color : COLOR;
        UNITY_VERTEX_INPUT_INSTANCE_ID
      };

      struct v2f {
        float4 position : SV_POSITION;
        fixed4 color : COLOR;
        fixed4 vertexColor : TEXCOORD0;
        UNITY_VERTEX_OUTPUT_STEREO
      };

      uniform fixed4 _OutlineColor;
      uniform float _OutlineWidth;
      uniform float _UseAlpha;
      uniform float _AlphaThreshold;

      v2f vert(appdata input) {
        v2f output;

        UNITY_SETUP_INSTANCE_ID(input);
        UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

        float3 normal = any(input.smoothNormal) ? input.smoothNormal : input.normal;
        float3 viewPosition = UnityObjectToViewPos(input.vertex);
        float3 viewNormal = normalize(mul((float3x3)UNITY_MATRIX_IT_MV, normal));

        output.position = UnityViewToClipPos(viewPosition + viewNormal * -viewPosition.z * _OutlineWidth / 1000.0);
        output.color = _OutlineColor;
        output.vertexColor = input.color;

        return output;
      }

      fixed4 frag(v2f input) : SV_Target {
        // Check alpha threshold if enabled
        #ifdef _USEALPHA_ON
          if (input.vertexColor.a < _AlphaThreshold) {
            discard;
          }
        #endif
        
        // Premultiply alpha for correct blending
        return fixed4(input.color.rgb * input.color.a, input.color.a);
      }
      ENDCG
    }
  }
}
