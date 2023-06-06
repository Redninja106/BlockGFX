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

float3 getMax(float3 start, float3 step)
{
	return float3(
		step.x > 0 ? 1 - frac(start.x) : frac(start.x),
		step.y > 0 ? 1 - frac(start.y) : frac(start.y),
		step.z > 0 ? 1 - frac(start.z) : frac(start.z));
}

bool raycast(Ray ray, out float outT, out float3 outNormal, out int3 outVoxel)
{
	int3 voxel = int3(floor(ray.origin));
	int3 step = sign(ray.direction);
	
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
	float3 tMax = tDelta * getMax(start, step);
	
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
		
		if (tMax.x < tMax.y)
		{
			if (tMax.x < tMax.z)
			{
				voxel.x += step.x;
				tMax.x += tDelta.x;
				t += tDelta.x;
				outNormal = float3(-step.x, 0, 0);
			}
			else
			{
				voxel.z += step.z;
				tMax.z += tDelta.z;
				t += tDelta.z;
				outNormal = float3(0, 0, -step.z);
			}
		}
		else
		{
			if (tMax.y < tMax.z)
			{
				voxel.y += step.y;
				tMax.y += tDelta.y;
				t += tDelta.y;
				outNormal = float3(0, -step.y, 0);
			}
			else
			{
				voxel.z += step.z;
				tMax.z += tDelta.z;
				t += tDelta.z;
				outNormal = float3(0, 0, -step.z);
			}
		}
	}
	
	return false;
}
static Random rng;

float2 CalcFaceUV(int3 voxel, float3 pos, float3 normal)
{
	float3 components = (pos - voxel) * (float3(1, 1, 1) - normal);
			
	float2 uv;
	if (components.x == 0)
		uv = components.zy;
	else if (components.y == 0)
		uv = components.xz;
	else
		uv = components.xy;
	
	uv.y = 1 - uv.y;
	
	if (normal.x > 0 || normal.z < 0 || normal.y > 0)
		uv.x = 1 - uv.x;
	
	return uv;
}

#define GLOWSTONE_STR_FLOAT .4
#define GLOWSTONE_STR float3(GLOWSTONE_STR_FLOAT,GLOWSTONE_STR_FLOAT,GLOWSTONE_STR_FLOAT)

float3 rayColor(Ray ray)
{
	static const int bounces = 4;
	
	uint elements, stride;
	boxes.GetDimensions(elements, stride);
	
	static float3 emissions[bounces];
	static float3 strengths[bounces];
	
	[fastopt] 
	for (int i = 0; i < bounces-1; i++)
	{
		float t;
		float3 normal;
		int3 voxel;
		if (raycast(ray, t, normal, voxel))
		{
			// determine face uv
			float3 pos = ray.at(t);
			
			if (normal.x > 0 || normal.y > 0 || normal.z > 0)
				voxel = int3(float3(voxel) - normal);
			
			float2 uv = CalcFaceUV(voxel, pos, normal);
			uint id = blockData[voxel.zxy];
			
			float3 attenuation = atlas[int2(uv.x * 16, (uv.y + (id - 1)) * 16)];
			
			strengths[i] = float3(.5, .5, .5);// attenuation;
			
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
			emissions[i] = max(0, dot(sunDirection, ray.direction)) * float3(0.56078434, 0.8509804, 0.91764706);
			strengths[i] = float3(0, 0, 0);
			break;
		}
		
		ray = CreateRay(ray.at(t), normal + randomUnitVector(rng));
	}
	
	float3 color = float3(1, 1, 1);
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
	rng.seed = dispatchID.x * dispatchID.y ^ threadID.x + threadID.y ^ groupID.x + groupID.y ^ ticks;
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
		col += float4(atlasColor.xyz * rayColor(ray), 1);
		if (face.atlasLocationY == 5*16)
		{
			//col += float4(GLOWSTONE_STR, 0);
		}
	}
	
	col.xyz /= float(samples);
	
	col = max(float4(0, 0, 0, 0), min(float4(1, 1, 1, 1), col));
	
	// (hitAny ? float4(.4, .4, .4, 1) : (.4 + .6 * max(0, dot(sunDirection, -normal))));
	col = pow(col, 1/2.2);
	
	blockmeshFaces[dispatchID.xy] = float4(col.xyz, 1);
	
}
