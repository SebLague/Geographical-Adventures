Shader "Custom/TestEarth"
{
	Properties
	{
		_MainTex ("Albedo", 2D) = "white" {}
		_Longitude("Longitude", Range(-180, 180)) = 0
		_Latitude("Latitude", Range(-90, 90)) = 0
		_TestRadiusKM("Test Radius KM", Float) = 100
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
			float _Longitude, _Latitude, _TestRadiusKM;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.worldNormal = UnityObjectToWorldNormal(v.normal);
				o.objPos = v.vertex;
				return o;
			}

			float4 frag (v2f i) : SV_Target
			{
				
				float3 spherePos = normalize(i.objPos);
				float2 texCoord = pointToUV(spherePos);
				float4 col = tex2D(_MainTex, texCoord);
				
				// Test circle
				float2 coord = float2(_Longitude, _Latitude) * (3.1415 / 180);
				float3 testPoint = longitudeLatitudeToPoint(coord);
				const float earthRadiusKM = 6371;
				float dstKM = distanceBetweenPointsOnUnitSphere(testPoint, spherePos) * earthRadiusKM;
				if (dstKM < 25) {
					return 1;
				}
				if (dstKM < _TestRadiusKM) {
					return lerp(col, float4(1,0,0,0), 0.75);
				}
				
				return col;
			}
			ENDCG
			
		}
		
	}
	Fallback "VertexLit"
}
