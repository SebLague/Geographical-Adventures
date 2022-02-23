Shader "Unlit/Moon"
{
	Properties
	{
		_MainTex ("Albedo", 2D) = "white" {}
	}
	SubShader
	{

		Tags { "Queue"="Background" }

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"
			#include "Assets/Scripts/Shader Common/GeoMath.hlsl"

			struct appdata
			{
				float4 vertex : POSITION;
				float3 normal : NORMAL;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float3 objPos : TEXCOORD0;
				float3 worldNormal : NORMAL;
			};

			sampler2D _MainTex;
			float _NormalStrength;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.worldNormal = mul(unity_ObjectToWorld, float4(v.normal, 0)).xyz;
				o.objPos = v.vertex;
				return o;
			}

			float4 frag (v2f i) : SV_Target
			{
				
				float3 spherePos = normalize(i.objPos);
				float2 texCoord = pointToUV(spherePos);

				float3 dirToSun = _WorldSpaceLightPos0;
				float shading = saturate(dot(i.worldNormal, dirToSun));
				//float shading = 1;
				shading = pow(shading, 1/2.2);
			
				shading *= (1.1);
				//shading = 1;

				float4 col = tex2D(_MainTex, texCoord) * shading;
			
				return col;
			}
			ENDCG
			
		}
		
	}
	Fallback "VertexLit"
}
