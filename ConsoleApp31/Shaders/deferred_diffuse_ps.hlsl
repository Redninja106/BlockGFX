#include "deferred_common.hlsl"

struct PointLight
{
	float3 position;
};

StructuredBuffer<PointLight> lights : register(t0);

float4 main(float4 viewportPosition : SV_Position) : SV_Target
{
	float2 uv = ViewportPositionToUV(viewportPosition.xy);
	
	float3 position = positionTexture.Sample(pointSampler, uv).rgb;
	float3 albedo = albedoTexture.Sample(pointSampler, uv).rgb;
	
	float3 lightPos = float3(0, 5, 0);
	
	float3 pixelPos = ceil(position * 16) / 16;
	
	float3 diff = lightPos - pixelPos;
	float falloff = clamp(.5 / dot(diff, diff), 0, 1);
	
	float3 diffuse = falloff * albedo;
	
	return float4(diffuse, 1);
}