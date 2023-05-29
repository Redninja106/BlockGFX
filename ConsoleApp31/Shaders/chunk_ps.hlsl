Texture2D grass : register(t0);
SamplerState grassSampler : register(s0);

struct VSOut
{
	float4 position : SV_Position;
	float2 uv : TEXCOORD;
};

float4 main(VSOut vsout) : SV_Target
{
	return grass.Sample(grassSampler, vsout.uv);
}