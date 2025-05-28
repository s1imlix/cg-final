Shader "Custom/MarchingCubeMesh" {
    // Shader assigned to material and material is assigned to meshes, 
    // here we define how the meshes should be rendered
    Properties {
        _Color ("Color", Color) = (1,1,1,1)
    }
    SubShader {
        Tags { "RenderType"="Opaque" }
        LOD 200
        
        Pass {
            CGPROGRAM 
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            
            struct Vertex {
                float3 pos;
                float3 normal;
            };
            
            /*struct Triangle {
                Vertex vertexA;
                Vertex vertexB; 
                Vertex vertexC;
            };*/
            
            StructuredBuffer<Vertex> VertexBuffer; // Changed to Triangle buffer
            float4 _Color; // Color property from the shader
            float3 _Localscale;
            
            struct appdata
            {
                uint vertexID : SV_VertexID;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 normal : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                Vertex vertexCur = VertexBuffer[v.vertexID];
                
                float3 worldPos = vertexCur.pos * _Localscale;
                // o.vertex = UnityObjectToClipPos(float4(vertexCur.pos, 1.0));
                o.vertex = UnityObjectToClipPos(float4(worldPos, 1.0));
                o.normal = vertexCur.normal; 
                return o;
            }

            // https://docs.unity3d.com/cn/2021.1/Manual/SL-ShaderSemantics.html
            float4 frag(v2f i) : SV_Target
            {
                // diffuse: interpolated normal i.normal
                return _Color * (dot(normalize(i.normal), _WorldSpaceLightPos0) * 0.5 + 0.5); 
            }   
            ENDCG
        } 
    }
}