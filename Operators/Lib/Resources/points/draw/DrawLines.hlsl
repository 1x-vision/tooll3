#include "shared/point.hlsl"
#include "shared/quat-functions.hlsl"

static const float3 Corners[] = 
{
  float3(0, -1, 0),
  float3(1, -1, 0), 
  float3(1,  1, 0), 
  float3(1,  1, 0), 
  float3(0,  1, 0), 
  float3(0, -1, 0),  
};

cbuffer Params : register(b0)
{
    float4 Color;

    float Size;
    float ShrinkWithDistance;
    float OffsetU;
    float UseWForWidth;
    float UseWForU;
    float UvScale;
};


cbuffer Transforms : register(b1)
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

cbuffer FogParams : register(b2)
{
    float4 FogColor;
    float FogDistance;
    float FogBias;  
}

struct psInput
{
    float4 position : SV_POSITION;
    float4 color : COLOR;
    float2 texCoord : TEXCOORD;
    float fog: FOG;
};

sampler texSampler : register(s0);

StructuredBuffer<Point> Points : t0;
Texture2D<float4> texture2 : register(t1);

psInput vsMain(uint id: SV_VertexID)
{
    psInput output;
    float discardFactor = 1;

    uint SegmentCount, Stride;
    Points.GetDimensions(SegmentCount, Stride);

    float4 aspect = float4(CameraToClipSpace[1][1] / CameraToClipSpace[0][0],1,1,1);
    int quadIndex = id % 6;
    uint particleId = id / 6;
    float3 cornerFactors = Corners[quadIndex];
    
    Point pointAA = Points[ particleId<1 ? 0: particleId-1];
    Point pointA = Points[particleId];
    Point pointB = Points[particleId+1];
    Point pointBB = Points[particleId > SegmentCount-2 ? SegmentCount-2: particleId+2];

    float3 posInObject = cornerFactors.x < 0.5
        ? pointA.Position
        : pointB.Position;


    float4 aaInScreen  = mul(float4(pointAA.Position,1), ObjectToClipSpace) * aspect;
    aaInScreen /= aaInScreen.w;
    float4 aInScreen  = mul(float4(pointA.Position,1), ObjectToClipSpace) * aspect;
    if(aInScreen.z < -0)
        discardFactor = 0;
    aInScreen /= aInScreen.w;

    
    float4 bInScreen  = mul(float4(pointB.Position,1), ObjectToClipSpace) * aspect;
    if(bInScreen.z < -0)
        discardFactor = 0;

    bInScreen /= bInScreen.w;
    float4 bbInScreen  = mul(float4(pointBB.Position,1), ObjectToClipSpace) * aspect;
    bbInScreen /= bbInScreen.w;

    float3 direction = (aInScreen - bInScreen).xyz;
    float3 directionA = particleId > 0 
                            ? (aaInScreen - aInScreen).xyz
                            : direction;
    float3 directionB = particleId < SegmentCount- 1
                            ? (bInScreen - bbInScreen).xyz
                            : direction;

    float3 normal =  normalize( cross(direction, float3(0,0,1))); 
    float3 normalA =  normalize( cross(directionA, float3(0,0,1))); 
    float3 normalB =  normalize( cross(directionB, float3(0,0,1))); 
    
    //if(isnan(pointAA.w) || pointAA.w < 0.000001) {
    if(isnan(pointAA.W)) {
        normalA =normal;
    }
    //if(isnan(pointBB.w) || pointAA.w < 0.000001) {
    if(isnan(pointBB.W)) {
        normalB =normal;
    }

    float3 neighboarNormal = lerp(normalA, normalB, cornerFactors.x);
    float3 meterNormal = (normal + neighboarNormal) / 2;
    float4 pos = lerp(aInScreen, bInScreen, cornerFactors.x);
    

    float4 posInCamSpace = mul(float4(posInObject,1), ObjectToCamera);
    posInCamSpace.xyz /= posInCamSpace.w;
    posInCamSpace.w = 1;

    float wAtPoint = lerp( pointA.W  , pointB.W , cornerFactors.x);
        
    if(UseWForU > 0.5 && !isnan(wAtPoint)) 
    {
        output.texCoord = float2(wAtPoint, cornerFactors.y /2 +0.5);
    }
    else {
        float strokeFactor = (particleId+ cornerFactors.x) / SegmentCount;
        output.texCoord = float2(strokeFactor, cornerFactors.y /2 +0.5);
    }

    output.texCoord.x *= UvScale;
    output.texCoord.x += OffsetU;
    

    float thickness = Size * discardFactor * lerp(1, 1/(posInCamSpace.z), ShrinkWithDistance);

    thickness *= UseWForWidth < 0 ? lerp(1, 1-wAtPoint, -UseWForWidth) 
                                : lerp(1, wAtPoint, UseWForWidth) ;

    float miter = dot(-meterNormal, normal);
    pos+= cornerFactors.y * 0.1f * thickness * float4(meterNormal,0) / clamp(miter, -1,-0.16) ;   

    output.position = pos / aspect;
    
    float3 n = cornerFactors.x < 0.5 
        ? cross(pointA.Position - pointAA.Position, pointA.Position - pointB.Position)
        : cross(pointB.Position - pointA.Position, pointB.Position - pointBB.Position);
    n =normalize(n);

    output.fog = pow(saturate(-posInCamSpace.z/FogDistance), FogBias);
    output.color =  Color * lerp(pointA.Color, pointB.Color, cornerFactors.x);
    return output;    
}

float4 psMain(psInput input) : SV_TARGET
{
    float4 imgColor = texture2.Sample(texSampler, input.texCoord);

    float dFromLineCenter= abs(input.texCoord.y -0.5)*2;

    float4 col = input.color * imgColor;
    col.rgb = lerp(col.rgb, FogColor.rgb, input.fog);
    return clamp(col, float4(0,0,0,0), float4(1000,1000,1000,1));
}
