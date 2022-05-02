Shader "Custom/SimpleShaded"
{
	Properties
	{
		_Color("Tint", Color) = (1,1,1,1)
		_MainTex ("Texture", 2D) = "white" {}
		_ShadingPow("Shading Power", Float) = 1
		_Brightness("Brightness", Float) = 1
		_Specular("Specular", Float) = 0
		_Ambient("Ambient", Color) = (0,0,0,0)
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

			float4 _Color;
			sampler2D _MainTex;

			float _ShadingPow, _Brightness, _Specular;
			float4 _Ambient;

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
				float3 worldNormal = normalize(i.worldNormal);
				float3 dirToSun = _WorldSpaceLightPos0.xyz;
	
				float3 viewDir = normalize(i.worldPos - _WorldSpaceCameraPos.xyz);
				float specularHighlight = calculateSpecular(worldNormal, viewDir, dirToSun, _Specular);
				
				float shadows = LIGHT_ATTENUATION(i);

				float3 shading = saturate(saturate(dot(worldNormal, dirToSun)) + 0.25);
				shading = pow(shading, _ShadingPow) * _Brightness * shadows + _Ambient.rgb + specularHighlight;
				float3 col = tex2D(_MainTex, i.uv).rgb * _Color;
				return float4(col * shading, 1);
			}
			ENDCG
		}
	}
	Fallback "VertexLit"
}
