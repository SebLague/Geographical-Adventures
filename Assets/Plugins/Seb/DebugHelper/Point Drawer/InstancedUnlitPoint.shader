Shader "DebugHelper/InstancedUnlitPoint" {
	Properties {
		
	}
	SubShader {

		Tags { "Queue"="Geometry" }

		Pass {

			CGPROGRAM

			#pragma vertex vert
			#pragma fragment frag
			#pragma target 4.5

			#pragma shader_feature_local Use3DPoints

			#include "UnityCG.cginc"

			#if Use3DPoints
				StructuredBuffer<float3> Points;
			#else
				StructuredBuffer<float2> Points;
			#endif

			float sizeMultiplier;
			float3 colour;

			struct v2f
			{
				float4 pos : SV_POSITION;
			};

			v2f vert (appdata_full v, uint instanceID : SV_InstanceID)
			{
				#if Use3DPoints
					float3 pointCentre = Points[instanceID];
				#else
					float3 pointCentre = float3(Points[instanceID], 0);
				#endif

				float3 worldVertPos = pointCentre + mul(unity_ObjectToWorld, v.vertex) * sizeMultiplier;
				float3 objectVertPos = mul(unity_WorldToObject, float4(worldVertPos.xyz, 1));
				v2f o;

				o.pos = UnityObjectToClipPos(objectVertPos);
				return o;
			}

			float4 frag (v2f i) : SV_Target
			{
				return float4(colour, 1);
			}

			ENDCG
		}
	}
}