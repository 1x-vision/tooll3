#include "lib/shared/hash-functions.hlsl"
#include "lib/shared/noise-functions.hlsl"
#include "lib/shared/point.hlsl"

cbuffer Params : register(b0)
{
    float3 Center;
    float Radius;

    float RadiusFallOff;
    float RadialForce;
    float UseWForMass;
    float Variation;

    float3 Gravity;
    float ForceDecayRate;
}

// struct Point {
//     float3 Position;
//     float W;
// };

RWStructuredBuffer<Point> ResultPoints : u0; 

[numthreads(64,1,1)]
void main(uint3 i : SV_DispatchThreadID)
{
    float3 variationOffset = hash31((float)(i.x%1234)/0.123 ) * Variation;

    float3 pos = ResultPoints[i.x].position;
    //float3 noise = snoiseVec3((pos + variationOffset + Phase ) * Frequency)* (Amount/100) * AmountDistribution;

    float3 localPos = pos - Center;
    float distance = max(length(localPos), 0.02);
    float3 direction = localPos / distance;

    float effect = saturate(1-(distance - Radius) / RadiusFallOff)/100;

    float3 radialForce = direction / clamp( pow(distance, ForceDecayRate) , 0.02,1000) * RadialForce;

    ResultPoints[i.x].position += (Gravity + radialForce) * effect;
    ResultPoints[i.x].w += 0;
}

