using UnityEngine;
using UnityEngine.Experimental.Rendering;
using ComputeShaderUtility;
using System.Collections.Generic;

// This class contains some helper functions to make life a little easier working with compute shaders
// (Very work-in-progress!)
public enum DepthMode { None = 0, Depth16 = 16, Depth24 = 24 }

public static class ComputeHelper
{

	public const FilterMode defaultFilterMode = FilterMode.Bilinear;
	public const GraphicsFormat defaultGraphicsFormat = GraphicsFormat.R32G32B32A32_SFloat;

	static ComputeShader clearTextureCompute;
	static ComputeShader swizzleTextureCompute;
	static ComputeShader copy3DCompute;
	static Shader bicubicUpscale;

	/// Convenience method for dispatching a compute shader.
	/// It calculates the number of thread groups based on the number of iterations needed.
	public static void Dispatch(ComputeShader cs, int numIterationsX, int numIterationsY = 1, int numIterationsZ = 1, int kernelIndex = 0)
	{
		Vector3Int threadGroupSizes = GetThreadGroupSizes(cs, kernelIndex);
		int numGroupsX = Mathf.CeilToInt(numIterationsX / (float)threadGroupSizes.x);
		int numGroupsY = Mathf.CeilToInt(numIterationsY / (float)threadGroupSizes.y);
		int numGroupsZ = Mathf.CeilToInt(numIterationsZ / (float)threadGroupSizes.y);
		cs.Dispatch(kernelIndex, numGroupsX, numGroupsY, numGroupsZ);
	}

	/// Convenience method for dispatching a compute shader.
	/// It calculates the number of thread groups based on the size of the given texture.
	public static void Dispatch(ComputeShader cs, RenderTexture texture, int kernelIndex = 0)
	{
		Vector3Int threadGroupSizes = GetThreadGroupSizes(cs, kernelIndex);
		Dispatch(cs, texture.width, texture.height, texture.volumeDepth, kernelIndex);
	}

	public static void Dispatch(ComputeShader cs, Texture2D texture, int kernelIndex = 0)
	{
		Vector3Int threadGroupSizes = GetThreadGroupSizes(cs, kernelIndex);
		Dispatch(cs, texture.width, texture.height, 1, kernelIndex);
	}

	public static int GetStride<T>()
	{
		return System.Runtime.InteropServices.Marshal.SizeOf(typeof(T));
	}

	public static ComputeBuffer CreateAppendBuffer<T>(int capacity)
	{
		int stride = GetStride<T>();
		ComputeBuffer buffer = new ComputeBuffer(capacity, stride, ComputeBufferType.Append);
		buffer.SetCounterValue(0);
		return buffer;

	}


	public static void CreateStructuredBuffer<T>(ref ComputeBuffer buffer, int count)
	{
		int stride = GetStride<T>();
		bool createNewBuffer = buffer == null || !buffer.IsValid() || buffer.count != count || buffer.stride != stride;
		if (createNewBuffer)
		{
			Release(buffer);
			buffer = new ComputeBuffer(count, stride, ComputeBufferType.Structured);
		}
	}


	public static ComputeBuffer CreateStructuredBuffer<T>(T[] data)
	{
		var buffer = new ComputeBuffer(data.Length, GetStride<T>());
		buffer.SetData(data);
		return buffer;
	}

	public static ComputeBuffer CreateStructuredBuffer<T>(List<T> data) where T : struct
	{
		var buffer = new ComputeBuffer(data.Count, GetStride<T>());
		buffer.SetData<T>(data);
		return buffer;
	}

	public static ComputeBuffer CreateStructuredBuffer<T>(int count)
	{
		return new ComputeBuffer(count, GetStride<T>());
	}

	public static void CreateStructuredBuffer<T>(ref ComputeBuffer buffer, T[] data)
	{
		CreateStructuredBuffer<T>(ref buffer, data.Length);
		buffer.SetData(data);
	}

	public static ComputeBuffer CreateAndSetBuffer<T>(T[] data, ComputeShader cs, string nameID, int kernelIndex = 0)
	{
		ComputeBuffer buffer = null;
		CreateAndSetBuffer<T>(ref buffer, data, cs, nameID, kernelIndex);
		return buffer;
	}

