using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

[CreateAssetMenu(menuName = "Clouds")]
public class CloudsEffect : PostProcessingEffect
{

	public enum CloudResolution { Full = 1, Half = 2, Quater = 4, Eighth = 8 }
	public CloudResolution resolution = CloudResolution.Quater;

	public bool autoUpdateNoise;

	public ComputeShader noiseCompute;

	public float cloudMinAltitude;
	[Min(0.1f)]
	public float cloudLayerThickness;

	public float noiseScale;
	public float densityMultiplier;
	public Vector4 testParams;

	public Shader decodeDepthShader;
	Material decodeDepthMat;

	[Header("Atmosphere")]
	public AtmosphereEffect atmosphereEffect;

	[Header("Light Cone")]
	public int lightConeSeed;
	public float lightConeSpread;
	public float lightConeStepSize;
	public bool drawLightConeGizmos;

	[Header("Noise Settings")]
	public uint seed;
	public int noiseTextureSize = 32;
	public int worleyScale = 5;

	[Header("Noise Preview")]
	public bool showNoisePreview;
	public PreviewChannel previewChannel;
	[Range(0, 1)]
	public float noisePreviewSliceZ;
	public float previewTiling;
	public int previewMipLevel;

	public enum PreviewChannel { R, G, B, A }

	RenderTexture noiseTexture;
	bool settingsUpToDate;

	public override void OnEnable()
	{
		base.OnEnable();
		settingsUpToDate = false;
	}

	void CreateNoiseTexture()
	{
		GraphicsFormat format = GraphicsFormat.R16G16B16A16_SFloat;
		ComputeHelper.CreateRenderTexture3D(ref noiseTexture, noiseTextureSize, format, mipmaps: true);

		noiseCompute.SetTexture(0, "NoiseTexture", noiseTexture);
		noiseCompute.SetInt("size", noiseTextureSize);
		noiseCompute.SetInt("worleyScale", worleyScale);
		noiseCompute.SetInt("seed", (int)seed);
		ComputeHelper.Dispatch(noiseCompute, noiseTexture, kernelIndex: 0);

		noiseTexture.GenerateMips();
	}

	public void ApplyPreviewMat(Material preview)
	{
		preview.SetTexture("NoiseTex", noiseTexture);
		preview.SetFloat("depthSlice", noisePreviewSliceZ);
		preview.SetFloat("tiling", previewTiling);
		preview.SetVector("channelMask", ChannelMask);//
		preview.SetInt("mipLevel", previewMipLevel);
	}

	public Vector4 ChannelMask
	{
		get
		{
			Vector4 channelWeight = new Vector4(
				(previewChannel == PreviewChannel.R) ? 1 : 0,
				(previewChannel == PreviewChannel.G) ? 1 : 0,
				(previewChannel == PreviewChannel.B) ? 1 : 0,
				(previewChannel == PreviewChannel.A) ? 1 : 0
			);
			return channelWeight;
		}
	}


	void SetProperties()
	{

		Vector3 dirToSun = -FindObjectOfType<Light>().transform.forward;
		material.SetVector("dirToSun", dirToSun);

		// Constant properties
		if (!settingsUpToDate || !Application.isPlaying)
		{
			CreateNoiseTexture();
			CreateMaterial(ref decodeDepthMat, decodeDepthShader);

			material.SetTexture("NoiseTex", noiseTexture);
			material.SetFloat("densityMultiplier", densityMultiplier);
			material.SetFloat("noiseScale", noiseScale);
			material.SetVector("testParams", testParams);
			material.SetFloat("cloudRadiusMin", atmosphereEffect.bodyRadius + cloudMinAltitude);
			material.SetFloat("cloudRadiusMax", atmosphereEffect.bodyRadius + cloudMinAltitude + cloudLayerThickness);

			Vector4[] lightConePoints = CreateLightCone(dirToSun);
			material.SetVectorArray("lightConePoints", lightConePoints);


			SetAtmosphereProperties();
			settingsUpToDate = true;
		}
	}

