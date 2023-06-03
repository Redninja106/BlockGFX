// https://gamedev.stackexchange.com/questions/32681/random-number-hlsl

#define RANDOM_IA 16807
#define RANDOM_IM 2147483647
#define RANDOM_AM (1.0f/float(RANDOM_IM))
#define RANDOM_IQ 127773u
#define RANDOM_IR 2836
#define RANDOM_MASK 123459876

struct Random
{
	void Cycle();
	
	int seed;

	float NextFloat()
	{
		Cycle();
		return RANDOM_AM * seed;
	}

	int GetCurrentInt()
	{
		Cycle();
		return seed;
	}

	void Cycle()
	{
		seed ^= RANDOM_MASK;
		int k = seed / RANDOM_IQ;
		seed = RANDOM_IA * (seed - k * RANDOM_IQ) - RANDOM_IR * k;

		if (seed < 0) 
			seed += RANDOM_IM;

		seed ^= RANDOM_MASK;
	}
};
