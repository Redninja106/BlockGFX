cbuffer matrixBuffer : register(b0)
{
	float4x4 world;
	float4x4 view;
	float4x4 proj;
};

struct VSOut
{
	float4 viewportPosition : SV_Position;
	float2 uv : TEXCOORD0;
	float4 worldPosition : TEXCOORD1;
};

VSOut main(float3 position : POSITION, float2 uv : TEXCOORD)
{
	VSOut result;
	result.worldPosition = float4(position, 1);
	
	result.worldPosition = mul(result.worldPosition, world);
	
	result.viewportPosition = mul(result.worldPosition, view);
	result.viewportPosition = mul(result.viewportPosition, proj);
	
	result.uv = uv;
	
	return result;
	
}