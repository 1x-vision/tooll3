#include "lib/shared/point.hlsl"
#include "lib/shared/point-light.hlsl"

static const float3 Corners[] =
    {
        float3(-1, -1, 0),
        float3(1, -1, 0),
        float3(1, 1, 0),
        float3(1, 1, 0),
        float3(-1, 1, 0),
        float3(-1, -1, 0),
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

cbuffer Params : register(b2)
{
    float4 Color;

    float Size;
    float SegmentCount;
    float CutOffTransparent;
    float FadeNearest;
    float UseWForSize;
};

cbuffer FogParams : register(b3)
{
    float4 FogColor;
    float FogDistance;
    float FogBias;
}

cbuffer PointLights : register(b4)
{
    PointLight Lights[8];
    int ActiveLightCount;
}

struct psInput
{
    float4 position : SV_POSITION;
    float4 color : COLOR;
    float2 texCoord : TEXCOORD;
    float fog : FOG;
};

sampler texSampler : register(s0);

StructuredBuffer<Point> Points : t0;
Texture2D<float4> texture2 : register(t1);

psInput vsMain(uint id
               : SV_VertexID)
{
    psInput output;

    int quadIndex = id % 6;
    int particleId = id / 6;
    Point pointDef = Points[particleId];

    // float4 aspect = float4(CameraToClipSpace[1][1] / CameraToClipSpace[0][0],1,1,1);
    float3 quadPos = Corners[quadIndex];
    output.texCoord = float2(0, 1) + (quadPos.xy * 0.5 + 0.5) * float2(1, -1);

    float4 posInObject = float4(pointDef.position, 1);
    float4 quadPosInCamera = mul(posInObject, ObjectToCamera);
    output.color = Color;

    // Shrink too close particles
    float4 posInCamera = mul(posInObject, ObjectToCamera);
    float tooCloseFactor = saturate(-posInCamera.z / FadeNearest - 1);
    output.color.a *= tooCloseFactor;

    float sizeFactor = UseWForSize > 0.5 ? pointDef.w : 1;

    quadPosInCamera.xy += quadPos.xy * 0.050 * sizeFactor * Size;
    output.position = mul(quadPosInCamera, CameraToClipSpace);
    float4 posInWorld = mul(posInObject, ObjectToWorld);

    // Fog
    output.fog = pow(saturate(-posInCamera.z / FogDistance), FogBias);
    return output;
}

float4 psMain(psInput input) : SV_TARGET
{
    float4 textureCol = texture2.Sample(texSampler, input.texCoord);

    if (textureCol.a < CutOffTransparent)
        discard;

    float4 col = input.color * textureCol;
    col.rgb = lerp(col.rgb, FogColor.rgb, input.fog * FogColor.a);
    return clamp(col, float4(0, 0, 0, 0), float4(1000, 1000, 1000, 1));
}
