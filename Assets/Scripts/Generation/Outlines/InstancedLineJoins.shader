Shader "Instanced/InstancedLineJoins" {
	Properties {
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
	}
	SubShader {

		Pass {

			Tags {"LightMode"="ForwardBase"}
			

			CGPROGRAM

			#pragma vertex vert
			#pragma fragment frag
			#pragma target 4.5

			#include "UnityCG.cginc"

			struct LineSegment {
				float3 pointA;
				float3 pointB;
			};


			StructuredBuffer<LineSegment> lineSegments;
			float width;
			float4 colour;

			struct v2f
			{
				float4 pos : SV_POSITION;
			};

			v2f vert (appdata_full v, uint instanceID : SV_InstanceID)
			{
			
				float3 worldCentre = lineSegments[instanceID].pointA;
				float flipY = _ProjectionParams.x;
				float aspect = _ScreenParams.y/_ScreenParams.x;
				float4 clipCentre = mul(UNITY_MATRIX_VP, float4(worldCentre, 1));

				float2 screenCentre = (clipCentre.xy * float2(1, flipY) / clipCentre.w) * 0.5 + 0.5;
				float2 screenPos = screenCentre + v.vertex.xy * float2(aspect,1) * width * 0.5;

				float4 clipPos = float4((screenPos * 2 - 1) * clipCentre.w * float2(1, flipY), clipCentre.zw);
				

				v2f o;
				o.pos = clipPos;
				return o;
			}

			float4 frag (v2f i) : SV_Target
			{
				return colour;
			}

			ENDCG
		}
	}
}