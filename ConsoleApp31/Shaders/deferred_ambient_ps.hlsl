#include "deferred_common.hlsl"

float4 main(float4 position : SV_Position) : SV_Target
{
	float3 albedo = albedoTexture.Sample(pointSampler, ViewportPositionToUV(position.xy)).rgb;
	
	float3 ambient = albedo * .2f;
	
	return float4(ambient, 1);
}