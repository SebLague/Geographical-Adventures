Shader "Custom/Earth" {
	Properties
	{

		// Unorganized
		_CountryIndexMap ("Country Index Map", 2D) = "white" {}
		_Noise ("Noise", 2D) = "white" {}
		_NoiseB ("Noise B", 2D) = "white" {}
		_AmbientLight ("Ambient Light", Float) = 0

		_DetailNormalStrength ("Detail Normal Strength", Range(0,1)) = 0
		
		_TestParams ("Test Params", Vector) = (0,0,0,0)

		[Space()] [Header(Country)] [Space()]
		_CountryHighlightStrength ("Country Highlight Strength", Float) = 1.3
		_CountryOutlineColDay ("Country Outline Day", Color) = (1,1,1,1)
		_CountryOutlineColNight ("Country Outline Night", Color) = (1,1,1,1)

		// ---- Ocean ----
		[Space()] [Header(Ocean)] [Space()]
		_SpecularSmoothness ("Specular Smoothness", Float) = 0
		_WaveNormalScale ("Wave Normal Scale", Float) = 1
		_WaveStrength ("Wave Strength", Range(0, 1)) = 1
		_WaveSpeed ("Wave Speed", Float) = 1
		[NoScaleOffset] _WaveNormalA ("Wave Normal A", 2D) = "bump" {}
		[NoScaleOffset] _WaveNormalB ("Wave Normal B", 2D) = "bump" {}
		[NoScaleOffset] _WaveDstMap ("Wave Dst Map", 2D) = "white" {} // Note: used in Waves.hlsl include

		_Refraction ("Refraction", Float) = -0.1

		[Header(Vertex Waves)]
		WaveNoiseScale("Wave Noise Scale", Float) = 10
		WaveNoiseSpeed("Wave Noise Speed", Float) = 0.1
		WaveNoiseAmplitude("Wave Noise Amplitude", Float) = 0.1
		WaveNoiseStretch("Wave Noise Stretch", Float) = 0.1
		WaveShoreFalloff("Wave Shore Falloff", Float) = 0.1

		// Foam
		[Header(Foam)]
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
	SubShader {

		Pass {
			Tags { "LightMode" = "ForwardBase" "Queue" = "Geometry"}
		
			CGPROGRAM

			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fwdbase
			#include "UnityCG.cginc"
			
			
			#include "Assets/Scripts/Shader Common/GeoMath.hlsl"
			#include "Assets/Scripts/Shader Common/Shading.hlsl"
			#include "Assets/Scripts/Shader Common/Triplanar.hlsl"
			#include "Assets/Scripts/Shader Common/SimplexNoise.hlsl"//
			#include "Waves.hlsl"

			#include "UnityLightingCommon.cginc"
			#include "AutoLight.cginc"

			// Textures
			sampler2D ColourMap;
			float4 ColourMap_TexelSize;

			sampler2D _CountryIndexMap;
			sampler2D NormalMap;
			sampler2D _Noise;
			sampler2D _NoiseB;
			float2 tileTexCoordOffset;

			sampler2D _MainTex;
			sampler2D CountryData;
			float _CountryHighlightStrength;


			// ---- Ocean ----
			float _SpecularSmoothness;

			float _WaveSpeed;
			float _WaveStrength;
			sampler2D _WaveNormalA;
			sampler2D _WaveNormalB;
		

			float _WaveNormalScale;
			float _Refraction;

			// Foam
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
		

			// ---- Other ----
			float _AmbientLight;
			float _DetailNormalStrength;
			float4 _CountryOutlineColDay;
			float4 _CountryOutlineColNight;
			float4 _TestParams;

			float3 dirToSun;
			float shadowStrength;

			sampler2D _CloudShadowMap;
			float4x4 _WorldToCloudShadowMatrix;

			StructuredBuffer<float> CountryHighlights;

			struct appdata
			{
				float4 vertex : POSITION;
				float3 normal : NORMAL;
			};

			SamplerState my_linear_clamp_sampler;


			struct v2f
			{
				float4 pos : SV_POSITION;
				float3 worldNormal : NORMAL;
				float3 worldPos : TEXCOORD0;
				LIGHTING_COORDS(4,5)
			};

	
			v2f vert(appdata v) {
				v2f o;

				float3 worldPos = mul(unity_ObjectToWorld,v.vertex).xyz;
				
				float3 waveNormal = v.normal;
				float3 wavePos = waveCalc(worldPos, waveNormal);
				v.normal = waveNormal;//
				v.vertex = float4(wavePos, 1);
	
				o.pos = UnityObjectToClipPos (v.vertex);
				o.worldPos = worldPos;
				o.worldNormal = mul(unity_ObjectToWorld, float4(v.normal.xyz, 0)).xyz;

				// hanks to https://alastaira.wordpress.com/2014/12/30/adding-shadows-to-a-unity-vertexfragment-shader-in-7-easy-steps/
				// The TRANSFER_VERTEX_TO_FRAGMENT macro populates the chosen LIGHTING_COORDS in the v2f structure
				// with appropriate values to sample from the shadow/lighting map
				TRANSFER_VERTEX_TO_FRAGMENT(o);
				
				return o;
			}


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
			float4 calculateFoam(float2 uv, float3 pointOnUnitSphere, float dstFromShore) {
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
				float foam = sin(dstFromShore * _FoamFrequency - _Time.y * _FoamSpeed + foamNoise);
				foam = saturate(smoothstep(_FoamWidth * 0.1 + _FoamEdgeBlend * 0.1, _FoamWidth * 0.1, foam+1)) * foamMask;
				// Create constant line of foam at the shore
				float foamAtShore = smoothstep(_ShoreFoamDst + 0.1, _ShoreFoamDst, dstFromShore);
				foam = saturate(foam + foamAtShore);

				// Fade out foam as it gets further away
				foam *= 1-smoothstep(0.7, 1, dstFromShore);
				
				float3 foamColour = lerp(1, _FoamColour.rgb, dstFromShore);
				return float4(foamColour, foam);
			}

			float4 frag(v2f i) : COLOR
			{
				// ------------------------ Texture coordinate stuff ------------------------
				// * Calculate texture coordinates
				const int2 tileCount = int2(4, 2);
				float3 pointOnUnitSphere = normalize(i.worldPos);
				float2 texCoord = pointToUV(pointOnUnitSphere);
				float2 tileTexCoord = texCoord * tileCount - tileTexCoordOffset;
				
				// * Calculate mip level (doing manually to avoid mipmap seam where texture wraps on x axis -- there's probably a better way?)
				//return mipTexCoord.y;
				float2 dx, dy;
				if (texCoord.x < 0.75 && texCoord.x > 0.25) {
					dx = ddx(texCoord);
					dy = ddy(texCoord);
				}
				else {
					// Shift texCoord so seam is on other side of world
					dx = ddx((texCoord + float2(0.5, 0)) % 1);
					dy = ddy((texCoord + float2(0.5, 0)) % 1);
				}
				float mipMapWeight = 0.5f;
				int2 texSize = ColourMap_TexelSize.zw;
				texSize = tileCount * ColourMap_TexelSize.zw;
				dx *= texSize * mipMapWeight;
				dy *= texSize * mipMapWeight;

				// Thanks to https://community.khronos.org/t/mipmap-level-calculation-using-dfdx-dfdy/67480/2
				float maxSqrLength = max(dot(dx, dx), dot(dy, dy));
				float mipLevel = 0.5 * log2(maxSqrLength); // 0.5 * log2(x^2) == log2(x)
				// Clamp mip level to prevent value blowing up at poles
				const int maxMipLevel = 8;
				mipLevel = min(maxMipLevel, mipLevel);
				//return (mipLevel)/(float)maxMipLevel;
			
				// * Sample maps
				float4 colour = tex2Dlod(ColourMap, float4(tileTexCoord.xy, 0, mipLevel));
				float outlineDstData = colour.a;
				//return colour;

				float3 sphereNormal = pointOnUnitSphere;
			
				// int value 0 to 9 encodes outline; int value 10 to 255 encodes shore distance
				float outline = 1-smoothstep(0, 10/255.0, outlineDstData);
				float dstFromShore = remap01(10/255.0, 1, outlineDstData);
				
				// Calculate country highlight
				float countryHighlight = 1.1; // Default brightness
				float countryIndexUNorm = tex2Dlod(_CountryIndexMap, float4(texCoord.xy, 0, 0)).r;
				//return c;
				int countryIndex = floor(countryIndexUNorm * 255.0)-1;
				float countryHightlightT = 0;
				if (countryIndex >= 0) {
					
					countryHightlightT = CountryHighlights[countryIndex];
					countryHighlight = lerp(countryHighlight, _CountryHighlightStrength, countryHightlightT);
				}

				float oceanT = (outlineDstData > 11/255.0) * (countryIndex < 0);

				float3 detailNormal = tex2Dlod(NormalMap, float4(tileTexCoord, 0, mipLevel)).rgb;
				detailNormal = normalize(detailNormal * 2 - 1);
				// Blend detail normal with mesh normal
				float3 normal = normalize(i.worldNormal * 2 + detailNormal * 1);

			
				float shadows = LIGHT_ATTENUATION(i);
				// Manually add shadow of earth when sun far on opposite side of earth
				// (increasing camera far clip plane would fix this, but is costly as fewer terrain chunks can be culled)
				if (dot(dirToSun, pointOnUnitSphere) < -0.2) {
					shadows = min (shadows, 1-shadowStrength);
				}

				// Ocean col
				float3 tang;
				float3 waveNormal = calculateWaveNormals(i.worldPos, sphereNormal, tang);
				float2 oceanRefractionTexCoord = tileTexCoord + tang.xy * 0.0005 * _Refraction;
				float poleT = abs(texCoord.y - 0.5) * 2;
				float oceanMipLevel = mipLevel + 3 * saturate(lerp(4, 0, poleT));
				float4 oceanCol = tex2Dlod(ColourMap, float4(oceanRefractionTexCoord.xy, 0, oceanMipLevel));
			
				
				float3 viewDir = normalize(i.worldPos - _WorldSpaceCameraPos.xyz);

				float3 specularNormal = lerp(normal, waveNormal, oceanT);
				float specularHighlight = saturate(calculateSpecular(specularNormal, viewDir, dirToSun, _SpecularSmoothness));

				float waveShading = max(saturate(dot(lerp(normal, waveNormal,0.2), _WorldSpaceLightPos0)),0.2);
				waveShading = saturate(dot(normalize(i.worldNormal),_WorldSpaceLightPos0.xyz));
				

				float4 foam = calculateFoam(texCoord, pointOnUnitSphere, dstFromShore);
				// Fade foam based on view angle (at sharp angles it's very aliased and ugly)
				float foamFade = saturate(dot(viewDir, -sphereNormal));
				foamFade = foamFade * foamFade * 3;
				//return pow(mipLevel/3,1);
				foam.a = saturate(saturate(foam.a) * foamFade * 0.75);

				// Reduce wave specular highlights on foam
				float specularStrength = saturate(1-foam.a * 3);
				specularStrength = lerp(0.05, specularStrength, oceanT);

				specularHighlight *= specularStrength;
				float4 sunCol = _LightColor0;
				oceanCol = saturate(oceanCol * (1-specularHighlight) * waveShading);

				float shading = max(saturate(dot(normal, _WorldSpaceLightPos0)), _AmbientLight);
				//shading = pow(shading, 1/2.2);

				oceanCol = lerp(oceanCol, foam, foam.a);

				// Add country outlines
				float outlineNightT = smoothstep(0.25, -0.25, dot(sphereNormal, dirToSun));
				float4 countryOutlineCol = lerp(_CountryOutlineColDay, _CountryOutlineColNight, outlineNightT);
				
				float4 landCol = colour * countryHighlight;
				outline *= lerp(1,0.25,saturate(mipLevel / 3)*1);
				
				landCol *= shading;
				
				float4 finalCol = lerp(landCol, oceanCol, oceanT);
				
				finalCol *= shadows;
				finalCol = lerp(finalCol, countryOutlineCol, outline);
				finalCol += specularHighlight * sunCol;
				float alpha = lerp(1,3,outlineNightT * outline);
				finalCol.a = alpha;
				return finalCol;

			}

			ENDCG
		}
		 // Pass to render object as a shadow caster
		Pass
		{
			Name "ShadowCaster"
			Tags { "LightMode" = "ShadowCaster" }

			ZWrite On ZTest LEqual Cull Off

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 2.0
			#pragma multi_compile_shadowcaster
			#include "UnityCG.cginc"
			#include "Waves.hlsl"

			struct appdata
			{
				float4 vertex : POSITION;
				float3 normal : NORMAL;
			};

			struct v2f {
				V2F_SHADOW_CASTER;
				UNITY_VERTEX_OUTPUT_STEREO
			};

			v2f vert( appdata v )
			{
				v2f o;
				float3 worldPos = mul(unity_ObjectToWorld,v.vertex).xyz;
				float3 normal = 0;
				float3 wavePos = waveCalc(worldPos, normal);
				v.vertex = float4(wavePos, 1);

				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
				return o;
			}

			float4 frag( v2f i ) : SV_Target
			{
				SHADOW_CASTER_FRAGMENT(i)
			}
			ENDCG
		}
	}
}