#include "lib/shared/point.hlsl"
#include "lib/shared/pbr.hlsl"

cbuffer Transforms : register(b0)
{
    float4x4 CameraToClipSpace;
    float4x4 ClipSpaceToCamera;
    float4x4 WorldToCamera;
    float4x4 CameraToWorld;
    float4x4 WorldToClipSpace;
    float4x4 ClipSpaceToWorld;
    float4x4 ObjectToWorld;
    float4x4 WorldToObject;
    float4x4 ObjectToCamera;
    float4x4 ObjectToClipSpace;
};

cbuffer Params : register(b1)
{
    float4x4 Transform;
    float Mode;
}


StructuredBuffer<PbrVertex> Vertices : t0;

RWStructuredBuffer<PbrVertex> ResultVertices : u0;    // output


Texture2D<float4> inputTexture : register(t1);
sampler texSampler : register(s0);

[numthreads(256,4,1)]
void main(uint3 i : SV_DispatchThreadID)
{
    uint vertexIndex = i.x;
    uint stride, vertexCount;
    Vertices.GetDimensions(vertexCount, stride);
    if(vertexIndex >= vertexCount) {
        return; 
    }

    PbrVertex v = Vertices[vertexIndex];
    float3 posInObject = v.Position;


    //float4x4 orientationMatrix = transpose(quaternion_to_matrix(p.rotation));
    //float4x4 t = Transform;
    //t=transpose(t);

    v.TexCoord = mul( float4(posInObject.xyz,1), Transform).xy;
    ResultVertices[vertexIndex] = v;
}