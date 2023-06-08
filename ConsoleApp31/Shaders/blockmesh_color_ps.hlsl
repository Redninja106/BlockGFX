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
	
	float2 size = float2(width, height);
	float2 facesUV = ((float2(vsout.faceIndex, 0) + vsout.localuv) * 16) / size;
	
	float3 lightingColor = faces.Sample(pointSampler, facesUV).xyz;
	// float3 atlasColor = textureAtlas.Sample(pointSampler, vsout.uv).xyz;
	
	// float3 color = lightingColor * atlasColor;
	
	// color.xyz = pow(color.xyz, 1/2.2);
	
	return float4(lightingColor, 1);
}