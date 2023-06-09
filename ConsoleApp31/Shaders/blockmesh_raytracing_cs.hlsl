#include "random.hlsl"

struct Box
{
	float3 min;
	float3 max;
};

struct Light
{
	float4x4 projection;
	
	// TODO: instead of reuploading this data, just upload a face index
	float3 position;
	float3 normal;
	float3 up;
	float3 right;
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
	uint lightCount;
	uint3 blockDataSize;
};


// samplers
SamplerState pointSampler;

// shader resources
StructuredBuffer<Light> lights : register(t0);
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

Ray CreateRay(float3 origin, float3 direction, float length)
{
	Ray result;
	result.origin = origin;
	result.direction = direction;
	result.inverseDirection = 1 / direction;
	result.length = length;
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
bool PartialBoxRaycast(Ray ray, Box box, out float tNear, out float tFar)
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
	PartialBoxRaycast(ray, box, tNear, tFar);
	
	float3 start = ray.at(max(0, tNear));
	float3 end = ray.at(tFar);
	
	float3 d = end - start;
	float3 tDelta = step / d;
	float3 tMax = tDelta * getMax(start, step);
	
	int dist = 100;
	
	float t = 0;
	
	while (--dist && t < ray.length)
	{
		if (voxel.x < 0 || voxel.x >= 16 || voxel.y < 0 || voxel.y >= 16 || voxel.z < 0 || voxel.z >= 16)
		{
			return false;
		}
		
		if (blockData[voxel.zxy] != 0)
		{
			outT = max(max(tMax.x - tDelta.x, tMax.y - tDelta.y), tMax.z - tDelta.z) * length(d);
			outVoxel = voxel;
			return t <= ray.length;
		}
		
		if (tMax.x < tMax.y)
		{
			if (tMax.x < tMax.z)
			{
				voxel.x += step.x;
				tMax.x += tDelta.x;
				outNormal = float3(-step.x, 0, 0);
			}
			else
			{
				voxel.z += step.z;
				tMax.z += tDelta.z;
				outNormal = float3(0, 0, -step.z);
			}
		}
		else
		{
			if (tMax.y < tMax.z)
			{
				voxel.y += step.y;
				tMax.y += tDelta.y;
				outNormal = float3(0, -step.y, 0);
			}
			else
			{
				voxel.z += step.z;
				tMax.z += tDelta.z;
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

float3 LightContribution(Light light, float3 pixelPos, float3 surfNormal)
{
	// multiply by light's projection matrix to determine if we are in light's frustum
	float4 clip = mul(light.projection, float4(pixelPos, 1));
	
	if (abs(clip.x) >= clip.w || abs(clip.y) >= clip.w || abs(clip.z) >= clip.w)
		return float3(0, 0, 0); // not in light frustum, no contribution
	
	// return float3(.5, .5, .5);
	float3 lightPos = light.position + light.normal * .5 * (17.0/16.0);
	
	float dist = length(lightPos - pixelPos);
	Ray ray = CreateRay(pixelPos, normalize(lightPos - pixelPos), dist);
	
	float t;
	float3 hitNormal;
	int3 voxel;
	
	if (!raycast(ray, t, hitNormal, voxel) || t > dist)
	{
		return (1 - dot(surfNormal, light.normal) * .5 + .5) * dot(ray.direction, surfNormal) * (.25 / (dist * dist));
	}
	else
	{
		return float3(0, 0, 0);
	}
	
	
	//float dist = length(lightPos - pixelPos);
	//Ray ray1 = CreateRay(pixelPos, normalize((lightPos + light.up * .49) - pixelPos), dist);
	//Ray ray2 = CreateRay(pixelPos, normalize((lightPos - light.up * .49) - pixelPos), dist);
	//Ray ray3 = CreateRay(pixelPos, normalize((lightPos + light.right * .49) - pixelPos), dist);
	//Ray ray4 = CreateRay(pixelPos, normalize((lightPos - light.right * .49) - pixelPos), dist);
	
	//float t;
	//float3 hitNormal;
	//int3 voxel;
	
	//float3 col1 = (!raycast(ray1, t, hitNormal, voxel) || t > dist) ? dot(surfNormal, ray1.direction) * (.2 / (dist * dist)) : float3(0, 0, 0);
	//float3 col2 = (!raycast(ray2, t, hitNormal, voxel) || t > dist) ? dot(surfNormal, ray1.direction) * (.2 / (dist * dist)) : float3(0, 0, 0);
	//float3 col3 = (!raycast(ray3, t, hitNormal, voxel) || t > dist) ? dot(surfNormal, ray1.direction) * (.2 / (dist * dist)) : float3(0, 0, 0);
	//float3 col4 = (!raycast(ray4, t, hitNormal, voxel) || t > dist) ? dot(surfNormal, ray1.direction) * (.2 / (dist * dist)) : float3(0, 0, 0);

	//return col1 + col2 + col3 + col4;
}

[numthreads(16, 16, 1)]
void main(uint3 dispatchID : SV_DispatchThreadID, uint3 groupID : SV_GroupID, uint3 threadID : SV_GroupThreadID)
{
	rng.seed = dispatchID.x * dispatchID.y ^ threadID.x + threadID.y ^ groupID.x + groupID.y ^ ticks;
	rng.Cycle();
	
	float4 prevCol = blockmeshFaces[dispatchID.xy];
	
	if (prevCol.a == 0)
		return;
	
	FaceInfo face = faces[groupID.x + groupID.y * 64];
	
	float3 normal = cross(face.up, face.right);
	
	float2 uv = float2(threadID.xy) / 16.0;
	
	float3 pos = float3(.5, .5, .5) + face.position + .5 * (normal * 1.0001 - face.up * (uv.y * 2 - 1 + (1 / 32.0)) - face.right * (uv.x * 2 - 1 + (1 / 32.0)));
	
	float3 pos1 = pos + face.up * (1 / 128.0) + face.right * (1 / 128.0);
	float3 pos2 = pos - face.up * (1 / 128.0) - face.right * (1 / 128.0);
	float3 pos3 = pos - face.up * (1 / 128.0) + face.right * (1 / 128.0);
	float3 pos4 = pos + face.up * (1 / 128.0) - face.right * (1 / 128.0);
	
	float3 atlasColor = atlas[uint2(face.atlasLocationX, face.atlasLocationY) + uint2((uint) (uv.x * 16), (uint) (uv.y * 16))].xyz;
	
	float3 col = float3(0, 0, 0);
	float3 ambient = float3(.01, .01, .01);
	
	float t;
	float3 n;
	int3 v;
	Ray sunlightRay = CreateRay(pos, -sunDirection, 1000);
	if (!raycast(sunlightRay, t, n, v))
	{
		col += .6 * max(0, dot(sunDirection, -normal));
	}
	
	col += ambient;
	
	for (int i = 0; i < lightCount; i++)
	{
		Light l = lights[i];
		col += LightContribution(l, pos1, normal);
		col += LightContribution(l, pos2, normal);
		col += LightContribution(l, pos3, normal);
		col += LightContribution(l, pos4, normal);
	}
	
	col.xyz = min(col.xyz, float3(1, 1, 1));
	
	col *= atlasColor;
	col.xyz = pow(col.xyz, 1/2.2);
	
	blockmeshFaces[dispatchID.xy] = float4(col.xyz, 1);
	// blockmeshFaces[dispatchID.xy] = float4(atlasColor, 1);
}