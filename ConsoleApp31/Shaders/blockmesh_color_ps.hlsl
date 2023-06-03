#include "blockmesh.hlsl"


SamplerState pointSampler;
Texture2D<float4> textureAtlas;
Texture2D<float4> faces;

float4 main(VertexShaderOutput vsout) : SV_Target
{
	uint width, height, numlevels;
	faces.GetDimensions(0, width, height, numlevels);
	
	
	uint2 face = uint2(vsout.faceIndex * 16, 0);
	face += uint2(vsout.localuv * 16);
	
	float2 facesUV = ((float2(vsout.faceIndex, 0) + vsout.localuv) * 16) / float2(width, height);
	
	
	float4 col = faces.Sample(pointSampler, facesUV);
	col.a = 1;
	
	return textureAtlas.Sample(pointSampler, vsout.uv) * col;
}