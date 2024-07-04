#include "lib/shared/hash-functions.hlsl"
#include "lib/shared/noise-functions.hlsl"
#include "lib/shared/point.hlsl"
#include "lib/shared/quat-functions.hlsl"

cbuffer Params : register(b0)
{
    float4x4 TransformMatrix;
    float UpdateRotation;
    float ScaleW;
    float OffsetW;
    float CoordinateSpace;
    float WIsWeight;
}

StructuredBuffer<Point> SourcePoints : t0;
RWStructuredBuffer<Point> ResultPoints : u0;

static const float PointSpace = 0;
static const float ObjectSpace = 1;
static const float WorldSpace = 2;

[numthreads(64, 1, 1)] void main(uint3 i
                                 : SV_DispatchThreadID)
{
    uint numStructs, stride;
    SourcePoints.GetDimensions(numStructs, stride);
    if (i.x >= numStructs)
        return;

    Point p = SourcePoints[i.x];

    float w = p.W;
    // float3 pOrg = p.Position;
    float3 pos = p.Position;

    // float4 orgRot;
    // float v = q_separate_v(SourcePoints[i.x].rotation, orgRot);

    float4 orgRot = p.Rotation;
    float4 rotation = orgRot;

    if (CoordinateSpace < 0.5)
    {
        pos.xyz = 0;
        rotation = float4(0, 0, 0, 1);
    }

    float3 pLocal = pos;
    pos = mul(float4(pos, 1), TransformMatrix).xyz;

    float4 newRotation = rotation;

    // Transform rotation is kind of tricky. There might be more efficient ways to do this.
    if (UpdateRotation > 0.5)
    {
        // Extract scale from TransformMatrix
        float3 scale;
        scale.x = length(TransformMatrix._m00_m01_m02);
        scale.y = length(TransformMatrix._m10_m11_m12);
        scale.z = length(TransformMatrix._m20_m21_m22);

        // Remove scale from the matrix to get pure rotation
        float3x3 rotationMatrix = float3x3(
            TransformMatrix._m00_m01_m02 / scale.x,
            TransformMatrix._m10_m11_m12 / scale.y,
            TransformMatrix._m20_m21_m22 / scale.z
        );

        newRotation = normalize(qFromMatrix3Precise(transpose(rotationMatrix)));

        // Adjust rotation in point space
        if (CoordinateSpace < 0.5)
        {
            newRotation = qMul(orgRot, newRotation);
        }
        else
        {
            newRotation = qMul(newRotation, orgRot);
        }
    }

    float weight = 1;

    if (WIsWeight >= 0.5)
    {
        float3 weightedOffset = (pos - pLocal) * w;
        pos = pLocal + weightedOffset;
        weight = w;
        newRotation = qSlerp(orgRot, newRotation, w);
    }

    if (CoordinateSpace < 0.5)
    {
    pos.xyz = qRotateVec3(pos.xyz, orgRot).xyz;
    pos += p.Position;
    
    // Extract scale from TransformMatrix
    float3 scale;
    scale.x = length(TransformMatrix._m00_m01_m02);
    scale.y = length(TransformMatrix._m10_m11_m12);
    scale.z = length(TransformMatrix._m20_m21_m22);
    
    // Apply scale to Stretch
    p.Stretch.xyz *= scale; 
    }

    p.Position = pos.xyz;
    p.Rotation = newRotation;

    p.W = lerp(p.W, p.W * ScaleW + OffsetW, weight);

    ResultPoints[i.x] = p;
}
