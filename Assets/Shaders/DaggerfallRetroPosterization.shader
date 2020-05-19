Shader "Daggerfall/RetroPosterization"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}
	SubShader
	{
		// No culling or depth
		Lighting Off 
       Cull Off
       ZWrite On
       ZTest Always
       Fog { Mode Off }

		Pass
		{
			CGPROGRAM
            
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"
            
            #define gamma 2.0

			struct appdata
			{
				float4 vertex : POSITION;
				float2 texcoord : TEXCOORD0;
			};

			struct v2f
			{
              float4 vertex : SV_POSITION;
				float2 texcoord : TEXCOORD0;
			};

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.texcoord = v.texcoord;
				return o;
			}
			
			sampler2D _MainTex;

			fixed4 frag (v2f i, out float outDepth : SV_Depth) : SV_Target
			{
                float4 color = tex2D(_MainTex, i.texcoord);
                float depth = SAMPLE_DEPTH_TEXTURE(_MainTex, i.texcoord);
                outDepth = depth;
                if (depth == 0)
                    return color;

                // Decrease color depth to 4 bits per component
                color.rgb = pow(round(pow(color.rgb, 1/gamma) * 16.0) / 16.0, gamma);
                return color;
			}
			ENDCG
		}
	}
}
