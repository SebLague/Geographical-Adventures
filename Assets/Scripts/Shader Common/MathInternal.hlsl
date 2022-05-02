#ifndef MATH_INTERNAL_INCLUDED
#define MATH_INTERNAL_INCLUDED

static const float PI = 3.141592653589793238462643383279;

// Schechter - Bridson hash: www.cs.ubc.ca/~rbridson/docs/schechter-sca08-turbulence.pdf
uint hash(uint state)
{
	state = (state ^ 2747636419u) * 2654435769u;
	state = (state ^ (state >> 16)) * 2654435769u;
	state = (state ^ (state >> 16)) * 2654435769u;
	return state;
}

// Scales a uint to range [0, 1]
float scaleToRange01(uint state)
{
	return state / 4294967295.0;
}

#endif