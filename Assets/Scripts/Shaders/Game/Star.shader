Shader "Instanced/Star" {
	Properties {
		
	}
	SubShader {

		ZWrite Off
		ZTest Off
		Cull Off // Without culling off was having a strange issue where stars would *sometimes* not be rendered. Would love to know why...

		Tags { "Queue"="Background" }
		//Blend OneMinusDstColor One // Soft additive blend
		Blend One One

		Pass {

			CGPROGRAM

			#pragma vertex vert
			#pragma fragment frag
			#pragma target 4.5

			#include "UnityCG.cginc"
			
			struct Star {
				float3 dir;
				float brightnessT;
				float4 colour;
			};

			StructuredBuffer<Star> StarData;
			float3 testParams;
			float size;
			//float3 dirToSun;
			float brightnessMultiplier;
			float4x4 rotationMatrix;

			struct v2f
			{
				float4 pos : SV_POSITION;
				float2 offset : TEXCOORD0;
				float brightness : TEXCOORD1;
				float4 colour : TEXCOORD2;
			};

			v2f vert (appdata_full v, uint instanceID : SV_InstanceID)
			{
				Star star = StarData[instanceID];//
				float farClipPlane = _ProjectionParams.z;

				float3 starWorldDir = mul(rotationMatrix, float4(star.dir, 0));

				float3 objectPos = starWorldDir * 10;

				float4 p = mul(UNITY_MATRIX_VP, float4(_WorldSpaceCameraPos.xyz + objectPos, 1.0f));
				float4 screenPos = ComputeScreenPos(p);
				float2 uv = screenPos.xy / screenPos.w;
				
				float aspect = _ScreenParams.x / _ScreenParams.y;
				p += float4(v.vertex.x, -v.vertex.y * aspect, 0, 0) * size * 0.001 * lerp(0.5,1.5, star.brightnessT);

				//float3 dirToSun = _WorldSpaceLightPos0;
				//float dayModifier = saturate(1 - dot(dirToSun, starWorldDir));

				v2f o;
				o.offset = v.vertex.xy;
				o.brightness = lerp(0.2,1,saturate(star.brightnessT * 5));
				o.pos = p;
				o.colour = star.colour;
				return o;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				//return 1;//fh
				//return i.uv.y;
				//return i.brightness;
				float dstT = saturate(1-length(i.offset));
				float falloff = min(1, dstT * 1.1);
				falloff = falloff * falloff * falloff;

				float3 col = saturate(falloff * lerp(1, i.colour, 1-falloff)) * i.brightness;

				return float4(col, i.brightness);
				//float4 starData = float4(brightness, 1000, 0, 0);

			}

			ENDCG
		}
	}
}