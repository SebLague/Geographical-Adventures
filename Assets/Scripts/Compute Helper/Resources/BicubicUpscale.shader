Shader "Hidden/BicubicUpscale"
{
	Properties
	{
		_MainTex ("tex2D", 2D) = "white" {}
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

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}

			sampler2D _MainTex;
			float2 textureSize;

			// Thanks to https://www.shadertoy.com/view/MllSzX
			float3 CubicHermite (float3 A, float3 B, float3 C, float3 D, float t)
			{
				float t2 = t*t;
				float t3 = t*t*t;
				float3 a = -A/2.0 + (3.0*B)/2.0 - (3.0*C)/2.0 + D/2.0;
				float3 b = A - (5.0*B)/2.0 + 2.0*C - D / 2.0;
				float3 c = -A/2.0 + C/2.0;
				float3 d = B;
				
				return a*t3 + b*t2 + c*t + d;
			}

			float3 BicubicHermitetex2DSample (float2 uv)
			{
				float2 d = 1.0 / textureSize;
		
				float2 pixel = uv * textureSize + 0.5;
				
				float2 frac = pixel - floor(pixel);
				pixel = floor(pixel) / textureSize - d/2.0;
				
				float3 C00 = tex2D(_MainTex, pixel + float2(-d.x, -d.y)).rgb;
				float3 C10 = tex2D(_MainTex, pixel + float2(0, -d.y)).rgb;
				float3 C20 = tex2D(_MainTex, pixel + float2(d.x, -d.y)).rgb;
				float3 C30 = tex2D(_MainTex, pixel + float2(d.x * 2, -d.y)).rgb;
				
				float3 C01 = tex2D(_MainTex, pixel + float2(-d.x, 0)).rgb;
				float3 C11 = tex2D(_MainTex, pixel + float2(0, 0)).rgb;
				float3 C21 = tex2D(_MainTex, pixel + float2(d.x, 0)).rgb;
				float3 C31 = tex2D(_MainTex, pixel + float2(d.x * 2, 0.0)).rgb;
				
				float3 C02 = tex2D(_MainTex, pixel + float2(-d.x, d.y)).rgb;
				float3 C12 = tex2D(_MainTex, pixel + float2(0, d.y)).rgb;
				float3 C22 = tex2D(_MainTex, pixel + float2(d.x, d.y)).rgb;
				float3 C32 = tex2D(_MainTex, pixel + float2(d.x * 2, d.y)).rgb;
				
				float3 C03 = tex2D(_MainTex, pixel + float2(-d.x, d.y * 2)).rgb;
				float3 C13 = tex2D(_MainTex, pixel + float2(0, d.y * 2)).rgb;
				float3 C23 = tex2D(_MainTex, pixel + float2(d.x, d.y * 2)).rgb;
				float3 C33 = tex2D(_MainTex, pixel + float2(d.x * 2, d.y * 2)).rgb;
				
				float3 CP0X = CubicHermite(C00, C10, C20, C30, frac.x);
				float3 CP1X = CubicHermite(C01, C11, C21, C31, frac.x);
				float3 CP2X = CubicHermite(C02, C12, C22, C32, frac.x);
				float3 CP3X = CubicHermite(C03, C13, C23, C33, frac.x);
				
				return CubicHermite(CP0X, CP1X, CP2X, CP3X, frac.y);
			}


			float4 frag (v2f i) : SV_Target
			{
				float3 col = BicubicHermitetex2DSample(i.uv);
				return float4(col, 1);
			}
			ENDCG
		}
	}
}
