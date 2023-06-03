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
};


// samplers
SamplerState pointSampler;

// shader resources
StructuredBuffer<Box> boxes;
StructuredBuffer<FaceInfo> faces;
Texture2D<unorm float4> atlas;
Texture3D<uint> ints;

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

float3 rayColor(Ray ray)
{
	uint elements, stride;
	boxes.GetDimensions(elements, stride);
	
	bool hitAny = false;
	for (uint i = 0; i < elements; i++)
	{
		Box box = boxes[i];
		
		float t;
		float3 hitNormal;
		if (BoxRaycast(box, ray, t, hitNormal))
		{
			hitAny = true;
		}
	}
	return float3(0, 0, 0);
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
	ray.origin = float3(.5, .5, .5) + face.position + .5 * (cross(face.up, face.right) - face.up * (uv.y * 2 - 1 + (1.0 / 32)) - face.right * (uv.x * 2 - 1 + (1.0 / 32)));
	ray.direction = -sunDirection;
	ray.inverseDirection = 1.0 / ray.direction;
	ray.length = 100;
	
	int samples = 100;
	float3 color = float3(0, 0, 0);
	
	uint elements, stride;
	boxes.GetDimensions(elements, stride);
	
	bool hitAny = false;
	for (uint i = 0; i < elements; i++)
	{
		Box box = boxes[i];
		
		float t;
		float3 hitNormal;
		if (BoxRaycast(box, ray, t, hitNormal))
		{
			hitAny = true;
		}
	}
	
	float4 col = atlas[uint2(face.atlasLocationX, face.atlasLocationY) + uint2((uint) (uv.x * 16), (uint) (uv.y * 16))];
	
	blockmeshFaces[dispatchID.xy] = col * (hitAny ? float4(.4, .4, .4, 1) : .4 + .6 * max(0, dot(sunDirection, -normal)));
}