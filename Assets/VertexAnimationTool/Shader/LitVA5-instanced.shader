Shader "VertexAnimation/LitVA5-instanced"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}

        // Vertex Animation texture (clip property array hide in inspector)
        _VaPositionTex ("VaPositionTex", 2D) = "white" {}
        _VaNormalTex ("VaNormalTex", 2D) = "white" {}

        [HideInInspector]
        _MaxVaCount ("Max Va Count", Integer) = 5
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            // https://docs.unity3d.com/ScriptReference/Graphics.RenderMeshInstanced.html
            // By default, Unity uses an objectToWorld matrix and a worldToObject matrix for each instance, which means you can render a maximum of 511 instances at once. 
            // To remove the worldToObject matrix from the instance data, add #pragma instancing_options assumeuniformscaling to the shader.

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            // Vertex Animation
            #define MAX_VA_COUNT 5	// Support 5 Vertex Animation
            #include "LitVACore.hlsl"

            ENDHLSL
        }
        //UsePass "Universal Render Pipeline/Lit/ShadowCaster"
    }
}
