Shader "Unlit/PropellerCircle"
{
	Properties
	{
		_Color ("Colour", Color) = (1,1,1,1)
		_RimWidth("Rim Width", Range(0,0.2)) = 0.05
		_RimBlend("Rim Blend", Range(0,1)) = 0
	}
	SubShader
	{
		Tags { "Queue"="Transparent" }
		ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha
		Cull Off

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
				float3 worldDir : TEXCOORD1;
			};

			float4 _Color;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;

				float3 worldPos = mul(unity_ObjectToWorld, v.vertex);
				float3 worldCentre = mul(unity_ObjectToWorld, float4(0,0,0,1));
				o.worldDir = normalize(worldPos - worldCentre);
				return o;
			}

			float _RimWidth, _RimBlend;

			float4 frag (v2f i) : SV_Target
			{
				float dst = length(i.uv-0.5) * 2;
				float circleAlpha = 1-smoothstep(0.99,1,dst);
				float rim = smoothstep(1-_RimWidth,1-_RimWidth * (1-_RimBlend),dst);

				float3 dirToSun = _WorldSpaceLightPos0.xyz;
				float d = dot(i.worldDir, dirToSun);

				float alpha = _Color.a * circleAlpha;
				alpha = lerp(alpha, 0.25, saturate(circleAlpha * rim * d));
				//return float4(d,d,d,1);

				//return float4(i.worldDir*0.5+0.5,1);
				
				float3 col = lerp(_Color.rgb, 3, saturate(rim * d));
				return float4(col, alpha);
			}
			ENDCG
		}
	}
}
