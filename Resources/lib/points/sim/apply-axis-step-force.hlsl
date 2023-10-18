#include "lib/shared/hash-functions.hlsl"
#include "lib/shared/point.hlsl"

cbuffer Params : register(b0)
{
    float ApplyTrigger;
    float Strength;
    float RandomizeStrength;
    float SelectRatio;

    float3 AxisDistribution;
    float AddOriginalVelocity;

    float3 StrengthDistribution;
    float Seed;

    float AxisSpace;
}

RWStructuredBuffer<Particle> Particles : u0;

[numthreads(64,1,1)]
void main(uint3 i : SV_DispatchThreadID)
{
    uint gi = i.x;
    uint maxParticleCount, _;
    Particles.GetDimensions(maxParticleCount, _);
    if(gi >= maxParticleCount) {
        return;
    }

    float4 randForPos = hash41u(gi + (uint)Seed * 1103515245U);
    float4 randForEffects = hash41u(gi + (uint)Seed * 1103515245U+ 83339);

    float selected = randForPos.w < SelectRatio ? 1 : 0;
    float f =  selected* Strength *  (1+ RandomizeStrength * (randForEffects.r - 0.5));

    float3 axis = abs(randForPos.zyx * AxisDistribution);
    float3 direction = axis.x > axis.y ? (axis.x > axis.z ? float3(1,0,0) : float3(0,0,1))
                                       : (axis.y > axis.z ? float3(0,1,0) : float3(0,0,1));
    
    direction *=  (randForEffects.g < 0.5 ? 1 : -1) * StrengthDistribution * f; 

    if(AxisSpace < 0.5) {

    }
    else if(AxisSpace < 1.5) 
    {
        direction = rotate_vector(direction, Particles[gi].p.rotation);
    }

    float3 origVelocity = Particles[gi].velocity;
    Particles[gi].velocity = lerp( origVelocity,
                                   origVelocity* AddOriginalVelocity + direction * f,
                                ApplyTrigger * selected);
}

