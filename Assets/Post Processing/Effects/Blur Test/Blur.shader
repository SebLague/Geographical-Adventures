// Thanks to Loadus: https://www.shadertoy.com/view/Mtl3Rj
Shader "Hidden/Blur"
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

			float SCurve(float x) {
				x = x * 2.0 - 1.0;
				return -x * abs(x) * 0.5 + x + 0.5;
			}

			float4 BlurV (sampler2D source, float2 size, float2 uv, float radius) {
				
				float4 col = tex2D(source, uv);

				if (radius >= 1.0)
				{
					float4 A = 0; 
					float4 C = 0; 

					float height = 1.0 / size.y;

					float divisor = 0.0; 
					float weight = 0.0;
					
					float radiusMultiplier = 1.0 / radius;

					for (float y = -radius; y <= radius; y++)
					{
						A = tex2D(source, uv + float2(0.0, y * height));
						weight = SCurve(1.0 - (abs(y) * radiusMultiplier)); 
						C += A * weight; 
						divisor += weight; 
					}

					col = float4(C.r / divisor, C.g / divisor, C.b / divisor, 1.0);
				}

				return col;
			}
			float blurRadius;

			fixed4 frag (v2f i) : SV_Target
			{
				return BlurV(_MainTex, _ScreenParams.xy, i.uv, blurRadius);
			}
			ENDCG
		}
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

			float SCurve(float x) {
				x = x * 2.0 - 1.0;
				return -x * abs(x) * 0.5 + x + 0.5;
			}

			float4 BlurH (sampler2D source, float2 size, float2 uv, float radius) {
				float4 col = tex2D(source, uv);

				if (radius > 1.0)
				{
					float4 A = 0; 
					float4 C = 0; 

					float width = 1.0 / size.x;

					float divisor = 0.0; 
					float weight = 0.0;
					
					float radiusMultiplier = 1.0 / radius;
					
					for (float x = -radius; x <= radius; x++)
					{
						A = tex2D(source, uv + float2(x * width, 0.0));
						weight = SCurve(1.0 - (abs(x) * radiusMultiplier)); 
						C += A * weight; 
						divisor += weight; 
					}

					col = float4(C.r / divisor, C.g / divisor, C.b / divisor, 1.0);
				}

				return col;
			}

			float blurRadius;

			fixed4 frag (v2f i) : SV_Target
			{
				float4 b= BlurH(_MainTex, _ScreenParams.xy, i.uv, blurRadius);
				return b;
			}
			ENDCG
		}
	}
}
