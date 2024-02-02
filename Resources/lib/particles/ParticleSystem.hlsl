#include "lib/shared/point.hlsl"
#include "lib/shared/quat-functions.hlsl"

cbuffer Params : register(b0)
{
    float TriggerEmit;    
    float Reset;
    float Speed; 
    float Drag;

    float InitialVelocity;
    float Time;
    float OrientTowardsVelocity;
    float RadiusFromW;

    float LifeTime;
}


cbuffer IntParams : register(b1)
{
    int CollectCycleIndex;
    int WMode;
    int EmitMode;
} 

StructuredBuffer<Point> EmitPoints : t0;
RWStructuredBuffer<Particle> Particles : u0;
RWStructuredBuffer<Point> ResultPoints : u1;

#define W_KEEP_ORIGINAL 0
#define W_PARTICLE_AGE 1
#define W_PARTICLE_SPEED 2


[numthreads(64,1,1)]
void main(uint3 i : SV_DispatchThreadID)
{
    uint newPointCount, pointStride;
    EmitPoints.GetDimensions(newPointCount, pointStride);

    uint maxParticleCount, pointStride2;
    Particles.GetDimensions(maxParticleCount, pointStride2);

    uint gi = i.x;
    if(gi >= maxParticleCount)
        return;

    if(Reset > 0.5)
    {
        Particles[gi].BirthTime = NAN;
        Particles[gi].Position =  NAN;
    }

    // Insert emit points
    int addIndex = 0;
    if(EmitMode == 0) {
        addIndex = (gi + CollectCycleIndex + maxParticleCount) % maxParticleCount;
    } 
    else {
        int t = (gi + CollectCycleIndex / newPointCount) % maxParticleCount;
        int blockSize = maxParticleCount / newPointCount;
        int particleBlock = t / blockSize;
        int t2 = t - (particleBlock * blockSize);
        addIndex =  t2 > 0 ?  -1 : particleBlock;
    }

    if( TriggerEmit > 0.5 && addIndex >= 0 && addIndex < (int)newPointCount )
    {
        if(EmitMode != 0) {
            Particles[(gi-1) % maxParticleCount].BirthTime = NAN;
            Particles[(gi-1) % maxParticleCount].Radius = NAN;
        }

        Particles[gi].Position = EmitPoints[addIndex].Position;
        Particles[gi].Rotation = EmitPoints[addIndex].Rotation;
        Particles[gi].Radius = EmitPoints[addIndex].W * RadiusFromW;
        Particles[gi].BirthTime = Time;
        Particles[gi].Velocity = qRotateVec3(float3(0,0,1), normalize(Particles[gi].Rotation)) * InitialVelocity;
        Particles[gi].Radius = EmitPoints[addIndex].W * RadiusFromW;

        // These will not change over lifetime...
        Particles[gi].Color = EmitPoints[addIndex].Color;
        //ResultPoints[gi].Color = 1;//EmitPoints[addIndex].Color;
        //Particles[gi].Selected = EmitPoints[addIndex].Selected;
    }

    if(Particles[gi].BirthTime == NAN)
        return;

    float3 velocity = Particles[gi].Velocity;
    velocity *= (1-Drag);
    Particles[gi].Velocity = velocity;
    float speed = length(velocity);

    float3 pos = Particles[gi].Position;
    pos += velocity * Speed * 0.01;
    Particles[gi].Position = pos;

    if(speed > 0.0001) 
    {
        float f = saturate(speed * OrientTowardsVelocity);
        Particles[gi].Rotation =  qSlerp(Particles[gi].Rotation, qLookAt(velocity / speed, float3(0,1,0)),  f );
    }

    // Copy result
    // Todo: This could by optimized by not copying color 
    ResultPoints[gi] = Particles[gi];

    // Attempt with lerping to smooth position updates
    // ResultPoints[gi].position = lerp(Particles[gi].p.position, ResultPoints[gi].position, 0);
    // ResultPoints[gi].rotation = Particles[gi].p.rotation;
    // ResultPoints[gi].w = Particles[gi].p.w;
    float lifeTime = LifeTime < 0.0 ? (float)(maxParticleCount/(newPointCount*60)) : LifeTime; 
    
    float normalizedAge = (Time - Particles[gi].BirthTime) / lifeTime;
    bool tooOld =  normalizedAge >= 1;

    if(WMode == W_KEEP_ORIGINAL) {
        if(tooOld) {
          ResultPoints[gi].W = NAN;
        }
        else {
          ResultPoints[gi].W = Particles[gi].Radius / RadiusFromW;
        }
    }
    else if (WMode == W_PARTICLE_AGE) 
    {
        ResultPoints[gi].W = (isnan(Particles[gi].BirthTime) || tooOld) ? NAN : normalizedAge;
    } 
    else if(WMode == W_PARTICLE_SPEED) 
    {
        ResultPoints[gi].W = tooOld ? NAN : speed * 100;
    }

    ResultPoints[gi].Selected = 1;
    ResultPoints[gi].Stretch = 1;

}
