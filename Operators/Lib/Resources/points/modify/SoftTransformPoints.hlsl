#include "shared/hash-functions.hlsl"
#include "shared/noise-functions.hlsl"
#include "shared/point.hlsl"
#include "shared/quat-functions.hlsl"
#include "shared/bias-functions.hlsl"

cbuffer Params : register(b0)
{
    float4x4 TransformVolume;

    float3 Translate; // 16
    float ScatterTranslate;

    float3 Scale; // 20
    float ScaleMagnitude;

    float3 RotateAxis; // 24
    // float RotateAngle;

    float VolumeShape; // 28
    float FallOff;
    float Bias;

    float UseWAsWeight;
    float Phase;
    float Threshold;

    float ScaleW;
    float OffsetW;
}

StructuredBuffer<Point> SourcePoints : t0;
RWStructuredBuffer<Point> ResultPoints : u0;

float sdEllipsoid(float3 p, float3 r)
{
    float k0 = length(p / r);
    float k1 = length(p / (r * r));
    return k0 * (k0 - 1.0) / k1;
}

float Bias2(float x, float bias)
{
    return bias < 0
               ? pow(x, clamp(bias + 1, 0.005, 1))
               : 1 - pow(1 - x, clamp(1 - bias, 0.005, 1));
}

static const float VolumeSphere = 0.5;
static const float VolumeBox = 1.5;
static const float VolumePlane = 2.5;
static const float VolumeZebra = 3.5;
// static const float VolumeNoise = 4.5;

[numthreads(64, 1, 1)] void main(uint3 i
                                 : SV_DispatchThreadID)
{
    uint numStructs, stride;
    SourcePoints.GetDimensions(numStructs, stride);
    if (i.x >= numStructs)
        return;

    Point p = SourcePoints[i.x];

    float3 posInObject = p.Position;
    float3 posInVolume = mul(float4(posInObject, 1), TransformVolume).xyz;

    float s = 1;

    if (VolumeShape < VolumeSphere)
    {
        float distance = length(posInVolume);
        s = smoothstep(1 + FallOff, 1, distance);
    }
    else if (VolumeShape < VolumeBox)
    {
        float3 t = abs(posInVolume);
        float distance = max(max(t.x, t.y), t.z) + Phase;
        s = smoothstep(1 + FallOff, 1, distance);
    }
    else if (VolumeShape < VolumePlane)
    {
        float distance = posInVolume.y;
        s = smoothstep(FallOff, 0, distance);
    }
    else if (VolumeShape < VolumeZebra)
    {
        float distance = 1 - abs(mod(posInVolume.y * 1 + Phase, 2) - 1);
        s = smoothstep(Threshold + 0.5 + FallOff, Threshold + 0.5, distance);
    }

    float dBiased = Bias2(s, Bias);

    if (UseWAsWeight > 0.50)
    {
        dBiased *= p.W;
    }

    float3 rot = RotateAxis * PI / 180 * dBiased;

    float4 rotationX = qFromAngleAxis(rot.x, float3(1, 0, 0));
    float4 rotationY = qFromAngleAxis(rot.y, float3(0, 1, 0));
    float4 rotationZ = qFromAngleAxis(rot.z, float3(0, 0, 1));

    float3 volumeCenter = TransformVolume._m30_m31_m32_m03.xyz;
    float3 posInVolume2 = posInObject + volumeCenter.xyz;

    posInVolume2 = qRotateVec3(posInVolume2, rotationX);
    posInVolume2 = qRotateVec3(posInVolume2, rotationY);
    posInVolume2 = qRotateVec3(posInVolume2, rotationZ);

    p.Position = lerp(p.Position, -volumeCenter.xyz + posInVolume2 * Scale * ScaleMagnitude, dBiased) + dBiased * Translate;
    p.Rotation = qMul(p.Rotation, rotationX);

    float orgW = SourcePoints[i.x].W;
    p.W = lerp(orgW, orgW * ScaleW + OffsetW, dBiased);
    ResultPoints[i.x] = p;
}
