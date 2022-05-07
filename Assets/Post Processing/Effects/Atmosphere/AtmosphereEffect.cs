using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

[CreateAssetMenu(menuName = "PostProcessing/Atmosphere")]
public class AtmosphereEffect : PostProcessingEffect
{

	public Shader drawSkyShader;

	public Color sunColTest;
	public Light light;

	public float bodyRadius;
	public float atmosphereThickness;

	public Shader atmosphereShader;
	public Vector4 testParams = new Vector4(7, 1.26f, 0.1f, 3);

	// Num raymarch steps when drawing aerial perspective (planet viewed through atmosphere)
	public int numAerialScatteringSteps = 20;


	[Header("Rayleigh Scattering")]
	// Wavelengths of red, green, and blue light (nanometres)
	public Vector3 wavelengthsRGB = new Vector3(700, 530, 460);
	// Scale value to adjust all wavelengths at once
	public float wavelengthScale = 300;
	// Altitude [0, 1] at which the average density of particles causing rayleigh scattering is found
	[Range(0, 1)] public float rayleighDensityAvg = 0.1f;

	[Header("Mie Scattering")]
	// Altitude [0, 1] at which the average density of particles causing mie scattering is found
	[Range(0, 1)] public float mieDensityAvg = 0.1f;
	// Strength of mie scattering
	public float mieCoefficient;
	// Strength of mie absorption
	public float mieAbsorption;

	[Header("Ozone")]
	//Altitude [0, 1] at which ozone density is at the greatest
	[Range(0, 1)] public float ozonePeakDensityAltitude = 0.25f;
	[Range(0, 10)] public float ozoneDensityFalloff = 4;
	[Range(0, 5)] public float ozoneStrength = 1;
	public Vector3 ozoneAbsorption;

	[Header("Sun Disc")]
	public float sunDiscSize;
	public float sunDiscBlurA;
	public float sunDiscBlurB;

	[Header("Transmittance LUT (2D)")]
	public ComputeShader transmittanceLUTCompute;
	public Vector2Int transmittanceLUTSize;

	[Header("Aerial Perpspective LUT")]
	public ComputeShader aerialPerspectiveLUTCompute;
	public int aerialPerspectiveLUTSize;
	// Allow control over how strongly atmosphere affects appearance of terrain
	[Range(0, 1)] public float aerialPerspectiveStrength = 1;


	[Header("Sky Texture")]
	// Num raymarch steps when drawing the sky (this is drawn small and upscaled, so can afford to be fairly high)
	public int numSkyScatteringSteps = 100;
	public ComputeShader skyRenderCompute;
	// Note: since sky colours change quite smoothly this can be very small (e.g. 128x64)
	// However, the vertical resolution should be increased (~128x256) so that earth shadow isn't too jaggedy
	public Vector2Int skyRenderSize;
	// Allow control over how strongly atmosphere affects appearance of objects in the sky (moon, stars)
	[Range(0, 1)] public float skyTransmittanceWeight = 1;

	[Header("Tone mapping")]
	public float intensity = 1;
	public float contrast = 1.45f;
	public float whitePoint = 1.1f;


	[Header("Later")]
	public float ditherStrength = 0.8f;
	public Texture2D blueNoise;
	public FilterMode filterMode;

	[Header("Debug")]
	public Vector3 rayleighCoefficients;

	[Header(("Debug"))]
	public RenderTexture transmittanceLUT;
	public RenderTexture aerialPerspectiveLuminance;
	public RenderTexture aerialPerspectiveTransmittance;//
	public RenderTexture sky;
	bool settingsUpToDate;

	ShaderValues sharedAtmosphereValues;
	public event System.Action onSettingsUpdated;
	CommandBuffer drawSkyCommand;
	Material drawSkyMaterial;
	bool lutUpdateRequired;

	public override void OnEnable()
	{
		base.OnEnable();
		settingsUpToDate = false;
		SetProperties();
		EditorOnlyInit();

		Camera.onPreCull -= RenderLUTs;
		Camera.onPreCull += RenderLUTs;
	}

	public void SetupSkyRenderingCommand(CommandBuffer skyRenderCommand)
	{
		lutUpdateRequired = true;
		drawSkyMaterial = new Material(drawSkyShader);

		int id = Shader.PropertyToID("_TempSkyRenderTexture");
		skyRenderCommand.GetTemporaryRT(id, -1, -1, 0, FilterMode.Bilinear);
		skyRenderCommand.Blit(BuiltinRenderTextureType.CameraTarget, id, drawSkyMaterial);
		skyRenderCommand.Blit(id, BuiltinRenderTextureType.CameraTarget);
		skyRenderCommand.ReleaseTemporaryRT(id);



		SetDrawSkyShaderParameters(drawSkyMaterial);
	}


