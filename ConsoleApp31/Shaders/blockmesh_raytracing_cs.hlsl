#include "random.hlsl"

struct Box
{
	float3 min;
	float3 max;
};

struct FaceInfo
{
	float3 position;
	float3 up;
	float3 right;
	uint atlasLocationX;
	uint atlasLocationY;
};

cbuffer Constants
{
	float3 camPos;
	uint ticks;
	float3 sunDirection;
	uint blockMeshAge;
	float2 atlasTileSize;
	uint3 blockDataSize;
};


// samplers
SamplerState pointSampler;

// shader resources
StructuredBuffer<Box> boxes : register(t0);
StructuredBuffer<FaceInfo> faces : register(t1);
Texture2D<unorm float4> atlas : register(t2);
Texture3D<uint> blockData : register(t3);

// uavs
RWTexture2D<unorm float4> blockmeshFaces;

struct Ray
{
	float3 origin;
	float3 direction;
	float3 inverseDirection;
	float length;
	
	float3 at(float t)
	{
		return origin + direction * t;
	}
};

Ray CreateRay(float3 origin, float3 direction)
{
	Ray result;
	result.origin = origin;
	result.direction = direction;
	result.inverseDirection = 1 / direction;
	result.length = 100;
	return result;
}

bool BoxRaycast(Box box, Ray ray, out float t, out float3 normal)
{
	float t1 = (box.min.x - ray.origin.x) * ray.inverseDirection.x;
	float t2 = (box.max.x - ray.origin.x) * ray.inverseDirection.x;
	float t3 = (box.min.y - ray.origin.y) * ray.inverseDirection.y;
	float t4 = (box.max.y - ray.origin.y) * ray.inverseDirection.y;
	float t5 = (box.min.z - ray.origin.z) * ray.inverseDirection.z;
	float t6 = (box.max.z - ray.origin.z) * ray.inverseDirection.z;

	float tNear = max(max(min(t1, t2), min(t3, t4)), min(t5, t6));
	float tFar = min(min(max(t1, t2), max(t3, t4)), max(t5, t6));

	if (tNear <= tFar && tFar > 0 && tNear < ray.length)
	{
		t = tNear < 0 ? tFar : tNear;

		if (t == t1)
			normal = float3(-1, 0, 0);
		else if (t == t2)
			normal = float3(1, 0, 0);
		else if (t == t3)
			normal = float3(0, -1, 0);
		else if (t == t4)
			normal = float3(0, 1, 0);
		else if (t == t5)
			normal = float3(0, 0, -1);
		else if (t == t6)
			normal = float3(0, 0, 1);
		else
			normal = float3(0, 0, 0); // huh?

		return true;
	}

	return false;
}

float3 randomUnitVector(inout Random rand)
{
	float3 result;
	do
	{
		result = float3(rand.NextFloat() * 2 - 1, rand.NextFloat() * 2 - 1, rand.NextFloat() * 2 - 1);
	}
	while (dot(result, result) > 1);
	
	return normalize(result);
}

// http://www.cs.yorku.ca/~amana/research/grid.pdf
// https://stackoverflow.com/questions/12367071/how-do-i-initialize-the-t-variables-in-a-fast-voxel-traversal-algorithm-for-ray
bool box_raycast(Ray ray, Box box, out float tNear, out float tFar)
{
	float t1 = (box.min.x - ray.origin.x) * ray.inverseDirection.x;
	float t2 = (box.max.x - ray.origin.x) * ray.inverseDirection.x;
	float t3 = (box.min.y - ray.origin.y) * ray.inverseDirection.y;
	float t4 = (box.max.y - ray.origin.y) * ray.inverseDirection.y;
	float t5 = (box.min.z - ray.origin.z) * ray.inverseDirection.z;
	float t6 = (box.max.z - ray.origin.z) * ray.inverseDirection.z;

	tNear = max(max(min(t1, t2), min(t3, t4)), min(t5, t6));
	tFar = min(min(max(t1, t2), max(t3, t4)), max(t5, t6));

	if (tNear <= tFar && tFar > 0 && tNear < ray.length)
	{
		return true;
	}

	return false;
}

