#include "blockmesh.hlsl"


SamplerState pointSampler;
Texture2D<float4> textureAtlas : register(t0);
Texture2D<float4> faces : register(t1);

float4 main(VertexShaderOutput vsout) : SV_Target
{
	uint width, height, numlevels;
	faces.GetDimensions(0, width, height, numlevels);
	
	
	uint2 face = uint2(vsout.faceIndex * 16, 0);
	face += uint2(vsout.localuv * 16);
	
	float2 facesUV = ((float2(vsout.faceIndex, 0) + vsout.localuv) * 16) / float2(width, height);
	
	float4 col = faces.Sample(pointSampler, facesUV);
	col.a = 1;
	
	// float4 a = textureAtlas.Sample(pointSampler, vsout.uv) * col;
	return col;
}