	// Called on camera pre-cull
	void RenderLUTs(Camera activeCamera)
	{

		if (lutUpdateRequired)
		{
			if (activeCamera == cam)
			{
				lutUpdateRequired = false;
				RenderSky(activeCamera);
				RenderAerialPerspectiveLUTs(activeCamera);
			}
		}
	}

	void OnDisable()
	{
		// Todo: only clear own cmd buffer
		if (cam != null)
		{
			cam.RemoveAllCommandBuffers();
		}
		drawSkyCommand?.Release();
		Camera.onPreCull -= RenderLUTs;
	}

	protected override void RenderEffectToTarget(RenderTexture source, RenderTexture target)
	{
		SetProperties();
		lutUpdateRequired = true;
		Graphics.Blit(source, target, material);
	}

	public void SetProperties()
	{

		if (light == null)
		{
			GameObject sunObject = GameObject.FindGameObjectWithTag("Sun");
			light = sunObject?.GetComponent<Light>();
		}

		if (material != null && light != null)
		{
			material.SetVector(ShaderParamID.dirToSun, -light.transform.forward);
		}
		if (!settingsUpToDate || !Application.isPlaying)
		{
			sharedAtmosphereValues = GetShaderValues();
			sharedAtmosphereValues.Apply(material);
			sharedAtmosphereValues.Apply(transmittanceLUTCompute);
			sharedAtmosphereValues.Apply(aerialPerspectiveLUTCompute);
			sharedAtmosphereValues.Apply(skyRenderCompute);


			InitAndRenderTransmittanceLUT();
			InitAeiralPerspectiveLUTs();
			InitSkyLUT();

			// Set shader params after all LUTs have been initialized
			SetDrawAerialPerspectiveShaderParams(material);

			// Draw sky settings
			if (drawSkyMaterial != null)
			{
				SetDrawSkyShaderParameters(drawSkyMaterial);
			}

			// Done
			settingsUpToDate = true;
			onSettingsUpdated?.Invoke();
		}
	}

	ShaderValues GetShaderValues()
	{
		ShaderValues values = new ShaderValues();
		// Size values
		values.floats.Add(("atmosphereThickness", atmosphereThickness));
		values.floats.Add(("atmosphereRadius", bodyRadius + atmosphereThickness));
		values.floats.Add(("planetRadius", bodyRadius));
		values.floats.Add(("terrestrialClipDst", bodyRadius));

		// Rayleigh values
		values.floats.Add(("rayleighDensityAvg", rayleighDensityAvg));
		// Arbitrary scale to give nicer range of reasonable values for the scattering constant
		// Strength of (rayleigh) scattering is dependent on wavelength (~ 1/wavelength^4)
		Vector3 inverseWavelengths = new Vector3(1 / wavelengthsRGB.x, 1 / wavelengthsRGB.y, 1 / wavelengthsRGB.z);
		rayleighCoefficients = Pow(inverseWavelengths * wavelengthScale, 4);
		values.vectors.Add(("rayleighCoefficients", rayleighCoefficients));

		// Mie values
		values.floats.Add(("mieDensityAvg", mieDensityAvg));
		values.floats.Add(("mieCoefficient", mieCoefficient));
		values.floats.Add(("mieAbsorption", mieAbsorption));//

		// Ozone values
		values.floats.Add(("ozonePeakDensityAltitude", ozonePeakDensityAltitude));
		values.floats.Add(("ozoneDensityFalloff", ozoneDensityFalloff));
		values.vectors.Add(("ozoneAbsorption", ozoneAbsorption * ozoneStrength * 0.1f));

		return values;
	}

	void SetDrawSkyShaderParameters(Material drawSky)
	{
		// Textures
		drawSky.SetTexture("Sky", sky);
		drawSky.SetTexture("TransmittanceLUT", transmittanceLUT);
		drawSky.SetTexture("BlueNoise", blueNoise);

		// Values
		drawSky.SetFloat("atmosphereThickness", atmosphereThickness);
		drawSky.SetFloat("planetRadius", bodyRadius);
		drawSky.SetFloat("sunDiscSize", sunDiscSize);
		drawSky.SetFloat("sunDiscBlurA", sunDiscBlurA);
		drawSky.SetFloat("sunDiscBlurB", sunDiscBlurB);
		drawSky.SetFloat("ditherStrength", ditherStrength);
		drawSky.SetFloat("intensity", intensity);
		drawSky.SetFloat("contrast", contrast);
		drawSky.SetFloat("whitePoint", whitePoint);
		drawSky.SetFloat("skyTransmittanceWeight", skyTransmittanceWeight);
	}


