Texture2D grass : register(t0);
SamplerState grassSampler : register(s0);

struct VSOut
{
	float4 viewportPosition : SV_Position;
	float2 uv : TEXCOORD0;
	float4 worldPosition : TEXCOORD1;
};

struct PixelShaderOut
{
	float4 position : SV_Target0;
	float4 albedo : SV_Target1;
};

PixelShaderOut main(VSOut vsout)
{
	PixelShaderOut psout;
	
	psout.position = vsout.worldPosition;
	psout.albedo = grass.Sample(grassSampler, vsout.uv);
	
	return psout;
}