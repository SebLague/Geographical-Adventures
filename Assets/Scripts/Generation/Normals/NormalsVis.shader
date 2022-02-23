Shader "Unlit/NormalsVis"
{
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

			sampler2D NormalsWest;
			sampler2D NormalsEast;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				float3 dataWest = tex2D(NormalsWest, i.uv * float2(2, 1));
				float3 dataEast = tex2D(NormalsEast, (i.uv - float2(0.5, 0)) * float2(2, 1));
				float3 data = (i.uv.x <= 0.5)?dataWest:dataEast;

				return float4(data.xyz * 0.5 + 0.5, 1);
		
				/*
				float nx = data.x*2-1;
				float ny = data.y*2-1;
				float nz = sqrt(1 - nx * nx + ny * ny);
				float3 normal = normalize(float3(nx, ny, nz));
			

				//return float4(normal.z*0.5 + 0.5,0,0,0);
				return float4(normal * 0.5 + 0.5, 1);
				*/
			}
			ENDCG
		}
	}
}
