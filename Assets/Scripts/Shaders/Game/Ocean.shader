Shader "Custom/Ocean"
{
	Properties
	{
		_OceanCol("Ocean Colour", 2D) = "white" {}
		_Noise ("Noise", 2D) = "white" {}

		_SpecularSmoothness ("Specular Smoothness", Float) = 0
		_WaveNormalScale ("Wave Normal Scale", Float) = 1
		_WaveStrength ("Wave Strength", Range(0, 1)) = 1
		_WaveSpeed ("Wave Speed", Float) = 1
		[NoScaleOffset] _WaveNormalA ("Wave Normal A", 2D) = "bump" {}
		[NoScaleOffset] _WaveNormalB ("Wave Normal B", 2D) = "bump" {}

		_Refraction ("Refraction", Float) = 0
		_ShadowStrength ("Shadow Strength", Range(0,1)) = 1

		_Tint("Tint", Color) = (1,1,1,1)
		_Specular("Specular", Float) = 0
		_Ambient("Ambient", Color) = (0,0,0,0)
		_FresnelCol("Fresnel Col", Color) = (0,0,0,0)
		_FresnelWeight("Fresnel Weight", Float) = 0
		_FresnelPower("Fresnel Power", Float) = 0
		_TestParams("Test Params", Vector) = (0,0,0,0)

		[Header(Foam)]
		[NoScaleOffset] _FoamDistanceMap ("Foam Distance Map", 2D) = "white" {}
		_FoamDst ("Foam Dst", Range(0,1)) = 1
		_FoamSpeed ("Foam Speed", Float) = 1
		_FoamFrequency ("Foam Frequency", Float) = 1
		_FoamWidth ("Foam Width", Float) = 1
		_FoamEdgeBlend ("Foam Edge Blend", Float) = 1
		_ShoreFoamDst ("Shore Foam Dst", Range(0, 1)) = 0.1
		_FoamNoiseSpeed ("Foam Noise Speed", Float) = 1
		_FoamNoiseStrength ("Foam Noise Strength", Float) = 1
		_FoamNoiseScale ("Foam Noise Scale", Float) = 1
		_FoamColour ("Foam Colour", Color) = (1,1,1,1)
		_FoamMaskScale ("Foam Mask Scale", Float) = 1
		_FoamMaskBlend ("Foam Mask Blend", Float) = 1
	}
	SubShader
	{
		Pass
		{
			Offset 1, 1 // In a Z-fight with the terrain, the ocean should lose (see https://docs.unity3d.com/Manual/SL-Offset.html)
			Tags { "LightMode" = "ForwardBase" "Queue" = "Geometry"}
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fwdbase

			#include "UnityCG.cginc"
			#include "UnityLightingCommon.cginc"
			#include "AutoLight.cginc"

			#include "Assets/Scripts/Shader Common/GeoMath.hlsl"
			#include "Assets/Scripts/Shader Common/Triplanar.hlsl"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float3 normal : NORMAL;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 pos : SV_POSITION;
				float3 worldNormal : NORMAL;
				float3 worldPos : TEXCOORD1;
				LIGHTING_COORDS(4,5)
			};

			
			float4 _TestParams;

			float4 _Tint;
			sampler2D _OceanCol;
			float4 _OceanCol_TexelSize;

			float _SpecularSmoothness;
			float _WaveNormalScale, _WaveStrength, _WaveSpeed;
			sampler2D _WaveNormalA, _WaveNormalB, _Noise;

			float _Refraction;
			float _ShadowStrength;
			float4 _Ambient;
			
			float4 _FresnelCol;
			float _FresnelWeight, _FresnelPower;

			// Foam
			sampler2D _FoamDistanceMap;
			float _FoamSpeed;
			float _FoamFrequency;
			float _ShoreFoamDst;
			float _FoamWidth;
			float _FoamEdgeBlend;
			float _FoamDst;
			float _FoamNoiseSpeed;
			float _FoamNoiseScale;
			float _FoamNoiseStrength;
			float4 _FoamColour;
			float _FoamMaskScale;
			float _FoamMaskBlend;

			float3 calculateWaveNormals(float3 pos, float3 sphereNormal, out float3 tang) {
				float noise = triplanar(sphereNormal, sphereNormal, 0.15, _Noise).r;
	
				float waveSpeed = 0.35 * _WaveSpeed;
				float2 waveOffsetA = float2(_Time.x * waveSpeed, _Time.x * waveSpeed * 0.8);
				float2 waveOffsetB = float2(_Time.x * waveSpeed * - 0.8, _Time.x * waveSpeed * -0.5);

				float3 waveA = triplanarNormal(_WaveNormalA, pos, sphereNormal, _WaveNormalScale, waveOffsetA,_WaveStrength);
				float3 waveB = triplanarNormal(_WaveNormalA, pos, sphereNormal, _WaveNormalScale*0.9, waveOffsetA + float2(0.3,0.7),_WaveStrength);
				//float3 triplanarNormal(sampler2D normalMap, float3 pos, float3 normal, float3 scale, float2 offset, float normalScale, out float3 tangentNormal) {
				float3 waveNormal = triplanarNormal(_WaveNormalB, pos, lerp(waveA, waveB, noise), _WaveNormalScale * 1.25, waveOffsetB, _WaveStrength, tang);

				//return lerp(sphereNormal, waveNormal, _WaveStrength);
				return waveNormal;
			}

			// Calculate foam (rgb = colour; alpha = strength)
			float4 calculateFoam(float2 uv, float3 pointOnUnitSphere, float3 viewDir) {
				float dstFromShore = tex2D(_FoamDistanceMap, uv);
				dstFromShore = saturate(dstFromShore / _FoamDst);

				// Foam noise, used to make foam lines a bit jaggedy
				float2 noiseOffset = float2(0.0617, 0.0314) * _FoamNoiseSpeed * _Time.x;
				float foamNoise = triplanar(pointOnUnitSphere, pointOnUnitSphere, _FoamNoiseScale * 0.1, _Noise, noiseOffset).r;
				foamNoise = (foamNoise - 0.5) * _FoamNoiseStrength * dstFromShore; // increase noise strength further from the shore

				// More foam noise, this time used to fade out sections of the foam lines to break them up a bit
				float2 foamMaskOffset = float2(-0.021, 0.07) * _FoamNoiseSpeed * _Time.x;
				float foamMask = triplanar(pointOnUnitSphere, pointOnUnitSphere, _FoamMaskScale * 0.1, _Noise, foamMaskOffset).r;
				float threshold = lerp(0.375, 0.55, saturate(dstFromShore)); // mask out more further from the shore
				foamMask = smoothstep(threshold, threshold + _FoamMaskBlend * 0.01, foamMask);
				
				// Create foam lines radiating from shore using sin wave
				float foamStrength = sin(dstFromShore * _FoamFrequency - _Time.y * _FoamSpeed + foamNoise);
				foamStrength = saturate(smoothstep(_FoamWidth * 0.1 + _FoamEdgeBlend * 0.1, _FoamWidth * 0.1, foamStrength+1)) * foamMask;
				// Create constant line of foam at the shore
				float foamAtShore = smoothstep(_ShoreFoamDst + 0.1, _ShoreFoamDst, dstFromShore);
				foamStrength = saturate(foamStrength + foamAtShore);

				// Fade out foam as it gets further away
				foamStrength *= 1-smoothstep(0.7, 1, dstFromShore);

				// Fade based on view angle (to combat aliasing)
				float angleStrength = 1-smoothstep(-0.33 - 0.2, -0.33 + 0.2, dot(viewDir, pointOnUnitSphere));

				foamStrength = saturate(foamStrength * angleStrength);
				
				float3 foamColour = lerp(1, _FoamColour.rgb, dstFromShore);
				return float4(foamColour, foamStrength);
			}


			v2f vert (appdata v)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				o.worldNormal = UnityObjectToWorldNormal(v.normal);
				o.worldPos =  mul(unity_ObjectToWorld, float4(v.vertex.xyz, 1));

				TRANSFER_VERTEX_TO_FRAGMENT(o);
				return o;
			}

			float calculateSpecular(float3 normal, float3 viewDir, float3 dirToSun, float smoothness) {
				float specularAngle = acos(dot(normalize(dirToSun - viewDir), normal));
				float specularExponent = specularAngle / smoothness;
				float specularHighlight = exp(-max(0,specularExponent) * specularExponent);
				return specularHighlight;
			}


			float4 frag (v2f i) : SV_Target
			{
			
				float3 pointOnUnitSphere = normalize(i.worldPos);
				float3 sphereNormal = pointOnUnitSphere;

				float2 texCoord = pointToUV(pointOnUnitSphere);
				float mipLevel = calculateGeoMipLevel(texCoord, _OceanCol_TexelSize.zw);

				float shadows = LIGHT_ATTENUATION(i);
				float3 dirToSun = _WorldSpaceLightPos0.xyz;
				float3 viewDir = normalize(i.worldPos - _WorldSpaceCameraPos.xyz);
				
				
				// ---- Calculate normals ----
				float3 tang;
				float3 waveNormal = calculateWaveNormals(i.worldPos, sphereNormal, tang);

				// ---- Get ocean colour ----
				float2 oceanRefractionTexCoord = texCoord + tang.xy * 0.0005 * _Refraction;
				float3 oceanCol = tex2Dlod(_OceanCol, float4(oceanRefractionTexCoord.xy, 0, mipLevel));
	

				// ---- Calculate specular highlight---- 
				float3 specularNormal = waveNormal;
				float specularHighlight = saturate(calculateSpecular(specularNormal, viewDir, dirToSun, _SpecularSmoothness));
				float specularStrength = lerp(0, 1, saturate(shadows * 5));
				specularStrength *= smoothstep(0.4f, 0.5, shadows);
				specularHighlight *= specularStrength;

				// # Apply shading and specular highlight
				float shading = dot(sphereNormal, dirToSun) * 0.5 + 0.5;
				shading = shading * shading;
				float waveShading = dot(waveNormal, dirToSun);
				//waveShading = max(0.5, waveShading);
				//return waveShading;
				//shading = lerp(shading, waveShading, 0.25);
				float grey = dot(oceanCol, float3(0.3, 0.3, 0.4));
				//return 1-grey;
				//shading += saturate(waveShading-0.75) * (1-grey) * 0.75;
				float waveShadeMask = lerp(0.4, 0.95, smoothstep(0.2, 1, dot(sphereNormal, dirToSun)));
			//	return dot(waveNormal, viewDir);
				//shading += smoothstep(-0.1,0.5,dot(waveNormal, viewDir)) * 3;
				float ripple = saturate(smoothstep(-0.53,0.54,dot(waveNormal, viewDir)));
				//return ripple;
				oceanCol += ripple * 0.15;
				//shading += ripple *1;
				//oceanCol = oceanCol * lerp(ripple,1,_TestParams.z);


			//	//return waveShadeMask;
				//shading += saturate(waveShading-waveShadeMask) * (1-grey) * 1;
				//return saturate(waveShading-waveShadeMask) * (1-grey) * 1;
				oceanCol = saturate(oceanCol * (1-specularHighlight) * shading) + specularHighlight * _LightColor0.rgb;

				// # Apply foam
				float4 foam = calculateFoam(texCoord, pointOnUnitSphere, viewDir);
				oceanCol = lerp(oceanCol, foam.rgb, foam.a);

				// ---- Apply shadows ----
				// First, a little fix to the shadow value. When sun is on far side of planet, the far chunks of the earth
				// often don't get rendered for shadows due to culling distance. This means the ocean sometimes has chunks of
				// shadow missing. So crude fix is to just force the shadow value to zero (shadows on) when sufficiently dark.
				float nightT = saturate(dot(sphereNormal,-dirToSun)); // 0 at sunrise/sunset to 1 at midnight
				float nightShadowFixT = smoothstep(0.2,0.3,nightT);
				shadows = lerp(shadows, 0, smoothstep(0.2,0.3,nightT));
				// Apply the shadows to the ocean colour
				oceanCol *= lerp(1, shadows, _ShadowStrength);
				// # Add ambient colour
				oceanCol = saturate(oceanCol + _Ambient * 0.1);

				// Add rim light to help distinguish between ocean and sky at night
				float fresnel = saturate(_FresnelWeight * pow(1 + dot(viewDir, pointOnUnitSphere), _FresnelPower));
				oceanCol += fresnel * _FresnelCol;
				//return fresnel;
				
				return float4(oceanCol, 1);
			}
			ENDCG
		}
	}
	Fallback "VertexLit"
}