	void SetDrawAerialPerspectiveShaderParams(Material drawAerial)
	{
		// Textures
		drawAerial.SetTexture("AerialPerspectiveLUT", aerialPerspectiveLuminance);
		drawAerial.SetTexture("TransmittanceLUT3D", aerialPerspectiveTransmittance);
		drawAerial.SetTexture("BlueNoise", blueNoise);

		// Values
		drawAerial.SetVector("params", testParams);
		drawAerial.SetFloat("ditherStrength", ditherStrength);
		drawAerial.SetFloat("aerialPerspectiveStrength", aerialPerspectiveStrength);

		drawAerial.SetFloat("intensity", intensity);
		drawAerial.SetFloat("contrast", contrast);
		drawAerial.SetFloat("whitePoint", whitePoint);
	}

	// Create lookup texture for the transmittance (proportion of light reaching given point through the atmosphere)
	// This only needs to be created once at the start (or whenever atmosphere parameters are changed)
	void InitAndRenderTransmittanceLUT()
	{
		GraphicsFormat transmittanceLUTFormat = GraphicsFormat.R16G16B16A16_UNorm;//
		ComputeHelper.CreateRenderTexture(ref transmittanceLUT, transmittanceLUTSize.x, transmittanceLUTSize.y, FilterMode.Bilinear, transmittanceLUTFormat, "Transmittance LUT");
		transmittanceLUTCompute.SetTexture(0, "TransmittanceLUT", transmittanceLUT);
		transmittanceLUTCompute.SetInt("width", transmittanceLUTSize.x);
		transmittanceLUTCompute.SetInt("height", transmittanceLUTSize.y);
		ComputeHelper.Dispatch(transmittanceLUTCompute, transmittanceLUT);
	}

	void InitAeiralPerspectiveLUTs()
	{
		GraphicsFormat aerialPerspectiveLUTFormat = GraphicsFormat.R16G16B16A16_SFloat;
		GraphicsFormat transmittance3DFormat = GraphicsFormat.R16G16B16A16_UNorm;
		ComputeHelper.CreateRenderTexture3D(ref aerialPerspectiveLuminance, aerialPerspectiveLUTSize, aerialPerspectiveLUTFormat, TextureWrapMode.Clamp, "Aerial Perspective");
		ComputeHelper.CreateRenderTexture3D(ref aerialPerspectiveTransmittance, aerialPerspectiveLUTSize, transmittance3DFormat, TextureWrapMode.Clamp, "Transmittance LUT 3D");

		// Assign textures
		aerialPerspectiveLUTCompute.SetTexture(0, "AerialPerspectiveLuminance", aerialPerspectiveLuminance);
		aerialPerspectiveLUTCompute.SetTexture(0, "AerialPerspectiveTransmittance", aerialPerspectiveTransmittance);
		aerialPerspectiveLUTCompute.SetTexture(0, "TransmittanceLUT", transmittanceLUT);

		// Assign constant values
		aerialPerspectiveLUTCompute.SetInt("size", aerialPerspectiveLUTSize);
		aerialPerspectiveLUTCompute.SetInt("numScatteringSteps", numAerialScatteringSteps);
	}

	void RenderAerialPerspectiveLUTs(Camera cam)
	{
		// Assign dynamic values
		SetRaymarchParams(cam, aerialPerspectiveLUTCompute);
		aerialPerspectiveLUTCompute.SetFloat(ShaderParamID.nearClip, cam.nearClipPlane);
		aerialPerspectiveLUTCompute.SetFloat(ShaderParamID.farClip, cam.farClipPlane);
		aerialPerspectiveLUTCompute.SetVector(ShaderParamID.dirToSun, -light.transform.forward);
		// Render
		ComputeHelper.Dispatch(aerialPerspectiveLUTCompute, aerialPerspectiveLuminance);
	}


	void InitSkyLUT()
	{
		GraphicsFormat skyFormat = GraphicsFormat.R16G16B16A16_SFloat;
		ComputeHelper.CreateRenderTexture(ref sky, skyRenderSize.x, skyRenderSize.y, FilterMode.Bilinear, skyFormat, "Sky", useMipMaps: true);

		skyRenderCompute.SetTexture(0, "TransmittanceLUT", transmittanceLUT);
		skyRenderCompute.SetTexture(0, "Sky", sky);
		skyRenderCompute.SetInt("numScatteringSteps", numSkyScatteringSteps);
		skyRenderCompute.SetInts("size", skyRenderSize.x, skyRenderSize.y);
	}

