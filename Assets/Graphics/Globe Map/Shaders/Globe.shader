Shader "Custom/Globe"
{
	Properties
	{
		_Color("Colour", Color) = (1,1,1,1)
		_FresnelCol("Fresnel Col", Color) = (1,1,1,1)
		_FresnelPow("Fresnel Power", Float) = 1
		_ShadingPow("Shading Power", Float) = 1
		_Brightness("Brightness", Float) = 1
		_Specular("Specular", Float) = 0
		_Ambient("Ambient", Color) = (0,0,0,0)
		_BlueNoise("Blue Noise", 2D) = "white" {}
		_DitherStrength("Dither Str", Float) = 0
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

			float4 _Color;

			float _ShadingPow, _Brightness, _Specular;
			float4 _Ambient;

			sampler2D _BlueNoise;
			float4 _BlueNoise_TexelSize;
			float _DitherStrength;
			float _FresnelPow;
			float4 _FresnelCol;

			v2f vert (appdata v)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				o.worldNormal = mul(unity_ObjectToWorld, float4(v.normal, 0)).xyz;
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

			
			// Remap noise to triangular distribution
			// See pg. 45 to 57 of www.gdcvault.com/play/1023002/Low-Complexity-High-Fidelity-INSIDE
			// Thanks to https://www.shadertoy.com/view/MslGR8 See also https://www.shadertoy.com/view/4t2SDh
			float remap_noise_tri_erp(float v)
			{
				float r2 = 0.5 * v;
				float f1 = sqrt(r2);
				float f2 = 1.0 - sqrt(r2 - 0.25);
				return (v < 0.5) ? f1 : f2;
			}

			float3 getBlueNoise(float2 uv) {
				float2 screenSize = _ScreenParams.xy;
				
				uv = (uv * screenSize) * _BlueNoise_TexelSize.xy;

				float3 blueNoise = tex2D(_BlueNoise, uv).rgb;
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


			float4 frag (v2f i) : SV_Target
			{
				float3 worldNormal = normalize(i.worldNormal);
				float3 sphereNormal = normalize(i.worldPos);
				float3 dirToSun = _WorldSpaceLightPos0.xyz;
	
				float3 viewDir = normalize(i.worldPos - _WorldSpaceCameraPos.xyz);
				float specularHighlight = calculateSpecular(worldNormal, viewDir, dirToSun, _Specular);
				
				float shadows = LIGHT_ATTENUATION(i);
				float sphereShadow = smoothstep(-0.01,0.01, dot(dirToSun, sphereNormal));
				shadows *= sphereShadow;

				shadows = lerp(0.6,1,shadows);
				float fresnel = saturate(1.5 * pow(1 + dot(viewDir, worldNormal), _FresnelPow));
				

				float3 shading = saturate(saturate(dot(worldNormal, dirToSun)) + 0.5);
				shading = pow(shading, _ShadingPow) * _Brightness * shadows + _Ambient.rgb + specularHighlight;
				float3 col = (_Color.rgb + fresnel * _FresnelCol) * shading;

				col = blueNoiseDither(col, i.screenPos.xy/i.screenPos.w, _DitherStrength);
				//return float4(i.screenPos.xy/i.screenPos.w,0,0);
			
				
				return float4(col, 1);
			}
			ENDCG
		}
	}
	Fallback "VertexLit"
}
