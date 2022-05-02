Shader "Instanced/InstancedLines" {
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
			/*
			v2f vert (appdata_full v, uint instanceID : SV_InstanceID)
			{
				LineData data = lineData[instanceID];
				float3 lineOffset = data.pointB - data.pointA;
				float3 linePerpDir = normalize(cross(lineOffset, float3(0,0,-1)));
				float3 vertexPos = v.vertex.xyz;
				float3 pos = data.pointA + lineOffset * vertexPos.x + linePerpDir * vertexPos.y * width;

				v2f o;
				o.pos = mul(UNITY_MATRIX_VP, float4(pos, 1.0f));
				return o;
			}
			*/

			v2f vert (appdata_full v, uint instanceID : SV_InstanceID)
			{
				LineSegment segment = lineSegments[instanceID];
				float3 a = segment.pointA;
				float3 b = segment.pointB;
				float flipY = _ProjectionParams.x; // 1 or -1 (flipped depending on platform)
				float aspect = _ScreenParams.y/_ScreenParams.x;
				float4 clipPointA = mul(UNITY_MATRIX_VP, float4(a, 1.0f));
				float4 clipPointB = mul(UNITY_MATRIX_VP, float4(b, 1.0f));
				float2 screenPointA = (clipPointA.xy * float2(1,flipY) / clipPointA.w) * 0.5 + 0.5;
				float2 screenPointB = (clipPointB.xy * float2(1,flipY) / clipPointB.w) * 0.5 + 0.5;

				float2 screenLineOffset = screenPointB - screenPointA;
				float2 screenLineNormal = normalize(float2(-screenLineOffset.y, screenLineOffset.x));

				float2 screenVertPos = screenPointA + screenLineOffset * v.vertex.x + screenLineNormal * float2(aspect,1) * v.vertex.y * width;

				float4 clip = lerp(clipPointA, clipPointB, v.vertex.x);
				float4 clipVertPos = float4((screenVertPos * 2 - 1) * float2(1,flipY) * clip.w, clip.z, clip.w);

				v2f o;
				o.pos = clipVertPos;
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