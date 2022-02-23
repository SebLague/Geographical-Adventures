Shader "Hidden/Clouds"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}
	SubShader
	{
		// No culling or depth
		Cull Off ZWrite Off ZTest Always

		CGINCLUDE
		#pragma vertex vert
		#pragma fragment frag

		#include "UnityCG.cginc"
		#include "CloudMath.hlsl"

		struct appdata
		{
			float4 vertex : POSITION;
			float2 uv : TEXCOORD0;
		};

		struct v2f
		{
			float2 uv : TEXCOORD0;
			float4 vertex : SV_POSITION;
			float3 viewVector : TEXCOORD1;
		};

		v2f vert (appdata v)
		{
			v2f o;
			o.vertex = UnityObjectToClipPos(v.vertex);
			o.uv = v.uv;

			float3 viewVector = mul(unity_CameraInvProjection, float4(v.uv.xy * 2 - 1, 0, -1));
			o.viewVector = mul(unity_CameraToWorld, float4(viewVector, 0));

			return o;
		}

		sampler2D DepthTextureSmall;

		Texture3D NoiseTex;
		SamplerState samplerNoiseTex;
		
		float densityMultiplier;
		float noiseScale;
		float4 testParams;
		float3 dirToSun;

		float planetRadius;
		float cloudRadiusMin;
		float cloudRadiusMax;

		static const int numLightConePoints = 6;
		float4 lightConePoints[numLightConePoints];

		// Atmosphere LUTs
		Texture3D AerialPerspectiveLuminance;
		Texture3D AerialPerspectiveTransmittance;
		SamplerState linear_repeat_sampler3D;
		sampler2D atmosphereTransmittanceLUT;
		float atmosphereThickness;

		float3 sampleAtmosphereTransmittanceLUT(float3 pos, float3 dir) {
			float dstFromCentre = length(pos);
			float height = dstFromCentre - planetRadius;
			float height01 = saturate(height / atmosphereThickness);

			float uvX = 1 - (dot(pos / dstFromCentre, dir) * 0.5 + 0.5);
			return tex2Dlod(atmosphereTransmittanceLUT, float4(uvX, height01, 0, 0)).rgb;
		}

		float sampleCloudDensity(float3 pos, int mipLevel) {

			float3 samplePos = pos / 200 * noiseScale;
			float heightT = (length(pos) - cloudRadiusMin) / (cloudRadiusMax - cloudRadiusMin);
			float d = min((heightT-0.05) * 4, 1);
			d = min(d, 1.5-heightT * 1.5);

			float4 data = NoiseTex.SampleLevel(samplerNoiseTex, samplePos, mipLevel);
			return max(0, data.b-testParams.x) * densityMultiplier * d;
		}

		float3 lightMarch(float3 rayPos, float3 rayDir, int mipLevel) {
			// Sun blocked by planet
			if (raySphere(0, planetRadius, rayPos, rayDir).y > 0) {
				return 0;
			}

			float opticalDepth = 0;

			for (int i = 0; i < numLightConePoints; i ++) {
				float4 coneOffset = lightConePoints[i];
				float3 p = rayPos + coneOffset.xyz;
				float density = sampleCloudDensity(p, 1);
				opticalDepth += max(0, density) * coneOffset.w; // coneOffset.w = stepSize between cone points		
			}

			float3 atmosphereTransmittance = sampleAtmosphereTransmittanceLUT(rayPos, rayDir);
			return exp(-opticalDepth) * atmosphereTransmittance;
		}

		// Returns lum (rgb) and transmittance (a)
		float4 raymarch(float3 rayPos, float3 rayDir, float rayLength, float2 uv) {
			
			const int mipLevel = 0;
			float transmittance = 1;
			float3 lum = 0;

			const int maxSteps = 256;
			float maxViewLength = raySphere(0, cloudRadiusMax, float3(0, cloudRadiusMin,0), float3(1,0,0)).y * 2;
			float stepSize = maxViewLength / maxSteps;
			
			//rayLength = lerp(rayLength, testParams.x, testParams.y);
			const float transmittanceThreshold = 0.01;
			float nearClipPlane = _ProjectionParams.y;
			float farClipPlane = _ProjectionParams.z;
			float3 lastAtmoLum = 0;
			
			float dst = 0;
			while (dst < rayLength) {
				rayPos += rayDir * stepSize;
				float density = sampleCloudDensity(rayPos, mipLevel);

				if (density > 0) {
					
					
					float depthT = remap01(nearClipPlane, farClipPlane, dst);
					//float4 atmo = tex3Dlod(atmosphereAerialPerspectiveLUT, float4(uv,depthT,0));
					//float4 atmoT = tex3Dlod(atmosphereTransmittanceLUT3D, float4(uv,depthT,0));
					float4 atmo = AerialPerspectiveLuminance.SampleLevel(linear_repeat_sampler3D, float3(uv, depthT), 0);
					float4 atmoT = AerialPerspectiveTransmittance.SampleLevel(linear_repeat_sampler3D, float3(uv, depthT), 0);;
					float3 deltaAtmoLum = atmo.rgb - lastAtmoLum;
					lastAtmoLum = atmo.rgb;

					float sampleTransmittance = exp(-density * stepSize);

					float3 sunTransmittance = lightMarch(rayPos, dirToSun, mipLevel + 1);
					float3 inScattering = density * sunTransmittance;
					lum += inScattering * transmittance * stepSize * atmoT + deltaAtmoLum * transmittance;
					//lum += inScattering * transmittance * stepSize;

					transmittance *= sampleTransmittance;

					if (transmittance < transmittanceThreshold) {
						break;
					}
				}

				dst += stepSize;
			}
			// since we'll be adding this to the background col (which already has aerial perspective applied), we need to subtract
			// the atmosphere light that gets scattered in between the camera and the end of the clouds (otherwise it will be counted twice)
			lum -= lastAtmoLum * transmittance; 

			return float4(lum, transmittance);
		}

		float4 renderClouds(float2 uv, float3 viewDir, float depth) {

			float3 luminance = 0;
			float transmittance = 1;

			float3 viewPos = _WorldSpaceCameraPos;
			float2 cloudSphereHitInfo = raySphere(0, cloudRadiusMax, viewPos, viewDir);
			float dstToCloudSphere = cloudSphereHitInfo.x;
			float dstThroughCloudSphere = cloudSphereHitInfo.y;
			float dstThroughCloudLayer = min(cloudSphereHitInfo.y, depth - cloudSphereHitInfo.x);

			if (dstThroughCloudLayer > 0) {
				float3 inPoint = viewPos + viewDir * dstToCloudSphere;
				float3 outPoint = viewPos + viewDir * min(dstToCloudSphere + dstThroughCloudSphere, depth);
				float rayLength = length(outPoint - inPoint);

				float4 result = raymarch(inPoint, viewDir, rayLength, uv);
				luminance = result.rgb;
				transmittance = result.a;
			}

			return float4(luminance, transmittance);
		}
		ENDCG

		// Pass 0: render clouds at low resolution (rgb = luminance, a = transmittance)
		Pass 
		{
			CGPROGRAM

			float4 frag (v2f i) : SV_Target
			{
				float depth = tex2Dlod(DepthTextureSmall, float4(i.uv, 0, 0));
				float3 viewDir = normalize(i.viewVector);

				return renderClouds(i.uv, viewDir, depth);
			}

			ENDCG
		}

		// Pass 1: composite low res clouds onto full res scene.
		// Also detect where low res clouds were rendered with very wrong depth value fill in with full res clouds
		Pass 
		{
			CGPROGRAM

			sampler2D _MainTex; // background on which to composite clouds
			sampler2D _CloudTex; // lower resolution cloud texture (rgb: luminance, a: transmittance)
			sampler2D _CameraDepthTexture;
			

			float4 frag (v2f i) : SV_Target
			{
				float4 uv = float4(i.uv, 0, 0);
				float4 cloud = tex2Dlod(_CloudTex, uv);
				float4 originalCol = tex2Dlod(_MainTex,uv);
				float depthFull = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv)) * length(i.viewVector);

				//float depthFull = tex2Dlod(DepthTexture, uv);
				float depthSmall = tex2Dlod(DepthTextureSmall, uv);

				if (abs(depthFull - depthSmall) > 3) {
					float4 r = renderClouds(i.uv, normalize(i.viewVector), depthFull);
					cloud = r;
					//return 1;
				}


				return float4(originalCol.rgb * cloud.a + cloud.rgb, 1);
			}

			ENDCG
		}
	}
}
