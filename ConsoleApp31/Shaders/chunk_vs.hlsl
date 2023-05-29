cbuffer matrixBuffer : register(b0)
{
	float4x4 world;
	float4x4 view;
	float4x4 proj;
};

struct VSOut
{
	float4 position : SV_Position;
	float2 uv : TEXCOORD;
};

VSOut main(float3 position : POSITION, float2 uv : TEXCOORD)
{
	VSOut result;
	result.position = float4(position, 1);
	
	result.position = mul(result.position, world);
	result.position = mul(result.position, view);
	result.position = mul(result.position, proj);
	
	result.uv = uv;
	return result;
	
}