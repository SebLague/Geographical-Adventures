Shader "Unlit/DashedLine"
{
	Properties
	{
		_Color ("Colour", Color) = (1,1,1,1)
		_DashCount("Dash Count", Float) = 10
		_DashSmoothing("Dash Smoothing", Range(0,1)) = 0.1
	}
	SubShader
	{
		Tags { "Queue"="Transparent" }
		//ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha

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

			float4 _Color;
			float _DashCount;
			float _DashSmoothing;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}

			float4 frag (v2f i) : SV_Target
			{
				float t = i.uv.x;
				float dashT = (t * _DashCount) % 1;
				float centredDashT = abs(dashT-0.5)*2;
				float spacing = 0.3;
			
				
				float alpha = smoothstep(spacing - _DashSmoothing * 0.1, spacing + _DashSmoothing * 0.1, centredDashT);
				//return float4(alpha.xxx,1);
				//float alpha = smoothstep(0.25 + _DashSmoothing, 0.75 + _DashSmoothing, dashT);

				return float4(_Color.rgb, _Color.a * alpha);
			}
			ENDCG
		}
	}
}
