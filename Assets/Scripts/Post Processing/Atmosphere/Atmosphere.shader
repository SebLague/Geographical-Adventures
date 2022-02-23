Shader "Hidden/Atmosphere"
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
			#include "AtmosphereCommon.hlsl"

			struct appdata {
					float4 vertex : POSITION;
					float4 uv : TEXCOORD0;
			};

			struct v2f {
					float4 pos : SV_POSITION;
					float2 uv : TEXCOORD0;
					float3 viewVector : TEXCOORD1;
			};

			v2f vert (appdata v) {
					v2f output;
					output.pos = UnityObjectToClipPos(v.vertex);
					output.uv = v.uv;
					float3 viewVector = mul(unity_CameraInvProjection, float4(v.uv.xy * 2 - 1, 0, -1));
					output.viewVector = mul(unity_CameraToWorld, float4(viewVector,0));
					return output;
			}

			sampler2D _MainTex;
			sampler2D _CameraDepthTexture;

			sampler2D TransmittanceLUT;
			sampler3D AerialPerspectiveLUT;
			sampler3D TransmittanceLUT3D;
			sampler2D Sky;
			sampler2D BlueNoise;
			float4 BlueNoise_TexelSize;

			float4 params;

			// Sun disc
			float sunDiscSize;
			float sunDiscBlurA;
			float sunDiscBlurB;

			// Tone mapping
			float intensity;
			float contrast;
			float whitePoint;


			// Other
			float ditherStrength;
			float aerialPerspectiveStrength;
			float skyTransmittanceWeight;	

			// Thanks to https://www.shadertoy.com/view/slSXRW
			float3 sunDiscWithBloom(float3 rayDir, float3 sunDir) {
				const float sunSolidAngle = sunDiscSize*PI/180.0;
				const float minSunCosTheta = cos(sunSolidAngle);

				float cosTheta = dot(rayDir, sunDir);
				if (cosTheta >= minSunCosTheta) return 1;
				
				float offset = minSunCosTheta - cosTheta;
				float gaussianBloom = exp(-offset*1000 * sunDiscBlurA)*0.5;
				float invBloom = 1.0/(0.02 + offset * 100 * sunDiscBlurB)*0.01;
				return gaussianBloom + invBloom;
			}

			// https://64.github.io/tonemapping/
			float3 reinhard_extended(float3 v, float max_white)
			{
				float3 numerator = v * (1.0f + (v / (max_white * max_white)));
				return numerator / (1.0f + v);
			}

			float3 smoothMax (float3 a, float3 b, float k) {
				k = -abs (k);
				float h = saturate((b - a + k) / (2 * k));
				return a * h + b * (1 - h) - k * h * (1 - h);
			}

			float3 toneMap(float3 lum) {
				lum *= intensity;
				lum = lerp(0.5, lum, contrast);
				lum = reinhard_extended(lum, whitePoint);

				lum = smoothMax(lum, -0.05, 0.05);
				
				return lum;
			}

			// Remap noise to triangular distribution
			// See pg. 45 to 57 of www.gdcvault.com/play/1023002/Low-Complexity-High-Fidelity-INSIDE
			// Thanks to https://www.shadertoy.com/view/MslGR8 See also https://www.shadertoy.com/view/4t2SDh
			float remap_noise_tri_erp(float v )
			{
				float r2 = 0.5 * v;
				float f1 = sqrt(r2);
				float f2 = 1.0 - sqrt(r2 - 0.25);    
				return (v < 0.5) ? f1 : f2;
			}

			float3 getBlueNoise(float2 uv) {
				float2 screenSize = _ScreenParams.xy;
				
				uv = (uv * screenSize) * BlueNoise_TexelSize.xy;
			
				float3 blueNoise = tex2D(BlueNoise, uv).rgb;
				float3 m = 0;
				m.r = remap_noise_tri_erp(blueNoise.r);
				m.g = remap_noise_tri_erp(blueNoise.g);	
				m.b = remap_noise_tri_erp(blueNoise.b);

				float3 weightedNoise = (m * 2.0 - 0.5);
				return weightedNoise;
			}

			float3 blueNoiseDither(float3 col, float2 uv, float strength) {
				float3 weightedNoise = getBlueNoise(uv) / 255.0 * strength;
			
				return col + weightedNoise;
			}

			// Remap a value from the range [minOld, maxOld] to [0, 1]
			float remap01(float minOld, float maxOld, float val) {
				return saturate((val - minOld) / (maxOld - minOld));
			}

			float4 frag (v2f i) : SV_Target
			{	
				float4 originalCol = tex2D(_MainTex, i.uv);

				float viewLength = length(i.viewVector);
				float3 viewDir = i.viewVector / viewLength;
				float nonlin_depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv);
				float sceneDepth = LinearEyeDepth(nonlin_depth) * viewLength;

				float nearClipPlane = _ProjectionParams.y;
				float farClipPlane = _ProjectionParams.z;
			
				float depthT = remap01(nearClipPlane, terrestrialClipDst, sceneDepth);

				float3 rayOrigin = _WorldSpaceCameraPos;
				float3 rayDir = viewDir;

				// Account for flipped y on some platforms (not quite sure where this needs to be used currently, will need to test...)
				float2 uv = float2(i.uv.x, 1-i.uv.y);
				#if UNITY_UV_STARTS_AT_TOP
					uv = i.uv;
				#endif
			

				// Draw sky since no 'terrestial' object has been rendered here.
				// Non terrestial objects would be stuff like stars and moon, which sky should be drawn on top of
				// (sky is rendered each frame into small texture, so just need to composite here)
				if (sceneDepth > terrestrialClipDst) {//
					float3 skyLum = tex2D(Sky, uv).rgb;
					// Composite sun disc (not included in sky texture because resolution is too low for good results)
					float3 sunDisc = sunDiscWithBloom(rayDir, dirToSun) * (sceneDepth >= farClipPlane);
					float3 transmittance = getSunTransmittanceLUT(TransmittanceLUT, rayOrigin, rayDir);
					skyLum += sunDisc * transmittance;
				
					skyLum = toneMap(skyLum);
					//return originalCol.r > 5;
					
		
					skyLum = originalCol.rgb * lerp(dot(transmittance,1/3.0), transmittance, skyTransmittanceWeight) + skyLum;
					
					// Apply dithering to try combat banding
					skyLum = blueNoiseDither(skyLum, i.uv, ditherStrength);
					
					return float4(skyLum, 1);
				}

				float2 hitInfo = raySphere(0, atmosphereRadius, rayOrigin, rayDir);
				float dstToAtmosphere = hitInfo.x;
				float dstThroughAtmosphere = hitInfo.y;
				
				// View ray goes through atmosphere (and not blocked by anything in front of it)
				if (dstThroughAtmosphere > 0 && dstToAtmosphere < sceneDepth) {
					float3 inPoint = rayOrigin + rayDir * (dstToAtmosphere);
					float3 outPoint = rayOrigin + rayDir * min(dstToAtmosphere + dstThroughAtmosphere, sceneDepth);

					float3 transmittance = tex3Dlod(TransmittanceLUT3D, float4(i.uv, depthT, 0)).rgb;
					float3 luminance = tex3Dlod(AerialPerspectiveLUT, float4(i.uv,depthT, 0)).rgb;
					
					luminance = toneMap(luminance);
					luminance = originalCol.rgb * transmittance + luminance;
					

					float3 outputCol = blueNoiseDither(luminance, i.uv, ditherStrength);
					outputCol = lerp(originalCol.rgb, outputCol, aerialPerspectiveStrength);
					return float4(outputCol, 1);
				}

				// Not looking at atmosphere, return original colour
				return float4(originalCol.rgb, 1);
			}
			ENDCG
		}
	}
}