Shader "Instanced/CityLightDebug" {
	Properties {
		
	}
	SubShader {

		Tags { "Queue"="Geometry" }
	
		Pass {

			CGPROGRAM

			#pragma vertex vert
			#pragma fragment frag
			#pragma target 4.5

			#include "UnityCG.cginc"
			
			struct CityLight {
				float3 pointOnSphere;
				float height;
				float intensity;
				float randomT;
			};

			StructuredBuffer<CityLight> CityLights;
			float size;

			struct v2f
			{
				float4 pos : SV_POSITION;
			};

			v2f vert (appdata_full v, uint instanceID : SV_InstanceID)
			{
				CityLight cityLight = CityLights[instanceID];

				float3 vertexOffset = v.vertex.xyz * size;
				float3 worldCentre = cityLight.pointOnSphere * cityLight.height;
				float3 worldPosition = worldCentre + vertexOffset;

				v2f o;
				o.pos = mul(UNITY_MATRIX_VP, float4(worldPosition, 1.0f));
				return o;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				return 1;
			}

			ENDCG
		}
	}
}