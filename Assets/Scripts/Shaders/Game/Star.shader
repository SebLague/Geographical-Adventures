Shader "Instanced/Star" {
	Properties {
		
	}
	SubShader {

		ZWrite Off

		Tags { "Queue"="Geometry+1" }

		Pass {

			CGPROGRAM

			#pragma vertex vert
			#pragma fragment frag
			#pragma target 4.5

			#include "UnityCG.cginc"
			
			struct Star {
				float3 dir;
				float brightnessT;
			};

			StructuredBuffer<Star> StarData;
			float3 testParams;
			float size;
			float4 centre;
			//float3 dirToSun;
			float brightnessMultiplier;
			float4x4 rotationMatrix;
			sampler2D Sky;

			struct v2f
			{
				float4 pos : SV_POSITION;
				float2 offset : TEXCOORD0;
				float brightness : TEXCOORD1;
			};

			v2f vert (appdata_full v, uint instanceID : SV_InstanceID)
			{
				Star star = StarData[instanceID];
				float farClipPlane = _ProjectionParams.z;

				float3 objectPos = mul(rotationMatrix, float4(star.dir * (farClipPlane-1), 1));


				float4 p = mul(UNITY_MATRIX_VP, float4(centre + objectPos, 1.0f));
				float4 screenPos = ComputeScreenPos(p);
				float2 uv = screenPos.xy / screenPos.w;
				
				p += float4(v.vertex.x / _ScreenParams.x, -v.vertex.y / _ScreenParams.y, 0, 0) * size;

				// Fade out stars based on sky brightness
				const int mipLevel = 3;
				float4 skyLum = tex2Dlod(Sky, float4(uv, 0, mipLevel));
				float skyBrightness = dot(skyLum.rgb, 1/3.0);
				float dayT = smoothstep(0.05,0.2,skyBrightness);
				//nightT = skyBrightness < 0.5;
				//nightT = 1;
				
				//float nightT = saturate(saturate(dot(star.dir, -dirToSun) + 0.5) * 4);
				//nightT = 1;
				v2f o;
				o.offset = v.vertex.xy;
				o.brightness = star.brightnessT * brightnessMultiplier * (1-saturate(dayT));
				//o.uv = uv;
				//o.brightness = star.brightnessT * nightT;
				//o.brightness = dot(star.dir, dirToSun) > 0;
				////o.pos = mul(UNITY_MATRIX_VP, float4(worldPosition, 1.0f));
				o.pos = p;
				return o;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				//return i.uv.y;
				//return i.brightness;
				float brightness = (length(i.offset) < 1) * i.brightness;
				//float4 starData = float4(brightness, 1000, 0, 0);
				return brightness;
			}

			ENDCG
		}
	}
}