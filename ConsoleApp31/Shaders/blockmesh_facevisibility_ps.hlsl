#include "blockmesh.hlsl"

RWTexture2D<unorm float4> blockmeshFaces : register(u1);

//float4 main(VertexShaderOutput vsout) : SV_Target
void main(VertexShaderOutput vsout)
{
	uint width, height;
	blockmeshFaces.GetDimensions(width, height);
	float2 facesUV = (vsout.localuv * 16) / width;
	
	uint2 face = uint2(vsout.faceIndex * 16, 0);
	face += uint2(vsout.localuv * 16);
	
	blockmeshFaces[face] = float4(blockmeshFaces[face].xyz, 1);
}