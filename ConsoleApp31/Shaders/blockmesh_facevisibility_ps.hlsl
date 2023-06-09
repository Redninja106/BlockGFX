#include "blockmesh.hlsl"

RWTexture2D<float4> blockmeshFaces : register(u1);

//float4 main(VertexShaderOutput vsout) : SV_Target
void main(VertexShaderOutput vsout)
{
	uint2 face = uint2((vsout.faceIndex % 64) * 16, (vsout.faceIndex / 64) * 16);
	face += uint2(vsout.localuv * 16);
	
	blockmeshFaces[face] = float4(1, 1, 1, 1);
}