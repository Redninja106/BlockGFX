float4 main(uint vertexID : SV_VertexID) : SV_Position
{
	switch (vertexID)
	{
		case 0:
			return float4(0, 3, 0, 1);
		case 1:
			return float4(3, -1, 0, 1);
		case 2:
			return float4(-3, -1, 0, 1);
	}
	
	return float4(0, 0, 0, 0);
}