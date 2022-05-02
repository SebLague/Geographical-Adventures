Shader "Unlit/OutlineTest"
{
	Properties
	{
		_NightCol("Night Col", Color) = (1,1,1,1)
		_DayCol("Day Col", Color) = (1,1,1,1)
		
		_DayThreshold("Day Threshold", Range(-1, 1)) = 0
		_DayBlend("Day/Night Blend", Float) = 0
		
	}
	SubShader
	{
		Tags {"RenderType"="Opaque" "Queue"="Geometry+10" }

		Offset -1, -1 // In a Z fight with the terrain, the outline should win
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
			};

			struct v2f
			{
				float4 pos : SV_POSITION;
				float3 worldPos : TEXCOORD0;
			};

			v2f vert (appdata v)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.worldPos = mul(unity_ObjectToWorld, v.vertex);
				return o;
			}

			float4 _DayCol, _NightCol;
			float _DayThreshold, _DayBlend;

			float4 frag (v2f i) : SV_Target
			{
				float3 dirToSun = _WorldSpaceLightPos0;
				float3 normal = normalize(i.worldPos);
				float time = dot(dirToSun, normal); // -1 = midnight, 1 = midday
				float t = smoothstep(_DayThreshold - _DayBlend * 0.01, _DayThreshold + _DayBlend * 0.01, time);

				return lerp(_NightCol, _DayCol, t);
			}
			ENDCG
		}
	}
}