	// Render the sky to a small texture, which then will be upscaled to reduce expensive raymarching
	// This is rendered every frame
	void RenderSky(Camera cam)
	{
		SetRaymarchParams(cam, skyRenderCompute);
		skyRenderCompute.SetVector(ShaderParamID.dirToSun, -light.transform.forward);
		ComputeHelper.Dispatch(skyRenderCompute, sky);
	}

	void SetRaymarchParams(Camera cam, ComputeShader raymarchCompute)
	{
		Vector3 topLeftDir = CalculateViewDirection(cam, new Vector2(0, 1));
		Vector3 topRightDir = CalculateViewDirection(cam, new Vector2(1, 1));
		Vector3 bottomLeftDir = CalculateViewDirection(cam, new Vector2(0, 0));
		Vector3 bottomRightDir = CalculateViewDirection(cam, new Vector2(1, 0));

		raymarchCompute.SetVector(ShaderParamID.topLeftDir, topLeftDir);
		raymarchCompute.SetVector(ShaderParamID.topRightDir, topRightDir);
		raymarchCompute.SetVector(ShaderParamID.bottomLeftDir, bottomLeftDir);
		raymarchCompute.SetVector(ShaderParamID.bottomRightDir, bottomRightDir);
		raymarchCompute.SetVector(ShaderParamID.camPos, cam.transform.position);
	}

	public static class ShaderParamID
	{
		public static int topLeftDir = Shader.PropertyToID("topLeftDir");
		public static int topRightDir = Shader.PropertyToID("topRightDir");
		public static int bottomLeftDir = Shader.PropertyToID("bottomLeftDir");
		public static int bottomRightDir = Shader.PropertyToID("bottomRightDir");
		public static int camPos = Shader.PropertyToID("camPos");
		public static int nearClip = Shader.PropertyToID("nearClip");
		public static int farClip = Shader.PropertyToID("farClip");

		public static int dirToSun = Shader.PropertyToID("dirToSun");

	}

	Vector3 CalculateViewDirection(Camera camera, Vector2 texCoord)
	{
		Matrix4x4 camInverseMatrix = GL.GetGPUProjectionMatrix(camera.projectionMatrix, true).inverse;
		Matrix4x4 localToWorldMatrix = camera.transform.localToWorldMatrix;

		Vector3 viewVector = camInverseMatrix * new Vector4(texCoord.x * 2 - 1, texCoord.y * 2 - 1, 0, -1);
		viewVector = localToWorldMatrix * new Vector4(viewVector.x, viewVector.y, viewVector.z, 0);
		return viewVector.normalized;
	}

	public override void OnDestroy()
	{
		ComputeHelper.Release(aerialPerspectiveLuminance, sky, transmittanceLUT, aerialPerspectiveTransmittance);
	}



	void OnValidate()
	{
		if (Application.isEditor)
		{
			settingsUpToDate = false;
		}
	}

	Vector3 Pow(Vector3 vector, float power)
	{
		return new Vector3(Mathf.Pow(vector.x, power), Mathf.Pow(vector.y, power), Mathf.Pow(vector.z, power));
	}

	void EditorOnlyInit()
	{
#if UNITY_EDITOR
		EditorShaderHelper.onRebindRequired += () => settingsUpToDate = false;
#endif
	}//



	public class ShaderValues
	{
		public List<(string name, float value)> floats;
		public List<(string name, int value)> ints;
		public List<(string name, Vector4 value)> vectors;

		public ShaderValues()
		{
			floats = new List<(string name, float value)>();
			ints = new List<(string name, int value)>();
			vectors = new List<(string name, Vector4 value)>();
		}

		public void Apply(Material material)
		{
			foreach (var data in floats)
			{
				material.SetFloat(data.name, data.value);
			}

			foreach (var data in ints)
			{
				material.SetInt(data.name, data.value);
			}

			foreach (var data in vectors)
			{
				material.SetVector(data.name, data.value);
			}
		}

		public void Apply(ComputeShader compute)
		{
			foreach (var data in floats)
			{
				compute.SetFloat(data.name, data.value);
			}

			foreach (var data in ints)
			{
				compute.SetInt(data.name, data.value);
			}

			foreach (var data in vectors)
			{
				compute.SetVector(data.name, data.value);
			}
		}


	}
}