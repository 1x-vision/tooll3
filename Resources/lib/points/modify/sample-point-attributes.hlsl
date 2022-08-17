#include "lib/shared/point.hlsl"

static const float4 Factors[] = 
{
  //     x  y  z  w
  float4(0, 0, 0, 0), // 0 nothing
  float4(1, 0, 0, 0), // 1 for x
  float4(0, 1, 0, 0), // 2 for y
  float4(0, 0, 1, 0), // 3 for z
  float4(0, 0, 0, 1), // 4 for w
  float4(0, 0, 0, 0), // avoid rotation effects
};

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
    float L;
    float LFactor;
    float LOffset;

    float R;
    float RFactor;
    float ROffset;

    float G;
    float GFactor;
    float GOffset;

    float B;
    float BFactor;
    float BOffset;

    // float A;
    // float AFactor;
    // float AOffset;
    float __padding;

    float3 Center;
}



StructuredBuffer<Point> Points : t0;
RWStructuredBuffer<Point> ResultPoints : u0;    // output


Texture2D<float4> inputTexture : register(t1);
sampler texSampler : register(s0);

[numthreads(256,4,1)]
void main(uint3 i : SV_DispatchThreadID)
{
    uint index = i.x; 

    Point P = Points[index];
    float3 pos = P.position;
    pos -= Center;
    
    float3 posInObject = mul(float4(pos.xyz,0), WorldToObject).xyz;
    //float3 posInObject = pos.xyz;
  
    float4 c = inputTexture.SampleLevel(texSampler, posInObject.xy * float2(1,-1) + float2(0.5, 0.5) , 0.0);
    float gray = (c.r + c.g + c.b)/3;
    //float4 gray = float4(g.xxx, 0);

    float4 ff =
              Factors[(uint)clamp(L, 0, 5.1)] * (gray * LFactor + LOffset) 
            + Factors[(uint)clamp(R, 0, 5.1)] * (c.r * RFactor + ROffset)
            + Factors[(uint)clamp(G, 0, 5.1)] * (c.g * GFactor + GOffset)
            + Factors[(uint)clamp(B, 0, 5.1)] * (c.b * BFactor + BOffset);
    //ResultPoints[index] = P;

    ResultPoints[index].position = P.position + float3(ff.xyz);
    ResultPoints[index].w = P.w + ff.w;
    

    
    float4 rot = P.rotation;
    ResultPoints[index].rotation = P.rotation;

    float rotXFactor = (R == 5 ? (c.r * RFactor + ROffset) : 0)
                     + (G == 5 ? (c.g * GFactor + GOffset) : 0)
                     + (B == 5 ? (c.b * BFactor + BOffset) : 0)
                     + (L == 5 ? (gray * LFactor + LOffset) : 0);

    float rotYFactor = (R == 6 ? (c.r * RFactor + ROffset) : 0)
                     + (G == 6 ? (c.g * GFactor + GOffset) : 0)
                     + (B == 6 ? (c.b * BFactor + BOffset) : 0)
                     + (L == 6 ? (gray * LFactor + LOffset) : 0);

    float rotZFactor = (R == 7 ? (c.r * RFactor + ROffset) : 0)
                     + (G == 7 ? (c.g * GFactor + GOffset) : 0)
                     + (B == 7 ? (c.b * BFactor + BOffset) : 0)
                     + (L == 7 ? (gray * LFactor + LOffset) : 0);
                     
    if(rotXFactor != 0) { rot = qmul(rot, rotate_angle_axis(rotXFactor, float3(1,0,0))); }
    if(rotYFactor != 0) { rot = qmul(rot, rotate_angle_axis(rotYFactor, float3(0,1,0))); }
    if(rotZFactor != 0) { rot = qmul(rot, rotate_angle_axis(rotZFactor, float3(0,0,1))); }
    ResultPoints[index].rotation = normalize(rot);
}