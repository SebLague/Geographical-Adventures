using UnityEngine;
using static System.Math;

namespace SolarSystem
{
	public class Orbit
	{

		// Calculate point on orbit at t (going from 0 at the start of the orbit, to 1 at the end of the orbit)
		// The orbit is defined by the periapsis (distance of closest approach) and apoapsis (farthest distance)
		public static Vector2 CalculatePointOnOrbit(double periapsis, double apoapsis, double t)
		{
			// Calculate some parameters of the ellipse
			// (see en.wikipedia.org/wiki/Ellipse#Parameters)
			double semiMajorLength = (apoapsis + periapsis) / 2;
			double linearEccentricity = semiMajorLength - periapsis; // distance between centre and focus
			double eccentricity = linearEccentricity / semiMajorLength; // (0 = perfect circle, and up to 1 is increasingly elliptical) 
			double semiMinorLength = Sqrt(Pow(semiMajorLength, 2) - Pow(linearEccentricity, 2));

			// Angle to where body would be if it had a circular orbit
			double meanAnomaly = t * PI * 2;
			// Solve for eccentric anomaly (angle to where body actually is in its elliptical orbit)
			double eccentricAnomaly = SolveKepler(meanAnomaly, eccentricity);

			// Calculate point in orbit from angle
			double ellipseCentreX = -linearEccentricity;
			double pointX = Cos(eccentricAnomaly) * semiMajorLength + ellipseCentreX;
			double pointY = Sin(eccentricAnomaly) * semiMinorLength;

			return new Vector2((float)pointX, (float)pointY);
		}


		// Newton-Rhapson method
		static double SolveKepler(double meanAnomaly, double eccentricity, int maxIterations = 100)
		{
			const double h = 0.0001; // step size for approximating gradient of the function
			const double acceptableError = 0.00000001;
			double guess = meanAnomaly;

			for (int i = 0; i < maxIterations; i++)
			{
				double y = KeplerEquation(guess, meanAnomaly, eccentricity);
				// Exit early if output of function is very close to zero
				if (Abs(y) < acceptableError)
				{
					break;
				}
				// Update guess to value of x where the slope of the function intersects the x-axis
				double slope = (KeplerEquation(guess + h, meanAnomaly, eccentricity) - y) / h;
				double step = y / slope;
				guess -= step;
			}
			return guess;

			// Kepler's equation: M = E - e * sin(E)
			// M is the Mean Anomaly (angle to where body would be if its orbit was actually circular)
			// E is the Eccentric Anomaly (angle to where the body is on the ellipse)
			// e is the eccentricity of the orbit (0 = perfect circle, and up to 1 is increasingly elliptical) 
			double KeplerEquation(double E, double M, double e)
			{
				// Here the equation has been rearranged. We're trying to find the value for E where this will return 0.
				return M - E + e * Sin(E);
			}
		}
	}
}