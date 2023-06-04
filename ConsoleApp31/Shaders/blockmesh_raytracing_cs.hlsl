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
	float3 sunDirection;
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

static groupshared Random rng;
float random()
{
	return rng.NextFloat();
}

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

float3 randomUnitVector()
{
	float3 result;
	do
	{
		result = float3(random(), random(), random());
	}
	while (dot(result, result) > 1);
	
	return result;
}

float3 rayColor(Ray ray, out Ray bounce)
{
	uint elements, stride;
	boxes.GetDimensions(elements, stride);
	
	float closestT = (1.0 / 0.0);
	float3 closestNormal;
	bool hitAny;
	for (uint i = 0; i < elements; i++)
	{
		Box box = boxes[i];
		
		float t;
		float3 hitNormal;
		if (BoxRaycast(box, ray, t, hitNormal))
		{
			if (t < closestT)
			{
				closestT = t;
				closestNormal = hitNormal;
				hitAny = true;
			}
		}
	}
	
	bounce = CreateRay(ray.at(closestT), closestNormal + randomUnitVector());
	
	if (hitAny)
		return max(0, dot(sunDirection, closestNormal));
	else
		return float3(.5, .5, .7);
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

bool raycast(Ray ray)
{
	int3 voxel = int3(ray.origin);
	int3 step = sign(ray.direction);
	
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
	
	int dist = 1000;
	
	while (--dist)
	{
		if (voxel.x < 0 || voxel.x >= 16 || voxel.y < 0 || voxel.y >= 16 || voxel.z < 0 || voxel.z >= 16)
		{
			return false;
		}
		
		if (blockData[voxel.zxy] != 0)
		{
			return true;
		}
		
		if (tMax.x < tMax.y)
		{
			if (tMax.x < tMax.z)
			{
				voxel.x += step.x;
				tMax.x += tDelta.x;
			}
			else
			{
				voxel.z += step.z;
				tMax.z += tDelta.z;
			}
		}
		else
		{
			if (tMax.y < tMax.z)
			{
				voxel.y += step.y;
				tMax.y += tDelta.y;
			}
			else
			{
				voxel.z += step.z;
				tMax.z += tDelta.z;
			}
		}
	}
	
	return false;
}

[numthreads(16, 16, 1)]
void main(uint3 dispatchID : SV_DispatchThreadID, uint3 groupID : SV_GroupID, uint3 threadID : SV_GroupThreadID)
{
	float4 prevCol = blockmeshFaces[dispatchID.xy];

	if (prevCol.a == 0)
		return;
	
	FaceInfo face = faces[groupID.x];
	
	float3 normal = cross(face.up, face.right);
	
	float2 uv = threadID.xy / 16.0;
	
	Ray ray;
	ray.origin = float3(.5, .5, .5) + face.position + .5 * (1.001*normal - face.up * (uv.y * 2 - 1 + (1.0 / 32)) - face.right * (uv.x * 2 - 1 + (1.0 / 32)));
	ray.direction = -sunDirection;
	ray.inverseDirection = 1.0 / ray.direction;
	ray.length = 100;
	
	int samples = 30;
	
	bool hitAny = raycast(ray);
	
	float4 col = atlas[uint2(face.atlasLocationX, face.atlasLocationY) + uint2((uint) (uv.x * 16), (uint) (uv.y * 16))];
	
	blockmeshFaces[dispatchID.xy] = col * (hitAny ? float4(.4, .4, .4, 1) : (.4 + .6 * max(0, dot(sunDirection, -normal))));
}
