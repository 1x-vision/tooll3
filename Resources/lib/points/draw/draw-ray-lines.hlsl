#include "lib/shared/point.hlsl"
// struct Point
// {
//     float3 position;
//     float size;
// };

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
    
    //Point pointAA = Points[ particleId<1 ? 0: particleId-1];
    Point pointA = Points[particleId];
    Point pointB = Points[particleId+1];

    if(isnan(pointA.w) || isnan(pointB.w) ) 
    {
        output.position = 0;
        return output;
    }

    float4 forward = mul(float4(0,0,-1,0), CameraToWorld);
    //forward.xyz/= forward.w;

    forward = mul(float4(forward.xyz,0), WorldToObject);
    //forward.xyz /= forward.w;
    float4 camUpInWorld = mul(float4(0,1,0,0), CameraToWorld);
    float4 camUpInObject = mul(camUpInWorld, WorldToObject);


    //float3 forward = float3(0,1,0);

    float3 posInObject = cornerFactors.x < 0.5 ? pointA.position : pointB.position;
    float3 posAInCamera = mul(float4(pointA.position,1), ObjectToCamera).xyz;
    float3 posBInCamera = mul(float4(pointB.position,1), ObjectToCamera).xyz;
    float4 lineInCamera = float4(posAInCamera - posBInCamera, 1);
    float3 forwardInCamera = float3(0,0,-1);

    float3 lineCenterInCamera = lerp(posAInCamera, posBInCamera, 0.5);
    float3 sideInCamera = normalize(cross(lineCenterInCamera, lineInCamera.xyz)); 

    output.texCoord = float2(
        //lerp( pointA.w  , pointB.w , cornerFactors.x), 
        cornerFactors.x ,
        cornerFactors.y /2 +0.5);

    float4 posInCamera = mul(float4(posInObject,1), ObjectToCamera);
    posInCamera.xyz += sideInCamera * Size / 1000 * cornerFactors.y;

    output.position = mul(posInCamera, CameraToClipSpace);

    // float4 posInCamSpace = mul(float4(posInObject,1), ObjectToCamera);
    // posInCamera.xyz /= posInCamera.w;
    // posInCamera.w = 1;

    output.fog = pow(saturate(-posInCamera.z/FogDistance), FogBias);
    output.color.rgb = Color.rgb;

    output.color.a = Color.a;
    return output;    
}

float4 psMain(psInput input) : SV_TARGET
{
    //return float4(1,1,0,1);
    float4 imgColor = texture2.Sample(texSampler, input.texCoord);
    //return float4(input.texCoord.xx,0,1);
    float dFromLineCenter= abs(input.texCoord.y -0.5)*2;
    //float a= 1;//smoothstep(1,0.95,dFromLineCenter) ;

    float4 col = input.color * imgColor;
    col.rgb = lerp(col.rgb, FogColor.rgb, input.fog);
    return clamp(col, float4(0,0,0,0), float4(1000,1000,1000,1));

    // float4 color = lerp(input.color * imgColor, FogColor, input.fog); // * input.color;
    // return clamp(float4(color.rgb, color.a * a), 0, float4(100,100,100,1));
}
