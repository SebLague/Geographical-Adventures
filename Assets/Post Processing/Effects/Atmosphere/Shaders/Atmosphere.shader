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
			#include "../Shader Common/AtmosphereCommon.hlsl"
			#include "../Shader Common/DrawAtmosphereCommon.hlsl"

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
			float4 _MainTex_TexelSize;
			sampler2D _CameraDepthTexture;

			sampler3D AerialPerspectiveLUT;
			sampler3D TransmittanceLUT3D;

			float4 params;
			float aerialPerspectiveStrength;

			// Remap a value from the range [minOld, maxOld] to [0, 1]
			float remap01(float minOld, float maxOld, float val) {
				return saturate((val - minOld) / (maxOld - minOld));
			}

			float3 getAtmoCol(float2 uv, float3 originalCol, float viewLength, float3 viewDir) {
				float3 outputCol = originalCol;
				float nonlin_depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv);
				float sceneDepth = LinearEyeDepth(nonlin_depth) * viewLength;

				float nearClipPlane = _ProjectionParams.y;
				float farClipPlane = _ProjectionParams.z;
			
				float depthT = remap01(nearClipPlane, terrestrialClipDst, sceneDepth);

				float3 rayOrigin = _WorldSpaceCameraPos;
				float3 rayDir = viewDir;


				float2 hitInfo = raySphere(0, atmosphereRadius, rayOrigin, rayDir);
				float dstToAtmosphere = hitInfo.x;
				float dstThroughAtmosphere = hitInfo.y;
			
				if (sceneDepth >= farClipPlane) {
					// Sky
				}
				// View ray goes through atmosphere (and not blocked by anything in front of it)
				else if (dstThroughAtmosphere > 0 && dstToAtmosphere < sceneDepth) {
					float3 inPoint = rayOrigin + rayDir * (dstToAtmosphere);
					float3 outPoint = rayOrigin + rayDir * min(dstToAtmosphere + dstThroughAtmosphere, sceneDepth);

					float3 transmittance = tex3Dlod(TransmittanceLUT3D, float4(uv, depthT, 0)).rgb;
					float3 luminance = tex3Dlod(AerialPerspectiveLUT, float4(uv,depthT, 0)).rgb;
					
					luminance = toneMap(luminance);
					luminance = originalCol.rgb * transmittance + luminance;

					outputCol = blueNoiseDither(luminance, uv, ditherStrength);
					outputCol = lerp(originalCol.rgb, outputCol, aerialPerspectiveStrength);
					
				}

				return outputCol;
			}

			float4 frag (v2f i) : SV_Target
			{	
				float4 originalCol = tex2D(_MainTex, i.uv);

				float viewLength = length(i.viewVector);
				float3 viewDir = i.viewVector / viewLength;
			
				float3 c = getAtmoCol(i.uv, originalCol, viewLength, viewDir);

				return float4(c, 1);
			}
			ENDCG
		}
	}
}