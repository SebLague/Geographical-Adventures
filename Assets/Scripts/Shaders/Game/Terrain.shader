Shader "Custom/Terrain"
{
	Properties
	{
		_ColWest("Colour West", 2D) = "white" {}
		_ColEast("Colour East", 2D) = "white" {}
		_NormalMapWest("Normal Map West", 2D) = "white" {}
		_NormalMapEast("Normal Map East", 2D) = "white" {}
		_LightMap("Light Map", 2D) = "white" {}
		_LakeMask("Lake Mask", 2D) = "white" {}

		[Header(Lighting)]
		_AmbientNight("Ambient Night", Color) = (0,0,0,0)
		_CityLightAmbient("City Light Ambient", Color) = (0,0,0,0)
		_FresnelCol("Fresnel Col", Color) = (0,0,0,0)
		_Contrast ("Contrast", Float) = 1
		_BrightnessAdd("Brightness Add", Float) = 0
		_BrightnessMul("Brightness Mul", Float) = 1

		[Header(Shadows)]
		_ShadowStrength("Shadow Strength", Range(0,1)) = 1
		_ShadowEdgeCol("Shadow Edge Col", Color) = (0,0,0,0)
		_ShadowInnerCol("Shadow Inner Col", Color) = (0,0,0,0)

		[Header(Lakes)]
		_Specular("Specular", Float) = 0
		[NoScaleOffset] _WaveNormalA ("Wave Normal A", 2D) = "bump" {}
		_WaveNormalScale ("Wave Normal Scale", Float) = 1
		_WaveStrength ("Wave Strength", Range(0, 1)) = 1

		[Header(Test)]
		_TestParams("Test Params", Vector) = (0,0,0,0)

	}
	SubShader
	{
		Pass
		{
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
				float4 screenPos : TEXCOORD2;
				LIGHTING_COORDS(4,5)
			};

			sampler2D _ColWest, _ColEast, _NormalMapWest, _NormalMapEast;
			sampler2D _LightMap, _LakeMask, _WaveNormalA;
			float4 _ColWest_TexelSize;

			float _ShadowStrength;
			float3 _ShadowEdgeCol, _ShadowInnerCol;
			float _WaveNormalScale, _WaveStrength;

			float _ShadingPow, _BrightnessAdd, _BrightnessMul, _Specular, _Contrast;
			float4 _AmbientNight, _CityLightAmbient, _FresnelCol;
			float4 _TestParams;

			v2f vert (appdata v)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				o.worldNormal = UnityObjectToWorldNormal(v.normal);
				o.worldPos =  mul(unity_ObjectToWorld, float4(v.vertex.xyz, 1));
				o.screenPos = ComputeScreenPos(o.pos);
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
				float2 texCoord = pointToUV(pointOnUnitSphere);
				float lightMap = tex2D(_LightMap, texCoord);
				float lakeMask = tex2D(_LakeMask, texCoord);

				float3 detailNormal = 0;
				float3 unlitTerrainCol = 0;
				if (texCoord.x < 0.5) {
					float2 tileTexCoord = float2(texCoord.x * 2, texCoord.y);
					float mipLevel = calculateGeoMipLevel(tileTexCoord, _ColWest_TexelSize.zw);
					unlitTerrainCol = tex2Dlod(_ColWest, float4(tileTexCoord, 0, mipLevel));
					detailNormal = tex2D(_NormalMapWest, tileTexCoord);
				}
				else {
					float2 tileTexCoord = float2((texCoord.x - 0.5) * 2, texCoord.y);
					float mipLevel = calculateGeoMipLevel(tileTexCoord, _ColWest_TexelSize.zw);
					unlitTerrainCol = tex2Dlod(_ColEast, float4(tileTexCoord, 0, mipLevel));
					detailNormal = tex2D(_NormalMapEast, tileTexCoord);
				}


				float3 meshWorldNormal = normalize(i.worldNormal);
				detailNormal = normalize(detailNormal * 2 - 1);
				// Blend detail normal with mesh normal
				float3 worldNormal = normalize(meshWorldNormal * 2 + detailNormal * 1.25);

				float3 dirToSun = _WorldSpaceLightPos0.xyz;
	
				float3 viewDir = normalize(i.worldPos - _WorldSpaceCameraPos.xyz);

				float3 waveA = triplanarNormal(_WaveNormalA, i.worldPos, pointOnUnitSphere, _WaveNormalScale, 0,_WaveStrength);
				float lakeSpecular = calculateSpecular(waveA, viewDir, dirToSun, _Specular) * lakeMask;
				//return lakeSpecular;
				
				float shadows = LIGHT_ATTENUATION(i);
				float3 shadowCol = lerp(_ShadowEdgeCol, _ShadowInnerCol, saturate((1-shadows) * 1.5));
				shadows = lerp(1, shadows, _ShadowStrength);
				 
				float fakeLighting = pow(dot(worldNormal, pointOnUnitSphere), 3);
				
				// ---- Calculate night colour ----
				float nightShading = fakeLighting;
			
				float greyscale = dot(unlitTerrainCol, float3(0.299, 0.587, 0.114));
				float3 nightCol = (pow(greyscale, 0.67) * nightShading + nightShading * 0.3) * lerp(_AmbientNight * 0.1, _CityLightAmbient, saturate(lightMap * 1));
				float fresnel = saturate(1.5 * pow(1 + dot(viewDir, worldNormal), 5));
				nightCol += fresnel * _FresnelCol;
				float nightT = smoothstep(-0.25, 0.25, dot(pointOnUnitSphere, dirToSun));
			
				
				// ---- Calculate day colour ----
				float3 shading = saturate(saturate(dot(worldNormal, dirToSun) + _BrightnessAdd)) * _BrightnessMul;
				float3 terrainCol = unlitTerrainCol * shading + lakeSpecular * 1;
				// Apply shadows
				terrainCol = lerp(terrainCol, shadowCol, 1-shadows);
				// Adjust contrast
				terrainCol = lerp(0.5, terrainCol, _Contrast);
				terrainCol *= lerp(fakeLighting, 1, 0.5); // helps to make terrain look less flat and featureless when sun is directly overhead

				// ---- Interpolate between night and day for final colour ----
				float3 finalTerrainCol = lerp(nightCol, terrainCol, nightT);
				return float4(finalTerrainCol, 1);
			}
			ENDCG
		}
	}
	Fallback "VertexLit"
}
