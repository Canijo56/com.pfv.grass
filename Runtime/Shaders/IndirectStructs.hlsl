
struct DrawIndirectArgs
{
    uint indexCountPerInstance;
    uint instanceCount;
    uint startIndexLocation;
    uint baseVertexLocation;
    uint startInstanceLocation;
};
struct ComputeIndirectArgs
{
    uint threadGroupsX;
    uint threadGroupsY;
    uint threadGroupsZ;
};
struct GrassBladeInstanceData
{
    float3 positionWS;
    uint batchIndex;
    float3 normalWS;
};
struct BladeSourceData
{
    uint triangleIndex;
    uint bladeIndexPerTri;
};
struct GrassVertex
{
    float3 position;
    float3 normal;
    float density;
};
struct GrassTriangle
{
    uint vertexA;
    uint vertexB;
    uint vertexC;
};
struct VertexCullResult
{
    float visible;
};
