Shader "Unlit/Point"
{
	Properties {
		_MainTex ("Main Tex", 2D) = "white" {}
	}
	SubShader
	{
		Tags { "RenderType"="Transparent" "Queue"="Transparent" }
		LOD 100
		Blend SrcAlpha OneMinusSrcAlpha
		ZWrite Off

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
					float4 col : COLOR;
			};

			struct v2f
			{
					float2 uv : TEXCOORD0;
					float4 vertex : SV_POSITION;
					float4 col : COLOR;
			};

			sampler2D _MainTex;

			v2f vert (appdata v)
			{
					v2f o;
					o.vertex = UnityObjectToClipPos(v.vertex);
					o.uv = v.uv;
					o.col = v.col;
					return o;
			}

			float4 frag (v2f i) : SV_Target
			{
					float2 centreOffset = (i.uv.xy - 0.5) * 2;
					float sqrDst = dot(centreOffset, centreOffset);
					float delta = fwidth(sqrt(sqrDst));
					float alpha = 1 - smoothstep(1 - delta, 1 + delta, sqrDst);

					return i.col * float4(1,1,1,alpha);
			}
			ENDCG
		}
	}
}