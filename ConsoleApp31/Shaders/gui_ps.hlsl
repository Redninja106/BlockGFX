#include "gui.hlsl"

Texture2D image;
SamplerState imageSampler;

float4 main(VertexShaderOutput vsOut) : SV_Target
{
	return image.Sample(imageSampler, vsOut.uv);
}