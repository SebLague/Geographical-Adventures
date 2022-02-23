Shader "Custom/SimpleShaded"
{
	Properties
	{
		_Colour("Colour", Color) = (1,1,1,1)
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

			#include "UnityCG.cginc"//

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float3 normal : NORMAL;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
				float3 worldNormal : NORMAL;
				float3 worldPos : TEXCOORD1;
			};

			float4 _Colour;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				o.worldNormal = mul(unity_ObjectToWorld, float4(v.normal.xyz, 0));
				o.worldPos =  mul(unity_ObjectToWorld, float4(v.vertex.xyz, 1));
				return o;
			}


			float4 frag (v2f i) : SV_Target
			{
				float3 worldNormal = normalize(i.worldNormal);
				float3 dirToSun = _WorldSpaceLightPos0.xyz;
				float shading = saturate(saturate(dot(worldNormal, dirToSun)) + 0.25);
				shading = pow(shading, 0.8) * 1.25;
				return float4(_Colour.rgb * shading, 1);
			}
			ENDCG
		}
	}
	Fallback "VertexLit"
}
