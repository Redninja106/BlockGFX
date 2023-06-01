#include "gui.hlsl"

cbuffer RenderData
{
	float4 source;
	float4 dest;
	float aspectRatio;
};

VertexShaderOutput main(VertexPositionTexture vertex)
{
	VertexShaderOutput result;
	
	float2 pos = vertex.position.xy;
	pos *= dest.zw;
	pos.x /= aspectRatio;
	pos += dest.xy;
	pos = pos * 2 - float2(1, 1);
	
	result.position = float4(pos, vertex.position.y, 1);
	result.uv = vertex.uv;
	
	return result;
}