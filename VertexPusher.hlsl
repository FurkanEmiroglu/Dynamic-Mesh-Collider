#pragma target 5.0

RWStructuredBuffer<float3> _VertexPositionsBuffer : register(u1);

// you need to take vertex id's inside the vertex shader function by parameter uint id : SV_VertexIDO
void push_vertices(const int id, float3 vertexValues)
{
    _VertexPositionsBuffer[id] = float3(vertexValues.x, vertexValues.y, vertexValues.z);
}