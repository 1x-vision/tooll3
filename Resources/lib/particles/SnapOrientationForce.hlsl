#include "lib/shared/hash-functions.hlsl"
#include "lib/shared/noise-functions.hlsl"
#include "lib/shared/point.hlsl"
#include "lib/shared/quat-functions.hlsl"

cbuffer Params : register(b0)
{
    float Amount;
    float Subdivisions;
    float Phase;
    float Variation;
}

cbuffer Params : register(b1)
{
    int RandomSeed;
}

RWStructuredBuffer<Particle> Particles : u0; 

[numthreads(64,1,1)]
void main(uint3 i : SV_DispatchThreadID)
{
    uint maxParticleCount, _;
    Particles.GetDimensions(maxParticleCount, _);

    int id = i.x;

    if(i.x >= maxParticleCount) {
        return;
    }

    float4 hash = hash41u(id + RandomSeed);
    float3 v = Particles[i.x].Velocity;

    float lengthXY = length(v.xy);

    float2 normalizedV = normalize(v.xy);

    float a = atan2(normalizedV.x, normalizedV.y);

    float aNormalized = (a + PI) / (PI*2);
    aNormalized += (hash.x - 0.5) * Variation + Phase * 0.01;
    float t = aNormalized * Subdivisions;
    float tRounded = ((int)(t + 0.5)) / Subdivisions;
    
    float newAngle = lerp(aNormalized, tRounded, Amount);

    float alignedRotation = (newAngle - 0.5) * 2 * PI;

    float2 newXY = float2(sin(alignedRotation), cos(alignedRotation)) * lengthXY;

    Particles[i.x].Velocity = lerp(v, float3(newXY,v.z), 1);
}