float Frac(float f, float s)
{
	if (s > 0)
		return 1 - f + floor(f);
	else
		return f - floor(f);
}
float3 getMax(float3 start, float3 step)
{
	return float3(
		step.x > 0 ? 1 - frac(start.x) : frac(start.x),
		step.y > 0 ? 1 - frac(start.y) : frac(start.y),
		step.z > 0 ? 1 - frac(start.z) : frac(start.z));
}

bool raycast(Ray ray, out float outT, out float3 outNormal, out int3 outVoxel)
{
	ray.direction = normalize(ray.direction);
	float3 voxel = floor(ray.origin);
	float3 step = float3(sign(ray.direction));
	
	if (step.x == 0 && step.y == 0 && step.z == 0)
		return false;
	
	float tNear, tFar;
	Box box;
	box.min = float3(0, 0, 0);
	box.max = float3(blockDataSize);
	box_raycast(ray, box, tNear, tFar);
	
	float3 start = ray.at(max(0, tNear));
	float3 end = ray.at(tFar);
	
	float3 d = end - start;
	float3 tDelta = step / d;
	float3 tMax = tDelta * float3(Frac(start.x, step.x), Frac(start.y, step.y), Frac(start.z, step.z));
	
	int dist = 100;
	
	float t = 0;
	while (--dist)
	{
		if (voxel.x < 0 || voxel.x >= 16 || voxel.y < 0 || voxel.y >= 16 || voxel.z < 0 || voxel.z >= 16)
		{
			return false;
		}
		
		if (blockData[voxel.zxy] != 0)
		{
			outT = t;
			outVoxel = voxel;
			return true;
		}
		
		if (step.x != 0 && tMax.x < tMax.y)
		{
			if (step.x != 0 && tMax.x < tMax.z)
			{
				t = tMax.x;
				voxel.x += step.x;
				tMax.x += tDelta.x;
				outNormal = float3(-(step.x * step.x), 0, 0);
			}
			else
			{
				t = tMax.z;
				voxel.z += step.z;
				tMax.z += tDelta.z;
				outNormal = float3(0, 0, -(step.z * step.z));
			}
		}
		else
		{
			if (step.y != 0 && tMax.y < tMax.z)
			{
				t = tMax.y;
				voxel.y += step.y;
				tMax.y += tDelta.y;
				outNormal = float3(0, -(step.y * step.y), 0);
			}
			else
			{
				t = tMax.z;
				voxel.z += step.z;
				tMax.z += tDelta.z;
				outNormal = float3(0, 0, -(step.z * step.z));
			}
		}
	}
	
	return false;
}
static Random rng;

float2 CalcFaceUV(int3 voxel, float3 pos)
{
	static const float e = 0.0001;
	
	float3 diff = pos - voxel;
	if (diff.x <= 0 + e) return diff.yz;
	if (diff.x >= 1 - e) return diff.zy;
	if (diff.y <= 0 + e) return diff.xz;
	if (diff.y >= 1 - e) return diff.zx;
	if (diff.z <= 0 + e) return diff.xy;
	if (diff.z >= 1 - e) return diff.yx;
	return float2(.5f, .5f);
}

#define GLOWSTONE_STR_FLOAT .9
#define GLOWSTONE_STR float3(GLOWSTONE_STR_FLOAT,GLOWSTONE_STR_FLOAT,GLOWSTONE_STR_FLOAT)

int faceIDFromNormal(float3 normal)
{
	if (normal.y == 1) return 0;
	if (normal.y == -1) return 1;
	return 2;
}

