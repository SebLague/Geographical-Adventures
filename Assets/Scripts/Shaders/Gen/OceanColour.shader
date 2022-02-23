Shader "Unlit/OceanColour"
{
	Properties
	{
		[NoScaleOffset] _Bathymetry ("Bathymetry", 2D) = "white" {}
		[NoScaleOffset] _BathyShallow ("Bathy Shallow", 2D) = "white" {}
		[NoScaleOffset] _Chloro ("Chloro", 2D) = "white" {}
		_ShallowBlueA ("Shallow Blue A", Color) = (1,1,1,1)
		_ShallowBlueB ("Shallow Blue B", Color) = (1,1,1,1)
		_DeepBlue ("Deep Blue", Color) = (1,1,1,1)
		_ChloroWeak ("Chloro Weak", Color) = (1,1,1,1)
		_ChloroStrong ("Chloro Strong", Color) = (1,1,1,1)
		_OceanTestParams("Ocean Test Params", Vector) = (0,0,0,0)
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

			sampler2D _Bathymetry;
			sampler2D _BathyShallow;
			sampler2D _Chloro;
			float4 _ShallowBlueA;
			float4 _ShallowBlueB;
			float4 _DeepBlue;
			float4 _ChloroWeak;
			float4 _ChloroStrong;
			float4 _OceanTestParams;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}



			float4 frag (v2f i) : SV_Target
			{
				float2 uv = i.uv;
				float height = tex2D(_Bathymetry, uv).r;
				float chloro = tex2D(_Chloro, uv).r;
				
				float shallowHeight = tex2D(_BathyShallow, uv).r;
				float3 shallowCol = lerp(_ShallowBlueB, _ShallowBlueA, shallowHeight);
				float3 col = lerp(_DeepBlue, shallowCol, chloro * 0.4 + height);
				//return float4(col, 1);
				float chloroColStrength = pow(chloro, _OceanTestParams.x);
				//return chloroColStrength;
				float3 chloroCol = lerp(_ChloroWeak, _ChloroStrong, chloroColStrength);
				col = lerp(col, chloroCol, pow(chloro, _OceanTestParams.y));

				return float4(col, 1);
			}
			ENDCG
		}
	}
}
