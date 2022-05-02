Shader "Unlit/Parachute"
{
	Properties
	{
		_ColourA ("Colour A", Color) = (1,1,1,1)
		_ColourB ("Colour B", Color) = (1,1,1,1)
		_Specular("Specular", Float) = 0
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
				LIGHTING_COORDS(4,5)
			};

			float4 _ColourA, _ColourB;
			float _Specular;

			v2f vert (appdata v)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				o.worldNormal = mul(unity_ObjectToWorld, float4(v.normal.xyz, 0));
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
				float3 worldNormal = normalize(i.worldNormal);
				//return worldNormal.rgbb;
				float3 viewDir = normalize(i.worldPos - _WorldSpaceLightPos0.xyz);
				float3 dirToSun = _WorldSpaceLightPos0.xyz;
				float specularHighlight = calculateSpecular(worldNormal, viewDir, dirToSun, _Specular);
				float shading = saturate(saturate(dot(worldNormal, dirToSun)) + 0.1);
				shading = pow(shading, 0.8) * 1.15;
				
				float shadows = LIGHT_ATTENUATION(i);

				float2 uv = i.uv;
				int checker = (sign(uv.x - 0.5) * sign(uv.y - 0.5)) == 1;
				float3 col = lerp(_ColourA, _ColourB, checker);

				col = col * shading + specularHighlight;
				//col *= lerp(0.5, 1, shadows);

				return float4(col, 1);
			}
			ENDCG
		}
	}
	Fallback "VertexLit"
}
