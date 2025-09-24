#ifndef MAX_VA_COUNT
	#define MAX_VA_COUNT 1
#endif

	#define MAX_INSTANCED_BATCH_SIZE 1000

struct appdata
{
    float4 positionOS   : POSITION;
    float2 uv           : TEXCOORD0;
    float3 normalOS     : NORMAL;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct v2f
{
    float4 positionHCS  : SV_POSITION;
    float2 uv           : TEXCOORD0;
    float3 normal       : TEXCOORD1;
    float3 lightDir     : TEXCOORD2;
};

TEXTURE2D(_MainTex);
SAMPLER(sampler_MainTex);
sampler2D _VaPositionTex;
sampler2D _VaNormalTex;

CBUFFER_START(UnityPerMaterial)
    float4 _MainTex_ST;

    // va-region
    float _VaTextureWidth;
    float _VaTextureHeight;
    float _VaVertexCount;
    float3 _VaBoundMin;
    float3 _VaBoundMax;

    float _VaStartRow[MAX_VA_COUNT];
    float _VaClipLength[MAX_VA_COUNT];
    float _VaFrameCount[MAX_VA_COUNT];
    float _VaIsLoop[MAX_VA_COUNT];

#ifdef UNITY_INSTANCING_ENABLED
    float _BeginArray[MAX_INSTANCED_BATCH_SIZE];
    float _OffsetArray[MAX_INSTANCED_BATCH_SIZE];   
    float _SpeedArray[MAX_INSTANCED_BATCH_SIZE];
    #if MAX_VA_COUNT > 1
        float _VaIndexArray[MAX_INSTANCED_BATCH_SIZE];
    #endif
#endif
CBUFFER_END

// Calculate frame index (pixel unit)
inline float GetVaFrame(float time, uint vaIndex, uint instanceId)
{
    // Calculate time between 0 and clip length and multiply by fps to calculate frame index
    // (time % clip length) * fps
    // fps is calculated as baked frame count - 1 (last frame is excluded because it is the end of the animation)

    #ifdef UNITY_INSTANCING_ENABLED
        time -= _BeginArray[instanceId];	// elapsed time from VA start
        time *= _SpeedArray[instanceId];
        time += _OffsetArray[instanceId];
    #endif
    
    float fps = (_VaFrameCount[vaIndex] - 1.0) / _VaClipLength[vaIndex];
    float loopValue = step(1.0, _VaIsLoop[vaIndex]);
    float progressRepeat = time % _VaClipLength[vaIndex];
    float progressClamped = clamp(time, 0.0, _VaClipLength[vaIndex]);
    float progress = progressRepeat * loopValue + progressClamped * (1.0 - loopValue);
    float frame = progress * fps;

    return frame;
}

inline float2 GetVaUV(uint vid, float frameIndex, uint vaIndex, uint instanceId)
{
    // https://docs.unity3d.com/Manual/SL-UnityShaderVariables.html

    float2 uvIndex = float2(0, 0);
    uvIndex.x = vid;
    uvIndex.y = _VaStartRow[vaIndex] + frameIndex;
    uvIndex.x = (uvIndex.x + 0.5) / _VaTextureWidth;
    uvIndex.y = (uvIndex.y + 0.5) / _VaTextureHeight;
    return uvIndex;
}

float3 GetVaPosition(float2 uv)
{
    float3 pos = tex2Dlod(_VaPositionTex, float4(uv, 0, 0)).rgb;

    // Un-normalize by bound min, max (InverseLerp -> Lerp)
    pos = lerp(_VaBoundMin, _VaBoundMax, pos);

    return pos;
}

float3 GetVaNormal(float2 uv)
{
    float3 normal = tex2Dlod(_VaNormalTex, float4(uv, 0, 0)).rgb;
    normal = normal * 2.0 - 1.0;
    return normal;
}

v2f vert (appdata v, uint vid : SV_VertexID, uint instanceID : SV_InstanceID)
{
    v2f o;
    UNITY_SETUP_INSTANCE_ID(v);

    int vaIndex = 0;
    #if defined(UNITY_INSTANCING_ENABLED) && MAX_VA_COUNT > 1
        vaIndex = (int)_VaIndexArray[instanceID];
    #endif

    float frameIndex = GetVaFrame(_Time.y, vaIndex, instanceID);
    float frameIndex1 = floor(frameIndex);
    float frameIndex2 = ceil(frameIndex);
    float frameWeight = frac(frameIndex);

    float2 uv1 = GetVaUV(vid, frameIndex1, vaIndex, instanceID);
    float2 uv2 = GetVaUV(vid, frameIndex2, vaIndex, instanceID);

    float3 pos = lerp(GetVaPosition(uv1), GetVaPosition(uv2), frameWeight);
    float3 normal = lerp(GetVaNormal(uv1), GetVaNormal(uv2), frameWeight);

    o.positionHCS = TransformObjectToHClip(pos);
    o.uv = TRANSFORM_TEX(v.uv, _MainTex);
    o.normal = TransformObjectToWorldNormal(normal);
    o.lightDir = normalize(_MainLightPosition.xyz);
    return o;
}

half4 frag (v2f i) : SV_Target
{
    i.normal = normalize(i.normal);

    half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
    //color *= _BaseColor;
    float NdotL = saturate(dot(i.normal, i.lightDir));
    half3 ambient = SampleSH(i.normal);
    half3 lighting = NdotL * _MainLightColor.rgb + ambient;
    color.rgb *= lighting;

    return color;
}