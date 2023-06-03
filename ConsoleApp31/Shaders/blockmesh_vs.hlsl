#include "blockmesh.hlsl"
#include "matrixbuffer.hlsl"

VertexShaderOutput main(BlockVertex vertex)
{
	VertexShaderOutput result;
	
	result.faceIndex = vertex.faceIndex;
	result.uv = vertex.uv;
	result.localuv = vertex.localUV;
	
	result.position = float4(vertex.position, 1);
	result.position = mul(result.position, world);
	result.position = mul(result.position, view);
	result.position = mul(result.position, proj);
	
	return result;
}