	public static void CreateAndSetBuffer<T>(ref ComputeBuffer buffer, T[] data, ComputeShader cs, string nameID, int kernelIndex = 0)
	{
		int stride = System.Runtime.InteropServices.Marshal.SizeOf(typeof(T));
		CreateStructuredBuffer<T>(ref buffer, data.Length);
		buffer.SetData(data);
		cs.SetBuffer(kernelIndex, nameID, buffer);
	}

	public static ComputeBuffer CreateAndSetBuffer<T>(int length, ComputeShader cs, string nameID, int kernelIndex = 0)
	{
		ComputeBuffer buffer = null;
		CreateAndSetBuffer<T>(ref buffer, length, cs, nameID, kernelIndex);
		return buffer;
	}

	public static void CreateAndSetBuffer<T>(ref ComputeBuffer buffer, int length, ComputeShader cs, string nameID, int kernelIndex = 0)
	{
		CreateStructuredBuffer<T>(ref buffer, length);
		cs.SetBuffer(kernelIndex, nameID, buffer);
	}

	// Read data in append buffer to array
	// Note: this is very slow as it reads the data from the GPU to the CPU
	public static T[] ReadDataFromBuffer<T>(ComputeBuffer buffer, bool isAppendBuffer)
	{
		int numElements = buffer.count;
		if (isAppendBuffer)
		{
			// Get number of elements in append buffer
			ComputeBuffer sizeBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.IndirectArguments);
			ComputeBuffer.CopyCount(buffer, sizeBuffer, 0);
			int[] bufferCountData = new int[1];
			sizeBuffer.GetData(bufferCountData);
			numElements = bufferCountData[0];
			Release(sizeBuffer);
		}

		// Read data from append buffer
		T[] data = new T[numElements];
		buffer.GetData(data);

