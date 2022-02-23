Shader "Hidden/NoisePreview"
{
	SubShader
	{
		Tags
		{ 
			"RenderType"="Opaque"
			"PreviewType"="Plane"
		}
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

			Texture3D NoiseTex;
			SamplerState samplerNoiseTex;

			float depthSlice;
			float tiling;
			float4 channelMask;
			int mipLevel;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}

			float4 frag (v2f i) : SV_Target
			{
				// sample the texture
				float4 data = NoiseTex.SampleLevel(samplerNoiseTex, float3(i.uv * tiling, depthSlice), mipLevel);

				float display = dot(data * channelMask, 1);

				return display;
			}
			ENDCG
		}
	}
}