	void SetAtmosphereProperties()
	{
		material.SetTexture("AerialPerspectiveTransmittance", atmosphereEffect.aerialPerspectiveTransmittance);
		material.SetTexture("AerialPerspectiveLuminance", atmosphereEffect.aerialPerspectiveLuminance);
		material.SetTexture("atmosphereTransmittanceLUT", atmosphereEffect.transmittanceLUT);
		material.SetFloat("atmosphereThickness", atmosphereEffect.atmosphereThickness);
		material.SetFloat("planetRadius", atmosphereEffect.bodyRadius);

		// Get notified when atmosphere properties are changed
		atmosphereEffect.onSettingsUpdated -= SetAtmosphereProperties;
		atmosphereEffect.onSettingsUpdated += SetAtmosphereProperties;
	}

	protected override void RenderEffectToTarget(RenderTexture source, RenderTexture target)
	{
		SetProperties();

		Vector2Int cloudRes = new Vector2Int(source.width / (int)resolution, source.height / (int)resolution);

		RenderTexture decodedDepthTextureSmall = RenderTexture.GetTemporary(cloudRes.x, cloudRes.y, 0, GraphicsFormat.R16_SFloat);
		Graphics.Blit(null, decodedDepthTextureSmall, decodeDepthMat);
		material.SetTexture(CloudParamShaderID.depthTextureSmall, decodedDepthTextureSmall);

		RenderTexture cloudTex = RenderTexture.GetTemporary(cloudRes.x, cloudRes.y, 0, source.format);

		const int lowResCloudPass = 0;
		const int compositeCloudPass = 1;
		material.SetTexture(CloudParamShaderID.cloudTexture, cloudTex);

		Graphics.Blit(null, cloudTex, material, lowResCloudPass);
		Graphics.Blit(source, target, material, compositeCloudPass);

		// Release temp render textures
		RenderTexture.ReleaseTemporary(cloudTex);
		RenderTexture.ReleaseTemporary(decodedDepthTextureSmall);
	}

	public class CloudParamShaderID
	{
		public static int cloudTexture = Shader.PropertyToID("_CloudTex");
		public static int depthTextureSmall = Shader.PropertyToID("DepthTextureSmall");
	}

	Vector4[] CreateLightCone(Vector3 dirToSun)
	{
		const int numLightSamples = 6; // if changing this number, make sure to update array size in shader as well
		Vector4[] points = new Vector4[numLightSamples];

		System.Random prng = new System.Random(lightConeSeed);

		Vector3 axisA = Vector3.zero;
		Vector3 axisB = Vector3.zero;
		Vector3.OrthoNormalize(ref dirToSun, ref axisA, ref axisB);
		Vector3 prevPoint = Vector3.zero;

		for (int i = 0; i < numLightSamples; i++)
		{
			float randomAngle = (float)(prng.NextDouble()) * 2 * Mathf.PI;
			Vector3 offset = new Vector3(Mathf.Cos(randomAngle), 0, Mathf.Sin(randomAngle));
			Vector3 conePoint = (Vector3.up * lightConeStepSize + offset * lightConeSpread) * (i + 1);
			// Rotate cone point to align with dirToSun
			conePoint = Quaternion.FromToRotation(Vector3.up, dirToSun) * conePoint;

			float stepSize = (prevPoint - conePoint).magnitude;
			prevPoint = conePoint;
			points[i] = new Vector4(conePoint.x, conePoint.y, conePoint.z, stepSize);
		}

		return points;
	}

	public override void DrawGizmos()
	{
		if (drawLightConeGizmos)
		{
			Vector4[] points = CreateLightCone(Vector3.up);
			foreach (var p in points)
			{
				Gizmos.DrawSphere(p, 0.1f);
			}
		}
	}

	void OnValidate()
	{
		settingsUpToDate = false;
	}


	public override void OnDestroy()
	{
		base.OnDestroy();
		ComputeHelper.Release(noiseTexture);
	}
}
