Shader "Unlit/Projection"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_AngleHorizontal ("Angle Horizontal", Float) = 0
		_AngleVertical ("Angle Vertical", Float) = 0
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
			#include "Assets/Scripts/Shader Common/GeoMath.hlsl"

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

			sampler2D _MainTex;
			float _AngleHorizontal, _AngleVertical;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}

			float3 rotateAroundAxis(float3 vec, float3 axis, float angle)
			{
				float cosAngle = cos(angle);
				float sinAngle = sin(angle);
				return vec * cosAngle + cross(axis, vec) * sinAngle + axis * dot(axis, vec) * (1 - cosAngle);
			}

			float4 frag (v2f i) : SV_Target
			{
				float2 longLat = (i.uv - 0.5) * float2(2, 1) * PI;
				float3 spherePos = longitudeLatitudeToPoint(longLat);
				//spherePos = rotateAroundAxis(spherePos, normalize(_Axis), _Angle * PI / 180);
				spherePos = rotateAroundAxis(spherePos, float3(1,0,0), _AngleVertical * PI / 180);
				spherePos = rotateAroundAxis(spherePos, float3(0,1,0), _AngleHorizontal * PI / 180);


				float2 uv = pointToUV(spherePos);

				float4 col = tex2Dlod(_MainTex, float4(uv.xy, 0, 0));
				return col;
			}
			ENDCG
		}
	}
}
