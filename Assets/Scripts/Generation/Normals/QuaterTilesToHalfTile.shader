Shader "Unlit/QuaterTilesToHalfTile"
{
	Properties
	{
		_TileA ("A", 2D) = "white" {}
		_TileB ("B", 2D) = "white" {}
		_TileC ("C", 2D) = "white" {}
		_TileD ("D", 2D) = "white" {}
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

			sampler2D _TileA;
			sampler2D _TileB;
			sampler2D _TileC;
			sampler2D _TileD;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}

			float4 frag (v2f i) : SV_Target
			{
				float a = tex2D(_TileA, i.uv * 2 - float2(0, 1)).r;
				float b = tex2D(_TileB, i.uv * 2 - float2(1, 1)).r;
				float c = tex2D(_TileC, i.uv * 2).r;
				float d = tex2D(_TileD, i.uv * 2 - float2(1,0)).r;

				float n = (i.uv.x < 0.5)?a:b;
				float s = (i.uv.x < 0.5)?c:d;
				float full = (i.uv.y < 0.5) ? s : n;

				return full;
			}
			ENDCG
		}
	}
}