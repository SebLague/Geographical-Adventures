Shader "Unlit/JFADisplay"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_DistanceMultiplier("Distance Multiplier", float) = 10
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
			float _DistanceMultiplier;

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

				// Display direction
				float2 offset = nearestPosUV - i.uv * _MainTex_TexelSize.zw;
				float2 dir = dst > 0 ? normalize(offset) : 0;
			//	return float4(nearestPosUV/ _MainTex_TexelSize.zw,0,0) * ((dst > 0) ? 1:0.5);
				//return float4(dir * 0.5 + 0.5, 0, 0);

				return dst * _DistanceMultiplier * 0.001;

			
			}
			ENDCG
		}
	}
}
