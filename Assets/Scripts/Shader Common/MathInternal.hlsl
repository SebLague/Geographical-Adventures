#ifndef MATH_INTERNAL_INCLUDED
#define MATH_INTERNAL_INCLUDED

static const float PI = 3.14159265359;

// Hash function www.cs.ubc.ca/~rbridson/docs/schechter-sca08-turbulence.pdf
uint hash(uint state)
{
	state ^= 2747636419u;
	state *= 2654435769u;
	state ^= state >> 16;
	state *= 2654435769u;
	state ^= state >> 16;
	state *= 2654435769u;
	return state;
}

// Scales a uint to range [0, 1]
float scaleToRange01(uint state)
{
	return state / 4294967295.0;
}

#endif