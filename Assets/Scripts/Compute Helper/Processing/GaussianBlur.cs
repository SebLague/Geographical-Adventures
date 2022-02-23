using UnityEngine;

namespace ComputeShaderUtility
{

	public class GaussianBlur
	{
		ComputeShader blurCompute;
		ComputeBuffer kernelValueBuffer;
		RenderTexture horizontalPassTexture;

		int kernelSize;
		int currentHalfBlurSize;
		float currentSigma;

		public GaussianBlur()
		{
			blurCompute = (ComputeShader)Resources.Load("GaussianBlur");
		}

		void UpdateSettings(int halfBlurSize, float sigma)
		{
			if (halfBlurSize != currentHalfBlurSize || sigma != currentSigma || kernelValueBuffer == null || !kernelValueBuffer.IsValid())
			{
				currentHalfBlurSize = halfBlurSize;
				currentSigma = sigma;

				kernelSize = Mathf.Abs(halfBlurSize) * 2 + 1;
				float[] kernelValues = Calculate1DGaussianKernel(kernelSize, sigma);
				ComputeHelper.CreateStructuredBuffer(ref kernelValueBuffer, kernelValues);
			}
		}

		public void Blur(RenderTexture source, int halfBlurSize = 8, float sigma = 5, bool blurAlpha = true)
		{
			Blur(source, source, halfBlurSize, sigma, blurAlpha);
		}

		public void Blur(RenderTexture source, RenderTexture target, int halfBlurSize = 8, float sigma = 5, bool blurAlpha = true)
		{
			if (halfBlurSize <= 0 || sigma <= 0)
			{
				return;
			}
			UpdateSettings(halfBlurSize, sigma);


			ComputeHelper.CreateRenderTexture(ref horizontalPassTexture, source);

			blurCompute.SetBuffer(0, "kernelValues", kernelValueBuffer);
			blurCompute.SetTexture(0, "HorizontalPassTexture", horizontalPassTexture);
			blurCompute.SetTexture(0, "Source", source);

			blurCompute.SetBuffer(1, "kernelValues", kernelValueBuffer);
			blurCompute.SetTexture(1, "HorizontalPassTexture", horizontalPassTexture);
			blurCompute.SetTexture(1, "Source", source);
			blurCompute.SetTexture(1, "Target", target);

			blurCompute.SetInt("kernelSize", kernelSize);
			blurCompute.SetInt("width", source.width);
			blurCompute.SetInt("height", source.height);
			blurCompute.SetInt("alphaBlurWeight", (blurAlpha) ? 1 : 0);

			ComputeHelper.Dispatch(blurCompute, source.width, source.height, kernelIndex: 0);
			ComputeHelper.Dispatch(blurCompute, source.width, source.height, kernelIndex: 1);

		}

		static float CalculateGaussianValue(int x, float sigma)
		{
			float c = 2 * sigma * sigma;
			return Mathf.Exp(-x * x / c) / Mathf.Sqrt(c * Mathf.PI);
		}

		public static float[] Calculate1DGaussianKernel(int kernelSize, float sigma)
		{
			float[] kernelValues = new float[kernelSize];
			float sum = 0;
			for (int i = 0; i < kernelSize; i++)
			{
				kernelValues[i] = CalculateGaussianValue(i - kernelSize / 2, sigma);
				sum += kernelValues[i];
			}

			for (int i = 0; i < kernelSize; i++)
			{
				kernelValues[i] /= sum;
			}

			return kernelValues;
		}


		public void Release()
		{
			ComputeHelper.Release(kernelValueBuffer);
			ComputeHelper.Release(horizontalPassTexture);
		}
	}
}