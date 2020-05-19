Shader "Daggerfall/RetroPalettization"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
        _Lut ("Texture", 3D) = "white" {}
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
            #pragma target 3.0
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"
            
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
            sampler3D _Lut;
            
	        fixed4 frag (v2f i, out float outDepth : SV_DEPTH) : SV_Target
	        {
                // Explore color space!
                //float4 color = float4(i.texcoord, frac(_Time.x), 1.0);
                
                float4 color = tex2D(_MainTex, i.texcoord);
                float depth = SAMPLE_DEPTH_TEXTURE(_MainTex, i.texcoord);
                outDepth = depth;
                if (depth == 0)
                    return color;
                
                fixed4 col = fixed4(tex3D(_Lut, color.rgb).rgb, color.a);
                return col;
            }
			ENDCG
		}
	}
}
