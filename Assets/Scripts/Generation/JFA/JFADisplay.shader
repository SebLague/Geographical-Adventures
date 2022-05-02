Shader "Unlit/JFADisplay"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			float4 _MainTex_TexelSize;

			int displayMode;
			bool highlightLand;
			float dstMultiplier;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}

			float4 frag (v2f i) : SV_Target
			{
				float3 data = tex2D(_MainTex, i.uv).xyz;
				float2 nearestPosUV = data.xy;
				float dst = data.z;

				if (highlightLand && dst == 0) {
					return float4(1,0,0,0);
				}

				// Display distance
				if (displayMode == 0) {
					return dst * dstMultiplier * 0.001;
				}
				// Display direction
				else if (displayMode == 1) {
					float2 offset = nearestPosUV - i.uv * _MainTex_TexelSize.zw;
					float2 dir = dst > 0 ? normalize(offset) : 0;
					return float4(dir * 0.5 + 0.5, 0, 0);
				}

				return 0;
			}
			ENDCG
		}
	}
}
