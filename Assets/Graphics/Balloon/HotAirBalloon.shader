Shader "Custom/HotAirBalloon"
{
	Properties
	{
		_Tint("Tint", Color) = (1,1,1,1)
		_MainTex ("Texture", 2D) = "white" {}
		_AO ("AO", 2D) = "white" {}
		_ShadingPow("Shading Power", Float) = 1
		_Brightness("Brightness", Float) = 1
		_Specular("Specular", Float) = 0
		_FresnelCol("Fresnel Col", Color) = (0,0,0,0)
		_Fade("Fade Test", Float) = 1

		_FireRadius("Fire Radius", Float) = 1
		_FireStr("Fire Str", Float) = 1
		_FireCol("Fire Col", Color) = (0,0,0,0)
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

			float4 _Tint;
			sampler2D _MainTex, _AO;

			float _ShadingPow, _Brightness, _Specular;
			float4 _FresnelCol;
			float _Fade;

			float3 firePos;
			float _FireRadius, _FireStr;
			float4 _FireCol;

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
				// Threshold values for each 4x4 block of pixels
				const float4x4 thresholdMatrix =
				{
					1, 9, 3, 11,
					13, 5, 15, 7,
					4, 12, 2, 10,
					16, 8, 14, 6
				};
				// Multiply screen pos by (width, height) of screen to get pixel coord
				float2 pixelPos = i.screenPos.xy / i.screenPos.w * _ScreenParams.xy;

				// Get threshold of current pixel and divide by 17 to get in range (0, 1)
				float threshold = thresholdMatrix[pixelPos.x % 4][pixelPos.y % 4] / 17;
				float fade = _Fade;
				// Don't draw pixel if threshold is greater than the alpha
				// (the clip function discards the current pixel if the value is less than 0)
				clip(fade - threshold);

				float3 worldNormal = normalize(i.worldNormal);
				float3 dirToSun = _WorldSpaceLightPos0.xyz;
	
				float3 viewDir = normalize(i.worldPos - _WorldSpaceCameraPos.xyz);
				float specularHighlight = calculateSpecular(worldNormal, viewDir, dirToSun, _Specular);
				
				float shadows = LIGHT_ATTENUATION(i);
				shadows = 1; // turning shadows off for now, getting lots of artifacts
				float ao = tex2D(_AO, i.uv);
				//return ao;
				

				float3 shading = saturate(saturate(dot(worldNormal, dirToSun)) + 0.15);
				shading = pow(shading, _ShadingPow) * _Brightness * shadows + specularHighlight * 0.2;
				float fireFalloff = 1-saturate(length(i.worldPos - firePos) / _FireRadius);
				float3 fire = fireFalloff * fireFalloff * _FireStr * _FireCol;
				float3 col = tex2D(_MainTex, float2(i.uv.x, i.uv.y)).rgb * _Tint.rgb;//
				float fresnel = saturate(1.5 * pow(1 + dot(viewDir, worldNormal), 5));
				//return float4(worldNormal * 0.5 + 0.5,1);
				//return fresnel;
				return float4(col * shading * _LightColor0.rgb * ao + fresnel * _FresnelCol.rgb + fire, 1);
			}
			ENDCG
		}
	 	// Pass to render object as a shadow caster
		// Note: not using VertexLit fallback as this still shows 
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

			struct appdata
			{
				float4 vertex : POSITION;
				float3 normal : NORMAL;
			};

			struct v2f {
				float4 screenPos : TEXCOORD2;
				V2F_SHADOW_CASTER;
				UNITY_VERTEX_OUTPUT_STEREO
			};

			v2f vert( appdata v )
			{
				v2f o;
				float4 pos = UnityObjectToClipPos(v.vertex);
		
				o.screenPos = ComputeScreenPos(pos);

				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
				return o;
			}

			float _Fade;

			float4 frag( v2f i ) : SV_Target
			{
				// Threshold values for each 4x4 block of pixels
				const float4x4 thresholdMatrix =
				{
					1, 9, 3, 11,
					13, 5, 15, 7,
					4, 12, 2, 10,
					16, 8, 14, 6
				};
				// Multiply screen pos by (width, height) of screen to get pixel coord
				float2 pixelPos = i.screenPos.xy / i.screenPos.w * _ScreenParams.xy;

				// Get threshold of current pixel and divide by 17 to get in range (0, 1)
				float threshold = thresholdMatrix[pixelPos.x % 4][pixelPos.y % 4] / 17;
				float fade = _Fade;
				// Don't draw pixel if threshold is greater than the alpha
				// (the clip function discards the current pixel if the value is less than 0)
				clip(fade - threshold);

				SHADOW_CASTER_FRAGMENT(i)
			}
			ENDCG
		}
	}
}
