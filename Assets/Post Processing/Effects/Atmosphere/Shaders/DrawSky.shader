Shader "Hidden/DrawSky"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}
	SubShader
	{
		// No culling or depth
		Cull Off ZWrite Off ZTest Always

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"
			#include "../Shader Common/DrawAtmosphereCommon.hlsl"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;//
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
				float3 viewVector :TEXCOORD1;
			};

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				float3 viewVector = mul(unity_CameraInvProjection, float4(v.uv.xy * 2 - 1, 0, -1));
				o.viewVector = mul(unity_CameraToWorld, float4(viewVector,0));
				return o;
			}

			// Sky tex
			sampler2D _MainTex;
			sampler2D Sky;

			sampler2D TransmittanceLUT;
			float planetRadius;
			float atmosphereThickness;
			float skyTransmittanceWeight;

			// Sun disc settings
			float sunDiscSize;
			float sunDiscBlurA;
			float sunDiscBlurB;

			float3 sampleSunTransmittanceLUT(float3 pos, float3 dir) {
				float dstFromCentre = length(pos);
				float height = dstFromCentre - planetRadius;
				float height01 = saturate(height / atmosphereThickness);

				float uvX = 1 - (dot(pos / dstFromCentre, dir) * 0.5 + 0.5);
				return tex2Dlod(TransmittanceLUT, float4(uvX, height01, 0, 0)).rgb;
			}

			// Thanks to https://www.shadertoy.com/view/slSXRW
			float3 sunDiscWithBloom(float3 rayDir, float3 sunDir) {
				static const float PI = 3.1415;
				const float sunSolidAngle = sunDiscSize*PI/180.0;
				const float minSunCosTheta = cos(sunSolidAngle);

				float cosTheta = dot(rayDir, sunDir);
				if (cosTheta >= minSunCosTheta) return 1;
				
				float offset = minSunCosTheta - cosTheta;
				float gaussianBloom = exp(-offset*1000 * sunDiscBlurA)*0.5;
				float invBloom = 1.0/(0.02 + offset * 100 * sunDiscBlurB)*0.01;
				return gaussianBloom + invBloom;
			}

			float4 frag (v2f i) : SV_Target
			{
				float3 viewDir = normalize(i.viewVector);
				float3 dirToSun = _WorldSpaceLightPos0;

// Account for flipped y on some platforms
#if UNITY_UV_STARTS_AT_TOP
				float3 skyLum = tex2D(Sky, i.uv).rgb;
#else
				float3 skyLum = tex2D(Sky, float2(i.uv.x, 1-i.uv.y)).rgb;
#endif
				float3 sunDisc = sunDiscWithBloom(viewDir, dirToSun);
				float3 transmittance = sampleSunTransmittanceLUT(_WorldSpaceCameraPos, viewDir);
				skyLum += sunDisc * transmittance;
				
				skyLum = toneMap(skyLum);

				// So, this is a horrifying mess.
				// Trying to get background elements (stars, moon) to look good through atmosphere at different times of day and ended up with this hacky stuff.
				// TODO: make it good
				float4 originalCol = tex2D(_MainTex, i.uv);
				float backgroundBrightness = originalCol.a;
				float skyIntensity = saturate(dot(skyLum, float3(0.3, 0.5, 0.2)));
				float skyIntensity2 = pow(skyIntensity, 0.5);
				float t = saturate(skyIntensity2 * 4 - backgroundBrightness);
				float3 transmittedCol = lerp(originalCol, 0, t);
				float3 t2 = transmittedCol * lerp(dot(transmittance,1/3.0), transmittance, skyTransmittanceWeight);
				transmittedCol = lerp(originalCol, t2, saturate(skyIntensity * 4));
				
				// Combine background colour with sky colour
				skyLum = transmittedCol + skyLum;
					
					// Apply dithering to try combat banding
				skyLum = blueNoiseDither(skyLum, i.uv, ditherStrength);

				return float4(skyLum, 1);
			}
			ENDCG
		}
	}
}