float3 rayColor(Ray ray)
{
	static const int bounces = 4;
	
	static float3 emissions[bounces];
	static float3 strengths[bounces];
	
	[fastopt] 
	for (int i = 0; i < bounces; i++)
	{
		float t;
		float3 normal;
		int3 voxel;
		if (raycast(ray, t, normal, voxel))
		{
			// determine face uv
			
			float2 uv = CalcFaceUV(voxel, ray.at(t));
			uint id = blockData[voxel.zxy];
			
			float3 attenuation = atlas[int2((uv.x + faceIDFromNormal(normal)) * 16, (uv.y + (id - 1)) * 16)];
			strengths[i] = float3(.7f, .7f, .7f);
			// strengths[i] = float3(.1, .1, .1);
			// strengths[i] = attenuation;
			
			if (id == 6)
			{
				emissions[i] = GLOWSTONE_STR;
			}
			else
			{
				emissions[i] = float3(0, 0, 0);
			}
		}
		else
		{
			emissions[i] = float3(0, 0, 0);// max(0, dot(sunDirection, ray.direction)) * float3(0.56078434, 0.8509804, 0.91764706);
			strengths[i] = float3(0, 0, 0);
			break;
		}
		
		ray = CreateRay(ray.at(t), normal + randomUnitVector(rng));
	}
	
	float3 color = float3(0, 0, 0);
	for (int j = bounces - 1; j >= 0; j--)
	{
		color *= strengths[j];
		color += emissions[j];
	}
		
	return color;
}


[numthreads(16, 16, 1)]
void main(uint3 dispatchID : SV_DispatchThreadID, uint3 groupID : SV_GroupID, uint3 threadID : SV_GroupThreadID)
{
	rng.seed = dispatchID.x * dispatchID.y ^ (threadID.x << 7) + (threadID.y << 3) ^ groupID.x + groupID.y ^ (ticks << 3);
	rng.Cycle();
	
	float4 prevCol = blockmeshFaces[dispatchID.xy];

	if (prevCol.a == 0)
		return;
	
	FaceInfo face = faces[groupID.x];
	
	float3 normal = cross(face.up, face.right);
	
	float2 uv = threadID.xy / 16.0;
	
	Ray ray;
	ray.origin = float3(.5, .5, .5) + face.position + .5 * (normal * 1.0001 - face.up * (uv.y * 2 - 1 + (rng.NextFloat() + .5) * (1.0 / 16.0)) - face.right * (uv.x * 2 - 1 + (rng.NextFloat() + .5) * (1.0 / 16.0)));
	//ray.direction = normalize(reflect(normalize(ray.origin-camPos), normal));
	ray.direction = normalize(normal + randomUnitVector(rng));
	ray.inverseDirection = 1.0 / ray.direction;
	ray.length = 100;
	
	float4 atlasColor = atlas[uint2(face.atlasLocationX, face.atlasLocationY) + uint2((uint) (uv.x * 16), (uint) (uv.y * 16))];
	
	int samples = 1000;
	float4 col = float4(0, 0, 0, 0);
	
	[fastopt] for (int i = 0; i < samples; i++)
	{
		//ray.direction = normalize(reflect(normalize(ray.origin-camPos), normal));
		ray.origin = float3(.5, .5, .5) + face.position + .5 * (normal * 1.0001 - face.up * (uv.y * 2 - 1 + (rng.NextFloat() + .5) * (1.0 / 16.0)) - face.right * (uv.x * 2 - 1 + (rng.NextFloat() + .5) * (1.0 / 16.0)));
		ray.direction = normalize(normal + randomUnitVector(rng));
		ray.inverseDirection = 1.0 / ray.direction;
		float3 brightness = float3(0, 0, 0);
		if (face.atlasLocationY == 5 * 16)
			brightness = GLOWSTONE_STR;
		col += float4(atlasColor.xyz * (brightness + rayColor(ray)), 1);
	}
	
	col.xyz /= float(samples);
	
	col = max(float4(0, 0, 0, 0), min(float4(1, 1, 1, 1), col));
	
	// (hitAny ? float4(.4, .4, .4, 1) : (.4 + .6 * max(0, dot(sunDirection, -normal))));
	col = pow(col, 1/2.2);
	// blockmeshFaces[dispatchID.xy] = float4(col.xyz, 1);
	
	static const uint ageLimit = 30;
	
	uint age = blockMeshAge;
	if (age > ageLimit)
		age = ageLimit;
	
	float4 old = blockmeshFaces[dispatchID.xy];
	blockmeshFaces[dispatchID.xy] = float4(old.xyz * (1.0 - (1.0 / age)) + col.xyz * (1.0 / age), 1);
}