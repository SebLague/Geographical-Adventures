Shader "Unlit/Moon"
{
	Properties
	{
		_MainTex ("Albedo", 2D) = "white" {}
		_Brightness ("Brightness", Float) = 1
	}
	SubShader
	{


		Tags { "Queue"="Background" }
		ZTest Off
		

		Pass
		{
			ZClip Off
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
			float _Brightness;


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
				float3 worldNormal = normalize(i.worldNormal);

				float3 dirToSun = _WorldSpaceLightPos0;
				float shading = saturate(dot(worldNormal, dirToSun));
				//shading = dot(worldNormal, dirToSun) * 0.5 + 0.5;
				//shading = shading * shading;
				//return shading * shading;
				//float shading = 1;
				shading = pow(shading, 1/2.2);
			
				shading *= _Brightness;
				//shading = 1;

				float4 col = tex2D(_MainTex, texCoord) * shading;
			
				return float4(col.rgb, 3); // Note: alpha used to control interaction with atmosphere (TODO: figure out better approach?)
			}
			ENDCG
			
		}
		
	}
	Fallback "VertexLit"
}
