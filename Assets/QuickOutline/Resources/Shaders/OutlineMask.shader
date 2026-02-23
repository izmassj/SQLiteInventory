//
//  OutlineMask.shader
//  QuickOutline (Modified with Alpha Support)
//
//  Created by Chris Nolet on 2/21/18.
//  Modified to support alpha/transparency
//

Shader "Custom/Outline Mask" {
  Properties {
    [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest("ZTest", Float) = 0
    
    [Toggle] _UseAlpha("Ignore Transparent Areas", Float) = 1
    _AlphaThreshold("Alpha Threshold", Range(0, 1)) = 0.1
  }

  SubShader {
    Tags {
      "Queue" = "Transparent+100"
      "RenderType" = "Transparent"
    }

    Pass {
      Name "Mask"
      Cull Off
      ZTest [_ZTest]
      ZWrite Off
      ColorMask 0

      Stencil {
        Ref 1
        Pass Replace
      }
      
      CGPROGRAM
      #include "UnityCG.cginc"

      #pragma vertex vert
      #pragma fragment frag
      #pragma multi_compile _ _USEALPHA_ON

      struct appdata {
        float4 vertex : POSITION;
        float4 color : COLOR;
        UNITY_VERTEX_INPUT_INSTANCE_ID
      };

      struct v2f {
        float4 position : SV_POSITION;
        fixed4 vertexColor : TEXCOORD0;
        UNITY_VERTEX_OUTPUT_STEREO
      };

      uniform float _UseAlpha;
      uniform float _AlphaThreshold;

      v2f vert(appdata input) {
        v2f output;

        UNITY_SETUP_INSTANCE_ID(input);
        UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

        output.position = UnityObjectToClipPos(input.vertex);
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
        
        return fixed4(0, 0, 0, 0);
      }
      ENDCG
    }
  }
}
