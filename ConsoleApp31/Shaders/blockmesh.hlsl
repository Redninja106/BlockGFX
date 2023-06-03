struct BlockVertex
{
	float3 position : POSITION;
	uint faceIndex : FACEINDEX;
	float2 uv : UV;
	float2 localUV : LOCALUV;
};

struct VertexShaderOutput
{
	float4 position : SV_Position;
	uint faceIndex : TEXCOORD0;
	float2 uv : TEXCOORD1;
	float2 localuv : TEXCOORD2;
};