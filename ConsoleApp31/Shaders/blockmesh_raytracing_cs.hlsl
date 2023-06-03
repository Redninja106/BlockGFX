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
};

cbuffer Constants
{
	float3 sunDirection;
};

StructuredBuffer<Box> boxes;
StructuredBuffer<FaceInfo> faces;

RWTexture2D<unorm float4> blockmeshFaces;

struct Ray
{
	float3 origin;
	float3 direction;
	float3 inverseDirection;
	float length;
};

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

[numthreads(16, 16, 1)]
void main(uint3 dispatchID : SV_DispatchThreadID, uint3 groupID : SV_GroupID, uint3 threadID : SV_GroupThreadID)
{
	float4 prevCol = blockmeshFaces[dispatchID.xy];
	
	if (prevCol.a == 0)
		return;
	
	FaceInfo face = faces[groupID.x];
	
	uint elements, stride;
	boxes.GetDimensions(elements, stride);
	
	float3 normal = cross(face.right, face.up);
	
	float2 uv = threadID.xy / 16.0;
	
	bool hitAny = false;
	Ray ray;
	ray.origin = float3(.5, .5, .5) + face.position + .5 * (-normal + face.up * (uv.x * 2 - 1 + (1.0 / 32)) + face.right * (uv.y * 2 - 1 + (1.0 / 32)));
	ray.direction = -sunDirection;
	ray.inverseDirection = 1.0 / ray.direction;
	ray.length = 100;
	
	for (uint i = 0; i < elements; i++)
	{
		Box box = boxes[i];
		
		float t;
		float3 normal;
		if (BoxRaycast(box, ray, t, normal))
		{
			hitAny = true;
		}
	}
	
	blockmeshFaces[dispatchID.xy] = hitAny ? float4(.4, .4, .4, 1) : .4 + .6 * max(0, dot(normal, sunDirection));
}