		return data;

	}

	public static void ResetAppendBuffer(ComputeBuffer appendBuffer)
	{
		appendBuffer.SetCounterValue(0);
	}

	/// Releases supplied buffer/s if not null
	public static void Release(params ComputeBuffer[] buffers)
	{
		for (int i = 0; i < buffers.Length; i++)
		{
			if (buffers[i] != null)
			{
				buffers[i].Release();
			}
		}
	}

	/// Releases supplied render textures/s if not null
	public static void Release(params RenderTexture[] textures)
	{
		for (int i = 0; i < textures.Length; i++)
		{
			if (textures[i] != null)
			{
				textures[i].Release();
			}
		}
	}

	public static Vector3Int GetThreadGroupSizes(ComputeShader compute, int kernelIndex = 0)
	{
		uint x, y, z;
		compute.GetKernelThreadGroupSizes(kernelIndex, out x, out y, out z);
		return new Vector3Int((int)x, (int)y, (int)z);
	}

	// ------ Texture Helpers ------

	public static RenderTexture CreateRenderTexture(RenderTexture template)
	{
		RenderTexture renderTexture = null;
		CreateRenderTexture(ref renderTexture, template);
		return renderTexture;
	}

	public static RenderTexture CreateRenderTexture(int width, int height, FilterMode filterMode, GraphicsFormat format, string name = "Unnamed", DepthMode depthMode = DepthMode.None, bool useMipMaps = false)
	{
		RenderTexture texture = new RenderTexture(width, height, (int)depthMode);
		texture.graphicsFormat = format;
		texture.enableRandomWrite = true;
		texture.autoGenerateMips = false;
		texture.useMipMap = useMipMaps;
		texture.Create();

		texture.name = name;
		texture.wrapMode = TextureWrapMode.Clamp;
		texture.filterMode = filterMode;
		return texture;
	}

	public static void CreateRenderTexture(ref RenderTexture texture, RenderTexture template)
	{
		if (texture != null)
		{
			texture.Release();
		}
		texture = new RenderTexture(template.descriptor);
		texture.enableRandomWrite = true;
		texture.Create();
	}

	public static void CreateRenderTexture(ref RenderTexture texture, int width, int height)
	{
		CreateRenderTexture(ref texture, width, height, defaultFilterMode, defaultGraphicsFormat);
	}


	public static bool CreateRenderTexture(ref RenderTexture texture, int width, int height, FilterMode filterMode, GraphicsFormat format, string name = "Unnamed", DepthMode depthMode = DepthMode.None, bool useMipMaps = false)
	{
		if (texture == null || !texture.IsCreated() || texture.width != width || texture.height != height || texture.graphicsFormat != format || texture.depth != (int)depthMode || texture.useMipMap != useMipMaps)
		{
			if (texture != null)
			{
				texture.Release();
			}
			texture = CreateRenderTexture(width, height, filterMode, format, name, depthMode, useMipMaps);
			return true;
		}
		else
		{
			texture.name = name;
			texture.wrapMode = TextureWrapMode.Clamp;
			texture.filterMode = filterMode;
		}

		return false;
	}


	public static void CreateRenderTexture3D(ref RenderTexture texture, RenderTexture template)
	{
		CreateRenderTexture(ref texture, template);
	}

	public static void CreateRenderTexture3D(ref RenderTexture texture, int size, GraphicsFormat format, TextureWrapMode wrapMode = TextureWrapMode.Repeat, string name = "Untitled", bool mipmaps = false)
	{
		if (texture == null || !texture.IsCreated() || texture.width != size || texture.height != size || texture.volumeDepth != size || texture.graphicsFormat != format)
		{
			//Debug.Log ("Create tex: update noise: " + updateNoise);
			if (texture != null)
			{
				texture.Release();
			}
			const int numBitsInDepthBuffer = 0;
			texture = new RenderTexture(size, size, numBitsInDepthBuffer);
			texture.graphicsFormat = format;
			texture.volumeDepth = size;
			texture.enableRandomWrite = true;
			texture.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
			texture.useMipMap = mipmaps;
			texture.autoGenerateMips = false;
			texture.Create();
		}
		texture.wrapMode = wrapMode;
		texture.filterMode = FilterMode.Bilinear;
		texture.name = name;
	}

	/// Copy the contents of one render texture into another. Assumes textures are the same size.
	public static void CopyRenderTexture(Texture source, RenderTexture target)
	{
		Graphics.Blit(source, target);
	}

	/// Copy the contents of one render texture into another. Assumes textures are the same size.
	public static void CopyRenderTexture3D(Texture source, RenderTexture target)
	{
		LoadComputeShader(ref copy3DCompute, "Copy3D");
		copy3DCompute.SetInts("dimensions", target.width, target.height, target.volumeDepth);
		copy3DCompute.SetTexture(0, "Source", source);
		copy3DCompute.SetTexture(0, "Target", target);
		Dispatch(copy3DCompute, target.width, target.height, target.volumeDepth);//
	}


	/// Swap channels of texture, or set to zero. For example, if inputs are: (green, red, zero, zero)
	/// then red and green channels will be swapped, and blue and alpha channels will be set to zero.
	public static void SwizzleTexture(Texture texture, Channel x, Channel y, Channel z, Channel w)
	{
		if (swizzleTextureCompute == null)
		{
			swizzleTextureCompute = (ComputeShader)Resources.Load("Swizzle");
		}

		swizzleTextureCompute.SetInt("width", texture.width);
		swizzleTextureCompute.SetInt("height", texture.height);
		swizzleTextureCompute.SetTexture(0, "Source", texture);
		swizzleTextureCompute.SetVector("x", ChannelToMask(x));
		swizzleTextureCompute.SetVector("y", ChannelToMask(y));
		swizzleTextureCompute.SetVector("z", ChannelToMask(z));
		swizzleTextureCompute.SetVector("w", ChannelToMask(w));
		Dispatch(swizzleTextureCompute, texture.width, texture.height, 1, 0);
	}

	/// ---- Processing -----

	/// Sets all pixels of supplied texture to 0
	public static void ClearRenderTexture(RenderTexture source)
	{
		LoadComputeShader(ref clearTextureCompute, "ClearTexture");

		clearTextureCompute.SetInt("width", source.width);
		clearTextureCompute.SetInt("height", source.height);
		clearTextureCompute.SetTexture(0, "Source", source);
		Dispatch(clearTextureCompute, source.width, source.height, 1, 0);
	}

	public static RenderTexture BicubicUpscale(RenderTexture original, int sizeMultiplier = 2)
	{
		RenderTexture upscaled = CreateRenderTexture(original.width * sizeMultiplier, original.height * sizeMultiplier, original.filterMode, original.graphicsFormat, original.name + " upscaled");
		upscaled.wrapModeU = original.wrapModeU;
		upscaled.wrapModeV = original.wrapModeV;
		LoadShader(ref bicubicUpscale, "BicubicUpscale");
		Material material = new Material(bicubicUpscale);
		material.SetVector("textureSize", new Vector2(original.width, original.height));
		Graphics.Blit(original, upscaled, material);
		return upscaled;
	}

	// ------ Instancing Helpers

	// Create args buffer for instanced indirect rendering
	public static ComputeBuffer CreateArgsBuffer(Mesh mesh, int numInstances)
	{
		const int subMeshIndex = 0;
		uint[] args = new uint[5];
		args[0] = (uint)mesh.GetIndexCount(subMeshIndex);
		args[1] = (uint)numInstances;
		args[2] = (uint)mesh.GetIndexStart(subMeshIndex);
		args[3] = (uint)mesh.GetBaseVertex(subMeshIndex);
		args[4] = 0; // offset

		ComputeBuffer argsBuffer = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);
		argsBuffer.SetData(args);
		return argsBuffer;
	}

	public static void CreateArgsBuffer(ref ComputeBuffer argsBuffer, Mesh mesh, int numInstances)
	{
		Release(argsBuffer);
		argsBuffer = CreateArgsBuffer(mesh, numInstances);
	}

	// Create args buffer for instanced indirect rendering (number of instances comes from size of append buffer)
	public static ComputeBuffer CreateArgsBuffer(Mesh mesh, ComputeBuffer appendBuffer)
	{
		ComputeBuffer argsBuffer = CreateArgsBuffer(mesh, 0);
		SetArgsBufferCount(argsBuffer, appendBuffer);
		return argsBuffer;
	}

	public static void SetArgsBufferCount(ComputeBuffer argsBuffer, ComputeBuffer appendBuffer)
	{
		ComputeBuffer.CopyCount(appendBuffer, argsBuffer, sizeof(uint));
	}

	// ------ Set compute shader properties ------

	public static void AssignTexture(ComputeShader compute, Texture texture, string name, params int[] kernels)
	{
		for (int i = 0; i < kernels.Length; i++)
		{
			compute.SetTexture(kernels[i], name, texture);
		}
	}

	public static void AssignBuffer(ComputeShader compute, ComputeBuffer texture, string name, params int[] kernels)
	{
		for (int i = 0; i < kernels.Length; i++)
		{
			compute.SetBuffer(kernels[i], name, texture);
		}
	}

	// Set all values from settings object on the shader. Note, variable names must be an exact match in the shader.
	// Settings object can be any class/struct containing vectors/ints/floats/bools
	public static void SetParams(System.Object settings, ComputeShader shader, string variableNamePrefix = "", string variableNameSuffix = "")
	{
		var fields = settings.GetType().GetFields();
		foreach (var field in fields)
		{
			var fieldType = field.FieldType;
			string shaderVariableName = variableNamePrefix + field.Name + variableNameSuffix;

			if (fieldType == typeof(UnityEngine.Vector4) || fieldType == typeof(Vector3) || fieldType == typeof(Vector2))
			{
				shader.SetVector(shaderVariableName, (Vector4)field.GetValue(settings));
			}
			else if (fieldType == typeof(int))
			{
				shader.SetInt(shaderVariableName, (int)field.GetValue(settings));
			}
			else if (fieldType == typeof(float))
			{
				shader.SetFloat(shaderVariableName, (float)field.GetValue(settings));
			}
			else if (fieldType == typeof(bool))
			{
				shader.SetBool(shaderVariableName, (bool)field.GetValue(settings));
			}
			else
			{
				Debug.Log($"Type {fieldType} not implemented");
			}
		}
	}

	// ------ MISC -------


	// https://cmwdexint.com/2017/12/04/computeshader-setfloats/
	public static float[] PackFloats(params float[] values)
	{
		float[] packed = new float[values.Length * 4];
		for (int i = 0; i < values.Length; i++)
		{
			packed[i * 4] = values[i];
		}
		return values;
	}




	static Vector4 ChannelToMask(Channel channel)
	{
		switch (channel)
		{
			case Channel.Red:
				return new Vector4(1, 0, 0, 0);
			case Channel.Green:
				return new Vector4(0, 1, 0, 0);
			case Channel.Blue:
				return new Vector4(0, 0, 1, 0);
			case Channel.Alpha:
				return new Vector4(0, 0, 0, 1);
			case Channel.Zero:
				return new Vector4(0, 0, 0, 0);
			default:
				return Vector4.zero;
		}
	}

	static void LoadComputeShader(ref ComputeShader shader, string name)
	{
		if (shader == null)
		{
			shader = (ComputeShader)Resources.Load(name);
		}
	}

	static void LoadShader(ref Shader shader, string name)
	{
		if (shader == null)
		{
			shader = (Shader)Resources.Load(name);
		}
	}
}
