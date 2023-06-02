cbuffer constants : register(b0)
{
	float2 invRenderSize;
};

SamplerState pointSampler : register(s0);
Texture2D positionTexture : register(t0);
Texture2D albedoTexture : register(t1);

float2 ViewportPositionToUV(float2 sv_position_xy)
{
	return sv_position_xy * invRenderSize;
}