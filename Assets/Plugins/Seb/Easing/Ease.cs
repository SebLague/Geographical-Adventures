using UnityEngine;

namespace Seb
{
	public static class Ease
	{


		public static class Cubic
		{
			public static float In(float t)
			{
				t = Mathf.Clamp01(t);
				return Mathf.Pow(t, 3);
			}

			public static float Out(float t)
			{
				t = Mathf.Clamp01(t);
				return 1 - Mathf.Pow(1 - t, 3);
			}

			public static float InOut(float t)
			{
				t = Mathf.Clamp01(t);
				return (t < 0.5f) ? 4 * t * t * t : 1 - Mathf.Pow(-2 * t + 2, 3) / 2;
			}
		}

		public class Quadratic
		{

			public static float In(float t)
			{
				t = Mathf.Clamp01(t);
				return Mathf.Pow(t, 2);
			}

			public static float Out(float t)
			{
				t = Mathf.Clamp01(t);
				return 1 - (1 - t) * (1 - t);
			}

			public static float InOut(float t)
			{
				t = Mathf.Clamp01(t);
				return Mathf.Lerp(In(t), Out(t), t);
			}
		}


	}
}