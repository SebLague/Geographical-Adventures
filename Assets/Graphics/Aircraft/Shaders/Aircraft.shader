Shader "Unlit/Aircraft"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
		_Colour("Colour", Color) = (1,1,1,1)
		_Specular("Specular", Float) = 0
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"
			//#include "UnityLightingCommon.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float3 normal : NORMAL;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
				float3 worldNormal : NORMAL;
				float3 worldPos : TEXCOORD1;
			};

			float4 _Colour;
			float _Specular;
			sampler2D _MainTex;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				o.worldNormal = mul(unity_ObjectToWorld, float4(v.normal.xyz, 0));
				o.worldPos =  mul(unity_ObjectToWorld, float4(v.vertex.xyz, 1));
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
				float3 viewDir = normalize(i.worldPos - _WorldSpaceCameraPos.xyz);
				float3 dirToSun = _WorldSpaceLightPos0.xyz;
				float specularHighlight = calculateSpecular(worldNormal, viewDir, dirToSun, _Specular);
				float shading = saturate(saturate(dot(worldNormal, dirToSun)) + 0.25);
				shading = pow(shading, 0.8) * 1.25;

				float4 col = tex2D(_MainTex, i.uv) * _Colour * shading + specularHighlight;
				return col;
			}
			ENDCG
		}
	}
	Fallback "VertexLit"
}
